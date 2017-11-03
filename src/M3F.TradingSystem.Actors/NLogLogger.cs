//-----------------------------------------------------------------------
// <copyright file="NLogLogger.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Dispatch;
using Akka.Event;
using NLog;
using NLogger = NLog.Logger;
using NLogLevel = NLog.LogLevel;

namespace M3F.TradingSystem.Actors
{
    /// <summary>
    /// This class is used to receive log events and sends them to
    /// the configured NLog logger. The following log events are
    /// recognized: <see cref="Debug"/>, <see cref="Info"/>,
    /// <see cref="Warning"/> and <see cref="Error"/>.
    /// </summary>
    public class NLogLogger : ReceiveActor, IRequiresMessageQueue<ILoggerMessageQueueSemantics>
    {
        const int LogSourceSkip = 17; // LogSource: [akka://mpm/user/gdax-ETH-USD-strategy-blsh#1114027313]
        readonly ILoggingAdapter _log = Context.GetLogger();

        static void Log (LogEvent logEvent, Action<NLogger, string, string> logStatement)
        {
            var logger = LogManager.GetLogger(logEvent.LogClass.Name);

            // Conventionally actor IDs are composed of strings separated by dash (-). 
            // E.g. gdax-ETH-USD-strategy-blsh#1114027313
            var subSystem = string.Empty;
            var actorId = logEvent.LogSource.Substring(LogSourceSkip);
            var actorIdSplits = actorId.Split(new char[1] { '-' });
            if (actorIdSplits.Length > 0)
            {
                subSystem = actorIdSplits[0].ToUpper();
            }

            logStatement(logger, logEvent.LogSource, subSystem);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NLogLogger"/> class.
        /// </summary>
        public NLogLogger ()
        {
            Receive<Error>(m => Log(m, 
                (logger, logSource, module) => LogEvent(logger, NLogLevel.Error, logSource, module, m.Cause, "{0}", m.Message)));
            Receive<Warning>(m => Log(m, 
                (logger, logSource, module) => LogEvent(logger, NLogLevel.Warn, logSource, module, "{0}", m.Message)));
            Receive<Info>(m => Log(m, 
                (logger, logSource, module) => LogEvent(logger, NLogLevel.Info, logSource, module, "{0}", m.Message)));
            Receive<Debug>(m => Log(m, 
                (logger, logSource, module) => LogEvent(logger, NLogLevel.Debug, logSource, module, "{0}", m.Message)));
            Receive<InitializeLogger>(m =>
            {
                _log.Info("NLogLogger started.");
                Sender.Tell(new LoggerInitialized());
            });
        }

        static void LogEvent (            
            NLogger logger,
            NLogLevel level,
            string logSource,
            string module,
            string message,
            params object[] parameters)
        {
            LogEvent(logger, level, logSource, module, null, message, parameters);
        }

        static void LogEvent (
            NLogger logger,
            NLogLevel level,
            string logSource,
            string module,
            Exception exception,
            string message,
            params object[] parameters)
        {
            var logEvent = new LogEventInfo(level, logger.Name, null, message, parameters, exception);
            logEvent.Properties["logSource"] = logSource;
            logEvent.Properties["module"] = module;
            logger.Log(logEvent);
        }
    }
}