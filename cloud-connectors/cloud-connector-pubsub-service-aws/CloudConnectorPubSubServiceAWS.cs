/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.PubSubServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_PubSubService_AWS
    {
        public static bool WithPubSubService(this CloudConnector _Connector, bool _bFailoverMechanismEnabled = true)
        {
            /*
            * Pub/Sub service initialization
            */
            _Connector.PubSubService = new PubSubServiceAWS(
                _Connector.RequiredEnvironmentVariables["AWS_ACCESS_KEY"],
                _Connector.RequiredEnvironmentVariables["AWS_SECRET_KEY"], 
                _Connector.RequiredEnvironmentVariables["AWS_REGION"],

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