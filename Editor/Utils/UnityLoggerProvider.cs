using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Unordinal.Editor.Utils
{
    public class UnityLoggerProvider: ILoggerProvider
    {

        public ILogger CreateLogger(string categoryName)
        {
            return new UnityLogger(categoryName);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private class UnityLogger : ILogger
        {
            private static readonly Dictionary<LogLevel, LogType> LogLevelMapping = new Dictionary<LogLevel, LogType>() {
                {LogLevel.Information, LogType.Log},
                {LogLevel.Warning, LogType.Warning},
                {LogLevel.Debug, LogType.Log},
                {LogLevel.Trace, LogType.Log},
                {LogLevel.Error, LogType.Error},
                {LogLevel.Critical, LogType.Exception},
            };

            private readonly string categoryName;

            public UnityLogger(string categoryName) {
                this.categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }
                Debug.unityLogger.Log(LogLevelMapping[logLevel], $"{categoryName}@{eventId.Id}", formatter(state, exception));
            }
        }
    }
}
