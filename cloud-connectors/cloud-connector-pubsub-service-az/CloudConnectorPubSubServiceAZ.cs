using CloudServiceUtilities;
using CloudServiceUtilities.PubSubServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>AZ_CLIENT_ID, AZ_CLIENT_SECRET, AZ_TENANT_ID, AZ_SERVICEBUS_NAMESPACE_ID, and AZ_SERVICEBUS_NAMESPACE_CONNECTION_STRING must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_PubSubService_AZ
    {
        public static bool WithPubSubService(this CloudConnector _Connector, bool _bFailoverMechanismEnabled = true)
        {
            /*
            * Pub/Sub service initialization
            */
            _Connector.PubSubService = new PubSubServiceAzure(
                _Connector.RequiredEnvironmentVariables["AZ_CLIENT_ID"],
                _Connector.RequiredEnvironmentVariables["AZ_CLIENT_SECRET"],
                _Connector.RequiredEnvironmentVariables["AZ_TENANT_ID"],
                _Connector.RequiredEnvironmentVariables["AZ_SERVICEBUS_NAMESPACE_ID"],
                _Connector.RequiredEnvironmentVariables["AZ_SERVICEBUS_NAMESPACE_CONNECTION_STRING"],
                _Connector.RequiredEnvironmentVariables["AZ_EVENTGRID_DOMAIN_ENDPOINT"],
                _Connector.RequiredEnvironmentVariables["AZ_EVENTGRID_DOMAIN_ACCESS_KEY"],

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