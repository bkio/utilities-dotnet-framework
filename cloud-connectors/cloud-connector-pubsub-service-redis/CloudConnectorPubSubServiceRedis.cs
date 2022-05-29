/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.PubSubServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>(REDIS_PUBSUB_ENDPOINT, REDIS_PUBSUB_PORT, REDIS_PUBSUB_PASSWORD) or (REDIS_ENDPOINT, REDIS_PORT, REDIS_PASSWORD) parameters must be provided and valid.</para>
    /// <para>REDIS_PUBSUB_SSL_ENABLED or REDIS_SSL_ENABLED can be sent to set SSL enabled, otherwise it will be false.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_PubSubService_Redis
    {
        public static bool WithPubSubService(this CloudConnector _Connector, bool _bFailoverMechanismEnabled = true)
        {
            /*
            * Pub/Sub service initialization
            */
            if (!_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_ENDPOINT") ||
                !_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_PORT") ||
                !int.TryParse(_Connector.RequiredEnvironmentVariables["REDIS_PUBSUB_PORT"], out int RedisPort) ||
                !_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_PASSWORD"))
            {
                if (!_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_ENDPOINT") ||
                    !_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_PORT") ||
                    !int.TryParse(_Connector.RequiredEnvironmentVariables["REDIS_PORT"], out RedisPort) ||
                    !_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_PASSWORD"))
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "(REDIS_PUBSUB_ENDPOINT, REDIS_PUBSUB_PORT, REDIS_PUBSUB_PASSWORD) or (REDIS_ENDPOINT, REDIS_PORT, REDIS_PASSWORD) parameters must be provided and valid."), _Connector.ProgramID, "Initialization");
                    return false;
                }
            }

            bool RedisSslEnabled = false;
            if (_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_SSL_ENABLED") 
                && !bool.TryParse(_Connector.RequiredEnvironmentVariables["REDIS_PUBSUB_SSL_ENABLED"], out RedisSslEnabled))
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Warning, "REDIS_PUBSUB_SSL_ENABLED parameter has been provided, but it has not a valid value. It will be continued without SSL."), _Connector.ProgramID, "Initialization");
            }
            if (!_Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_SSL_ENABLED") 
                && _Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_SSL_ENABLED") 
                && !bool.TryParse(_Connector.RequiredEnvironmentVariables["REDIS_SSL_ENABLED"], out RedisSslEnabled))
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Warning, "REDIS_SSL_ENABLED parameter has been provided, but it has not a valid value. It will be continued without SSL."), _Connector.ProgramID, "Initialization");
            }

            string RedisEndpoint = _Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_ENDPOINT") ? _Connector.RequiredEnvironmentVariables["REDIS_PUBSUB_ENDPOINT"] : _Connector.RequiredEnvironmentVariables["REDIS_ENDPOINT"];
            string RedisPassword = _Connector.RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_PASSWORD") ? _Connector.RequiredEnvironmentVariables["REDIS_PUBSUB_PASSWORD"] : _Connector.RequiredEnvironmentVariables["REDIS_PASSWORD"];

            _Connector.PubSubService = new PubSubServiceRedis(
                RedisEndpoint,
                RedisPort,
                RedisPassword,
                RedisSslEnabled,
                _bFailoverMechanismEnabled,

                (string Message) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                });

            if (_Connector.PubSubService == null || !_Connector.PubSubService.HasInitializationSucceed())
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "Pub/Sub service initialization has failed."), _Connector.ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}