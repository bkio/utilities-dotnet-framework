﻿/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;

namespace CloudServiceUtilities
{
    public struct BSMSServiceMessageStruct
    {
        public readonly string ReceiverPhoneNumber;
        public readonly string MessageContent;

        public BSMSServiceMessageStruct(
            string _ReceiverPhoneNumber,
            string _MessageContent)
        {
            ReceiverPhoneNumber = _ReceiverPhoneNumber;
            MessageContent = _MessageContent;
        }
    }

    public interface ISMSServiceInterface
    {
        /// <summary>
        /// 
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>Returns: Initialization succeed or failed</returns>
        /// 
        /// </summary>
        bool HasInitializationSucceed();

        /// <summary>
        /// 
        /// <para>SendSMSs</para>
        /// 
        /// <para>Sends SMSs to given phone numbers with given message contents</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Messages"/>                  List of messages to be sent</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  All operations success</returns>
        /// 
        /// </summary>
        bool SendSMSs(
            List<BSMSServiceMessageStruct> _Messages,
            Action<string> _ErrorMessageAction = null);
    }
}
