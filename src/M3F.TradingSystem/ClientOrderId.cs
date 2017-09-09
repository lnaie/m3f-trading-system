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
    public class ClientOrderId : IEquatable<ClientOrderId>
    {
        public Guid Id { get; }
        public ClientOrderId ()
            : this(Guid.NewGuid())
        {}

        public ClientOrderId (Guid id)
        {
            Id = id;
        }

        public bool
        Equals (ClientOrderId other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Id.Equals(other.Id);
        }

        public override bool
        Equals (object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((ClientOrderId)obj);
        }

        public override int
        GetHashCode ()
        {
            return Id.GetHashCode();
        }

        public static bool
        operator == (ClientOrderId left, ClientOrderId right)
        {
            return Equals(left, right);
        }

        public static bool
        operator != (ClientOrderId left, ClientOrderId right)
        {
            return !Equals(left, right);
        }

        public override string
        ToString ()
        {
            return Id.ToString();
        }

    }

} // namespace M3F.TradingSystem
