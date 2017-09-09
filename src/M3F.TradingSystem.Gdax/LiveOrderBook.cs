/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace M3F.TradingSystem.Gdax
{
    public class FullBookSnapshotDownloadedEventArgs : EventArgs
    {
        public FullBookSnapshotDownloadedEventArgs (JObject snapshot)
        {
            Snapshot = snapshot;
        }
        public JObject Snapshot { get; }
    }

    public class LiveOrderBookTicker : ITicker
    {
        readonly RestClient _client;
        readonly WebSocketClient _socket;
        readonly Instrument _instrument;
        readonly ConcurrentQueue<JObject> _pending;

        public event EventHandler<FullBookSnapshotDownloadedEventArgs> FullBookSnapshotDownloaded;
        public event EventHandler<GdaxWebSocketMessageEventArgs> BookUpdateReceived;

        public event EventHandler<InsideMarketChangedEventArgs> InsideMarketChanged;

        readonly MarketSide _bids;
        readonly MarketSide _asks;
        readonly Dictionary<Guid, Order> _allOrders;

        long _lastSequenceNumber = 0;
        InsideMarket _insideMarket;

        volatile bool _bookBuildingInProgress;


        public LiveOrderBookTicker (RestClient client, WebSocketClient socket, Instrument instrument)
        {
            _bookBuildingInProgress = true;
            _client = client;
            _socket = socket;
            _instrument = instrument;
            _pending = new ConcurrentQueue<JObject>();
            _bids = new MarketSide(Side.Bid);
            _asks = new MarketSide(Side.Ask);
            _allOrders = new Dictionary<Guid, Order>();

            _socket.MessageReceived += SocketMessageReceived;
            _socket.SocketReconnected += SocketReconnected;
        }

        public async Task BuildBookAsync ()
        {
            _bookBuildingInProgress = true;

            _bids.Clear();
            _asks.Clear();
            _allOrders.Clear();

            await _socket.Subscribe(_instrument);

            var bookResponse = await _client.GetFullBook(_instrument.Symbol);
            FullBookSnapshotDownloaded?.Invoke(this, new FullBookSnapshotDownloadedEventArgs(bookResponse));

            dynamic snapshot = bookResponse;
            _lastSequenceNumber = snapshot.sequence;

            PopulateMarketSideFromSnapshot(_asks, snapshot.asks);
            PopulateMarketSideFromSnapshot(_bids, snapshot.bids);

            JObject pendingUpdate;
            while (_pending.TryDequeue(out pendingUpdate))
            {
                dynamic jobj = pendingUpdate;
                if (jobj.sequence <= _lastSequenceNumber)
                {
                    continue;
                }

                ProcessLiveOrderMessage(jobj);
            }

            _bookBuildingInProgress = false;
        }

        async void SocketReconnected (object sender, EventArgs eventArgs)
        {
            await BuildBookAsync();
        }

        async void ProcessLiveOrderMessage (JObject jobj)
        {
            dynamic m = jobj;

            if ((decimal) m.sequence != 1m + _lastSequenceNumber)
            {
                await BuildBookAsync();
                return;
            }

            BookUpdateReceived?.Invoke(this, new GdaxWebSocketMessageEventArgs(jobj));

            _lastSequenceNumber = m.sequence;

            string type = m.type;

            switch (type)
            {
                case "received":
                    ProcessReceivedMessage(jobj);
                    break;

                case "open":
                    ProcessOpenMessage(jobj);
                    break;

                case "done":
                    ProcessDoneMessage(jobj);
                    break;

                case "match":
                    ProcessMatchMessage(jobj);
                    break;

                case "change":
                    ProcessChangeMessage(jobj);
                    break;

                case "error":
                    ProcessErrorMessage(jobj);
                    break;
                default:
                    return;
            }

            var newInsideMarket = new InsideMarket(_bids.GetBest(), _asks.GetBest());

            if (!_insideMarket.Equals(newInsideMarket))
            {
                var args = new InsideMarketChangedEventArgs(_instrument, newInsideMarket, _insideMarket);
                _insideMarket = newInsideMarket;

                InsideMarketChanged?.Invoke(this, args);
            }
        }

        void ProcessErrorMessage (JObject jobj)
        {
            Console.Error.WriteLine(jobj.ToString(Formatting.Indented));
        }

        void PopulateMarketSideFromSnapshot (MarketSide marketSide, JArray orderArray)
        {
            foreach (var jToken in orderArray)
            {
                var jsonOrder = (JArray) jToken;
                var order = new Order(
                                      (Guid) jsonOrder[2],
                                      marketSide.Side,
                                      (decimal) jsonOrder[0],
                                      (decimal) jsonOrder[1]);

                AddOrder(order);
            }
        }

        void SocketMessageReceived (object sender, GdaxWebSocketMessageEventArgs e)
        {
            if (e.Message["product_id"].ToString() == _instrument.Symbol)
            {
                if (_bookBuildingInProgress)
                {
                    _pending.Enqueue(e.Message);
                }
                else
                {
                    ProcessLiveOrderMessage(e.Message);
                }
            }
        }

        MarketSide GetMarketSide (Side side)
        {
            return side == Side.Ask ? _asks : _bids;
        }

        void AddOrder (Order order)
        {
            _allOrders.Add(order.Id, order);
            var side = GetMarketSide(order.Side);
            side.AddOrder(order);
        }

        void RemoveOrder (Order order)
        {
            var side = GetMarketSide(order.Side);
            side.RemoveOrder(order);
            _allOrders.Remove(order.Id);
        }

        void ProcessReceivedMessage (dynamic msg)
        {
            // Nothing
        }

        void ProcessOpenMessage (JObject openMsg)
        {
            dynamic msg = openMsg;
            var size = openMsg.GetDecimal("size", "remaining_size");
            if (!size.HasValue)
            {
                return;
            }

            var order = new Order((Guid) msg.order_id,
                                  ((JToken) msg.side).ParseSide(),
                                  (decimal) msg.price,
                                  size.Value);

            AddOrder(order);
        }

        void ProcessMatchMessage (dynamic msg)
        {
            // Trade happened between two orders.
            Guid makerId = msg.maker_order_id;
            if (_allOrders.TryGetValue(makerId, out var makerOrder))
            {
                decimal price  = msg.price;
                decimal size   = msg.size;
                var newSize    = makerOrder.Size - size;
                var marketSide = GetMarketSide(makerOrder.Side);

                var level = marketSide.GetLevel(price);
                Debug.Assert(level != null);
                Debug.Assert(level.Price == price);
                Debug.Assert(level.Orders[0].Id == makerId);

                // Update size
                level.AccumulatedSize -= size;
                makerOrder.Size = newSize;

                if (newSize == 0m)
                {
                    RemoveOrder(makerOrder);
                }
            }
        }

        void ProcessChangeMessage (JObject msg)
        {
            var orderId = (Guid) msg["order_id"];

            if (_allOrders.TryGetValue(orderId, out var order))
            {
                var side = msg["side"].ParseSide();
                var price = (decimal) msg["price"];
                var newSize = msg.GetDecimal("new_size", "new_funds");
                if (!newSize.HasValue)
                {
                    return;
                }

                Debug.Assert(order.Price == price);
                Debug.Assert(order.Side == side);

                var marketSide = GetMarketSide(order.Side);

                var level = marketSide.GetLevel(order.Price);
                var diff = newSize.Value - order.Size;
                level.AccumulatedSize -= diff;
                order.Size = newSize.Value;
            }
        }

        void ProcessDoneMessage (dynamic msg)
        {
            Guid id = msg.order_id;

            if (!_allOrders.TryGetValue(id, out Order existingOrder))
            {
                return;
            }

            RemoveOrder(existingOrder);
        }

        class Order : IEquatable<Order>
        {
            public Guid Id { get; }
            public decimal Price { get; }
            public decimal Size { get; set; }
            public Side Side { get; }

            public Order (Guid id, Side side, decimal price, decimal size)
            {
                Id = id;
                Side = side;
                Price = price;
                Size = size;
            }

            public override bool Equals (object obj)
            {
                return Equals(obj as Order);
            }

            public bool Equals (Order other)
            {
                if (!ReferenceEquals(other, null))
                {
                    return Id.Equals(other.Id) && Side == other.Side && Price == other.Price && Size == other.Size;
                }
                return false;
            }

            public override int GetHashCode ()
            {
                return
                    Id.GetHashCode() ^
                    Price.GetHashCode() ^
                    Side.GetHashCode();
            }
        }

        class MarketLevel
        {
            readonly List<Order> _orders;

            public MarketLevel (decimal price)
            {
                Price = price;
                _orders = new List<Order>();
            }

            public decimal Price { get; }

            public decimal AccumulatedSize { get; set; }

            public int Count => _orders.Count;

            public IReadOnlyList<Order> Orders => _orders;

            public void Add (Order order)
            {
                _orders.Add(order);
                AccumulatedSize += order.Size;
            }

            public bool Remove (Order order)
            {
                var removed = _orders.Remove(order);
                if (removed)
                {
                    AccumulatedSize -= order.Size;
                }
                return removed;
            }
        }

        class MarketSide
        {
            readonly SortedDictionary<decimal, MarketLevel> _orders;

            public MarketSide (Side side)
            {
                Side = side;

                IComparer<decimal> comparer;
                if (Side == Side.Buy)
                {
                    comparer = new ReverseComparer<decimal>(Comparer<decimal>.Default);
                }
                else
                {
                    comparer = Comparer<decimal>.Default;
                }

                _orders = new SortedDictionary<decimal, MarketLevel>(comparer);
            }

            public Side Side { get; }

            public MarketLevel GetLevel (decimal price)
            {
                if (!_orders.TryGetValue(price, out MarketLevel levelOrders))
                {
                    levelOrders = new MarketLevel(price);
                    _orders[price] = levelOrders;
                }
                return levelOrders;
            }

            public void AddOrder (Order order)
            {
                GetLevel(order.Price).Add(order);
            }

            public bool RemoveOrder (Order order)
            {
                if (_orders.TryGetValue(order.Price, out MarketLevel levelOrders))
                {
                    levelOrders.Remove(order);
                    if (levelOrders.Count == 0)
                    {
                        _orders.Remove(order.Price);
                    }
                    return true;
                }
                return false;
            }

            public PriveLevel GetBest ()
            {
                var levelOrders = _orders.Values.FirstOrDefault();
                if (levelOrders == null || levelOrders.Count == 0)
                {
                    return default(PriveLevel);
                }

                return new PriveLevel(levelOrders.Price, levelOrders.AccumulatedSize);
            }

            public void Clear ()
            {
                _orders.Clear();
            }
        }
    }
}