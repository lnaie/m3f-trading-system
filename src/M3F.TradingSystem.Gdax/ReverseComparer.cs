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

namespace M3F.TradingSystem.Gdax
{
    public sealed class ReverseComparer<T> : IComparer<T>
    {
        readonly IComparer<T> _originalComparer;

        public ReverseComparer(IComparer<T> originalComparer)
        {
            _originalComparer = originalComparer ?? throw new ArgumentNullException(nameof(originalComparer));
        }

        public int Compare(T x, T y)
        {
            return _originalComparer.Compare(y, x);
        }
    }
}