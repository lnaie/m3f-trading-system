/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Akka.Actor;
using M3F.TradingSystem.Gdax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Event;
using M3F.TradingSystem.Actors;
using Microsoft.Extensions.Configuration;

namespace M3F.TradingSystem.Mpm
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage:");
                Console.Error.WriteLine("mpm config.json");
                Console.Error.WriteLine();
                Environment.Exit(1);
            }

            PrepareLoggers();

            var app = new Program();
            app.Run(args[0]);
        }

        Newtonsoft.Json.Linq.JObject
            ReadJson(string configFilePath)
        {
            var json = System.IO.File.ReadAllText(configFilePath);
            var config = Newtonsoft.Json.Linq.JObject.Parse(json);

            return config;
        }

        void Run(string configFilePath)
        {
            var config = ReadJson(configFilePath);

            var commonSettingsPath = config["gdax_config_path"].ToString();
            commonSettingsPath = commonSettingsPath.Replace("~", "%HOME%");
            commonSettingsPath = System.Environment.ExpandEnvironmentVariables(commonSettingsPath);

            var gdaxConf = ReadJson(commonSettingsPath);

            var client = new RestClient(new Uri(gdaxConf["gdax_api_uri"].ToString()),
                gdaxConf["gdax_api_key"].ToString(),
                gdaxConf["gdax_api_secret"].ToString(),
                gdaxConf["gdax_api_passphrase"].ToString());

            var instrument = new Instrument(config["instrument"].ToString());

            var sock = new Gdax.WebSocketClient(new Uri(gdaxConf["gdax_ws_uri"].ToString()));
            sock.Start();
            sock.Subscribe(instrument).Wait();

            var orderMgr = new Gdax.RestOrderManager(client, sock);

            var actorSystemConfig = Akka.Configuration.ConfigurationFactory.ParseString(
                @"akka {
                    loggers = [""M3F.TradingSystem.Actors.NLogLogger, M3F.TradingSystem.Actors""]
                    suppress-json-serializer-warning = on
                    loglevel = info
                }");

            var system = ActorSystem.Create("TS", actorSystemConfig);
            var ticker = system.ActorOf(Props.Create(
                    () => new Ticker(new LiveTickerFactory(client, sock))),
                "ticker");

            var reporter = system.ActorOf(Props.Create(
                () => new InsideMarketReporterActor(
                    ticker,
                    instrument,
                    LogLevel.DebugLevel)));


            var blsh = system.ActorOf(Props.Create(() => new BuyLowSellHigh(ticker,
                    orderMgr,
                    instrument,
                    (decimal) config["order_size"],
                    (decimal) config["buy_offset"],
                    (decimal) config["sell_offset"],
                    (double) config["reload_buy_seconds"],
                    (double) config["new_order_delay_seconds"]
                )),
                "blsh-" + instrument.Symbol);


            blsh.Tell(new BuyLowSellHigh.Start());

            // blocks the main thread from exiting until the actor system is shut down
            system.WhenTerminated.Wait();
            Console.WriteLine("============> mpm done");
        }

        static void PrepareLoggers()
        {
            // Step 1. Create configuration object
            var config = new NLog.Config.LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration
            var file = new NLog.Targets.FileTarget("FileLogger");
            config.AddTarget(file);

            // Step 3. Set target properties
            file.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day;
            file.ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence;
            file.ArchiveOldFileOnStartup = true;
            file.Layout =
                @"${date:universalTime=true:format=yyyy-MM-ddTHH\:mm\:ss.fffZ}|${level:uppercase=true}|${logger} ${event-properties:item=logSource}|${message}";
            file.LineEnding = NLog.Targets.LineEndingMode.LF;
            file.FileName = "mpm.log";

            var console = new NLog.Targets.ConsoleTarget("console");
            config.AddTarget(console);
            console.Layout =
                @"[${date:universalTime=false:format=yyyy-MM-ddTHH\:mm\:ss.fffzzz}][${level:uppercase=true}][${logger}]: ${message}";

            // Step 4. Define rules
            config.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, file));
            config.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, console));

            NLog.LogManager.Configuration = config;
        }
    }
}