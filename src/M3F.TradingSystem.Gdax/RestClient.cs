/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace M3F.TradingSystem.Gdax
{
    public class RestClient
    {
        readonly HttpClient _httpClient;
        readonly Uri _endpoint;
        readonly string _apiKey;
        readonly string _apiSecret;
        readonly byte[] _apiSecretBytes;
        readonly string _apiPassphrase;

        public RestClient(Uri endpoint, string apiKey, string apiSecret, string apiPassphrase)
        {
            _endpoint = endpoint;
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _apiSecretBytes = Convert.FromBase64String(_apiSecret);
            _apiPassphrase = apiPassphrase;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("CB-ACCESS-KEY", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("CB-ACCESS-PASSPHRASE", _apiPassphrase);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "M3F.Gdax");
        }

        public Task<HttpResponseMessage> PerformGet (string relativeUrl)
        {
            return SendRequestAsync(HttpMethod.Get, relativeUrl);
        }

        public Task<HttpResponseMessage> PerformPost(string relativeUrl, string json)
        {
            return SendRequestAsync(HttpMethod.Post, relativeUrl, json);
        }

        public Task<HttpResponseMessage> SendRequestAsync (HttpMethod method, string relativeUrl, string json = null)
        {
            var timestamp = GetCurrentUnixTimestampSeconds().ToString();
            var requestMsg = new HttpRequestMessage(method, _endpoint + relativeUrl);
            requestMsg.Headers.Add("CB-ACCESS-TIMESTAMP", timestamp);

            var signature = ComputeSignature(timestamp, relativeUrl, method.ToString().ToUpperInvariant(), json);
            requestMsg.Headers.Add("CB-ACCESS-SIGN", signature);

            if (json != null)
            {
                requestMsg.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }

            return _httpClient.SendAsync(requestMsg);
        }

        string ComputeSignature(string timestamp, string relativeUrl, string method, string body)
        {
            var prehash = timestamp + method + relativeUrl + (body ?? String.Empty);
            return HashString(prehash, _apiSecretBytes);
        }

        static string HashString(string str, byte[] secret)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);

            using (var hmac = new System.Security.Cryptography.HMACSHA256(secret))
            {
                byte[] hash = hmac.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }


        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        static double GetCurrentUnixTimestampSeconds()
        {
            return (DateTime.UtcNow - UnixEpoch).TotalSeconds;
        }
    }
}
