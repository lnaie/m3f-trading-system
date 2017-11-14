///**
// * Copyright (c) 2017-present, CryptoPilot
// * Copyright (c) 2017-present, Filip Frącz
// * Copyright (c) 2017-present, M3F Innovations, LLC
// * All rights reserved.
// *
// * This source code is licensed under the BSD-style license found in the
// * LICENSE file in the root directory of this source tree.
// */

//using CryptoPilot.Models;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net.WebSockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace M3F.TradingSystem.Clients
//{
//    public class WebSocketClient : IWebSocketClient
//    {
//        private readonly HashSet<Market> _subscribedMarkets;
//        private readonly EventWaitHandle _waitHandle;
//        private readonly Uri _endpoint;
//        private ClientWebSocket _socket;
//        private readonly CancellationTokenSource _cancellationTokenSource;
//        private CancellationToken _cancellationToken;
//        private Thread _receiveThread;

//        public WebSocketClient(Uri endpoint)
//        {
//            _subscribedMarkets = new HashSet<Market>();
//            _waitHandle = new AutoResetEvent(false);
//            _endpoint = endpoint;
//            _cancellationTokenSource = new CancellationTokenSource();
//            _cancellationToken = _cancellationTokenSource.Token;
//        }

//        public event EventHandler<WebSocketJsonMessageEventArgs> MessageReceived;
//        public event EventHandler Reconnected;

//        public void Start()
//        {
//            lock (_cancellationTokenSource)
//            {
//                if (_receiveThread == null)
//                {
//                    _receiveThread = new Thread(() =>
//                    {
//                        while (!_cancellationToken.IsCancellationRequested)
//                        {
//                            _waitHandle.Reset();

//                            try
//                            {
//                                ReceiveLoopAsync().Wait(_cancellationToken);
//                            }
//                            catch (OperationCanceledException)
//                            {
//                                // User requested a stop. Do nothing.
//                                break;
//                            }
//                            catch
//                            {
//                                // Keep spinning
//                            }
//                        }
//                    });
//                }

//                _receiveThread.Start();
//            }

//            _waitHandle.WaitOne();
//        }

//        public void Disconnect()
//        {
//            _cancellationTokenSource.Cancel();

//            lock (_cancellationTokenSource)
//            {
//                _receiveThread?.Join();
//                _receiveThread = null;
//            }

//            if (_socket != null)
//            {
//                _socket.Dispose();
//                _socket = null;
//            }
//        }

//        public virtual Task SubscribeAsync(params Market[] markets)
//        {
//            return Task.CompletedTask;
//        }

//        protected virtual void OnMessage(string message)
//        {
//            EventHandler<WebSocketJsonMessageEventArgs> handler = MessageReceived;

//            if (handler != null)
//            {
//                JObject o = JObject.Parse(message);
//                handler(this, new WebSocketJsonMessageEventArgs(o));
//            }
//        }

//        protected HashSet<Market> SubscribedMarkets
//        {
//            get { return _subscribedMarkets; }
//        }

//        protected static Task SendStringAsync(
//            WebSocket socket,
//            string message,
//            CancellationToken token)
//        {
//            byte[] sendBytes = Encoding.UTF8.GetBytes(message);
//            var sendBuffer = new ArraySegment<byte>(sendBytes);

//            return socket.SendAsync(
//                sendBuffer,
//                WebSocketMessageType.Text,
//                endOfMessage: true,
//                cancellationToken: token);
//        }

//        protected static Task SendStringAsync(
//            WebSocket socket,
//            string message)
//        {
//            return SendStringAsync(socket, message, CancellationToken.None);
//        }

//        protected Task SendStringAsync(string message)
//        {
//            return SendStringAsync(_socket, message);
//        }

//        private async Task ReceiveLoopAsync()
//        {
//            await EnsureConnectionAsync();
//            _waitHandle.Set();

//            using (var memStream = new MemoryStream(1024 * 1024))
//            {
//                var buffer = new ArraySegment<byte>(new byte[100 * 1024]);

//                while (_socket.State == WebSocketState.Open)
//                {
//                    var result = await _socket.ReceiveAsync(buffer, _cancellationToken);

//                    if (result.CloseStatus.HasValue)
//                    {
//                        break;
//                    }

//                    memStream.Write(buffer.Array, buffer.Offset, result.Count);

//                    if (result.EndOfMessage)
//                    {
//                        long pos = memStream.Position;
//                        memStream.Seek(0, SeekOrigin.Begin);
//                        var str = Encoding.UTF8.GetString(memStream.ToArray(), 0, (int)pos);

//                        OnMessage(str);
//                    }
//                }
//            }
//        }

//        private async Task EnsureConnectionAsync()
//        {
//            _socket = new ClientWebSocket();
//            await _socket.ConnectAsync(_endpoint, _cancellationToken);

//            Market[] subscribeThese;

//            lock (_subscribedMarkets)
//            {
//                subscribeThese = _subscribedMarkets.ToArray();
//            }

//            await SubscribeAsync(subscribeThese);
//            Reconnected?.Invoke(this, EventArgs.Empty);
//        }
//    }
//}
