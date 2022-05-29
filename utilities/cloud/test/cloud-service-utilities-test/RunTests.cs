/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using CloudServiceUtilities.DatabaseServices;
using CloudServiceUtilities.FileServices;
using CloudServiceUtilities.LogServices;
using CloudServiceUtilities.MailServices;
using CloudServiceUtilities.MemoryServices;
using CloudServiceUtilities.PubSubServices;
using CloudServiceUtilitiesTest.Tests;
using CommonUtilities;

namespace CloudServiceUtilitiesTest
{
    /// <summary>
    /// 
    /// <para>Required environment variables:</para>
    /// 
    /// <para>APPINSIGHTS_INSTRUMENTATIONKEY</para>
    /// <para>GC_PROJECT_ID</para>
    /// <para>AWS_ACCESS_KEY</para>
    /// <para>AWS_SECRET_KEY</para>
    /// <para>AWS_REGION</para>
    /// <para>SENDGRID_API_KEY</para>
    /// <para>REDIS_ENDPOINT</para>
    /// <para>REDIS_PORT</para>
    /// <para>REDIS_PASSWORD</para>
    /// <para>FILESERVICE_BUCKET</para>
    /// <para>FILESERVICE_REMOTE_PATH</para>
    /// <para>FILESERVICE_TEST_FILE_LOCAL_PATH</para>
    /// 
    /// </summary>
    public static class RunTests
    {
        private class RelativeTestsResultComparator
        {
            private readonly List<string> Results = new List<string>();
            private readonly List<string> Results_Old = new List<string>();

            private readonly Action<string> PrintAction;

            public RelativeTestsResultComparator(Action<string> _PrintAction)
            {
                PrintAction = _PrintAction;
            }

            public void Next()
            {
                Results_Old.Clear();
                foreach (var _Result in Results)
                {
                    Results_Old.Add(_Result);
                }
                Results.Clear();
            }

            public void AddLine(string _Result)
            {
                PrintAction?.Invoke(_Result);
                Results.Add(_Result);
            }

            public bool Compare()
            {
                if (Results.Count != Results_Old.Count)
                {
                    PrintAction?.Invoke($"TestResultComparator->Compare has failed. Count mismatch. Results: {Results.Count} != Results_Old: {Results_Old.Count}");
                    return false;
                }

                for (int i = 0; i < Results.Count; i++)
                {
                    if (Results[i] != Results_Old[i])
                    {
                        PrintAction?.Invoke($"TestResultComparator->Compare has failed. Result mismatch. Results[{i}]: {Results[i]} != Results_Old[{i}]: {Results_Old[i]}");
                        return false;
                    }
                }
                return true;
            }
        }

        public static bool Run()
        {
            if (!Utility.GetEnvironmentVariables(out Dictionary<string, string> RequiredEnvVars,
                new string[][]
                {
                    new string[] { "GC_PROJECT_ID" }
                },
                Console.WriteLine)) return false;

            /*
             * Logging Services tests
             */
            var LoggingTests_GC = new LogServiceTest(
                new LogServiceGC(
                    RequiredEnvVars["GC_PROJECT_ID"],
                    Console.WriteLine),
                Console.WriteLine);
            if (!LoggingTests_GC.Start()) return false;

            var LoggingTests_AWS = new LogServiceTest(
                new LogServiceAWS(
                    RequiredEnvVars["AWS_ACCESS_KEY"],
                    RequiredEnvVars["AWS_SECRET_KEY"],
                    RequiredEnvVars["AWS_REGION"],
                    Console.WriteLine),
                Console.WriteLine);
            if (!LoggingTests_AWS.Start()) return false;

            var LoggingTests_Azure = new LogServiceTest(
                new LogServiceAzure(
                    RequiredEnvVars["APPINSIGHTS_INSTRUMENTATIONKEY"],
                    Console.WriteLine),
                Console.WriteLine);
            if (!LoggingTests_Azure.Start()) return false;

            /*
             * E-mail Services tests
             */
            var Comparator = new RelativeTestsResultComparator(Console.WriteLine);

            var MailTests_SendGrid = new EmailServicesTest(
                new MailServiceSendGrid(
                    RequiredEnvVars["SENDGRID_API_KEY"],
                    "test@test.com",
                    "Test",
                    Console.WriteLine),
                Comparator.AddLine);
            if (!MailTests_SendGrid.Start()) return false;

            /*
             * Database Services tests
             */
            Comparator = new RelativeTestsResultComparator(Console.WriteLine);

            var DBTests_GC = new DatabaseServicesTest(
                new DatabaseServiceGC(
                    RequiredEnvVars["GC_PROJECT_ID"],
                    Console.WriteLine),
                "Test", 
                "TestKey",
                Comparator.AddLine);
            if (!DBTests_GC.Start()) return false;

            Comparator.Next();

            var DBTests_AWS = new DatabaseServicesTest(
                new DatabaseServiceAWS(
                    RequiredEnvVars["AWS_ACCESS_KEY"],
                    RequiredEnvVars["AWS_SECRET_KEY"],
                    RequiredEnvVars["AWS_REGION"],
                    Console.WriteLine),
                "Test",
                "TestKey",
                Comparator.AddLine);
            if (!DBTests_AWS.Start()) return false;

            Comparator.Next();

            var DBTests_MongoDB = new DatabaseServicesTest(
                new DatabaseServiceMongoDB(
                    RequiredEnvVars["MONGO_DB_CONNECTION_STRING"],
                    RequiredEnvVars["MONGO_DB_DATABASE"],
                    Console.WriteLine),
                "Test",
                "TestKey",
                Comparator.AddLine);
            if (!DBTests_MongoDB.Start()) return false;

            if (!Comparator.Compare()) return false;

            /*
             * Memory and Pub/Sub Services tests
             */
            Comparator = new RelativeTestsResultComparator(Console.WriteLine);

            var MemTests_WithRedisPubSub = new MemoryPubSubServiceTest(
                new MemoryServiceRedis(
                    RequiredEnvVars["REDIS_ENDPOINT"],
                    int.Parse(RequiredEnvVars["REDIS_PORT"]),
                    RequiredEnvVars["REDIS_PASSWORD"],
                    bool.Parse(RequiredEnvVars["REDIS_SSL_ENABLED"]),
                    new PubSubServiceRedis(
                        RequiredEnvVars["REDIS_ENDPOINT"],
                        int.Parse(RequiredEnvVars["REDIS_PORT"]),
                        RequiredEnvVars["REDIS_PASSWORD"],
                        bool.Parse(RequiredEnvVars["REDIS_SSL_ENABLED"]),
                        true,
                        Console.WriteLine),
                    true,
                    Console.WriteLine),
                Comparator.AddLine);
            if (!MemTests_WithRedisPubSub.Start()) return false;

            Comparator.Next();

            var MemTests_WithGCPubSub = new MemoryPubSubServiceTest(
                new MemoryServiceRedis(
                    RequiredEnvVars["REDIS_ENDPOINT"],
                    int.Parse(RequiredEnvVars["REDIS_PORT"]),
                    RequiredEnvVars["REDIS_PASSWORD"],
                    bool.Parse(RequiredEnvVars["REDIS_SSL_ENABLED"]),
                    new PubSubServiceGC(
                        RequiredEnvVars["GC_PROJECT_ID"],
                        Console.WriteLine),
                    true,
                    Console.WriteLine),
                Comparator.AddLine);
            if (!MemTests_WithGCPubSub.Start()) return false;

            if (!Comparator.Compare()) return false;

            Comparator.Next();

            var MemTests_WithAWSPubSub = new MemoryPubSubServiceTest(
                new MemoryServiceRedis(
                    RequiredEnvVars["REDIS_ENDPOINT"],
                    int.Parse(RequiredEnvVars["REDIS_PORT"]),
                    RequiredEnvVars["REDIS_PASSWORD"],
                    bool.Parse(RequiredEnvVars["REDIS_SSL_ENABLED"]),
                    new PubSubServiceAWS(
                        RequiredEnvVars["AWS_ACCESS_KEY"],
                        RequiredEnvVars["AWS_SECRET_KEY"],
                        RequiredEnvVars["AWS_REGION"],
                        Console.WriteLine),
                    true,
                    Console.WriteLine),
                Comparator.AddLine);
            if (!MemTests_WithAWSPubSub.Start()) return false;

            if (!Comparator.Compare()) return false;

            Comparator.Next();

            var MemTests_WithAzurePubSub = new MemoryPubSubServiceTest(
                new MemoryServiceRedis(
                    RequiredEnvVars["REDIS_ENDPOINT"],
                    int.Parse(RequiredEnvVars["REDIS_PORT"]),
                    RequiredEnvVars["REDIS_PASSWORD"],
                    bool.Parse(RequiredEnvVars["REDIS_SSL_ENABLED"]),
                    new PubSubServiceAzure(
                        RequiredEnvVars["AZURE_CLIENT_ID"],
                        RequiredEnvVars["AZURE_CLIENT_SECRET"],
                        RequiredEnvVars["AZURE_TENANT_ID"],
                        RequiredEnvVars["AZURE_NAMESPACE_ID"],
                        RequiredEnvVars["AZURE_NAMESPACE_CONNSTR"],
                        RequiredEnvVars["AZURE_EVENTGRID_DOMAIN_ENDPOINT"],
                        RequiredEnvVars["AZURE_EVENTGRID_DOMAIN_ACCESS_KEY"],
                        Console.WriteLine),
                    true,
                    Console.WriteLine),
                Comparator.AddLine);
            if (!MemTests_WithAWSPubSub.Start()) return false;

            if (!Comparator.Compare()) return false;

            /*
             * File Services tests
             */
            Comparator = new RelativeTestsResultComparator(Console.WriteLine);

            var FSTests_GC = new FileServiceTest(
                new FileServiceGC(
                    RequiredEnvVars["GC_PROJECT_ID"],
                    Console.WriteLine),
                RequiredEnvVars["FILESERVICE_BUCKET"],
                RequiredEnvVars["FILESERVICE_REMOTE_PATH"],
                RequiredEnvVars["FILESERVICE_TEST_FILE_LOCAL_PATH"],
                Comparator.AddLine);
            if (!FSTests_GC.Start()) return false;

            Comparator.Next();

            var FSTests_AWS = new FileServiceTest(
                new FileServiceAWS(
                    RequiredEnvVars["AWS_ACCESS_KEY"],
                    RequiredEnvVars["AWS_SECRET_KEY"],
                    RequiredEnvVars["AWS_REGION"],
                    Console.WriteLine),
                RequiredEnvVars["FILESERVICE_BUCKET"],
                RequiredEnvVars["FILESERVICE_REMOTE_PATH"],
                RequiredEnvVars["FILESERVICE_TEST_FILE_LOCAL_PATH"],
                Comparator.AddLine);
            if (!FSTests_AWS.Start()) return false;

            Comparator.Next();

            var FSTests_AZ = new FileServiceTest(
                new FileServiceAZ(
                    RequiredEnvVars["AZ_STORAGE_SERVICE"],
                    RequiredEnvVars["AZ_STORAGE_ACCOUNT"],
                    RequiredEnvVars["AZ_STORAGE_ACCOUNT_KEY"],
                    RequiredEnvVars["AZ_STORAGE_RESOURCE_GROUP"],
                    RequiredEnvVars["AZ_STORAGE_MANAGEMENT_APP_ID"],
                    RequiredEnvVars["AZ_STORAGE_MANAGEMENT_SECRET"],
                    RequiredEnvVars["AZ_SUBSCRIPTION_ID"],
                    RequiredEnvVars["AZ_TENANT_ID"],
                    RequiredEnvVars["AZ_STORAGE_LOCATION"],
                    Console.WriteLine),
                RequiredEnvVars["FILESERVICE_BUCKET"],
                RequiredEnvVars["FILESERVICE_REMOTE_PATH"],
                RequiredEnvVars["FILESERVICE_TEST_FILE_LOCAL_PATH"],
                Comparator.AddLine);
            if (!FSTests_AWS.Start()) return false;

            if (!Comparator.Compare()) return false;

            return true;
        }
    }
}