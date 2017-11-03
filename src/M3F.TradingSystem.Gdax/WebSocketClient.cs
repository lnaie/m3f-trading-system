/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * Copyright (c) 2017-present, lucian.naie@outlook.com
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace M3F.TradingSystem.Gdax
{
    public class GdaxWebSocketMessageEventArgs : EventArgs
    {
        public GdaxWebSocketMessageEventArgs (JObject message)
            => Message = message;

        public JObject Message { get; }
    }

    public class WebSocketClient: IWebSocketClient
    {
        readonly HashSet<Instrument> _subscribedInstruments;
        readonly EventWaitHandle _waitHandle;
        readonly Uri _endpoint;
        ClientWebSocket _socket;
        readonly CancellationTokenSource _cancellationTokenSource;
        CancellationToken _cancellationToken;
        Thread _receiveThread;

        public WebSocketClient (Uri endpoint)
        {
            _subscribedInstruments = new HashSet<Instrument>();
            _waitHandle = new AutoResetEvent(false);
            _endpoint = endpoint;
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }

        public void Start ()
        {
            lock (_cancellationTokenSource)
            {
                if (_receiveThread == null)
                {
                    _receiveThread = new Thread(() =>
                                                {
                                                    while (!_cancellationToken.IsCancellationRequested)
                                                    {
                                                        _waitHandle.Reset();
                                                        try
                                                        {
                                                            ReceiveLoop().Wait(_cancellationToken);
                                                        }
                                                        catch (OperationCanceledException)
                                                        {
                                                            // User requested a stop. Do nothing.
                                                            break;
                                                        }
                                                        catch
                                                        {
                                                            // Keep spinning                                                            
                                                        }
                                                    }
                                                });
                }
                _receiveThread.Start();
            }
            _waitHandle.WaitOne();
        }

        async Task ReceiveLoop ()
        {
            await EnsureConnection();
            _waitHandle.Set();

            using (var memStream = new MemoryStream(1024 * 1024))
            {
                var buffer = new ArraySegment<byte>(new byte[100 * 1024]);

                while (_socket.State == WebSocketState.Open)
                {
                    var result = await _socket.ReceiveAsync(buffer, _cancellationToken);

                    if (result.CloseStatus.HasValue)
                    {
                        break;
                    }

                    memStream.Write(buffer.Array, buffer.Offset, result.Count);
                    if (result.EndOfMessage)
                    {
                        long pos = memStream.Position;
                        memStream.Seek(0, SeekOrigin.Begin);
                        var str = Encoding.UTF8.GetString(memStream.ToArray(), 0, (int) pos);
                        OnMessage(str);
                    }
                }
            }
        }

        async Task EnsureConnection ()
        {
            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(_endpoint, _cancellationToken);

            Instrument[] subscribeThese;

            lock (_subscribedInstruments)
            {
                subscribeThese = _subscribedInstruments.ToArray();
            }

            await Subscribe(subscribeThese);
            SocketReconnected?.Invoke(this, EventArgs.Empty);
        }

        public void Disconnect ()
        {
            _cancellationTokenSource.Cancel();
            lock (_cancellationTokenSource)
            {
                _receiveThread?.Join();
                _receiveThread = null;
            }
            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }
        }

        public async Task Subscribe (params Instrument[] instruments)
        {
            Instrument[] subscribeTheseInstruments = null;

            lock (_subscribedInstruments)
            {
                foreach (var i in instruments)
                {
                    _subscribedInstruments.Add(i);
                }
                subscribeTheseInstruments = _subscribedInstruments.ToArray();
            }

            var sb = new StringBuilder();
            sb.Append("{ \"type\": \"subscribe\", \"product_ids\": [");
            for (int i = 0; i < subscribeTheseInstruments.Length; ++i)
            {
                var productId = subscribeTheseInstruments[i].Symbol;

                sb.Append("\"");
                sb.Append(productId);
                sb.Append("\"");

                if (i < subscribeTheseInstruments.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append("] }");

            await SendStringAsync(sb.ToString());
        }

        public event EventHandler<GdaxWebSocketMessageEventArgs> MessageReceived;
        public event EventHandler SocketReconnected;

        protected virtual void OnMessage (string message)
        {
            EventHandler<GdaxWebSocketMessageEventArgs> handler = MessageReceived;
            if (handler != null)
            {
                JObject o = JObject.Parse(message);
                handler(this, new GdaxWebSocketMessageEventArgs(o));
            }
        }

        protected static Task SendStringAsync (
            WebSocket socket,
            string message,
            CancellationToken token)
        {
            byte[] sendBytes = Encoding.UTF8.GetBytes(message);
            var sendBuffer = new ArraySegment<byte>(sendBytes);
            return socket.SendAsync(
                sendBuffer,
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: token);
        }

        protected static Task SendStringAsync (
            WebSocket socket,
            string message)
        {
            return SendStringAsync(socket, message, CancellationToken.None);
        }

        protected Task SendStringAsync (string message)
        {
            return SendStringAsync(_socket, message);
        }
    }
}
