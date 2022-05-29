/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CloudServiceUtilities.MailServices
{
    public class MailServiceSendGrid : IMailServiceInterface
    {
        private readonly bool bInitializationSucceed;

        private readonly string ApiKey;

        private readonly SendGridClient SGClient;
        private readonly EmailAddress SenderInfo = null;

        /// <summary>
        /// 
        /// <para>MailServiceSendGrid: Parametered Constructor</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ApiKey"/>                    SendGrid Api Key</para>
        /// <para><paramref name="_SenderEmail"/>               Sender e-mail address</para>
        /// <para><paramref name="_SenderName"/>                Sender name
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public MailServiceSendGrid(string _ApiKey, string _SenderEmail, string _SenderName, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                ApiKey = _ApiKey;
                SenderInfo = new EmailAddress(_SenderEmail, _SenderName);
                SGClient = new SendGridClient(ApiKey);
                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"MailServiceSendGrid->Constructor: {e.Message}, Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="IMailServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        /// <summary>
        ///
        /// <para>SendEmail:</para>
        /// 
        /// <para>Sends e-mail to given address with given body</para>
        /// 
        /// <para>Check <seealso cref="IMailServiceInterface.SendEmail"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool SendEmails(
            List<MailServiceMailStruct> _Messages,
            Action<string> _ErrorMessageAction = null)
        {
            if (_Messages.Count == 0) return false;

            try
            {
                foreach (var Message in _Messages)
                {
                    var SGMessage = MailHelper.CreateSingleEmail(SenderInfo, new EmailAddress(Message.Receiver.ReceiverEmail, Message.Receiver.ReceiverName), Message.Subject, Message.PlainText, Message.HtmlText);
                    using (var SendEmailTask = SGClient.SendEmailAsync(SGMessage))
                    {
                        SendEmailTask.Wait();
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"MailServiceSendGrid->SendEmails: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>BroadcastEmail:</para>
        /// 
        /// <para>Sends an e-mail to given addresses with given body</para>
        /// 
        /// <para>Check <seealso cref="IMailServiceInterface.BroadcastEmail"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool BroadcastEmail(
            List<MailServicerReceiverStruct> _Receivers,
            string _Subject,
            string _PlainText,
            string _HtmlText,
            Action<string> _ErrorMessageAction = null)
        {
            if (_Receivers.Count == 0) return false;

            try
            {
                var Receivers = new List<EmailAddress>();
                foreach (var Receiver in _Receivers)
                {
                    Receivers.Add(new EmailAddress(Receiver.ReceiverEmail, Receiver.ReceiverName));
                }

                var SGMessage = MailHelper.CreateSingleEmailToMultipleRecipients(SenderInfo, Receivers, _Subject, _PlainText, _HtmlText);
                using (var SendEmailTask = SGClient.SendEmailAsync(SGMessage))
                {
                    SendEmailTask.Wait();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"MailServiceSendGrid->BroadcastEmail: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }
    }
}