/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace M3F.TradingSystem.Gdax
{
    public static class GdaxRestClientExtentions
    {
        static async Task<JObject>
            GetBook (RestClient client, string product, int level)
        {
            var response = await client.PerformGet($"/products/{product}/book?level={level}");
            var json = await response.Content.ReadAsStringAsync();
            return JObject.Parse(json);
        }

        public static Task<JObject>
            GetLevel1Book (this RestClient client, string product)
        {
            return GetBook(client, product, 1);
        }

        public static Task<JObject>
            GetLevel2Book (this RestClient client, string product)
        {
            return GetBook(client, product, 2);
        }

        public static Task<JObject>
            GetFullBook (this RestClient client, string product)
        {
            return GetBook(client, product, 3);
        }

        public static async Task<IList<Fill>>
            GetFills (this RestClient client, Instrument instrument = null)
        {
            string url = "/fills";
            if (instrument != null)
            {
                url += $"?product_id={instrument.Symbol}";
            }

            var response = await client.PerformGet(url);
            var json = await response.Content.ReadAsStringAsync();
            var array = JArray.Parse(json);

            var fills = array.Select(jToken => new Fill()
                                               {
                                                   TradeId = (long) jToken["trade_id"],
                                                   Instrument = jToken["product_id"].ParseInstrument(),
                                                   Price = (decimal) jToken["price"],
                                                   Size = (decimal) jToken["size"],
                                                   OrderId = (Guid) jToken["order_id"],
                                                   CreatedAt = (DateTimeOffset) jToken["created_at"],
                                                   Fee = (decimal) jToken["fee"],
                                                   IsSettled = (bool) jToken["settled"],
                                                   Side = jToken["side"].ParseSide(),
                                                   Liquidity =
                                                       ((string) jToken["liquidity"]) == "M"
                                                           ? Fill.LiquidityType.Maker
                                                           : Fill.LiquidityType.Taker,
                                               })
                             .ToList();

            return fills;
        }

        public static async Task<IList<Account>>
            GetAccounts (this RestClient client)
        {
            string url = "/accounts";

            var response = await client.PerformGet(url);
            var json = await response.Content.ReadAsStringAsync();
            var array = JArray.Parse(json);

            var accounts = array.Select(jToken => new Account()
                                                  {
                                                      Id = (string) jToken["id"],
                                                      Currency = jToken["currency"].ParseCurrency(),
                                                      Balance = (decimal) jToken["balance"],
                                                      Available = (decimal) jToken["available"],
                                                      Hold = (decimal) jToken["hold"]
                                                  })
                                .ToList();

            return accounts;
        }

        public static Task<JObject> SendOrder (
            this RestClient client,
            ClientOrderId clientOrderId,
            Instrument instrument,
            Side side,
            ExchangeOrderType orderType,
            decimal? price,
            decimal size,
            bool postOnly = true
            )
        {
            // TODO: Add Time-in-Force

            var jobj = new JObject();
            dynamic o = jobj;
            o.client_oid = clientOrderId.Id;
            o.product_id = instrument.Symbol;
            o.type = orderType.ToString().ToLowerInvariant();
            o.side = side.ToString().ToLowerInvariant();


            o.size = size;

            switch (orderType)
            {
                case ExchangeOrderType.Limit:
                    o.stp = "cn"; // Self trading prevention -> cancel newest
                    o.post_only = postOnly;
                    goto case ExchangeOrderType.Stop;

                case ExchangeOrderType.Stop:
                    o.price = price;
                    break;

                case ExchangeOrderType.Market:
                    break;
            }

            return DoPost(client, jobj);
        }

        public static Task CancelOrder (this RestClient client,
                                        string workingOrderId)
        {
            return client.SendRequestAsync(System.Net.Http.HttpMethod.Delete,
                                           $"/orders/{workingOrderId}");
        }

        static async Task<JObject> DoPost (RestClient client, JObject json)
        {
            var jsonStr = json.ToString();
            var response = await client.PerformPost("/orders", jsonStr);
            var responseJsonStr = await response.Content.ReadAsStringAsync();
            return JObject.Parse(responseJsonStr);
        }
    }
}
