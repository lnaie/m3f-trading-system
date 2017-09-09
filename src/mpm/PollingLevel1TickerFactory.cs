/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Threading.Tasks;
using M3F.TradingSystem.Actors;
using M3F.TradingSystem.Gdax;

namespace M3F.TradingSystem.Mpm
{
    public class PollingLevel1TickerFactory : ITickerFactory
    {
        readonly RestClient _client;

        public PollingLevel1TickerFactory (RestClient client)
        {
            _client = client;
        }

        public Task<ITicker> CreateTicker (Instrument instrument)
        {
            var quoter = new PollingLevel1Ticker(_client, instrument);
            quoter.Start();
            return Task.FromResult<ITicker>(quoter);
        }
    }
}