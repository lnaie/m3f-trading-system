/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace M3F.TradingSystem
{
    public class Instrument
    {
        public static readonly Instrument BtcUsd = new Instrument ("BTC-USD");

        public static readonly Instrument EthUsd = new Instrument ("ETH-USD");

        public Instrument (string symbol)
        {
            Symbol = symbol;
        }

        public string Symbol { get; }

        public override string
        ToString ()
        {
            return Symbol;
        }

        public override bool
        Equals (object obj)
        {
            return Equals(obj as Instrument);
        }

        public bool
        Equals (Instrument other)
        {
            if (ReferenceEquals(other, null))
                return false;
            return Symbol == other.Symbol;
        }

        public override int
        GetHashCode ()
        {
            return Symbol.GetHashCode();
        }

        public static bool
        operator == (Instrument val1, Instrument val2)
        {
            if (ReferenceEquals(val1, null))
                return ReferenceEquals(val2, null);

            return val1.Equals(val2);
        }

        public static bool
        operator != (Instrument val1, Instrument val2)
        {
            return !(val1 == val2);
        }

    }

} // namespace M3F.TradingSystem
