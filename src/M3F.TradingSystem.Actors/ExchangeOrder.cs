/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Akka.Actor;

namespace M3F.TradingSystem.Actors
{
    public class ExchangeOrder : ReceiveActor
    {
        public Instrument Instrument { get; }
        public Side Side { get; }
        public ExchangeOrderType OrderType { get; }

        public class ChangeRequest
        {
            public ChangeRequest(decimal size)
            {
                Size = size;
            }

            public ChangeRequest(decimal? price, decimal size, bool isPostOnly)
            {
                Price = price;
                Size = size;
                IsPostOnly = isPostOnly;
            }

            public decimal? Price { get; }
            public decimal Size { get; }
            public bool IsPostOnly { get; }
        }

        public class CancelRequest
        {
        }

        public class Response
        {
            protected Response(decimal price, decimal workingSize, decimal filledSize)
            {
                Price = price;
                WorkingSize = workingSize;
                FilledSize = filledSize;
            }

            public decimal Price { get; }
            public decimal WorkingSize { get; }
            public decimal FilledSize { get; }

            public bool IsFullyFilled => WorkingSize == 0;
        }

        public class Cancelled : Response
        {
            public Cancelled(decimal price, decimal workingSize, decimal filledSize) : base(price,
                workingSize,
                filledSize)
            {
            }
        }

        public class Filled : Response
        {
            public Filled(decimal price, decimal workingSize, decimal filledSize) : base(price,
                workingSize,
                filledSize)
            {
            }
        }

        readonly IActorRef _self;
        readonly IOrderManager _orderManager;
        ClientOrderId _clientOrderId = null;
        ExchangeOrderId _exchangeOrderId = null;
        decimal _originalSize = 0m;

        public ExchangeOrder(IOrderManager orderManager,
            ExchangeOrderType orderType,
            Instrument instrument,
            Side side)
        {
            _self = Self;

            _orderManager = orderManager;
            _orderManager.ExchangeOrderAcknowledged += ForwardEvent;
            _orderManager.ExchangeOrderCancelled += ForwardEvent;
            _orderManager.ExchangeOrderFilled += ForwardEvent;
            _orderManager.ExchangeOrderRejected += ForwardEvent;

            OrderType = orderType;
            Instrument = instrument;
            Side = side;

            Become(StateNotInMarket);
        }

        void StateNotInMarket()
        {
            ReceiveAsync<ChangeRequest>(async change =>
            {
                _clientOrderId = new ClientOrderId();
                _originalSize = change.Size;

                var nos = new NewOrderSingle
                {
                    ClientOrderId = _clientOrderId,
                    Instrument = Instrument,
                    Side = Side,
                    OrderType = OrderType,
                    Price = change.Price,
                    Size = change.Size,
                    IsMakerOrCancel = false
                };

                await _orderManager.NewExchangeOrder(nos);
                Become(StatePendingNew);
            });
        }

        void StatePendingNew()
        {
            Receive<ExchangeOrderAcknowledgedEventArgs>(e => e.ClientOrderId == _clientOrderId,
                args =>
                {
                    _exchangeOrderId = args.ExchangeOrderId;
                    Become(StatePendingFill);
                });

            Receive<ExchangeOrderRejectedEventArgs>(e => e.ClientOrderId == _clientOrderId,
                args =>
                {
                    Context.Parent.Tell(new Failure());
                    StopSelf();
                });
        }

        void StatePendingFill()
        {
            Receive<ExchangeOrderCancelledEventArgs>(e => e.ClientOrderId == _clientOrderId,
                args =>
                {
                    Context.Parent.Tell(new Cancelled(args.Price ?? 0m,
                        args.RemainingSize,
                        _originalSize - args.RemainingSize));
                    StopSelf();
                });


            Receive<ExchangeOrderFilledEventArgs>(e => e.ClientOrderId == _clientOrderId,
                args =>
                {
                    Context.Parent.Tell(new Filled(args.Price ?? 0m,
                        args.WorkingSize,
                        args.FilledSize));

                    if (args.IsFullyFilled)
                    {
                        StopSelf();
                    }
                });

            ReceiveAsync<CancelRequest>(async _ => { await _orderManager.CancelExchangeOrder(_exchangeOrderId); });
        }

        void StopSelf()
        {
            Context.Stop(_self);
        }

        void ForwardEvent<TEventArgs>(object sender, TEventArgs args)
        {
            _self.Tell(args);
        }

        protected override void PostStop()
        {
            _orderManager.ExchangeOrderAcknowledged -= ForwardEvent;
            _orderManager.ExchangeOrderCancelled -= ForwardEvent;
            _orderManager.ExchangeOrderFilled -= ForwardEvent;
            _orderManager.ExchangeOrderRejected -= ForwardEvent;
        }
    }
}