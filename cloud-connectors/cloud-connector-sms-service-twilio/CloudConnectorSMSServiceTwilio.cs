/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.SMSServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN, TWILIO_FROM_PHONE_NO must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_SMSService_Twilio
    {
        public static bool WithSMSService(this CloudConnector _Connector)
        {
            /*
            * SMS service initialization
            */
            if (!_Connector.RequiredEnvironmentVariables.ContainsKey("TWILIO_ACCOUNT_SID") ||
                !_Connector.RequiredEnvironmentVariables.ContainsKey("TWILIO_AUTH_TOKEN") ||
                !_Connector.RequiredEnvironmentVariables.ContainsKey("TWILIO_FROM_PHONE_NO"))
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN, TWILIO_FROM_PHONE_NO parameters must be provided and valid."), _Connector.ProgramID, "Initialization");
                return false;
            }

            _Connector.SMSService = new SMSServiceTwilio(
                _Connector.RequiredEnvironmentVariables["TWILIO_ACCOUNT_SID"],
                _Connector.RequiredEnvironmentVariables["TWILIO_AUTH_TOKEN"],
                _Connector.RequiredEnvironmentVariables["TWILIO_FROM_PHONE_NO"],

                (string Message) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                });

            if (_Connector.SMSService == null || !_Connector.SMSService.HasInitializationSucceed())
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "SMS service initialization has failed."), _Connector.ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}
