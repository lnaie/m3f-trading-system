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
    public struct PriveLevel : IEquatable<PriveLevel>
    {
        public PriveLevel (decimal price, decimal size)
        {
            Price = price;
            Size  = size;
        }

        public decimal Price { get; }
        public decimal Size { get; }

        public override bool
        Equals (object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            return Equals((PriveLevel)obj);
        }

        public bool
        Equals (PriveLevel other)
        {
            return Price == other.Price && Size == other.Size;
        }

        public override int
        GetHashCode ()
        {
            return Price.GetHashCode() ^ Size.GetHashCode();
        }

    }

}
