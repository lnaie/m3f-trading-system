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
    public class LiveOrderBookTickerFactory : ITickerFactory
    {
        readonly RestClient _client;
        readonly WebSocketClient _webSocketClient;

        public LiveOrderBookTickerFactory (RestClient client, WebSocketClient webSocketClient)
        {
            _client = client;
            _webSocketClient = webSocketClient;
        }

        public async Task<ITicker> CreateTicker (Instrument instrument)
        {
            var quoter = new LiveOrderBookTicker(_client, _webSocketClient, instrument);
            await quoter.BuildBookAsync();
            return quoter;
        }
    }
}