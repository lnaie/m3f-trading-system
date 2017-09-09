/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Akka.Actor;
using Akka.Event;

namespace M3F.TradingSystem.Actors
{
    public class InsideMarketReporterActor : ReceiveActor
    {
        readonly Akka.Event.ILoggingAdapter _log = Akka.Event.Logging.GetLogger(Context);

        readonly LogLevel _logLevel;
        readonly IActorRef _ticker;
        readonly Instrument _instrument;

        public InsideMarketReporterActor (IActorRef ticker, Instrument instrument, LogLevel logLevel)
        {
            _instrument = instrument;
            _logLevel = logLevel;
            _ticker = ticker;
            _ticker.Tell(new Ticker.Subscribe(instrument));

            Receive<InsideMarketChangedEventArgs>(e =>
            {                
                if (_log.IsEnabled(_logLevel))
                {
                    _log.Log(
                        _logLevel,
                        "{0} | b: {1:0.00} [{2:0.00000000}] | a: {3:0.00} [{4:0.00000000}]",
                        _instrument.Symbol,
                        e.NewInsideMarket.Bid.Price, e.NewInsideMarket.Bid.Size,
                        e.NewInsideMarket.Ask.Price, e.NewInsideMarket.Ask.Size);
                }
            });
        }
    }
}

