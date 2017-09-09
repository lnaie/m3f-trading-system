/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace M3F.TradingSystem
{
    public class Currency : IEquatable<Currency>
    {
        public static readonly Currency Btc = new Currency("BTC");
        public static readonly Currency Eth = new Currency("ETH");
        public static readonly Currency Usd = new Currency("USD");
        public static readonly Currency Ltc = new Currency("LTC");

        public Currency(string symbol)
        {
            Symbol = symbol;
        }

        public string Symbol { get; }

        public override string ToString()
        {
            return Symbol;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Currency);
        }

        public bool Equals (Currency other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Symbol, other.Symbol);
        }

        public override int GetHashCode()
        {
            return (Symbol != null ? Symbol.GetHashCode() : 0);
        }

        public static bool operator ==(Currency val1, Currency val2)
        {
            if (ReferenceEquals(val1, null))
            {
                return ReferenceEquals(val2, null);
            }

            return val1.Equals(val2);
        }

        public static bool operator !=(Currency val1, Currency val2)
        {
            return !(val1 == val2);
        }


    }
}
