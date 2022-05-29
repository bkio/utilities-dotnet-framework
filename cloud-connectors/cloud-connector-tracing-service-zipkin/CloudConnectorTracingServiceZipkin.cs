/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.TracingServices;
using System;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>ZIPKIN_SERVER_IP, ZIPKIN_SERVER_PORT must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_TracingService_Zipkin
    {
        public static bool WithTracingService(this CloudConnector _Connector)
        {
            /*
            * Tracing service initialization
            */
            if (!_Connector.RequiredEnvironmentVariables.ContainsKey("ZIPKIN_SERVER_IP") ||
                !_Connector.RequiredEnvironmentVariables.ContainsKey("ZIPKIN_SERVER_PORT"))
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "ZIPKIN_SERVER_IP, ZIPKIN_SERVER_PORT parameters must be provided and valid."), _Connector.ProgramID, "Initialization");
                return false;
            }

            var LogServiceLogger = new LogServiceLoggerZipkin(
                _Connector.LogService,
                Console.WriteLine,
                _Connector.ProgramID);

            if (!int.TryParse(_Connector.RequiredEnvironmentVariables["ZIPKIN_SERVER_PORT"], out int ZipkinServerPort))
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "Given zipkin server port is invalid."), _Connector.ProgramID, "Initialization");
                return false;
            }

            _Connector.TracingService = new TracingServiceZipkin(
                LogServiceLogger,
                _Connector.ProgramID,
                _Connector.RequiredEnvironmentVariables["ZIPKIN_SERVER_IP"],
                ZipkinServerPort,
                (string Message) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                });

            if (_Connector.TracingService == null || !_Connector.TracingService.HasInitializationSucceed())
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "Tracing service initialization has failed."), _Connector.ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}