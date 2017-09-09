/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Threading.Tasks;

namespace M3F.TradingSystem.Actors
{
    public interface ITickerFactory
    {
        Task<ITicker> CreateTicker (Instrument instrument);
    }
}
