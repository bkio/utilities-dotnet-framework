﻿/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using CommonUtilities;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace CloudServiceUtilities.LogServices
{
    public class LogServiceAzure : ILogServiceInterface
    {
        private readonly string InstrumentationKey;

        private ILogger<LogServiceAzure> AzureLogger;

        private readonly ITelemetryChannel AzureTelemetryChannel;

        /// <summary>
        /// <para>Holds initialization success</para>
        /// </summary>
        private readonly bool bInitializationSucceed;

        public LogServiceAzure(
            string _InstrumentationKey,
            Action<string> _ErrorMessageAction = null)
        {
            InstrumentationKey = _InstrumentationKey;

            try
            {
                // Create the DI container.
                IServiceCollection services = new ServiceCollection();

                // Channel is explicitly configured to do flush on it later.
                AzureTelemetryChannel = new InMemoryChannel();
                services.Configure<TelemetryConfiguration>(
                    (telemetryConfiguration) =>
                    {
                        telemetryConfiguration.TelemetryChannel = AzureTelemetryChannel;
                    }
                );

                // Add the logging pipelines to use. We are using Application Insights only here.
                services.AddLogging(loggingBuilder =>
                {
                    // Optional: Apply filters to configure LogLevel Debug or above is sent to
                    // Application Insights for all categories. (It won't send LogLevel Trace)
                    loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Debug);

                    loggingBuilder.AddApplicationInsights(InstrumentationKey);
                });

                // Build ServiceProvider.
                IServiceProvider serviceProvider = services.BuildServiceProvider();

                AzureLogger = serviceProvider.GetRequiredService<ILogger<LogServiceAzure>>();

                bInitializationSucceed = true;
            }
            catch (System.Exception e)
            {
                _ErrorMessageAction?.Invoke($"LogServiceAzure->Constructor: {e.Message}, Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }

        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="ILogServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_Messages"></param>
        /// <param name="_LogGroupName"></param>
        /// <param name="_LogStreamName"></param>
        /// <param name="_bAsync"></param>
        /// <param name="_ErrorMessageAction"></param>
        /// <returns></returns>
        public bool WriteLogs(List<LogParametersStruct> _Messages, string _LogGroupName, string _LogStreamName, bool _bAsync = true, Action<string> _ErrorMessageAction = null)
        {
            if (_Messages == null || _Messages.Count == 0) return false;

            if (_bAsync)
            {
                TaskWrapper.Run(() =>
                {
                    WriteLogs(_Messages, _LogGroupName, _LogStreamName, false, _ErrorMessageAction);
                });
                return true;
            }
            else
            {
                _LogGroupName = Utility.EncodeStringForTagging(_LogGroupName);
                _LogStreamName = Utility.EncodeStringForTagging(_LogStreamName);

                if (!Utility.CalculateStringMD5(DateTime.Now.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds.ToString(), out string Timestamp, _ErrorMessageAction))
                {
                    _ErrorMessageAction?.Invoke("LogServiceAzure->WriteLogs: Timestamp generation has failed.");
                    return false;
                }

                string StreamIDBase = $"{_LogGroupName}-{_LogStreamName}-{Timestamp}";

                try
                {
                    using (AzureLogger.BeginScope(StreamIDBase))
                    {

                        foreach (var Message in _Messages)
                        {
                            var level = LogLevel.Information;
                            var message = $"Info-> { Message.Message}";

                            switch (Message.LogType)
                            {
                                case ELogServiceLogType.Debug:
                                    level = LogLevel.Debug;
                                    message = $"Debug-> { Message.Message}";
                                    break;
                                case ELogServiceLogType.Warning:
                                    level = LogLevel.Warning;
                                    message = $"Warning-> { Message.Message}";
                                    break;
                                case ELogServiceLogType.Error:
                                    level = LogLevel.Error;
                                    message = $"Error-> { Message.Message}";
                                    break;
                                case ELogServiceLogType.Critical:
                                    level = LogLevel.Critical;
                                    message = $"Critical-> { Message.Message}";
                                    break;
                                default:
                                    level = LogLevel.Information;
                                    message = $"Info-> { Message.Message}";
                                    break;
                            }

                            AzureLogger.Log(level, message);
                        }
                    }

                    // Explicitly call Flush() followed by sleep is required in Console Apps.
                    // This is to ensure that even if application terminates, telemetry is sent to the back-end.
                    AzureTelemetryChannel?.Flush();
                    Thread.Sleep(1000);

                    return true;
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"LogServiceAzure->WriteLogs: {e.Message}, Trace: {e.StackTrace}");
                }
            }

            return false;
        }
    }
}
