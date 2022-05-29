/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;

namespace CloudServiceUtilities
{
    public struct MailServicerReceiverStruct
    {
        public readonly string ReceiverEmail;
        public readonly string ReceiverName;

        public MailServicerReceiverStruct(
            string _ReceiverEmail,
            string _ReceiverName)
        {
            ReceiverEmail = _ReceiverEmail;
            ReceiverName = _ReceiverName;
        }
    }

    public struct MailServiceMailStruct
    {
        public readonly MailServicerReceiverStruct Receiver;
        public readonly string Subject;
        public readonly string PlainText;
        public readonly string HtmlText;

        public MailServiceMailStruct(
            MailServicerReceiverStruct _Receiver,
            string _Subject,
            string _PlainText,
            string _HtmlText)
        {
            Receiver = _Receiver;
            Subject = _Subject;
            PlainText = _PlainText;
            HtmlText = _HtmlText;
        }
    }

    public interface IMailServiceInterface
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
        /// <para>SendEmail</para>
        /// 
        /// <para>Sends e-mails to given addresses with given bodies</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Messages"/>                  List of messages to be sent</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool SendEmails(
            List<MailServiceMailStruct> _Messages,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>BroadcastEmail</para>
        /// 
        /// <para>Sends an e-mail to given addresses with given body</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Receivers"/>                 List of receivers</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool BroadcastEmail(
            List<MailServicerReceiverStruct> _Receivers,
            string _Subject,
            string _PlainText,
            string _HtmlText,
            Action<string> _ErrorMessageAction = null);
    }
}
