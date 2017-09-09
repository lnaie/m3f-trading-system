/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Threading;

namespace M3F.TradingSystem.Gdax
{
    public class PollingLevel1Ticker : ITicker
    {
        readonly RestClient _client;
        readonly TimeSpan _timeout;
        readonly Instrument _instrument;

        int _keepRunning;
        Thread _workerThread;
        InsideMarket _insideMarket;

        public PollingLevel1Ticker (RestClient client, Instrument instrument)
            : this(client, instrument, TimeSpan.FromMilliseconds(250))
        {
        }

        public PollingLevel1Ticker (RestClient client, Instrument instrument, TimeSpan pollingInterval)
        {
            _client = client;
            _instrument = instrument;
            _timeout = pollingInterval;
            _keepRunning = 0;
        }

        public event EventHandler<InsideMarketChangedEventArgs> InsideMarketChanged;

        public void Start ()
        {
            if (Interlocked.CompareExchange(ref _keepRunning, 1, 0) == 0)
            {
                _workerThread = new Thread(() =>
                {
                    while (_keepRunning != 0)
                    {
                        try
                        {
                            var json = _client.GetLevel1Book(_instrument.Symbol).Result;
            
                            InsideMarket oldInsideMarket = _insideMarket;

                            _insideMarket = new InsideMarket(GetPriceSize(json, "bids"),
                                                             GetPriceSize(json, "asks"));
                            
                            if (!oldInsideMarket.Equals(_insideMarket))
                            {
                                InsideMarketChanged?.Invoke(
                                    this, 
                                    new InsideMarketChangedEventArgs(_instrument, oldInsideMarket, _insideMarket));
                            }

                            Thread.Sleep(_timeout);
                        }
                        catch (Exception ex)
                        {
                            // ignore for now
                            Console.WriteLine(ex.Message);
                        }
                    }
                });
                _workerThread.Start();
            }
        }

        public void Stop ()
        {
            _keepRunning = 0;
            Join();
            _workerThread = null;
        }

        public void Join ()
        {
            _workerThread?.Join();
        }
        
        static PriveLevel GetPriceSize (Newtonsoft.Json.Linq.JToken json, string side)
        {
            var quotes = ((Newtonsoft.Json.Linq.JArray)json[side]);
            var level = ((Newtonsoft.Json.Linq.JArray)quotes[0]);
            
            return new PriveLevel(
                price: Decimal.Parse(level[0].ToString()), 
                size: Decimal.Parse(level[1].ToString()));
        }
    }
}
