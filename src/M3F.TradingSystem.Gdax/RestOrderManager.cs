/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace M3F.TradingSystem.Gdax
{
    public class RestOrderManager : IOrderManager
    {
        readonly RestClient _restClient;
        readonly WebSocketClient _webSocketClient;
        readonly ConcurrentDictionary<Guid, OrderInfo> _watchedOrders;

        public RestOrderManager (RestClient restClient, WebSocketClient webSocketClient)
        {
            _restClient = restClient;
            _webSocketClient = webSocketClient;
            _watchedOrders = new ConcurrentDictionary<Guid, OrderInfo>();

            _webSocketClient.MessageReceived += WebSocketClientOnMessageReceived;
        }

        public event EventHandler<ExchangeOrderAcknowledgedEventArgs> ExchangeOrderAcknowledged;
        public event EventHandler<ExchangeOrderRejectedEventArgs> ExchangeOrderRejected;
        public event EventHandler<ExchangeOrderFilledEventArgs> ExchangeOrderFilled;
        public event EventHandler<ExchangeOrderCancelledEventArgs> ExchangeOrderCancelled;

        public async Task
        NewExchangeOrder (NewOrderSingle nos)
        {
            if (nos == null)
            {
                throw new ArgumentNullException(nameof(nos));
            }

            //await EnsureInstrumentSubscribed(nos.Instrument);

            var orderInfo = new OrderInfo
                            {
                                ClientOrderId = nos.ClientOrderId,
                                Instrument = nos.Instrument,
                                OrderType = nos.OrderType,
                                Side = nos.Side,
                                WorkingSize = nos.Size
                            };

            _watchedOrders[nos.ClientOrderId.Id] = orderInfo;

            JToken result = await _restClient.SendOrder(nos.ClientOrderId,
                                                        nos.Instrument,
                                                        nos.Side,
                                                        nos.OrderType,
                                                        nos.Price,
                                                        nos.Size,
                                                        nos.IsMakerOrCancel);

            var errorMsg = result["message"];
            if (errorMsg != null)
            {
                await Task.Run(() =>
                         {
                             var args = new ExchangeOrderRejectedEventArgs(DateTime.UtcNow,
                                                                           nos.ClientOrderId,
                                                                           nos.Instrument,
                                                                           nos.OrderType,
                                                                           nos.Side,
                                                                           nos.Price,
                                                                           nos.Size,
                                                                           errorMsg.ToString());
                             ExchangeOrderRejected?.Invoke(this, args);
                         });
            }
        }

        public Task
        CancelExchangeOrder (ExchangeOrderId exchangeOrderId)
        {
            return _restClient.CancelOrder(exchangeOrderId.ToString());
        }

        Task
        EnsureInstrumentSubscribed (Instrument instrument)
        {
            return _webSocketClient.Subscribe(instrument);
        }

        void
        WebSocketClientOnMessageReceived (object sender, GdaxWebSocketMessageEventArgs eventArgs)
        {
            dynamic m = eventArgs.Message;

            string type = m.type;

            switch (type)
            {
                case "received":
                    ProcessReceivedMessage(eventArgs.Message);
                    break;

                case "open":
                    ProcessOpenMessage(eventArgs.Message);
                    break;

                case "done":
                    ProcessDoneMessage(eventArgs.Message);
                    break;

                case "match":
                    ProcessMatchMessage(eventArgs.Message);
                    break;

                case "change":
                    ProcessChangeMessage(eventArgs.Message);
                    break;

                default:
                    return;
            }
        }

        decimal? ExtractPrice (JToken msg, decimal size)
        {
            decimal? price = null;

            var funds = msg["funds"];

            if (funds != null)
            {
                price = (decimal) funds / size;
            }
            else
            {
                var prc = msg["price"];
                if (prc != null)
                    price = (decimal) prc;
            }

            return price;
        }

        void
        ProcessReceivedMessage (JObject msg)
        {
            var clientOrderId = msg["client_oid"];

            if (clientOrderId != null)
            {
                var cid = (Guid) clientOrderId;

                if (_watchedOrders.TryGetValue(cid, out var orderInfo))
                {
                    // This is one of the orders we care about

                    orderInfo.ExchangeOrderId = (Guid) msg["order_id"];
                    _watchedOrders[orderInfo.ExchangeOrderId] = orderInfo;

                    decimal size = (decimal) msg["size"];
                    decimal? price = ExtractPrice(msg, size);

                    orderInfo.Price = price;

                    var args = new ExchangeOrderAcknowledgedEventArgs(
                        (DateTime) msg["time"],
                        new ExchangeOrderId(msg["order_id"].ToString()),
                        new ClientOrderId(cid),
                        orderInfo.Instrument,
                        orderInfo.OrderType,
                        orderInfo.Side,
                        price,
                        size
                        );

                    ExchangeOrderAcknowledged?.Invoke(this, args);
                }
            }
        }

        void
        ProcessOpenMessage (JToken msg)
        {
           // nada
        }

        void
        ProcessDoneMessage (JToken msg)
        {
            var exchangeOrderId = msg["order_id"];

            if (exchangeOrderId != null)
            {
                var orderId = (Guid) exchangeOrderId;

                if (_watchedOrders.TryGetValue(orderId, out var orderInfo))
                {
                    var reason = msg["reason"].ToString();

                    if (reason == "canceled")
                    {
                        var args = new ExchangeOrderCancelledEventArgs(
                            (DateTime) msg["time"],
                            orderInfo.ClientOrderId,
                            orderInfo.Instrument,
                            orderInfo.OrderType,
                            orderInfo.Side,
                            (decimal) msg["price"],
                            (decimal) msg["remaining_size"]);

                        ExchangeOrderCancelled?.Invoke(this, args);
                    }

                    _watchedOrders.TryRemove(orderId, out var ignore);
                    _watchedOrders.TryRemove(orderInfo.ExchangeOrderId, out ignore);
                }
            }
        }

        void
        ProcessMatchMessage (JToken msg)
        {
            var makerOrderId = (Guid) msg["maker_order_id"];
            var takerOrderId = (Guid) msg["taker_order_id"];

            OrderInfo orderInfo = null;

            if (_watchedOrders.TryGetValue(makerOrderId, out orderInfo)
                || _watchedOrders.TryGetValue(takerOrderId, out orderInfo))
            {
                var filledSize = (decimal) msg["size"];
                orderInfo.WorkingSize -= filledSize;

                var args = new ExchangeOrderFilledEventArgs((DateTime) msg["time"],
                                                            orderInfo.ClientOrderId,
                                                            orderInfo.Instrument,
                                                            orderInfo.OrderType,
                                                            orderInfo.Side,
                                                            (decimal) msg["price"],
                                                            orderInfo.WorkingSize,
                                                            filledSize);
                ExchangeOrderFilled?.Invoke(this, args);
            }
        }

        void
        ProcessChangeMessage (JToken msg)
        {
            var exchangeOrderId = msg["order_id"];

            if (exchangeOrderId != null)
            {
                var orderId = (Guid) exchangeOrderId;

                if (_watchedOrders.TryGetValue(orderId, out var orderInfo))
                {
                    orderInfo.WorkingSize = (decimal)msg["new_size"];

                    // TODO: Hanlde order change
                    // TODO: Implement change event
                }
            }
        }

        class OrderInfo
        {
            public Guid ExchangeOrderId;
            public ClientOrderId ClientOrderId;
            public Instrument Instrument;
            public ExchangeOrderType OrderType;
            public Side Side;
            public decimal WorkingSize;
            public decimal? Price;
        }
    }

} // namespace M3F.TradingSystem.Gdax
