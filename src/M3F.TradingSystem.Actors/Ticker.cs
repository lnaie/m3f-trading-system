/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Akka.Actor;

namespace M3F.TradingSystem.Actors
{
    public class Ticker : ReceiveActor
    {
        readonly ITickerFactory _tickerFactory;
        readonly Dictionary<Instrument, IActorRef> _tickers;

        public abstract class SubscriptionAction
        {
            protected SubscriptionAction (Instrument instrument)
            {
                Instrument = instrument;
            }

            public Instrument Instrument { get; }
        }

        public class Subscribe : SubscriptionAction
        {
            public Subscribe (Instrument instrument) : base(instrument)
            {
            }
        }

        public class Unsubscribe : SubscriptionAction
        {
            public Unsubscribe (Instrument instrument) : base(instrument)
            {
            }
        }

        public Ticker (ITickerFactory tickerFactory)
        {
            _tickers = new Dictionary<Instrument, IActorRef>();
            _tickerFactory = tickerFactory;

            Receive<Subscribe>(sub => { GetOrAddTicker(sub.Instrument).Forward(sub); });

            Receive<Unsubscribe>(sub => { GetOrAddTicker(sub.Instrument).Forward(sub); });
        }

        IActorRef GetOrAddTicker (Instrument instrument)
        {
            IActorRef ticker;

            if (!_tickers.TryGetValue(instrument, out ticker))
            {
                var quoter = _tickerFactory.CreateTicker(instrument).Result;
                ticker = Context.ActorOf(Props.Create(() => new InstrumentTicker(quoter)), instrument.Symbol);
                _tickers[instrument] = ticker;
            }

            return ticker;
        }

        class InstrumentTicker : ReceiveActor
        {
            readonly ITicker _ticker;
            readonly IActorRef _self;
            readonly List<IActorRef> _subscribers;

            public InstrumentTicker (ITicker ticker)
            {
                _self = Self;
                _subscribers = new List<IActorRef>();
                _ticker = ticker;
                _ticker.InsideMarketChanged += InsideMarketChanged;

                Receive<Subscribe>(sub => { _subscribers.Add(Context.Sender); });

                Receive<Unsubscribe>(sub => { _subscribers.Remove(Context.Sender); });

                Receive<InsideMarketChangedEventArgs>(args => { _subscribers.ForEach(s => s.Forward(args)); });
            }

            void InsideMarketChanged (object sender, InsideMarketChangedEventArgs e)
            {
                _self.Tell(e, _self);
            }
        }
    }
}
