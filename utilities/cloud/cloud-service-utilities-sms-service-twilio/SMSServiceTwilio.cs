/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace CloudServiceUtilities.SMSServices
{
    public class SMSServiceTwilio : ISMSServiceInterface
    {
        private readonly bool bInitializationSucceed;

        /*private readonly string AccountSID;
        private readonly string AuthToken;

        private readonly string FromPhoneNumber;*/
        private readonly Twilio.Types.PhoneNumber FromPhoneNumber_TwilioType;

        /// <summary>
        /// 
        /// <para>SMSServiceTwilio: Parametered Constructor</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_AccountSID"/>                Twilio account SID</para
        /// <para><paramref name="_AuthToken"/>                 Twilio auth token</para>
        /// <para><paramref name="_FromPhoneNumber"/>           Twilio phone number (sender)</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public SMSServiceTwilio(string _AccountSID, string _AuthToken, string _FromPhoneNumber, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                /*AccountSID = _AccountSID;
                AuthToken = _AuthToken;

                FromPhoneNumber = _FromPhoneNumber;*/
                FromPhoneNumber_TwilioType = new Twilio.Types.PhoneNumber(_FromPhoneNumber);

                TwilioClient.Init(_AccountSID, _AuthToken);

                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"SMSServiceTwilio->Constructor: {e.Message}, Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="SMSServiceTwilio.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        /// <summary>
        /// 
        /// <para>SendSMSs</para>
        /// 
        /// <para>Sends SMSs to given phone numbers with given message contents</para>
        /// 
        /// <para>Check <seealso cref="SMSServiceTwilio.SendSMSs"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool SendSMSs(List<BSMSServiceMessageStruct> _Messages, Action<string> _ErrorMessageAction = null)
        {
            bool bAllSucceed = true;

            try
            {
                foreach (var MessageStruct in _Messages)
                {
                    var TwilioSMS = MessageResource.Create(
                            body: MessageStruct.MessageContent,
                            from: FromPhoneNumber_TwilioType,
                            to: new Twilio.Types.PhoneNumber(MessageStruct.ReceiverPhoneNumber));
                    if (TwilioSMS.ErrorCode.HasValue)
                    {
                        bAllSucceed = false;
                        _ErrorMessageAction?.Invoke($"SMSServiceTwilio->SendSMSs: Error occured: Code: {TwilioSMS.ErrorCode.Value}, Message: {TwilioSMS.ErrorMessage}");
                    }
                }
            }
            catch (Exception e)
            {
                bAllSucceed = false;
                _ErrorMessageAction?.Invoke($"SMSServiceTwilio->SendSMSs: {e.Message}, Trace: {e.StackTrace}");
            }
            
            return bAllSucceed;
        }
    }
}
