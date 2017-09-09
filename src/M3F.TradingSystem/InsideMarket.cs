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
    public struct InsideMarket : IEquatable<InsideMarket>
    {
        public InsideMarket (PriveLevel bid, PriveLevel ask)
        {
            Bid = bid;
            Ask = ask;
        }

        public PriveLevel Bid { get; }
        public PriveLevel Ask { get; }

        public bool
        Equals (InsideMarket other)
        {
            return Bid.Equals(other.Bid) && Ask.Equals(other.Ask);
        }

        public override bool
        Equals (object obj)
        {
            if (obj is InsideMarket)
                return Equals((InsideMarket)obj);
            return false;
        }

        public override int
        GetHashCode ()
        {
            return Bid.GetHashCode() ^ Ask.GetHashCode();
        }

    }

}
