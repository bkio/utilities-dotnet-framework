/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using CloudServiceUtilities;

namespace CloudServiceUtilitiesTest.Tests
{
    public class EmailServicesTest
    {
        private readonly IMailServiceInterface SelectedMailService;

        private readonly Action<string> PrintAction;

        public EmailServicesTest(IMailServiceInterface _SelectedMailService, Action<string> _PrintAction)
        {
            SelectedMailService = _SelectedMailService;

            PrintAction = _PrintAction;
        }

        public bool Start()
        {
            PrintAction?.Invoke("EmailServicesTest->Info-> Test is starting.");

            if (SelectedMailService == null)
            {
                PrintAction?.Invoke("EmailServicesTest->Error-> Given SelectedMailService is null.");
                return false;
            }

            if (!SelectedMailService.HasInitializationSucceed())
            {
                PrintAction?.Invoke("EmailServicesTest->Error-> Initialization failed.");
                return false;
            }
            PrintAction?.Invoke("EmailServicesTest->Log-> Initialization succeed.");

            if (!SelectedMailService.SendEmails(new List<MailServiceMailStruct>
            {
                new MailServiceMailStruct(new MailServicerReceiverStruct("test@test.com", "Test"), "Test e-mail - 1", "Test E-mail - 1", "<strong>Test E-mail with strong html wrapper - 1</strong>"),
                new MailServiceMailStruct(new MailServicerReceiverStruct("test@gmail.com", "Test - 2"), "Test e-mail", "Test E-mail - 2", "<strong>Test E-mail with strong html wrapper - 2</strong>")
            },
            Console.WriteLine)) return false;
            
            if (!SelectedMailService.BroadcastEmail(new List<MailServicerReceiverStruct>
            {
                new MailServicerReceiverStruct("test@test.com", "Test"),
                new MailServicerReceiverStruct("test@gmail.com", "Test - 2")
            },
            "Test broadcast e-mail",
            "Broadcast test e-mail",
            "<strong>Broadcast test e-mail with strong html wrapper</strong>",
            Console.WriteLine)) return false;

            return true;
        }
    }
}