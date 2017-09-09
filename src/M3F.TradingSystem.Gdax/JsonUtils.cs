/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Newtonsoft.Json.Linq;

namespace M3F.TradingSystem.Gdax
{
    static class JsonUtils
    {
        public static Side ParseSide (this JToken token)
        {
            var side = token.Value<string>().ToLowerInvariant();
            switch (side)
            {
                case "buy":
                case "bid":
                    return Side.Buy;
                case "ask":
                case "sell":
                    return Side.Sell;
            }
            throw new ArgumentException("Unknown side: " + side);
        }

        public static Instrument ParseInstrument (this JToken token)
        {
            return new Instrument(token.Value<string>());
        }

        public static Currency ParseCurrency (this JToken token)
        {
            return new Currency(token.Value<string>());
        }

        public static decimal? GetDecimal (this JObject jobj, params string[] names)
        {
            foreach (var name in names)
            {
                var val = jobj.GetValue(name);
                if (val != null)
                {
                    var result = (decimal) val;
                    return result;
                }
            }

            return null;
        }
    }
}
