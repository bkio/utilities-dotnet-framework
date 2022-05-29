﻿/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using CommonUtilities;
using Newtonsoft.Json.Linq;
using Azure;
using Azure.Messaging;
using Microsoft.Azure.ServiceBus;

namespace CloudServiceUtilities.PubSubServices
{
    public class PubSubServiceAzure : IPubSubServiceInterface
    {
        /// <summary>
        /// Azure Manager for managing Azure resources
        /// </summary>
        private readonly Microsoft.Azure.Management.Fluent.IAzure AzureManager;

        /// <summary>
        /// Azure Namespace Manager for managing Azure Service Bus Namespaces
        /// </summary>
        private readonly Microsoft.Azure.Management.ServiceBus.Fluent.IServiceBusNamespace AzureNamespaceManager;

        private readonly EventGridPublisherClient EventGridDomainPublisher;

        /// <summary>
        /// Holds namespace connection string for ITopicClient and ISubscriptionClient connections.
        /// </summary>
        private readonly string ServiceBusNamespaceConnectionString;

        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        private BPubSubUniqueMessageDeliveryEnsurer UniqueMessageDeliveryEnsurer = null;

        private readonly object SubscriberThreadsDictionaryLock = new object();
        private readonly Dictionary<string, Tuple<Thread, Atomicable<bool>>> SubscriberThreadsDictionary = new Dictionary<string, Tuple<Thread, Atomicable<bool>>>();

        /// <summary>
        /// 
        /// <para>PubSubServiceAzure: Parameterized Constructor for Managed Service by Microsoft Azure</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ClientId"/>                                  Azure Client Id</para>
        /// <para><paramref name="_ClientSecret"/>                              Azure Client Secret</para>
        /// <para><paramref name="_TenantId"/>                                  Azure Tenant Id</para>
        /// <para><paramref name="_ServiceBusNamespaceId"/>                     Azure Service Bus Namespace Id</para>
        /// <para><paramref name="_ServiceBusNamespaceConnectionString"/>       Azure Service Bus Namespace Connection String</para>
        /// <para><paramref name="_EventGridDomainEndpoint"/>                   Event Grid Domain Endpoint</para>
        /// <para><paramref name="_EventGridDomainAccessKey"/>                  Event Grid Domain Access Key</para>
        /// <para><paramref name="_ErrorMessageAction"/>                        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public PubSubServiceAzure(
            string _ClientId,
            string _ClientSecret,
            string _TenantId,
            string _ServiceBusNamespaceId,
            string _ServiceBusNamespaceConnectionString,
            string _EventGridDomainEndpoint,
            string _EventGridDomainAccessKey,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                ServiceBusNamespaceConnectionString = _ServiceBusNamespaceConnectionString;

                var credentials = Microsoft.Azure.Management.ResourceManager.Fluent.SdkContext.AzureCredentialsFactory.FromServicePrincipal(_ClientId, _ClientSecret, _TenantId, Microsoft.Azure.Management.ResourceManager.Fluent.AzureEnvironment.AzureGlobalCloud);

                AzureManager = Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(Microsoft.Azure.Management.ResourceManager.Fluent.Core.HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                if (!string.IsNullOrEmpty(_EventGridDomainEndpoint) && !string.IsNullOrEmpty(_EventGridDomainAccessKey))
                {
                    EventGridDomainPublisher = new EventGridPublisherClient(new Uri(_EventGridDomainEndpoint), new AzureKeyCredential(_EventGridDomainAccessKey));
                }
                else
                {
                    EventGridDomainPublisher = null;
                }

                using (var GetNamespaceTask = AzureManager.ServiceBusNamespaces.GetByIdAsync(_ServiceBusNamespaceId))
                {
                    GetNamespaceTask.Wait();
                    AzureNamespaceManager = GetNamespaceTask.Result;
                }

                if (AzureNamespaceManager == null)
                {
                    _ErrorMessageAction?.Invoke("PubSubServiceAzure->GetNamespaceTask.Result is null. Please check your [AZ_SERVICEBUS_NAMESPACE_CONNECTION_STRING] and [AZ_SERVICEBUS_NAMESPACE_ID] information.");
                    bInitializationSucceed = false;
                }
                else
                {
                    bInitializationSucceed = true;
                }

                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"PubSubServiceAzure->Constructor: {e.Message}, Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        private bool CheckTopicExists(string _TopicName, out ITopicClient _TopicClient)
        {
            _TopicClient = null;

            if (AzureNamespaceManager == null)
                return false;

            try
            {
                using (var GetTopicTask = AzureNamespaceManager.Topics.GetByNameAsync(_TopicName))
                {
                    GetTopicTask.Wait();
                    if (GetTopicTask.Result != null && GetTopicTask.Result.Name != null && GetTopicTask.Result.Name.Length > 0)
                    {
                        _TopicClient = new TopicClient(ServiceBusNamespaceConnectionString, _TopicName);
                        return true;
                    }
                }
            }
            catch (Exception) { }

            return false;
        }

        private bool EnsureTopicExists(string _TopicName, out ITopicClient _TopicClient, Action<string> _ErrorMessageAction)
        {
            bool bExists = CheckTopicExists(_TopicName, out _TopicClient);
            if (!bExists)
            {
                if (AzureNamespaceManager == null)
                    return false;

                try
                {
                    using (var CreateTopicTask = AzureNamespaceManager.Topics.Define(_TopicName).CreateAsync())
                    {
                        CreateTopicTask.Wait();
                        if (CreateTopicTask.Result != null && CreateTopicTask.Result.Name != null && CreateTopicTask.Result.Name.Length > 0)
                        {
                            _TopicClient = new TopicClient(ServiceBusNamespaceConnectionString, _TopicName);
                            bExists = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    bExists = false;
                    _ErrorMessageAction?.Invoke($"PubSubServiceAzure->EnsureTopicExists: {e.Message}, Trace: {e.StackTrace}");
                    if (e.InnerException != null && e.InnerException != e)
                    {
                        _ErrorMessageAction?.Invoke($"PubSubServiceAzure->EnsureTopicExists->Inner: {e.InnerException.Message}, Trace: {e.InnerException.StackTrace}");
                    }
                }
            }
            return bExists;
        }

        private bool CheckSubscriptionExists(string _SubscriptionName, ITopicClient _TopicClient, out Microsoft.Azure.ServiceBus.ISubscriptionClient _SubscriptionClient)
        {
            _SubscriptionClient = null;

            if (AzureNamespaceManager == null)
                return false;

            try
            {
                using (var GetTopicTask = AzureNamespaceManager.Topics.GetByNameAsync(_SubscriptionName))
                {
                    GetTopicTask.Wait();
                    if (GetTopicTask.Result != null && GetTopicTask.Result.Name != null && GetTopicTask.Result.Name.Length > 0)
                    {
                        var TopicManager = GetTopicTask.Result;
                        using (var GetSubscriptionTask = TopicManager.Subscriptions.GetByNameAsync(_SubscriptionName))
                        {
                            GetSubscriptionTask.Wait();
                            if (GetSubscriptionTask.Result != null && GetSubscriptionTask.Result.Name != null && GetSubscriptionTask.Result.Name.Length > 0)
                            {
                                _SubscriptionClient = new Microsoft.Azure.ServiceBus.SubscriptionClient(ServiceBusNamespaceConnectionString, _TopicClient.Path, _SubscriptionName);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception) { }

            return false;
        }

        private bool EnsureSubscriptionExists(string _SubscriptionName, ITopicClient _TopicClient, out Microsoft.Azure.ServiceBus.ISubscriptionClient _SubscriptionClient, Action<string> _ErrorMessageAction)
        {
            bool bExists = CheckSubscriptionExists(_SubscriptionName, _TopicClient, out _SubscriptionClient);
            if (!bExists)
            {
                if (AzureNamespaceManager == null)
                    return false;

                try
                {
                    using (var GetTopicTask = AzureNamespaceManager.Topics.GetByNameAsync(_SubscriptionName))
                    {
                        GetTopicTask.Wait();
                        if (GetTopicTask.Result != null && GetTopicTask.Result.Name != null && GetTopicTask.Result.Name.Length > 0)
                        {
                            var TopicManager = GetTopicTask.Result;
                            using (var CreateSubscriptionTask = TopicManager.Subscriptions.Define(_SubscriptionName).CreateAsync())
                            {
                                CreateSubscriptionTask.Wait();
                                if (CreateSubscriptionTask.Result != null && CreateSubscriptionTask.Result.Name != null && CreateSubscriptionTask.Result.Name.Length > 0)
                                {
                                    _SubscriptionClient = new Microsoft.Azure.ServiceBus.SubscriptionClient(ServiceBusNamespaceConnectionString, _TopicClient.Path, _SubscriptionName);
                                    bExists = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    bExists = false;
                    _ErrorMessageAction?.Invoke($"PubSubServiceAzure->EnsureSubscriptionExists: {e.Message}, Trace: {e.StackTrace}");
                    if (e.InnerException != null && e.InnerException != e)
                    {
                        _ErrorMessageAction?.Invoke($"PubSubServiceAzure->EnsureSubscriptionExists->Inner: {e.InnerException.Message}, Trace: {e.InnerException.StackTrace}");
                    }
                }
            }
            return bExists;
        }

        public bool CustomPublish(string _CustomTopic, string _CustomMessage, Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0 
                && _CustomMessage != null && _CustomMessage.Length > 0)
            {
                _CustomTopic = GetAzureFriendlyTopicName(_CustomTopic);

                if (EnsureTopicExists(_CustomTopic, out ITopicClient _TopicClient, _ErrorMessageAction))
                {
                    string TimestampHash = null;
                    UniqueMessageDeliveryEnsurer?.Publish_PrependTimestampToMessage(ref _CustomMessage, out TimestampHash);

                    try
                    {
                        if (UniqueMessageDeliveryEnsurer != null)
                        {
                            if (UniqueMessageDeliveryEnsurer.Publish_EnsureUniqueDelivery(_CustomTopic, TimestampHash, _ErrorMessageAction))
                            {
                                var AzureMessage = new Message(Encoding.UTF8.GetBytes(_CustomMessage));
                                AzureMessage.Label = _CustomTopic;
                                using (var SendMessageTask = _TopicClient.SendAsync(AzureMessage))
                                {
                                    SendMessageTask.Wait();
                                }
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("PubSubServiceAzure->CustomPublish: UniqueMessageDeliveryEnsurer has failed.");
                                return false;
                            }
                        }
                        else
                        {
                            var AzureMessage = new Message(Encoding.UTF8.GetBytes(_CustomMessage));
                            AzureMessage.Label = _CustomTopic;
                            using (var SendMessageTask = _TopicClient.SendAsync(AzureMessage))
                            {
                                SendMessageTask.Wait();
                            }
                        }
                        if (EventGridDomainPublisher != null)
                        {
                            var CloudEventData = new CloudEvent(_CustomTopic, _CustomTopic, Encoding.UTF8.GetBytes(_CustomMessage));
                            CloudEventData.Subject = $"topics/{_CustomTopic}/subscriptions/{_CustomTopic}";

                            List <CloudEvent> _EventsList = new List<CloudEvent> { CloudEventData };
                            using (var SendCloudEventTask = EventGridDomainPublisher.SendEventsAsync(_EventsList))
                            {
                                SendCloudEventTask.Wait();
                            }
                        }
                        return true;
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke($"PubSubServiceAzure->CustomPublish: {e.Message}, Trace: {e.StackTrace}");
                        if (e.InnerException != null && e.InnerException != e)
                        {
                            _ErrorMessageAction?.Invoke($"PubSubServiceAzure->CustomPublish->Inner: {e.InnerException.Message}, Trace: {e.InnerException.StackTrace}");
                        }
                        return false;
                    }
                    finally
                    {
                        using (var CloseTask = _TopicClient.CloseAsync())
                        {
                            CloseTask.Wait();
                        }
                    }
                }
            }
            return false;
        }

        private void RemoveSubscriberThread(string _CustomTopic)
        {
            lock (SubscriberThreadsDictionaryLock)
            {
                if (SubscriberThreadsDictionary.ContainsKey(_CustomTopic))
                {
                    var SubscriberThread = SubscriberThreadsDictionary[_CustomTopic];
                    if (SubscriberThread != null)
                    {
                        SubscriberThread.Item2.Set(true);
                    }
                    SubscriberThreadsDictionary.Remove(_CustomTopic);
                }
            }
        }

        public bool CustomSubscribe(string _CustomTopic, Action<string, string> _OnMessage, Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0 
                && _OnMessage != null)
            {
                _CustomTopic = GetAzureFriendlyTopicName(_CustomTopic);

                if (EnsureTopicExists(_CustomTopic, out ITopicClient _TopicClient, _ErrorMessageAction))
                {
                    if(EnsureSubscriptionExists(_CustomTopic, _TopicClient, out Microsoft.Azure.ServiceBus.ISubscriptionClient _SubscriptionClient, _ErrorMessageAction))
                    {
                        var SubscriptionCancellationVar = new Atomicable<bool>(false, EProducerStatus.MultipleProducer);
                        var SubscriptionThread = new Thread(() =>
                        {
                            Thread.CurrentThread.IsBackground = true;
                            try
                            {
                                // Define exception receiver handler
                                Func<ExceptionReceivedEventArgs, Task> ExceptionReceiverHandler = (ExceptionReceivedEventArgs _EventArgs) =>
                                {
                                    _ErrorMessageAction?.Invoke($"PubSubServiceAzure->CustomSubscribe->ExceptionReceiverHandler: {_EventArgs.Exception.Message}, Action: {_EventArgs.ExceptionReceivedContext.Action}, EntityPath: {_EventArgs.ExceptionReceivedContext.EntityPath}, Endpoint: {_EventArgs.ExceptionReceivedContext.Endpoint}, ClientId: {_EventArgs.ExceptionReceivedContext.ClientId}, Trace: {_EventArgs.Exception.StackTrace}");

                                    RemoveSubscriberThread(_CustomTopic);
                                    if (!_SubscriptionClient.IsClosedOrClosing)
                                    {
                                        using (var CloseSubscriptionTask = _SubscriptionClient.CloseAsync())
                                        {
                                            CloseSubscriptionTask.Wait();
                                        }
                                    }

                                    return Task.CompletedTask;
                                };

                                // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
                                var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceiverHandler)
                                {
                                    // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                                    // Set it according to how many messages the application wants to process in parallel.
                                    MaxConcurrentCalls = 1,

                                    // Indicates whether the message pump should automatically complete the messages after returning from user callback.
                                    // False below indicates the complete operation is handled by the user callback as in ProcessMessagesAsync().
                                    AutoComplete = false
                                };

                                // Register the function that processes messages.
                                _SubscriptionClient.RegisterMessageHandler(async (Message MessageContainer, CancellationToken token) =>
                                {
                                    if (MessageContainer != null)
                                    {
                                        string Topic = GetTopicNameFromAzureFriendlyName(_CustomTopic);
                                        string Data = Encoding.UTF8.GetString(MessageContainer.Body);

                                        if (MessageContainer.Label != null
                                            && MessageContainer.Label.Equals(_CustomTopic))
                                        {
                                            _ErrorMessageAction?.Invoke("PubSubServiceAzure->CustomSubscribe: Received [Internal] pub/sub message.");
                                        }
                                        else
                                        {
                                            _ErrorMessageAction?.Invoke("PubSubServiceAzure->CustomSubscribe: Received [StorageAccount] pub/sub message.");
                                        }

                                        if (UniqueMessageDeliveryEnsurer != null)
                                        {
                                            UniqueMessageDeliveryEnsurer.Subscribe_ClearAndExtractTimestampFromMessage(ref Data, out string TimestampHash);

                                            if (UniqueMessageDeliveryEnsurer.Subscription_EnsureUniqueDelivery(Topic, TimestampHash, _ErrorMessageAction))
                                            {
                                                _OnMessage?.Invoke(Topic, Data);
                                                RemoveSubscriberThread(_CustomTopic);
                                                if (!_SubscriptionClient.IsClosedOrClosing)
                                                {
                                                    await _SubscriptionClient.CloseAsync();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _OnMessage?.Invoke(Topic, Data);
                                        }

                                        await _SubscriptionClient.CompleteAsync(MessageContainer.SystemProperties.LockToken);
                                    }
                                }, messageHandlerOptions);
                            }
                            catch (Exception e)
                            {
                                _ErrorMessageAction?.Invoke($"PubSubServiceAzure->CustomSubscribe: {e.Message}, Trace: {e.StackTrace}");
                                if (e.InnerException != null && e.InnerException != e)
                                {
                                    _ErrorMessageAction?.Invoke($"PubSubServiceAzure->CustomSubscribe->Inner: {e.InnerException.Message}, Trace: {e.InnerException.StackTrace}");
                                }

                                RemoveSubscriberThread(_CustomTopic);
                                if (!_SubscriptionClient.IsClosedOrClosing)
                                {
                                    using (var CloseSubscriptionTask = _SubscriptionClient.CloseAsync())
                                    {
                                        CloseSubscriptionTask.Wait();
                                    }
                                }
                            }

                            while (!SubscriptionCancellationVar.Get())
                            {
                                //Wait until delete
                                Thread.Sleep(500);
                            }

                            if (!_SubscriptionClient.IsClosedOrClosing)
                            {
                                using (var CloseSubscriptionTask = _SubscriptionClient.CloseAsync())
                                {
                                    CloseSubscriptionTask.Wait();
                                }
                            }
                        });
                        SubscriptionThread.Start();

                        lock (SubscriberThreadsDictionaryLock)
                        {
                            SubscriberThreadsDictionary.Add(_CustomTopic, new Tuple<Thread, Atomicable<bool>>(SubscriptionThread, SubscriptionCancellationVar));
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public void DeleteCustomTopicGlobally(string _CustomTopic, Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0 
                && AzureNamespaceManager != null)
            {
                _CustomTopic = GetAzureFriendlyTopicName(_CustomTopic);

                try
                {
                    RemoveSubscriberThread(_CustomTopic);

                    using (var DeleteTopicTask = AzureNamespaceManager.Topics.DeleteByNameAsync(_CustomTopic))
                    {
                        DeleteTopicTask.Wait();
                    }
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"PubSubServiceAzure->DeleteCustomTopicGlobally: {e.Message}, Trace: {e.StackTrace}");
                    if (e.InnerException != null && e.InnerException != e)
                    {
                        _ErrorMessageAction?.Invoke($"PubSubServiceAzure->DeleteCustomTopicGlobally->Inner: {e.InnerException.Message}, Trace: {e.InnerException.StackTrace}");
                    }
                }
            }
        }

        /// <summary>
        ///
        /// <para>DeleteTopicGlobally:</para>
        /// 
        /// <para>Deletes all messages and the topic of given workspace [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain] topic</para>
        /// 
        /// <para>Check <seealso cref="IPubSubServiceInterface.DeleteTopicGlobally"/> for detailed documentation</para>
        ///
        /// </summary>
        public void DeleteTopicGlobally(string _MemoryScopeKey, Action<string> _ErrorMessageAction = null)
        {
            DeleteCustomTopicGlobally(_MemoryScopeKey, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>EnsureUniqueMessageDelivery:</para>
        /// 
        /// <para>Sets up the unique message delivery ensurer</para>
        /// 
        /// <para>Check <seealso cref="IPubSubServiceInterface.EnsureUniqueMessageDelivery"/> for detailed documentation</para>
        ///
        /// </summary>
        public void EnsureUniqueMessageDelivery(IMemoryServiceInterface _EnsurerMemoryService, Action<string> _ErrorMessageAction = null)
        {
            UniqueMessageDeliveryEnsurer = new BPubSubUniqueMessageDeliveryEnsurer(_EnsurerMemoryService, this);
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="IPubSubServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        private static string GetAzureFriendlyTopicName(string Input)
        {
            return Input.Replace(":", "-");
        }
        private static string GetTopicNameFromAzureFriendlyName(string Input)
        {
            return Input.Replace("-", ":");
        }

        /// <summary>
        ///
        /// <para>Publish:</para>
        /// 
        /// <para>Publishes the given message to given workspace [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain]:[_MemoryScopeKey.Identifier] topic</para>
        /// 
        /// <para>Check <seealso cref="IPubSubServiceInterface.Publish"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool Publish(string _MemoryScopeKey, JObject _Message, Action<string> _ErrorMessageAction = null)
        {
            if (_Message == null) return false;

            string Message = _Message.ToString();

            return CustomPublish(_MemoryScopeKey, Message, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>Subscribe:</para>
        /// 
        /// <para>Subscribes to given workspace [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain]:[_MemoryScopeKey.Identifier] topic</para>
        /// 
        /// <para>Check <seealso cref="IPubSubServiceInterface.Subscribe"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool Subscribe(string _MemoryScopeKey, Action<string, JObject> _OnMessage, Action<string> _ErrorMessageAction = null)
        {
            if (_OnMessage == null) return false;

            return CustomSubscribe(_MemoryScopeKey, (string TopicParameter, string MessageParameter) =>
            {
                JObject AsJson;
                try
                {
                    AsJson = JObject.Parse(MessageParameter);
                }
                catch (Exception e)
                {
                    AsJson = null;
                    _ErrorMessageAction?.Invoke($"PubSubServiceAzure->Subscribe: {e.Message}, Trace: {e.StackTrace}");
                    if (e.InnerException != null && e.InnerException != e)
                    {
                        _ErrorMessageAction?.Invoke($"PubSubServiceAzure->Subscribe->Inner: {e.InnerException.Message}, Trace: {e.InnerException.StackTrace}");
                    }
                }

                if (AsJson != null)
                {
                    _OnMessage?.Invoke(TopicParameter, AsJson);
                }

            }, _ErrorMessageAction);
        }
    }
}