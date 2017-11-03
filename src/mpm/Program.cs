/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Akka.Actor;
using Newtonsoft.Json.Linq;
using System;

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
                Environment.Exit(1);
            }

            PrepareLoggers();

            var app = new Program();
            app.Run(args[0]);
        }

        void Run(string configFilePath)
        {
            var helpers = new Helpers();
            var logger = new ConsoleLogger();
            logger.Log("Starting MPM...");

            // Read configuration
            var mpmConfig = helpers.ReadJson(configFilePath);

            // Create System
            var systemConfig = Akka.Configuration.ConfigurationFactory.ParseString(
                @"akka {
                    loggers = [""M3F.TradingSystem.Actors.NLogLogger, M3F.TradingSystem.Actors""]
                    suppress-json-serializer-warning = on
                    loglevel = info
                }");
            var system = ActorSystem.Create("mpm", systemConfig);

            // Bootstrap sub-systems
            if (new GdaxBootstrapper(logger, helpers).Start(system, mpmConfig))
            {
                // Blocks the main thread from exiting until the actor system is shut down
                system.WhenTerminated.Wait();
                logger.Log("MPM done. Press any key to exit...");
            }
            else
            {
                logger.Log("Nothing to do, MPM stopped. Press any key to exit...");
            }

            Console.ReadLine();
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
            file.Layout = @"${date:universalTime=true:format=yyyy-MM-ddTHH\:mm\:ss.fffZ}|${level:uppercase=true}|${event-properties:item=module}|${logger}|${event-properties:item=logSource}|${message}";
            file.LineEnding = NLog.Targets.LineEndingMode.LF;
            file.FileName = "mpm.log";

            var console = new NLog.Targets.ConsoleTarget("console");
            config.AddTarget(console);
            console.Layout = @"[${date:universalTime=false:format=yyyy-MM-ddTHH\:mm\:ss.fffzzz}][${level:uppercase=true}][${event-properties:item=module}][${logger}]: ${message}";

            // Step 4. Define rules
            config.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, file));
            config.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, console));

            NLog.LogManager.Configuration = config;
        }
    }
}