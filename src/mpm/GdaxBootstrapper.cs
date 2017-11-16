/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * Copyright (c) 2017-present, lucian.naie@outlook.com
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Akka.Actor;
using Akka.Event;
using M3F.TradingSystem.Actors;
using M3F.TradingSystem.Gdax;
using Newtonsoft.Json.Linq;
using System;

namespace M3F.TradingSystem.Mpm
{
    public class GdaxBootstrapper
    {
        private readonly ConsoleLogger _logger;
        private readonly Helpers _helpers;

        public GdaxBootstrapper(
            ConsoleLogger logger, 
            Helpers helpers)
        {
            _logger = logger;
            _helpers = helpers;
        }

        /// <summary>
        /// Create the GDAX actor sub-system and try to start it.
        /// </summary>
        /// <param name="system">Root actor.</param>
        /// <param name="config">Root/app configuration.</param>
        /// <returns>True if GDAX sub-system has started.</returns>
        public bool Start(
            ActorSystem system, 
            JObject config)
        {
            try
            {
                _logger.Log("GDAX: processing...");

                var gdax = config["gdax"] as JToken;

                if (gdax == null || gdax["enabled"]?.ToString().ToLower() != "true")
                {
                    _logger.Log("GDAX: Error. Undefined section 'gdax' or 'gdax.enabled' is off in the config file.");
                    return false;
                }

                var gdaxConfigPath = gdax["config_path"]?.ToString();

                if (!string.IsNullOrWhiteSpace(gdaxConfigPath))
                {
                    gdaxConfigPath = gdaxConfigPath.Replace("~", "%HOME%");
                    gdaxConfigPath = System.Environment.ExpandEnvironmentVariables(gdaxConfigPath);
                }

                if (string.IsNullOrWhiteSpace(gdaxConfigPath))
                {
                    _logger.Log("GDAX: Error. Undefined property 'gdax.config_path' in the mpm config file.");
                    return false;
                }

                var gdaxConfig = _helpers.ReadJson(gdaxConfigPath);

                var restClient = new RestClient(new Uri(
                    gdaxConfig["gdax_api_uri"].ToString()),
                    gdaxConfig["gdax_api_key"].ToString(),
                    gdaxConfig["gdax_api_secret"].ToString(),
                    gdaxConfig["gdax_api_passphrase"].ToString());

                var marketsConf = gdax["markets"] as JObject;

                foreach (var marketConf in marketsConf)
                {
                    var marketKey = marketConf.Key.Trim();
                    var marketValue = marketConf.Value as JToken;

                    // Setup instrument dependencies
                    var instrument = new Instrument(marketKey);
                    _logger.Log($"GDAX:{instrument}: processing market...");

                    var wsClient = new WebSocketClient(new Uri(gdaxConfig["gdax_ws_uri"].ToString()));
                    wsClient.Start();
                    wsClient.Subscribe(instrument).Wait();

                    var orderManager = new Gdax.RestOrderManager(restClient, wsClient);
                    var ticker = system.ActorOf(Props.Create(
                        () => new TickerActor(new LiveOrderBookTickerFactory(restClient, wsClient))),
                        $"gdax-{instrument}-ticker");
                    var reporter = system.ActorOf(Props.Create(
                        () => new InsideMarketReporterActor(
                            ticker,
                            instrument,
                            LogLevel.DebugLevel)),
                        $"gdax-{instrument}-reporter");

                    var strategiesConf = marketValue["strategies"] as JObject;

                    // Start strategies
                    foreach (var strategyConf in strategiesConf)
                    {
                        var strategyKey = strategyConf.Key.ToString();
                        var strategyValue = strategyConf.Value as JToken;
                        var strategyName = strategyValue["name"]?.ToString() ?? strategyKey;

                        var blshStrategy = system.ActorOf(Props.Create(
                            () => new BuyLowSellHighActor(
                                ticker,
                                orderManager,
                                instrument,
                                (decimal)strategyValue["order_size"],
                                (decimal)strategyValue["buy_offset"],
                                (decimal)strategyValue["sell_offset"],
                                (double)strategyValue["reload_buy_seconds"],
                                (double)strategyValue["new_order_delay_seconds"]
                            )),
                            $"gdax-{instrument}-strategy-{strategyKey}");

                        blshStrategy.Tell(new BuyLowSellHighActor.Start());
                        _logger.Log($"GDAX:{instrument}: strategy '{strategyName}' started.");
                    }

                    _logger.Log($"GDAX:{instrument}: market started.");
                }

                _logger.Log("GDAX: started.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log($"GDAX: Error. {ex.Message}");
                return false;
            }
        }
    }
}
