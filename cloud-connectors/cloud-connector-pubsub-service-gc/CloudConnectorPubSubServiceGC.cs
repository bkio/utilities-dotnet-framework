/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.PubSubServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>GOOGLE_CLOUD_PROJECT_ID, GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS or GOOGLE_BASE64_CREDENTIALS) must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_PubSubService_GC
    {
        public static bool WithPubSubService(this CloudConnector _Connector, bool _bFailoverMechanismEnabled = true)
        {
            /*
            * Pub/Sub service initialization
            */
            _Connector.PubSubService = new PubSubServiceGC(
                _Connector.RequiredEnvironmentVariables["GOOGLE_CLOUD_PROJECT_ID"],

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