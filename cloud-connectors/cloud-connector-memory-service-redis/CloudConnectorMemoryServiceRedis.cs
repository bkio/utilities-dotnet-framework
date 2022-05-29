/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.MemoryServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>REDIS_ENDPOINT, REDIS_PORT, REDIS_PASSWORD must be provided and valid.</para>
    /// <para>REDIS_SSL_ENABLED can be sent to set SSL enabled, otherwise it will be false.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_MemoryService_Redis
    {
        public static bool WithMemoryService(this CloudConnector _Connector, bool _bFailoverMechanismEnabled = true, IPubSubServiceInterface _WithPubSubService = null)
        {
            /*
            * Memory service initialization
            */
            if (!_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_ENDPOINT") ||
                !_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_PORT") ||
                !int.TryParse(_Connector.RequiredEnvironmentVariables["REDIS_PORT"], out int RedisPort) ||
                !_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_PASSWORD"))
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "REDIS_ENDPOINT, REDIS_PORT, REDIS_PASSWORD parameters must be provided and valid."), _Connector.ProgramID, "Initialization");
                return false;
            }

            bool RedisSslEnabled = false;
            if (_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_SSL_ENABLED") && !bool.TryParse(_Connector.RequiredEnvironmentVariables["REDIS_SSL_ENABLED"], out RedisSslEnabled))
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Warning, "REDIS_SSL_ENABLED parameter has been provided, but it has not a valid value. It will be continued without SSL."), _Connector.ProgramID, "Initialization");
            }

            _Connector.MemoryService = new MemoryServiceRedis(
                _Connector.RequiredEnvironmentVariables["REDIS_ENDPOINT"],
                RedisPort,
                _Connector.RequiredEnvironmentVariables["REDIS_PASSWORD"],
                RedisSslEnabled,
                _WithPubSubService,
                _bFailoverMechanismEnabled,

                (string Message) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                });

            if (_Connector.MemoryService == null || !_Connector.MemoryService.HasInitializationSucceed())
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "Memory service initialization has failed."), _Connector.ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}