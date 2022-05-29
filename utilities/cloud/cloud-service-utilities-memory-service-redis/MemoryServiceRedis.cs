/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonUtilities;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace CloudServiceUtilities.MemoryServices
{
    public class MemoryServiceRedis : RedisCommonFunctionalities, IMemoryServiceInterface
    {
        private readonly IPubSubServiceInterface PubSubService = null;

        /// <summary>
        /// 
        /// <para>MemoryServiceRedis: Parametered Constructor</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_RedisEndpoint"/>           Redis Endpoint without Port</para>
        /// <para><paramref name="_RedisPort"/>               Redis Endpoint Port</para>
        /// <para><paramref name="_RedisPassword"/>           Redis Server Password</para>
        /// <para><paramref name="_PubSubService"/>           Pub/Sub Service Instance</para>
        /// 
        /// </summary>
        public MemoryServiceRedis(
            string _RedisEndpoint,
            int _RedisPort,
            string _RedisPassword,
            IPubSubServiceInterface _PubSubService,
            bool _bFailoverMechanismEnabled = true,
            Action<string> _ErrorMessageAction = null) : base("MemoryServiceRedis", _RedisEndpoint, _RedisPort, _RedisPassword, false, _bFailoverMechanismEnabled, _ErrorMessageAction)
        {
            PubSubService = _PubSubService;
        }

        /// <summary>
        /// 
        /// <para>MemoryServiceRedis: Parametered Constructor</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_RedisEndpoint"/>           Redis Endpoint without Port</para>
        /// <para><paramref name="_RedisPort"/>               Redis Endpoint Port</para>
        /// <para><paramref name="_RedisPassword"/>           Redis Server Password</para>
        /// <para><paramref name="_RedisSslEnabled"/>         Redis Server SSL Connection Enabled/Disabled</para>
        /// <para><paramref name="_PubSubService"/>           Pub/Sub Service Instance</para>
        /// 
        /// </summary>
        public MemoryServiceRedis(
            string _RedisEndpoint,
            int _RedisPort,
            string _RedisPassword,
            bool _RedisSslEnabled,
            IPubSubServiceInterface _PubSubService,
            bool _bFailoverMechanismEnabled = true,
            Action<string> _ErrorMessageAction = null) : base("MemoryServiceRedis", _RedisEndpoint, _RedisPort, _RedisPassword, _RedisSslEnabled, _bFailoverMechanismEnabled, _ErrorMessageAction)
        {
            PubSubService = _PubSubService;
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="IMemoryServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        private PrimitiveType ConvertRedisValueToPrimitiveType(RedisValue _Input)
        {
            if (_Input.IsNullOrEmpty) return null;
            if (_Input.IsInteger &&
                _Input.TryParse(out long AsInteger))
            {
                return new PrimitiveType(AsInteger);
            }

            string AsString = _Input.ToString();
            if (double.TryParse(AsString, out double AsDouble))
            {
                if (AsDouble % 1 == 0)
                {
                    return new PrimitiveType((long)AsDouble);
                }
                return new PrimitiveType(AsDouble);
            }
            return new PrimitiveType(AsString);
        }

        private RedisValue ConvertPrimitiveTypeToRedisValue(PrimitiveType _Input)
        {
            if (_Input != null)
            {
                if (_Input.Type == EPrimitiveTypeEnum.Double)
                {
                    return _Input.AsDouble;
                }
                else if (_Input.Type == EPrimitiveTypeEnum.Integer)
                {
                    return _Input.AsInteger;
                }
                else if (_Input.Type == EPrimitiveTypeEnum.String)
                {
                    return _Input.AsString;
                }
                else if (_Input.Type == EPrimitiveTypeEnum.ByteArray)
                {
                    return _Input.AsByteArray;
                }
            }
            return new RedisValue();
        }

        /// <summary>
        ///
        /// <para>SetKeyExpireTime:</para>
        ///
        /// <para>Sets given namespace's expire time</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.SetKeyExpireTime"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool SetKeyExpireTime(
             string _MemoryScopeKey,
             TimeSpan _TTL,
             Action<string> _ErrorMessageAction = null)
        {
            try
            {
                var bDoesKeyExist = RedisConnection.GetDatabase().KeyExpire(_MemoryScopeKey, _TTL);
                if (!bDoesKeyExist)
                {
                    RedisConnection.GetDatabase().SetAdd(_MemoryScopeKey, "");
                    return RedisConnection.GetDatabase().KeyExpire(_MemoryScopeKey, _TTL);
                }
                return true;
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return SetKeyExpireTime(_MemoryScopeKey, _TTL, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->SetKeyExpireTime: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }
            }
        }

        /// <summary>
        ///
        /// <para>GetKeyExpireTime:</para>
        ///
        /// <para>Gets given namespace's expire time</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.GetKeyExpireTime"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool GetKeyExpireTime(
            string _MemoryScopeKey,
            out TimeSpan _TTL,
            Action<string> _ErrorMessageAction = null)
        {
            _TTL = TimeSpan.Zero;

            TimeSpan? TTL;
            try
            {
                TTL = RedisConnection.GetDatabase().KeyTimeToLive(_MemoryScopeKey);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetKeyExpireTime(_MemoryScopeKey, out _TTL, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->GetKeyExpireTime: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }
            }

            if (TTL.HasValue)
            {
                _TTL = TTL.Value;
                return true;
            }
            return false;
        }

        /// <summary>
        ///
        /// <para>SetKeyValue:</para>
        ///
        /// <para>Sets given keys' values within given namespace and publishes message to [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain] topic</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.SetKeyValue"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool SetKeyValue(
            string _MemoryScopeKey,
            Tuple<string, PrimitiveType>[] _KeyValues,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            if (_KeyValues.Length == 0) return false;

            HashEntry[] ArrayAsHashEntries = new HashEntry[_KeyValues.Length];

            JObject ChangesObject = new JObject();

            int i = 0;
            foreach (Tuple<string, PrimitiveType> KeyValue in _KeyValues)
            {
                if (KeyValue.Item2 != null)
                {
                    if (KeyValue.Item2.Type == EPrimitiveTypeEnum.Double)
                    {
                        ArrayAsHashEntries[i++] = new HashEntry(KeyValue.Item1, KeyValue.Item2.AsDouble.ToString());
                        ChangesObject[KeyValue.Item1] = KeyValue.Item2.AsDouble;
                    }
                    else if (KeyValue.Item2.Type == EPrimitiveTypeEnum.Integer)
                    {
                        ArrayAsHashEntries[i++] = new HashEntry(KeyValue.Item1, KeyValue.Item2.AsInteger);
                        ChangesObject[KeyValue.Item1] = KeyValue.Item2.AsInteger;
                    }
                    else if (KeyValue.Item2.Type == EPrimitiveTypeEnum.String)
                    {
                        ArrayAsHashEntries[i++] = new HashEntry(KeyValue.Item1, KeyValue.Item2.AsString);
                        ChangesObject[KeyValue.Item1] = KeyValue.Item2.AsString;
                    }
                    else if (KeyValue.Item2.Type == EPrimitiveTypeEnum.ByteArray)
                    {
                        ArrayAsHashEntries[i++] = new HashEntry(KeyValue.Item1, KeyValue.Item2.AsByteArray);
                        ChangesObject[KeyValue.Item1] = KeyValue.Item2.ToString();
                    }
                }
            }

            if (i == 0) return false;
            if (i != _KeyValues.Length)
            {
                HashEntry[] ShrankArray = new HashEntry[i];
                for (int k = 0; k < i; k++)
                {
                    ShrankArray[k] = ArrayAsHashEntries[k];
                }
                ArrayAsHashEntries = ShrankArray;
            }

            JObject PublishObject = new JObject
            {
                ["operation"] = "SetKeyValue",
                ["changes"] = ChangesObject
            };

            FailoverCheck();
            try
            {
                RedisConnection.GetDatabase().HashSet(_MemoryScopeKey, ArrayAsHashEntries);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return SetKeyValue(_MemoryScopeKey, _KeyValues, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->SetKeyValue: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }
            }
            if (PubSubService == null || !_bPublishChange) return true; //Means PubSubService is not needed and memory set has succeed.
            return PubSubService.Publish(_MemoryScopeKey, PublishObject, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>SetKeyValue:</para>
        ///
        /// <para>Sets given keys' values within given namespace and publishes message to [_Domain]:[_SubDomain] topic;</para>
        /// <para>With a condition; if key does not exist.</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.SetKeyValueConditionally"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool SetKeyValueConditionally(
            string _MemoryScopeKey,
            Tuple<string, PrimitiveType> _KeyValue,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            if (_KeyValue == null) return false;

            JObject ChangesObject = new JObject();

            HashEntry Entry = new HashEntry();
            if (_KeyValue.Item2 != null)
            {
                if (_KeyValue.Item2.Type == EPrimitiveTypeEnum.Double)
                {
                    Entry = new HashEntry(_KeyValue.Item1, _KeyValue.Item2.AsDouble.ToString());
                    ChangesObject[_KeyValue.Item1] = _KeyValue.Item2.AsDouble;
                }
                else if (_KeyValue.Item2.Type == EPrimitiveTypeEnum.Integer)
                {
                    Entry = new HashEntry(_KeyValue.Item1, _KeyValue.Item2.AsInteger);
                    ChangesObject[_KeyValue.Item1] = _KeyValue.Item2.AsInteger;
                }
                else if (_KeyValue.Item2.Type == EPrimitiveTypeEnum.String)
                {
                    Entry = new HashEntry(_KeyValue.Item1, _KeyValue.Item2.AsString);
                    ChangesObject[_KeyValue.Item1] = _KeyValue.Item2.AsString;
                }
                else if (_KeyValue.Item2.Type == EPrimitiveTypeEnum.ByteArray)
                {
                    Entry = new HashEntry(_KeyValue.Item1, _KeyValue.Item2.AsByteArray);
                    ChangesObject[_KeyValue.Item1] = _KeyValue.Item2.ToString();
                }
            }

            JObject PublishObject = new JObject
            {
                ["operation"] = "SetKeyValue", //Identical with SetKeyValue
                ["changes"] = ChangesObject
            };

            var RedisValues = new RedisValue[]
            {
                Entry.Name,
                Entry.Value
            };
            var Script = @"
                if redis.call('hexists', KEYS[1], ARGV[1]) == 0 then
                return redis.call('hset', KEYS[1], ARGV[1], ARGV[2])
                else
                return nil
                end";

            FailoverCheck();
            try
            {
                var Result = (RedisValue)RedisConnection.GetDatabase().ScriptEvaluate(Script,
                    new RedisKey[]
                    {
                        _MemoryScopeKey
                    },
                    RedisValues);
                if (Result.IsNull) return false;
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    var Result = (RedisValue)RedisConnection.GetDatabase().ScriptEvaluate(Script,
                        new RedisKey[]
                        {
                            _MemoryScopeKey
                        },
                        RedisValues);
                    if (Result.IsNull) return false;
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->SetKeyValue: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }
            }

            if (PubSubService == null || !_bPublishChange) return true; //Means PubSubService is not needed and memory set has succeed.
            PubSubService.Publish(_MemoryScopeKey, PublishObject, _ErrorMessageAction);
            return true;
        }

        /// <summary>
        ///
        /// <para>GetKeyValue:</para>
        ///
        /// <para>Gets given key's value within given namespace [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain]</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.GetKeyValue"/> for detailed documentation</para>
        ///
        /// </summary>
        public PrimitiveType GetKeyValue(
            string _MemoryScopeKey,
            string _Key,
            Action<string> _ErrorMessageAction = null)
        {
            RedisValue ReturnedValue;

            FailoverCheck();
            try
            {
                ReturnedValue = RedisConnection.GetDatabase().HashGet(_MemoryScopeKey, _Key);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetKeyValue(_MemoryScopeKey, _Key, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->GetKeyValue: {e.Message}, Trace: {e.StackTrace}");
                    return null;
                }
            }
            return ConvertRedisValueToPrimitiveType(ReturnedValue);
        }

        /// <summary>
        ///
        /// <para>GetKeysValues:</para>
        ///
        /// <para>Gets given keys' values' within given namespace [_Domain]:[_SubDomain]</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.GetKeysValues"/> for detailed documentation</para>
        ///
        /// </summary>
        public Dictionary<string, PrimitiveType> GetKeysValues(
            string _MemoryScopeKey,
            List<string> _Keys,
            Action<string> _ErrorMessageAction = null)
        {
            if (_Keys == null || _Keys.Count == 0) return null;

            Dictionary<string, PrimitiveType> Results = new Dictionary<string, PrimitiveType>();

            RedisValue[] KeysAsRedisValues = new RedisValue[_Keys.Count];

            string Script = "return redis.call('hmget',KEYS[1]";

            int i = 0;
            foreach (var _Key in _Keys)
            {
                Script += $",ARGV[{(i + 1)}]";
                KeysAsRedisValues[i] = _Keys[i];
                i++;
            }
            Script += ")";

            RedisValue[] ScriptEvaluationResult;

            FailoverCheck();
            try
            {
                ScriptEvaluationResult = (RedisValue[])RedisConnection.GetDatabase().ScriptEvaluate(Script,
                    new RedisKey[]
                    {
                        _MemoryScopeKey
                    },
                    KeysAsRedisValues);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetKeysValues(_MemoryScopeKey, _Keys, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->GetKeysValues: {e.Message}, Trace: {e.StackTrace}");
                    return null;
                }
            }

            bool bNonNullValueFound = false;
            if (ScriptEvaluationResult != null && 
                ScriptEvaluationResult.Length == _Keys.Count)
            {
                int j = 0;
                foreach (var _Key in _Keys)
                {
                    var AsPrimitive = ConvertRedisValueToPrimitiveType(ScriptEvaluationResult[j++]);
                    Results[_Key] = AsPrimitive;
                    if (AsPrimitive != null)
                    {
                        bNonNullValueFound = true;
                    }
                }
            }
            else
            {
                _ErrorMessageAction?.Invoke("MemoryServiceRedis->GetKeysValues: redis.call returned null or result length is not equal to keys length.");
                return null;
            }
            return bNonNullValueFound ? Results : null;
        }

        /// <summary>
        ///
        /// <para>GetAllKeyValues:</para>
        ///
        /// <para>Gets all keys and keys' values of given namespace [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain]</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.GetAllKeyValues"/> for detailed documentation</para>
        ///
        /// </summary>
        public Tuple<string, PrimitiveType>[] GetAllKeyValues(
            string _MemoryScopeKey,
            Action<string> _ErrorMessageAction = null)
        {
            HashEntry[] ReturnedKeyValues;

            FailoverCheck();
            try
            {
                ReturnedKeyValues = RedisConnection.GetDatabase().HashGetAll(_MemoryScopeKey);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetAllKeyValues(_MemoryScopeKey, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->GetAllKeyValues: {e.Message}, Trace: {e.StackTrace}");
                    return null;
                }
            }

            if (ReturnedKeyValues.Length == 0) return null;

            Tuple<string, PrimitiveType>[] Result = new Tuple<string, PrimitiveType>[ReturnedKeyValues.Length];

            int i = 0;
            foreach (HashEntry Entry in ReturnedKeyValues)
            {
                if (Entry != null)
                {
                    var AsPrimitive = ConvertRedisValueToPrimitiveType(Entry.Value);
                    if (AsPrimitive != null)
                    {
                        Result[i++] = new Tuple<string, PrimitiveType>(Entry.Name.ToString(), AsPrimitive);
                    }
                }
            }

            if (i == 0) return null;
            if (i != ReturnedKeyValues.Length)
            {
                Tuple<string, PrimitiveType>[] ShrankArray = new Tuple<string, PrimitiveType>[i];
                for (int k = 0; k < i; k++)
                {
                    ShrankArray[k] = Result[k];
                }
                Result = ShrankArray;
            }

            return Result;
        }

        /// <summary>
        ///
        /// <para>DeleteKey:</para>
        ///
        /// <para>Deletes given key within given namespace [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain] and publishes message to [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain] topic</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.DeleteKey"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool DeleteKey(
            string _MemoryScopeKey,
            string _Key,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            bool bResult;

            FailoverCheck();
            try
            {
                bResult = RedisConnection.GetDatabase().HashDelete(_MemoryScopeKey, _Key);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return DeleteKey(_MemoryScopeKey, _Key, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->DeleteKey->HashDelete: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }
            }

            if (_bPublishChange && bResult)
            {
                JObject PublishObject = new JObject
                {
                    ["operation"] = "DeleteKey",
                    ["changes"] = _Key
                };

                PubSubService?.Publish(_MemoryScopeKey, PublishObject, _ErrorMessageAction);
            }

            return bResult;
        }

        /// <summary>
        ///
        /// <para>DeleteAllKeys:</para>
        ///
        /// <para>Deletes all keys for given namespace and publishes message to [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain] topic</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.DeleteAllKeys"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool DeleteAllKeys(
            string _MemoryScopeKey,
            bool _bWaitUntilCompletion = false,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            bool bResult;

            FailoverCheck();
            try
            {
                bResult = RedisConnection.GetDatabase().KeyDelete(_MemoryScopeKey, (_bWaitUntilCompletion ? CommandFlags.None : CommandFlags.FireAndForget));
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return DeleteAllKeys(_MemoryScopeKey, _bWaitUntilCompletion, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->DeleteAllKeys: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }
            }

            if (bResult && _bPublishChange)
            {
                JObject PublishObject = new JObject
                {
                    ["operation"] = "DeleteAllKeys"
                };

                PubSubService?.Publish(_MemoryScopeKey, PublishObject, _ErrorMessageAction);
            }

            return bResult;
        }

        /// <summary>
        ///
        /// <para>GetKeys:</para>
        ///
        /// <para>Gets all keys of given workspace [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain]</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.GetKeys"/> for detailed documentation</para>
        ///
        /// </summary>
        public string[] GetKeys(
            string _MemoryScopeKey,
            Action<string> _ErrorMessageAction = null)
        {
            RedisValue[] Results;

            FailoverCheck();
            try
            {
                Results = RedisConnection.GetDatabase().HashKeys(_MemoryScopeKey);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetKeys(_MemoryScopeKey, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->GetKeys: {e.Message}, Trace: {e.StackTrace}");
                    return null;
                }
            }

            if (Results.Length == 0) return null;

            string[] ResultsAsStrings = new string[Results.Length];

            int i = 0;
            foreach (RedisValue Current in Results)
            {
                if (!Current.IsNullOrEmpty)
                {
                    ResultsAsStrings[i++] = Current.ToString();
                }
            }

            if (i == 0) return null;
            if (i != Results.Length)
            {
                string[] ShrankArray = new string[i];
                for (int k = 0; k < i; k++)
                {
                    ShrankArray[k] = ResultsAsStrings[k];
                }
                ResultsAsStrings = ShrankArray;
            }

            return ResultsAsStrings;
        }

        /// <summary>
        ///
        /// <para>GetKeysCount:</para>
        ///
        /// <para>Returns number of keys of given workspace [_MemoryScopeKey.Domain]:[_MemoryScopeKey.SubDomain]</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.GetKeysCount"/> for detailed documentation</para>
        ///
        /// </summary>
        public long GetKeysCount(
            string _MemoryScopeKey,
            Action<string> _ErrorMessageAction = null)
        {
            long Count;

            FailoverCheck();
            try
            {
                Count = RedisConnection.GetDatabase().HashLength(_MemoryScopeKey);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetKeysCount(_MemoryScopeKey, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->GetKeysCount: {e.Message}, Trace: {e.StackTrace}");
                    return 0;
                }
            }
            return Count;
        }

        /// <summary>
        ///
        /// <para>IncrementKeyValues:</para>
        ///
        /// <para>Increments given keys' by given values within given namespace and publishes message to [_Domain]:[_SubDomain] topic</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.IncrementKeyValues"/> for detailed documentation</para>
        ///
        /// </summary>
        public void IncrementKeyValues(
            string _MemoryScopeKey,
            Tuple<string, long>[] _KeysAndIncrementByValues,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            if (_KeysAndIncrementByValues == null || _KeysAndIncrementByValues.Length == 0) return;

            RedisValue[] ArgumentsAsRedisValues = new RedisValue[_KeysAndIncrementByValues.Length * 2];

            string Script = "";
            string ScriptReturn = "return ";

            int i = 0;
            foreach (var _KeyIncrBy in _KeysAndIncrementByValues)
            {
                Script += $"local r{(i + 1)}=redis.call('hincrby',KEYS[1],ARGV[{(i + 1)}],ARGV[{(i + 2)}]){Environment.NewLine}";
                ScriptReturn += (i > 0 ? ("..\" \".. r") : "r") + (i + 1);
                ArgumentsAsRedisValues[i] = _KeyIncrBy.Item1;
                ArgumentsAsRedisValues[i + 1] = _KeyIncrBy.Item2;
                i += 2;
            }
            Script += ScriptReturn;

            string ScriptEvaluationResult;

            FailoverCheck();
            try
            {
                ScriptEvaluationResult = (string)RedisConnection.GetDatabase().ScriptEvaluate(Script,
                    new RedisKey[]
                    {
                        _MemoryScopeKey
                    },
                    ArgumentsAsRedisValues);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    IncrementKeyValues(_MemoryScopeKey, _KeysAndIncrementByValues, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->IncrementKeyValues: {e.Message}, Trace: {e.StackTrace}");
                }
                return;
            }

            if (_bPublishChange)
            {
                string[] ScriptEvaluationResults = ScriptEvaluationResult.Split(' ');

                JObject ChangesObject = new JObject();

                if (ScriptEvaluationResults != null &&
                    ScriptEvaluationResults.Length == _KeysAndIncrementByValues.Length)
                {
                    int j = 0;
                    foreach (Tuple<string, long> Entry in _KeysAndIncrementByValues)
                    {
                        if (Entry != null)
                        {
                            if (int.TryParse(ScriptEvaluationResults[j], out int NewValue))
                            {
                                ChangesObject[Entry.Item1] = NewValue;
                            }
                        }
                        j++;
                    }
                }
                else
                {
                    _ErrorMessageAction?.Invoke("MemoryServiceRedis->IncrementKeyValues: redis.call returned null or result length is not equal to keys length.");
                    return;
                }

                if (ChangesObject.Count == 0) return;

                JObject PublishObject = new JObject
                {
                    ["operation"] = "SetKeyValue", //We publish the results, therefore for listeners, this action is identical with SetKeyValue
                    ["changes"] = ChangesObject
                };

                PubSubService?.Publish(_MemoryScopeKey, PublishObject, _ErrorMessageAction);
            }
        }

        /// <summary>
        ///
        /// <para>IncrementKeyByValueAndGet:</para>
        ///
        /// <para>Increments given key by given value within given namespace, publishes message to [_Domain]:[_SubDomain] topic and returns new value</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.IncrementKeyByValueAndGet"/> for detailed documentation</para>
        ///
        /// </summary>
        public long IncrementKeyByValueAndGet(
            string _MemoryScopeKey,
            Tuple<string, long> _KeyValue,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            if (_KeyValue == null) return 0;

            long Result = 0;
            
            FailoverCheck();
            try
            {
                Result = RedisConnection.GetDatabase().HashIncrement(_MemoryScopeKey, _KeyValue.Item1, _KeyValue.Item2);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return IncrementKeyByValueAndGet(_MemoryScopeKey, _KeyValue, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->IncrementKeyByValueAndGet: {e.Message}, Trace: {e.StackTrace}");
                    return 0;
                }
            }

            if (_bPublishChange)
            {
                JObject ChangesObject = new JObject
                {
                    [_KeyValue.Item1] = Result
                };

                JObject PublishObject = new JObject
                {
                    ["operation"] = "SetKeyValue", //We publish the results, therefore for listeners, this action is identical with SetKeyValue
                    ["changes"] = ChangesObject
                };

                PubSubService?.Publish(_MemoryScopeKey, PublishObject, _ErrorMessageAction);
            }
            return Result;
        }

        private bool PushToList(
            bool _bToTail,
            string _MemoryScopeKey,
            string _ListName,
            PrimitiveType[] _Values,
            bool _bPushIfListExists = false,
            Action<string> _ErrorMessageAction = null,
            bool _bAsync = false,
            bool _bPublishChange = true)
        {
            if (_Values.Length == 0) return false;

            var Transaction = RedisConnection.GetDatabase().CreateTransaction();

            if (_bPushIfListExists)
            {
                Transaction.AddCondition(Condition.KeyExists($"{_MemoryScopeKey}:{_ListName}"));
            }

            RedisValue[] AsRedisValues = new RedisValue[_Values.Length];
            JArray ChangesArray = new JArray();

            int i = 0;
            foreach (PrimitiveType _Value in _Values)
            {
                if (_Value.Type == EPrimitiveTypeEnum.Double)
                {
                    AsRedisValues[i++] = _Value.AsDouble;
                    ChangesArray.Add(_Value.AsDouble);
                }
                else if (_Value.Type == EPrimitiveTypeEnum.Integer)
                {
                    AsRedisValues[i++] = _Value.AsInteger;
                    ChangesArray.Add(_Value.AsInteger);
                }
                else if (_Value.Type == EPrimitiveTypeEnum.String)
                {
                    AsRedisValues[i++] = _Value.AsString;
                    ChangesArray.Add(_Value.AsString);
                }
                else if (_Value.Type == EPrimitiveTypeEnum.ByteArray)
                {
                    AsRedisValues[i++] = _Value.AsByteArray;
                    ChangesArray.Add(_Value.ToString());
                }
            }

            var ChangesObject = new JObject
            {
                ["List"] = _ListName,
                ["Pushed"] = ChangesArray
            };

            var PublishObject = new JObject
            {
                ["operation"] = _bToTail ? "PushToListTail" : "PushToListHead",
                ["changes"] = ChangesObject
            };

            Task CreatedTask = null;
            if (_bToTail)
            {
                CreatedTask = Transaction.ListRightPushAsync($"{_MemoryScopeKey}:{_ListName}", AsRedisValues);
            }
            else
            {
                CreatedTask = Transaction.ListLeftPushAsync($"{_MemoryScopeKey}:{_ListName}", AsRedisValues);
            }

            if (_bAsync)
            {
                FailoverCheck();
                try
                {
                    Transaction.Execute();
                    try
                    {
                        CreatedTask?.Dispose();
                    }
                    catch (Exception) { }
                }
                catch (Exception e)
                {
                    if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                    {
                        OnFailoverDetected(_ErrorMessageAction);
                        return PushToList(_bToTail, _MemoryScopeKey, _ListName, _Values, _bPushIfListExists, _ErrorMessageAction, _bAsync, _bPublishChange);
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke($"MemoryServiceRedis->PushToList: {e.Message}, Trace: {e.StackTrace}");
                    }
                }
                if (_bPublishChange)
                {
                    PubSubService?.Publish(_MemoryScopeKey, PublishObject, _ErrorMessageAction);
                }
                return true;
            }
            else
            {
                bool Committed = false;

                FailoverCheck();
                try
                {
                    Committed = Transaction.Execute();
                    try
                    {
                        CreatedTask?.Dispose();
                    }
                    catch (Exception) {}
                }
                catch (Exception e)
                {
                    if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                    {
                        OnFailoverDetected(_ErrorMessageAction);
                        return PushToList(_bToTail, _MemoryScopeKey, _ListName, _Values, _bPushIfListExists, _ErrorMessageAction, _bAsync, _bPublishChange);
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke($"MemoryServiceRedis->PushToList: {e.Message}, Trace: {e.StackTrace}");
                    }
                }

                if (Committed)
                {
                    if (_bPublishChange)
                    {
                        PubSubService?.Publish(_MemoryScopeKey, PublishObject, _ErrorMessageAction);
                    }
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        ///
        /// <para>PushToListTail:</para>
        ///
        /// <para>Pushes the value(s) to the tail of given list, returns if push succeeds (If _bAsync is true, after execution order point, always returns true).</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.PushToListTail"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool PushToListTail(
            string _MemoryScopeKey,
            string _ListName,
            PrimitiveType[] _Values,
            bool _bPushIfListExists = false, 
            Action<string> _ErrorMessageAction = null,
            bool _bAsync = false,
            bool _bPublishChange = true)
        {
            return PushToList(
                true,
                _MemoryScopeKey,
                _ListName,
                _Values,
                _bPushIfListExists,
                _ErrorMessageAction,
                _bAsync,
                _bPublishChange);
        }

        /// <summary>
        ///
        /// <para>PushToListHead:</para>
        ///
        /// <para>Pushes the value(s) to the head of given list, returns if push succeeds (If _bAsync is true, after execution order point, always returns true).</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.PushToListHead"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool PushToListHead(
            string _MemoryScopeKey,
            string _ListName,
            PrimitiveType[] _Values,
            bool _bPushIfListExists = false, 
            Action<string> _ErrorMessageAction = null,
            bool _bAsync = false,
            bool _bPublishChange = true)
        {
            return PushToList(
                false,
                _MemoryScopeKey,
                _ListName,
                _Values,
                _bPushIfListExists,
                _ErrorMessageAction,
                _bAsync,
                _bPublishChange);
        }

        private PrimitiveType PopFromList(
            bool _bFromTail,
            string _MemoryScopeKey,
            string _ListName,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            PrimitiveType PoppedAsPrimitive = null;

            RedisValue PoppedValue;

            FailoverCheck();
            try
            {
                if (_bFromTail)
                {
                    PoppedValue = RedisConnection.GetDatabase().ListRightPop($"{_MemoryScopeKey}:{_ListName}");
                }
                else
                {
                    PoppedValue = RedisConnection.GetDatabase().ListLeftPop($"{_MemoryScopeKey}:{_ListName}");
                }

                if (PoppedValue.IsNullOrEmpty)
                {
                    return null;
                }
                PoppedAsPrimitive = ConvertRedisValueToPrimitiveType(PoppedValue);
                if (PoppedAsPrimitive == null) return null;
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return PopFromList(_bFromTail, _MemoryScopeKey, _ListName, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->PopFromList: {e.Message}, Trace: {e.StackTrace}");
                    return null;
                }
            }

            if (_bPublishChange)
            {
                JObject ChangesObject = new JObject
                {
                    ["List"] = _ListName
                };

                if (PoppedAsPrimitive.Type == EPrimitiveTypeEnum.Double)
                {
                    ChangesObject["Popped"] = PoppedAsPrimitive.AsDouble;
                }
                else if (PoppedAsPrimitive.Type == EPrimitiveTypeEnum.Integer)
                {
                    ChangesObject["Popped"] = PoppedAsPrimitive.AsInteger;
                }
                else if (PoppedAsPrimitive.Type == EPrimitiveTypeEnum.String)
                {
                    ChangesObject["Popped"] = PoppedAsPrimitive.AsString;
                }
                else if (PoppedAsPrimitive.Type == EPrimitiveTypeEnum.ByteArray)
                {
                    ChangesObject["Popped"] = PoppedAsPrimitive.ToString();
                }
                else return null;

                JObject PublishObject = new JObject
                {
                    ["operation"] = _bFromTail ? "PopLastElementOfList" : "PopFirstElementOfList",
                    ["changes"] = ChangesObject
                };

                PubSubService?.Publish(_MemoryScopeKey, PublishObject, _ErrorMessageAction);
            }
            
            return PoppedAsPrimitive;
        }

        /// <summary>
        ///
        /// <para>PopLastElementOfList:</para>
        ///
        /// <para>Pops the value from the tail of given list</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.PopLastElementOfList"/> for detailed documentation</para>
        ///
        /// </summary>
        public PrimitiveType PopLastElementOfList(
            string _MemoryScopeKey,
            string _ListName, 
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            return PopFromList(
                true,
                _MemoryScopeKey,
                _ListName,
                _ErrorMessageAction,
                _bPublishChange);
        }

        /// <summary>
        ///
        /// <para>PopFirstElementOfList:</para>
        ///
        /// <para>Pops the value from the head of given list</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.PopFirstElementOfList"/> for detailed documentation</para>
        ///
        /// </summary>
        public PrimitiveType PopFirstElementOfList(
            string _MemoryScopeKey,
            string _ListName, 
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            return PopFromList(
                false,
                _MemoryScopeKey,
                _ListName,
                _ErrorMessageAction,
                _bPublishChange);
        }

        /// <summary>
        ///
        /// <para>GetAllElementsOfList:</para>
        ///
        /// <para>Gets all values from the given list</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.GetAllElementsOfList"/> for detailed documentation</para>
        ///
        /// </summary>
        public PrimitiveType[] GetAllElementsOfList(
            string _MemoryScopeKey,
            string _ListName, 
            Action<string> _ErrorMessageAction = null)
        {
            RedisValue[] ReturnedValues = null;

            FailoverCheck();
            try
            {
                ReturnedValues = RedisConnection.GetDatabase().ListRange(_MemoryScopeKey);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetAllElementsOfList(_MemoryScopeKey, _ListName, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->GetAllElementsOfList: {e.Message}, Trace: {e.StackTrace}");
                }
            }

            if (ReturnedValues == null || ReturnedValues.Length == 0) return null;

            PrimitiveType[] Result = new PrimitiveType[ReturnedValues.Length];
            int i = 0;
            foreach (RedisValue Value in ReturnedValues)
            {
                var AsPrimitive = ConvertRedisValueToPrimitiveType(Value);
                if (AsPrimitive != null)
                {
                    Result[i++] = AsPrimitive;
                }
            }

            return Result;
        }

        /// <summary>
        ///
        /// <para>EmptyList:</para>
        ///
        /// <para>Empties the list</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.EmptyList"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool EmptyList(
            string _MemoryScopeKey,
            string _ListName,
            bool _bWaitUntilCompletion = false,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            bool bResult = false;

            JObject PublishObject = new JObject
            {
                ["operation"] = "EmptyList"
            };

            FailoverCheck();
            try
            {
                bResult = RedisConnection.GetDatabase().KeyDelete($"{_MemoryScopeKey}:{_ListName}", (_bWaitUntilCompletion ? CommandFlags.None : CommandFlags.FireAndForget));
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return EmptyList(_MemoryScopeKey, _ListName, _bWaitUntilCompletion, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->EmptyList: {e.Message}, Trace: {e.StackTrace}");
                }
            }

            if (_bPublishChange)
            {
                PubSubService?.Publish(_MemoryScopeKey, PublishObject, _ErrorMessageAction);
            }

            return bResult;
        }

        /// <summary>
        ///
        /// <para>EmptyListAndSublists:</para>
        ///
        /// <para>Fetches all elements in _ListName, iterates and empties all sublists (_SublistPrefix + Returned SublistName)</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.EmptyListAndSublists"/> for detailed documentation</para>
        ///
        /// </summary>
        public void EmptyListAndSublists(
            string _MemoryScopeKey,
            string _ListName,
            string _SublistPrefix,
            bool _bWaitUntilCompletion = false,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            JObject PublishObject = new JObject
            {
                ["operation"] = "EmptyListAndSublists"
            };

            string Script = @"
                local results=redis.call('lrange',KEYS[1],0,-1)
                for _,key in ipairs(results) do 
                    redis.call('del',ARGV[1] .. key)
                end
                redis.call('del',KEYS[1])";

            FailoverCheck();
            try
            {
                RedisConnection.GetDatabase().ScriptEvaluate(Script,
                    new RedisKey[]
                    {
                        $"{_MemoryScopeKey}:{_ListName}"
                    },
                    new RedisValue[]
                    {
                        $"{_MemoryScopeKey}:{_SublistPrefix}"
                    },
                    (_bWaitUntilCompletion ? CommandFlags.None : CommandFlags.FireAndForget));
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    EmptyListAndSublists(_MemoryScopeKey, _ListName, _SublistPrefix, _bWaitUntilCompletion, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->EmptyListAndSublists: {e.Message}, Trace: {e.StackTrace}");
                }
                return;
            }

            if (_bPublishChange)
            {
                PubSubService?.Publish(_MemoryScopeKey, PublishObject, _ErrorMessageAction);
            }
        }

        /// <summary>
        ///
        /// <para>ListSize:</para>
        ///
        /// <para>Returns number of elements of the given list</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.ListSize"/> for detailed documentation</para>
        ///
        /// </summary>
        public long ListSize(
            string _MemoryScopeKey,
            string _ListName,
            Action<string> _ErrorMessageAction = null)
        {
            long Result = 0;

            FailoverCheck();
            try
            {
                Result = RedisConnection.GetDatabase().ListLength(_MemoryScopeKey);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return ListSize(_MemoryScopeKey, _ListName, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke($"MemoryServiceRedis->ListSize: {e.Message}, Trace: {e.StackTrace}");
                }
            }
            return Result;
        }

        /// <summary>
        ///
        /// <para>ListContains:</para>
        ///
        /// <para>Returns if given list contains given value or not</para>
        ///
        /// <para>Check <seealso cref="IMemoryServiceInterface.ListContains"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool ListContains(
            string _MemoryScopeKey,
            string _ListName, 
            PrimitiveType _Value, 
            Action<string> _ErrorMessageAction = null)
        {
            PrimitiveType[] Elements = GetAllElementsOfList(
                _MemoryScopeKey,
                _ListName,
                _ErrorMessageAction);

            if (Elements == null || Elements.Length == 0) return false;
            foreach (PrimitiveType Primitive in Elements)
            {
                if (Primitive.Equals(_Value))
                {
                    return true;
                }
            }
            return false;
        }

        public IPubSubServiceInterface GetPubSubService()
        {
            return PubSubService;
        }
    }
}