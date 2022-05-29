/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace CloudServiceUtilities.PubSubServices
{
    public class PubSubServiceRedis : RedisCommonFunctionalities, IPubSubServiceInterface
    {
        /// <summary>
        /// 
        /// <para>PubSubServiceRedis: Parametered Constructor</para>
        /// <para>Note: Redis Pub/Sub service does not keep messages in a permanent queue, therefore if there is not any listener, message will be lost, unlike other Pub/Sub services.</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_RedisEndpoint"/>                 Redis Endpoint without Port</para>
        /// <para><paramref name="_RedisPort"/>                     Redis Endpoint Port</para>
        /// <para><paramref name="_RedisPassword"/>                 Redis Server Password</para>
        /// 
        /// </summary>
        public PubSubServiceRedis(
            string _RedisEndpoint,
            int _RedisPort,
            string _RedisPassword,
            bool _bFailoverMechanismEnabled = true,
            Action<string> _ErrorMessageAction = null) : base("PubSubServiceRedis", _RedisEndpoint, _RedisPort, _RedisPassword, false, _bFailoverMechanismEnabled,  _ErrorMessageAction)
        {
        }

        /// <summary>
        /// 
        /// <para>PubSubServiceRedis: Parametered Constructor</para>
        /// <para>Note: Redis Pub/Sub service does not keep messages in a permanent queue, therefore if there is not any listener, message will be lost, unlike other Pub/Sub services.</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_RedisEndpoint"/>                 Redis Endpoint without Port</para>
        /// <para><paramref name="_RedisPort"/>                     Redis Endpoint Port</para>
        /// <para><paramref name="_RedisPassword"/>                 Redis Server Password</para>
        /// <para><paramref name="_RedisSslEnabled"/>               Redis Server SSL Connection Enabled/Disabled</para>
        /// 
        /// </summary>
        public PubSubServiceRedis(
            string _RedisEndpoint,
            int _RedisPort,
            string _RedisPassword,
            bool _RedisSslEnabled,
            bool _bFailoverMechanismEnabled = true,
            Action<string> _ErrorMessageAction = null) : base("PubSubServiceRedis", _RedisEndpoint, _RedisPort, _RedisPassword, _RedisSslEnabled, _bFailoverMechanismEnabled, _ErrorMessageAction)
        {
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
                    _ErrorMessageAction?.Invoke($"PubSubServiceRedis->Subscribe: {e.Message}, Trace: {e.StackTrace}");
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
        public bool CustomSubscribe(
            string _CustomTopic,
            Action<string, string> _OnMessage,
            Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0 && _OnMessage != null)
            {
                FailoverCheck();
                try
                {
                    RedisConnection.GetSubscriber().Subscribe(
                        _CustomTopic,
                        (RedisChannel Channel, RedisValue Value) =>
                        {
                            if (UniqueMessageDeliveryEnsurer != null)
                            {
                                var Message = Value.ToString();

                                UniqueMessageDeliveryEnsurer.Subscribe_ClearAndExtractTimestampFromMessage(ref Message, out string TimestampHash);

                                if (UniqueMessageDeliveryEnsurer.Subscription_EnsureUniqueDelivery(Channel, TimestampHash, _ErrorMessageAction))
                                {
                                    _OnMessage?.Invoke(Channel, Message);
                                }
                            }
                            else
                            {
                                _OnMessage?.Invoke(Channel, Value.ToString());
                            }
                        });
                }
                catch (Exception e)
                {
                    if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                    {
                        OnFailoverDetected(_ErrorMessageAction);
                        return CustomSubscribe(_CustomTopic, _OnMessage, _ErrorMessageAction);
                    }

                    _ErrorMessageAction?.Invoke($"PubSubServiceRedis->CustomSubscribe: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }
                return true;
            }
            return false;
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
                FailoverCheck();
                try
                {
                    if (UniqueMessageDeliveryEnsurer != null)
                    {
                        UniqueMessageDeliveryEnsurer.Publish_PrependTimestampToMessage(ref _CustomMessage, out string TimestampHash);

                        if (UniqueMessageDeliveryEnsurer.Publish_EnsureUniqueDelivery(_CustomTopic, TimestampHash, _ErrorMessageAction))
                        {
                            using (var CreatedTask = RedisConnection.GetDatabase().PublishAsync(_CustomTopic, _CustomMessage))
                            {
                                CreatedTask.Wait();
                            }
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("PubSubServiceRedis->CustomPublish: UniqueMessageDeliveryEnsurer has failed.");
                            return false;
                        }
                    }
                    else
                    {
                        using (var CreatedTask = RedisConnection.GetDatabase().PublishAsync(_CustomTopic, _CustomMessage))
                        {
                            CreatedTask.Wait();
                        }
                    }
                }
                catch (Exception e)
                {
                    if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                    {
                        OnFailoverDetected(_ErrorMessageAction);
                        return CustomPublish(_CustomTopic, _CustomMessage, _ErrorMessageAction);
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke($"PubSubServiceRedis->CustomPublish: {e.Message}, Trace: {e.StackTrace}");
                        return false;
                    }
                }
                return true;
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
                try
                {
                    RedisConnection.GetSubscriber().Unsubscribe(_CustomTopic, null);
                }
                catch (Exception e)
                {
                    if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                    {
                        OnFailoverDetected(_ErrorMessageAction);
                        DeleteCustomTopicGlobally(_CustomTopic, _ErrorMessageAction);
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke($"PubSubServiceRedis->DeleteTopicGlobally: {e.Message}, Trace: {e.StackTrace}");
                    }
                }
            }
        }
    }
}