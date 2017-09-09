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
    public class ExchangeOrderId : IEquatable<ExchangeOrderId>
    {
        public ExchangeOrderId (string id)
        {
            Id = id;
        }

        public ExchangeOrderId (Guid id)
        {
            Id = id.ToString().Replace("{", String.Empty).Replace("}", String.Empty);
        }

        public string Id { get; }

        public override string 
        ToString()
        {
            return this.Id;
        }

        public bool
        Equals (ExchangeOrderId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id);
        }

        public override bool
        Equals (object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExchangeOrderId)obj);
        }

        public override int
        GetHashCode ()
        {
            return Id != null ? Id.GetHashCode() : 0;
        }

        public static bool
        operator == (ExchangeOrderId left, ExchangeOrderId right)
        {
            return Equals(left, right);
        }

        public static bool
        operator != (ExchangeOrderId left, ExchangeOrderId right)
        {
            return !Equals(left, right);
        }

    }

} // namespace M3F.TradingSystem
