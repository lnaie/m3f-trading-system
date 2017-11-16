/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Linq.Expressions;
using Akka.Actor;
using System.Threading.Tasks;
using M3F.TradingSystem.Actors;

namespace M3F.TradingSystem.Mpm
{
    public class BuyLowSellHighActor : ReceiveActor
    {
        readonly Akka.Event.ILoggingAdapter _log = Akka.Event.Logging.GetLogger(Context);

        readonly IOrderManager _orderManager;
        readonly Instrument _instrument;

        readonly decimal _orderSize;
        readonly decimal _buyPriceOffset;
        readonly decimal _sellPriceOffset;
        readonly double  _reloadBuySeconds;
        readonly double _placeNewOrderDelaySeconds;
        decimal _previousBuyPrice;
        decimal _profitAndLoss;

        InsideMarket? _insideMarket;

        IActorRef _buyOrder;
        IActorRef _sellOrder;

        public class Start
        {
            public bool Immediate {get;}
            public Start (bool immediate = false) => Immediate = immediate;
        }

        public class BuyTimerElapsed { }

        public BuyLowSellHighActor (IActorRef ticker,
                               IOrderManager orderManager,
                               Instrument instrument,
                               decimal size,
                               decimal buyOffset,
                               decimal sellOffset,
                               double reloadBuySeconds,
                               double placeNewOrderDelaySeconds)
        {
            _orderManager = orderManager;
            _instrument = instrument;

            _orderSize = size;
            _buyPriceOffset = buyOffset;
            _sellPriceOffset = sellOffset;
            _reloadBuySeconds = reloadBuySeconds;
            _placeNewOrderDelaySeconds = placeNewOrderDelaySeconds;

            ticker.Tell(new TickerActor.Subscribe(_instrument));

            Become(StateNotInMarket);
        }

        void StateNotInMarket ()
        {
            ReceiveAsync<Start>(async r =>
                           {
                               _buyOrder = spawnChild(() => new ExchangeOrderActor(_orderManager,
                                                                              ExchangeOrderType.Limit,
                                                                              _instrument,
                                                                              Side.Buy));

                               _sellOrder = spawnChild(() => new ExchangeOrderActor(_orderManager,
                                                                               ExchangeOrderType.Limit,
                                                                               _instrument,
                                                                               Side.Sell));

                               if (r.Immediate && _insideMarket.HasValue)
                               {
                                   var buyPrice = _insideMarket.Value.Ask.Price - _buyPriceOffset;
                                   await PlaceBuyOrder(buyPrice);
                               }
                               else
                               {
                                   Become(StateWaitingForPlacingBuy);
                               }
                           });
        }

        void StateWaitingForPlacingBuy ()
        {
            ReceiveAsync<InsideMarketChangedEventArgs>(async im =>
                                                  {
                                                      _insideMarket = im.NewInsideMarket;
                                                      var buyPrice = _insideMarket.Value.Ask.Price - _buyPriceOffset;
                                                      await PlaceBuyOrder(buyPrice);
                                                  });
        }

        async Task PlaceBuyOrder (decimal buyPrice)
        {
            await DelayNewOrderPlacement();

            _previousBuyPrice = buyPrice;
            _log.Info("Placing buy order of {0} @ {1}", _orderSize, buyPrice);
            _buyOrder.Tell(new ExchangeOrderActor.ChangeRequest(buyPrice,
                                                           _orderSize,
                                                           true));

            Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(_reloadBuySeconds),
                                                      Self,
                                                      new BuyTimerElapsed(),
                                                      Self);

            Become(StateWaitingForBuyToFill);
        }

        Task DelayNewOrderPlacement ()
        {
            return Task.Delay(TimeSpan.FromSeconds(_placeNewOrderDelaySeconds));
        }

        void StateWaitingForBuyToFill ()
        {
            _log.Info("Waiting for buy to fill");

            Receive<InsideMarketChangedEventArgs>(im =>
                                                  {
                                                      _insideMarket = im.NewInsideMarket;
                                                  });

            Receive<BuyTimerElapsed>(_ =>
                                     {
                                         var buyPrice = _insideMarket.Value.Ask.Price - _buyPriceOffset;
                                         if (buyPrice > _previousBuyPrice)
                                         {
                                             _log.Info("Cancelling buy order, as the market has moved up ({0})", _insideMarket.Value.Ask.Price);
                                             _buyOrder.Tell(new ExchangeOrderActor.CancelRequest());
                                         }
                                         else
                                         {
                                             _log.Info("Buy timer elapsed. Keeping same buy order.");                                             
                                             Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(_reloadBuySeconds),
                                                      Self,
                                                      new BuyTimerElapsed(),
                                                      Self);
                                         }
                                     });

            ReceiveAsync<ExchangeOrderActor.Filled>(async filled =>
                                          {
                                               _log.Info("Bought {0} @ {1}", filled.FilledSize, filled.Price);

                                              if (filled.IsFullyFilled)
                                              {
                                                  // TODO: Ensure this is higher than inside market
                                                  var targetPrice = filled.Price + _sellPriceOffset;

                                                  await DelayNewOrderPlacement();

                                                  _log.Info("Placing sell of {0} @ {1}", _orderSize, targetPrice);
                                                  _sellOrder.Tell(new ExchangeOrderActor.ChangeRequest(targetPrice,
                                                                                                  _orderSize,
                                                                                                  true));
                                                  Become(StateWaitingForSellToFill);
                                              }
                                          });

            Receive<ExchangeOrderActor.Cancelled>(c =>
                                             {
                                                 _log.Info("Buy cancelled");
                                                 Reload(immediate: false);
                                             });

            Receive<Failure>(_ =>
                             {
                                 _log.Info("Failed to place buy order. Retrying.");
                                 Reload();
                             });
        }

        void StateWaitingForSellToFill ()
        {
            Receive<ExchangeOrderActor.Filled>(filled =>
                                          {
                                              _profitAndLoss += filled.FilledSize * (filled.Price - _previousBuyPrice);
                                              _log.Info("Sold {0} at {1}. Realized P&L: {2}", filled.FilledSize, filled.Price, _profitAndLoss);

                                              if (filled.IsFullyFilled)
                                              {
                                                  Reload();
                                              }
                                          });

            Receive<ExchangeOrderActor.Cancelled>(c =>
                                             {
                                                 _log.Info("Sell cancelled");
                                                 Reload();
                                             });
        }

        IActorRef spawnChild<TActor> (Expression<Func<TActor>> factory, string name = null)
            where TActor : ActorBase
        {
            return Context.ActorOf(Props.Create(factory), name);
        }

        void Reload (bool immediate = false)
        {
            _buyOrder.Tell(PoisonPill.Instance);
            _sellOrder.Tell(PoisonPill.Instance);

            Become(StateNotInMarket);
            Self.Tell(new Start(immediate));
        }
    }
}
