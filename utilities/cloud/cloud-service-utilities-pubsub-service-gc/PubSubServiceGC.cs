/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using CommonUtilities;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Iam.V1;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Grpc.Auth;
using Grpc.Core;
using Newtonsoft.Json.Linq;

namespace CloudServiceUtilities.PubSubServices
{
    public class PubSubServiceGC : IPubSubServiceInterface
    {
        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        private readonly string ProjectID;

        private readonly ServiceAccountCredential PublishCredential;
        private readonly ServiceAccountCredential SubscribeCredential;

        private readonly Dictionary<string, PublisherServiceApiClient> PublisherTopicDictionary = new Dictionary<string, PublisherServiceApiClient>();
        private readonly List<Tuple<string, SubscriberServiceApiClient, SubscriptionName>> SubscriberTopicList = new List<Tuple<string, SubscriberServiceApiClient, SubscriptionName>>();
        private readonly Dictionary<SubscriptionName, Tuple<Thread, Atomicable<bool>>> SubscriberThreadsDictionary = new Dictionary<SubscriptionName, Tuple<Thread, Atomicable<bool>>>();
        private readonly Dictionary<string, object> PublisherTopicDictionaryLock = new Dictionary<string, object>();
        private object LockablePublisherTopicDictionaryObject(string _Topic)
        {
            lock (PublisherTopicDictionaryLock)
            {
                if (!PublisherTopicDictionaryLock.ContainsKey(_Topic))
                {
                    PublisherTopicDictionaryLock.Add(_Topic, new object());
                }
                return PublisherTopicDictionaryLock[_Topic];
            }
        }
        private readonly Dictionary<string, object> SubscriberTopicListLock = new Dictionary<string, object>();
        private object LockableSubscriberTopicListObject(string _Topic)
        {
            lock (SubscriberTopicListLock)
            {
                if (!SubscriberTopicListLock.ContainsKey(_Topic))
                {
                    SubscriberTopicListLock.Add(_Topic, new object());
                }
                return SubscriberTopicListLock[_Topic];
            }
        }

        private readonly object SubscriberThreadsDictionaryLock = new object();

        /// <summary>
        /// 
        /// <para>PubSubServiceGC: Parametered Constructor for Managed Service by Google</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ProjectID"/>              GC Project ID</para>
        /// <para><paramref name="_ErrorMessageAction"/>     Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public PubSubServiceGC(
            string _ProjectID,
            Action<string> _ErrorMessageAction = null)
        {
            ProjectID = _ProjectID;
            try
            {
                string ApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                string ApplicationCredentialsPlain = Environment.GetEnvironmentVariable("GOOGLE_PLAIN_CREDENTIALS");
                string ApplicationCredentialsBase64 = Environment.GetEnvironmentVariable("GOOGLE_BASE64_CREDENTIALS");
                if (ApplicationCredentials == null && ApplicationCredentialsPlain == null && ApplicationCredentialsBase64 == null)
                {
                    _ErrorMessageAction?.Invoke("PubSubServiceGC->Constructor: GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS or GOOGLE_BASE64_CREDENTIALS) environment variable is not defined.");
                    bInitializationSucceed = false;
                }
                else
                {
                    var PublishScopes = new List<string>();
                    var SubscribeScopes = new List<string>();
                    foreach (var Scope in PublisherServiceApiClient.DefaultScopes)
                    {
                        if (!PublishScopes.Contains(Scope))
                        {
                            PublishScopes.Add(Scope);
                        }
                    }
                    foreach (var Scope in SubscriberServiceApiClient.DefaultScopes)
                    {
                        if (!SubscribeScopes.Contains(Scope))
                        {
                            SubscribeScopes.Add(Scope);
                        }
                    }

                    if (ApplicationCredentials == null)
                    {
                        if (ApplicationCredentialsPlain != null && !Utility.HexDecode(out ApplicationCredentialsPlain, ApplicationCredentialsPlain, _ErrorMessageAction))
                        {
                            throw new Exception("Hex decode operation for application credentials plain has failed.");
                        }
                        else if (!Utility.Base64Decode(out ApplicationCredentialsPlain, ApplicationCredentialsBase64, _ErrorMessageAction))
                        {
                            throw new Exception("Base64 decode operation for application credentials plain has failed.");
                        }
                        PublishCredential = GoogleCredential.FromJson(ApplicationCredentialsPlain)
                                .CreateScoped(
                                PublishScopes.ToArray())
                                .UnderlyingCredential as ServiceAccountCredential;
                        SubscribeCredential = GoogleCredential.FromJson(ApplicationCredentialsPlain)
                                .CreateScoped(
                                SubscribeScopes.ToArray())
                                .UnderlyingCredential as ServiceAccountCredential;
                    }
                    else
                    {
                        using (var Stream = new FileStream(ApplicationCredentials, FileMode.Open, FileAccess.Read))
                        {
                            PublishCredential = GoogleCredential.FromStream(Stream)
                                .CreateScoped(
                                PublishScopes.ToArray())
                                .UnderlyingCredential as ServiceAccountCredential;
                            SubscribeCredential = GoogleCredential.FromStream(Stream)
                                .CreateScoped(
                                SubscribeScopes.ToArray())
                                .UnderlyingCredential as ServiceAccountCredential;
                        }
                    }
                    
                    if (PublishCredential != null && SubscribeCredential != null)
                    {
                        bInitializationSucceed = true;
                    }
                    else
                    {
                        bInitializationSucceed = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"PubSubServiceGC->Constructor: {e.Message}, Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        private static string GetGoogleFriendlyTopicName(string Input)
        {
            return HttpUtility.UrlEncode(Input);
        }
        private static string GetTopicNameFromGoogleFriendlyName(string Input)
        {
            return HttpUtility.UrlDecode(Input);
        }

        private bool GetPublisher(out PublisherServiceApiClient Result, TopicName GoogleFriendlyTopicName, Action<string> _ErrorMessageAction = null)
        {
            lock (LockablePublisherTopicDictionaryObject(GoogleFriendlyTopicName.TopicId))
            {
                if (PublisherTopicDictionary.ContainsKey(GoogleFriendlyTopicName.TopicId))
                {
                    Result = PublisherTopicDictionary[GoogleFriendlyTopicName.TopicId];
                    return true;
                }

                try
                {
                    Result = new PublisherServiceApiClientBuilder()
                    {
                        ChannelCredentials = PublishCredential.ToChannelCredentials()
                    }.Build();

                    PublisherTopicDictionary[GoogleFriendlyTopicName.TopicId] = Result;
                }
                catch (Exception e)
                {
                    Result = null;

                    _ErrorMessageAction?.Invoke($"PubSubServiceGC->GetPublisher: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }

                if (!EnsureTopicExistence(GoogleFriendlyTopicName, Result, _ErrorMessageAction))
                {
                    Result = null;
                    return false;
                }
                
                return true;
            }
        }

        private bool GetSubscriber(out SubscriberServiceApiClient APIClientVar, out SubscriptionName SubscriptionNameVar, string GoogleFriendlyTopicName, Action<string> _ErrorMessageAction = null)
        {
            lock (LockableSubscriberTopicListObject(GoogleFriendlyTopicName))
            {
                APIClientVar = null;
                SubscriptionNameVar = null;

                var TopicInstance = new TopicName(ProjectID, GoogleFriendlyTopicName);

                if (!EnsureTopicExistence(TopicInstance, null, _ErrorMessageAction))
                {
                    return false;
                }

                try
                {
                    APIClientVar = new SubscriberServiceApiClientBuilder()
                    {
                        ChannelCredentials = SubscribeCredential.ToChannelCredentials()
                    }.Build();
                }
                catch (Exception e)
                {
                    APIClientVar = null;
                    _ErrorMessageAction?.Invoke($"PubSubServiceGC->GetSubscriber: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }

                string SubscriptionIDBase = $"{GoogleFriendlyTopicName}-";
                int SubscriptionIDIncrementer = 1;
                SubscriptionNameVar = new SubscriptionName(ProjectID, $"{SubscriptionIDBase}{SubscriptionIDIncrementer}");

                bool bSubscriptionSuccess = false;
                while (!bSubscriptionSuccess)
                {
                    try
                    {
                        APIClientVar.CreateSubscription(SubscriptionNameVar, TopicInstance, null, 600);
                        bSubscriptionSuccess = true;
                    }
                    catch (Exception e)
                    {
                        if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.AlreadyExists)
                        {
                            SubscriptionIDIncrementer++;
                            SubscriptionNameVar = new SubscriptionName(ProjectID, $"{SubscriptionIDBase}{SubscriptionIDIncrementer}");
                        }
                        else
                        {
                            SubscriptionNameVar = null;
                            _ErrorMessageAction?.Invoke($"PubSubServiceGC->GetSubscriber: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }
                    }
                }
                
                SubscriberTopicList.Add(new Tuple<string, SubscriberServiceApiClient, SubscriptionName>(GoogleFriendlyTopicName, APIClientVar, SubscriptionNameVar));

                return true;
            }
        }

        private bool EnsureTopicExistence(TopicName _TopicInstance, PublisherServiceApiClient _PublisherAPIClient = null, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                if (_PublisherAPIClient == null)
                {
                    _PublisherAPIClient = new PublisherServiceApiClientBuilder()
                    {
                        ChannelCredentials = PublishCredential.ToChannelCredentials()
                    }.Build();
                }
                _PublisherAPIClient.CreateTopic(_TopicInstance);
            }
            catch (Exception e)
            {
                if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.AlreadyExists)
                {
                    //That is fine.
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"PubSubServiceGC->EnsureTopicExistence: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }
            }
            return true;
        }
        private bool DeleteTopic(TopicName _TopicInstance, Action<string> _ErrorMessageAction)
        {
            try
            {
                var PublisherAPIClient = new PublisherServiceApiClientBuilder()
                {
                    ChannelCredentials = PublishCredential.ToChannelCredentials()
                }.Build();

                PublisherAPIClient.DeleteTopic(_TopicInstance);
            }
            catch (Exception e)
            {
                if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.NotFound)
                {
                    //That is fine.
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"PubSubServiceGC->DeleteTopic: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }
            }
            return true;
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

        /// <summary>
        ///
        /// <para>EnsureUniqueMessageDelivery:</para>
        /// 
        /// <para>Sets up the unique message delivery ensurer</para>
        /// 
        /// <para>Check <seealso cref="IPubSubServiceInterface.EnsureUniqueMessageDelivery"/> for detailed documentation</para>
        ///
        /// </summary>
        public void EnsureUniqueMessageDelivery(
            IMemoryServiceInterface _EnsurerMemoryService,
            Action<string> _ErrorMessageAction = null)
        {
            UniqueMessageDeliveryEnsurer = new BPubSubUniqueMessageDeliveryEnsurer(_EnsurerMemoryService, this);
        }
        private BPubSubUniqueMessageDeliveryEnsurer UniqueMessageDeliveryEnsurer = null;

        /// <summary>
        ///
        /// <para>Subscribe:</para>
        /// 
        /// <para>Subscribes to given workspace [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain]:[_MemoryScopeKey.Identifier] topic</para>
        /// 
        /// <para>Check <seealso cref="IPubSubServiceInterface.Subscribe"/> for detailed documentation</para>
        ///
        /// </summary>
        [Obsolete]
        public bool Subscribe(
            string _MemoryScopeKey,
            Action<string, JObject> _OnMessage,
            Action<string> _ErrorMessageAction = null)
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
                    _ErrorMessageAction?.Invoke($"PubSubServiceGC->Subscribe: {e.Message}, Trace: {e.StackTrace}");
                }

                if (AsJson != null)
                {
                    _OnMessage?.Invoke(TopicParameter, AsJson);
                }

            }, _ErrorMessageAction);
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
        public bool Publish(
            string _MemoryScopeKey,
            JObject _Message,
            Action<string> _ErrorMessageAction = null)
        {
            if (_Message == null) return false;

            string Message = _Message.ToString();

            return CustomPublish(_MemoryScopeKey, Message, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>CustomSubscribe:</para>
        /// 
        /// <para>Subscribes to given custom topic</para>
        /// 
        /// <para>Check <seealso cref="IPubSubServiceInterface.CustomSubscribe"/> for detailed documentation</para>
        ///
        /// </summary>
        [Obsolete]
        public bool CustomSubscribe(
            string _CustomTopic,
            Action<string, string> _OnMessage,
            Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0 && _OnMessage != null)
            {
                _CustomTopic = GetGoogleFriendlyTopicName(_CustomTopic);

                if (GetSubscriber(out SubscriberServiceApiClient APIClientVar, out SubscriptionName SubscriptionNameVar, _CustomTopic, _ErrorMessageAction))
                {
                    var SubscriptionCancellationVar = new Atomicable<bool>(false, EProducerStatus.MultipleProducer);
                    var SubscriptionThread = new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        while (!SubscriptionCancellationVar.Get())
                        {
                            PullResponse Response = null;
                            try
                            {
                                Response = APIClientVar.Pull(SubscriptionNameVar, true, 1000);
                            }
                            catch (Exception e)
                            {
                                if (e is RpcException && (e as RpcException).StatusCode == StatusCode.DeadlineExceeded)
                                {
                                    Thread.Sleep(1000);
                                    continue;
                                }

                                Response = null;

                                _ErrorMessageAction?.Invoke($"PubSubServiceGC->CustomSubscribe: {e.Message}, Trace: {e.StackTrace}");
                                if (e.InnerException != null && e.InnerException != e)
                                {
                                    _ErrorMessageAction?.Invoke($"PubSubServiceGC->CustomSubscribe->Inner: {e.InnerException.Message}, Trace: {e.InnerException.StackTrace}");
                                }
                            }

                            if (Response == null || Response.ReceivedMessages == null || !Response.ReceivedMessages.Any())
                            {
                                Thread.Sleep(1000);
                                continue;
                            }

                            var MessageContainers = Response.ReceivedMessages.ToArray();
                            if (MessageContainers != null && MessageContainers.Length > 0)
                            {
                                var AckArray = new List<string>();

                                foreach (var MessageContainer in MessageContainers)
                                {
                                    if (MessageContainer != null)
                                    {
                                        if (!AckArray.Contains(MessageContainer.AckId))
                                        {
                                            AckArray.Add(MessageContainer.AckId);
                                        }

                                        string Topic = GetTopicNameFromGoogleFriendlyName(_CustomTopic);
                                        string Data = MessageContainer.Message.Data.ToStringUtf8();

                                        if (UniqueMessageDeliveryEnsurer != null)
                                        {
                                            UniqueMessageDeliveryEnsurer.Subscribe_ClearAndExtractTimestampFromMessage(ref Data, out string TimestampHash);

                                            if (UniqueMessageDeliveryEnsurer.Subscription_EnsureUniqueDelivery(Topic, TimestampHash, _ErrorMessageAction))
                                            {
                                                _OnMessage?.Invoke(Topic, Data);
                                            }
                                        }
                                        else
                                        {
                                            _OnMessage?.Invoke(Topic, Data);
                                        }
                                    }
                                }
                                
                                try
                                {
                                    APIClientVar.Acknowledge(SubscriptionNameVar, AckArray);
                                }
                                catch (Exception e)
                                {
                                    if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.InvalidArgument)
                                    {
                                        //That is fine, probably due to previous subscriptions
                                    }
                                    else
                                    {
                                        _ErrorMessageAction?.Invoke($"PubSubServiceGC->CustomSubscribe: {e.Message}, Trace: {e.StackTrace}");
                                        if (e.InnerException != null && e.InnerException != e)
                                        {
                                            _ErrorMessageAction?.Invoke($"PubSubServiceGC->CustomSubscribe->Inner: {e.InnerException.Message}, Trace: {e.InnerException.StackTrace}");
                                        }
                                    }
                                }
                            }
                        }
                        
                    });
                    SubscriptionThread.Start();

                    lock (SubscriberThreadsDictionaryLock)
                    {
                        SubscriberThreadsDictionary.Add(SubscriptionNameVar, new Tuple<Thread, Atomicable<bool>>(SubscriptionThread, SubscriptionCancellationVar));
                    }
                    return true;
                }
            }
            return false;
        }

        public struct BGCPubSubIamPolicy
        {
            public string Role;
            public List<string> Members;
        }

        /// <summary>
        ///
        /// <para>SetTopicIamPolicy:</para>
        /// 
        /// <para>Adds IAM policy to the given pub/sub topic</para>
        /// 
        /// </summary>
        public bool SetTopicIamPolicy(
            string _TopicName, 
            List<BGCPubSubIamPolicy> _PoliciesToAdd,
            Action<string> _ErrorMessageAction = null)
        {
            _TopicName = GetGoogleFriendlyTopicName(_TopicName);

            var TopicInstance = new TopicName(ProjectID, _TopicName);

            if (GetPublisher(out PublisherServiceApiClient Client, TopicInstance, _ErrorMessageAction))
            {
                try
                {
                    var NewPolicy = new Policy();
                    foreach (var PolicyToAdd in _PoliciesToAdd)
                    {
                        var NewBinding = new Binding()
                        {
                            Role = PolicyToAdd.Role
                        };
                        foreach (var Member in PolicyToAdd.Members)
                        {
                            NewBinding.Members.Add(Member);
                        }
                        NewPolicy.Bindings.Add(NewBinding);
                    }
                    var Request = new SetIamPolicyRequest
                    {
                        ResourceAsResourceName = TopicInstance,
                        Policy = NewPolicy
                    };
                    var Response = Client.IAMPolicyClient.SetIamPolicy(Request);
                    if (Response == null)
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.NotFound)
                    {
                        lock (LockablePublisherTopicDictionaryObject(_TopicName))
                        {
                            PublisherTopicDictionary.Remove(TopicInstance.TopicId);
                        }
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke($"PubSubServiceGC->SetTopicIamPolicy: {e.Message}, Trace: {e.StackTrace}");
                    }
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>CustomPublish:</para>
        /// 
        /// <para>Publishes the given message to given custom topic</para>
        /// 
        /// <para>Check <seealso cref="IPubSubServiceInterface.CustomPublish"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CustomPublish(
            string _CustomTopic,
            string _CustomMessage,
            Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0
                && _CustomMessage != null && _CustomMessage.Length > 0)
            {
                var CustomTopicGoogleFriendly = GetGoogleFriendlyTopicName(_CustomTopic);

                string TimestampHash = null;
                UniqueMessageDeliveryEnsurer?.Publish_PrependTimestampToMessage(ref _CustomMessage, out TimestampHash);

                ByteString MessageByteString = null;
                try
                {
                    MessageByteString = ByteString.CopyFromUtf8(_CustomMessage);
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"PubSubServiceGC->CustomPublish: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }

                var TopicInstance = new TopicName(ProjectID, CustomTopicGoogleFriendly);

                var MessageContent = new PubsubMessage()
                {
                    Data = MessageByteString
                };

                if (GetPublisher(out PublisherServiceApiClient Client, TopicInstance, _ErrorMessageAction))
                {
                    try
                    {
                        if (UniqueMessageDeliveryEnsurer != null)
                        {
                            if (UniqueMessageDeliveryEnsurer.Publish_EnsureUniqueDelivery(_CustomTopic, TimestampHash, _ErrorMessageAction))
                            {
                                using (var CreatedTask = Client.PublishAsync(TopicInstance, new PubsubMessage[] { MessageContent }))
                                {
                                    CreatedTask.Wait();
                                }
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("PubSubServiceGC->CustomPublish: UniqueMessageDeliveryEnsurer has failed.");
                                return false;
                            }
                        }
                        else
                        {
                            using (var CreatedTask = Client.PublishAsync(TopicInstance, new PubsubMessage[] { MessageContent }))
                            {
                                CreatedTask.Wait();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.NotFound)
                        {
                            lock (LockablePublisherTopicDictionaryObject(_CustomTopic))
                            {
                                PublisherTopicDictionary.Remove(TopicInstance.TopicId);
                            }
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke($"PubSubServiceGC->CustomPublish: {e.Message}, Trace: {e.StackTrace}");
                        }
                        return false;
                    }
                    return true;
                }
            }
            return false;
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
        public void DeleteTopicGlobally(
            string _MemoryScopeKey,
            Action<string> _ErrorMessageAction = null)
        {
            DeleteCustomTopicGlobally(_MemoryScopeKey, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>DeleteCustomTopicGlobally:</para>
        /// 
        /// <para>Deletes all messages and the topic of given workspace</para>
        /// 
        /// <para>Check <seealso cref="IPubSubServiceInterface.DeleteCustomTopicGlobally"/> for detailed documentation</para>
        ///
        /// </summary>
        public void DeleteCustomTopicGlobally(
            string _CustomTopic,
            Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0)
            {
                _CustomTopic = GetGoogleFriendlyTopicName(_CustomTopic);

                var TopicsToBeDeleted = new List<string>();
                var SubscriptionToBeRemoved = new List<Tuple<SubscriberServiceApiClient, SubscriptionName>>();
                var IndicesToBeRemoved = new List<int>();

                lock (LockableSubscriberTopicListObject(_CustomTopic))
                {
                    int i = 0;
                    foreach (var SubscriberTopic in SubscriberTopicList)
                    {
                        if (SubscriberTopic != null && SubscriberTopic.Item1 == _CustomTopic)
                        {
                            if (!TopicsToBeDeleted.Contains(_CustomTopic))
                            {
                                TopicsToBeDeleted.Add(_CustomTopic);
                            }
                            
                            SubscriptionToBeRemoved.Add(new Tuple<SubscriberServiceApiClient, SubscriptionName>(
                                SubscriberTopic.Item2,
                                SubscriberTopic.Item3));

                            IndicesToBeRemoved.Add(i);
                        }
                        i++;
                    }

                    for (int j = (IndicesToBeRemoved.Count - 1); j >= 0; j--)
                    {
                        SubscriberTopicList.RemoveAt(IndicesToBeRemoved[j]);
                    }

                    foreach (var Current in SubscriptionToBeRemoved)
                    {
                        if (Current != null && Current.Item2 != null)
                        {
                            if (Current.Item2 != null)
                            {
                                lock (SubscriberThreadsDictionaryLock)
                                {
                                    if (SubscriberThreadsDictionary.ContainsKey(Current.Item2))
                                    {
                                        var SubscriberThread = SubscriberThreadsDictionary[Current.Item2];
                                        if (SubscriberThread != null)
                                        {
                                            SubscriberThread.Item2.Set(true);
                                        }
                                        SubscriberThreadsDictionary.Remove(Current.Item2);
                                    }
                                }
                                try
                                {
                                    Current.Item1?.DeleteSubscription(Current.Item2);
                                }
                                catch (Exception e)
                                {
                                    if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.NotFound)
                                    {
                                        //That is fine.
                                    }
                                    else
                                    {
                                        _ErrorMessageAction?.Invoke($"PubSubServiceGC->DeleteCustomTopicGlobally: {e.Message}, Trace: {e.StackTrace}");
                                        if (e.InnerException != null && e.InnerException != e)
                                        {
                                            _ErrorMessageAction?.Invoke($"PubSubServiceGC->DeleteCustomTopicGlobally->Inner: {e.InnerException.Message}, Trace: {e.InnerException.StackTrace}");
                                        }
                                    }
                                }
                            }
                        }
                    
                        lock (LockablePublisherTopicDictionaryObject(_CustomTopic))
                        {
                            if (PublisherTopicDictionary.ContainsKey(_CustomTopic))
                            {
                                if (!TopicsToBeDeleted.Contains(_CustomTopic))
                                {
                                    TopicsToBeDeleted.Add(_CustomTopic);
                                }
                                PublisherTopicDictionary.Remove(_CustomTopic);
                            }

                            foreach (var Topic in TopicsToBeDeleted)
                            {
                                if (Topic != null)
                                {
                                    DeleteTopic(new TopicName(ProjectID, Topic), _ErrorMessageAction);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}