/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Datastore.V1;
using Grpc.Auth;
using CommonUtilities;

namespace CloudServiceUtilities.DatabaseServices
{
    public class DatabaseServiceGC : DatabaseServiceBase, IDatabaseServiceInterface
    {
        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        private readonly DatastoreClient DSClient;
        private readonly DatastoreDb DSDB;

        private readonly ServiceAccountCredential Credential;

        /// <summary>
        /// 
        /// <para>DatabaseServiceGC: Parametered Constructor for Managed Service by Google</para>
        ///
        /// <para><paramref name="_ProjectID"/>                     GC Project ID</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public DatabaseServiceGC(
            string _ProjectID,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                string ApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                string ApplicationCredentialsPlain = Environment.GetEnvironmentVariable("GOOGLE_PLAIN_CREDENTIALS");
                string ApplicationCredentialsBase64 = Environment.GetEnvironmentVariable("GOOGLE_BASE64_CREDENTIALS");
                if (ApplicationCredentials == null && ApplicationCredentialsPlain == null && ApplicationCredentialsBase64 == null)
                {
                    _ErrorMessageAction?.Invoke("DatabaseServiceGC->Constructor: GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS or GOOGLE_BASE64_CREDENTIALS) environment variable is not defined.");
                    bInitializationSucceed = false;
                }
                else
                {
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
                        Credential = GoogleCredential.FromJson(ApplicationCredentialsPlain)
                                         .CreateScoped(DatastoreClient.DefaultScopes)
                                         .UnderlyingCredential as ServiceAccountCredential;
                    }
                    else
                    {
                        using (var Stream = new FileStream(ApplicationCredentials, FileMode.Open, FileAccess.Read))
                        {
                            Credential = GoogleCredential.FromStream(Stream)
                                         .CreateScoped(DatastoreClient.DefaultScopes)
                                         .UnderlyingCredential as ServiceAccountCredential;
                        }
                    }

                    if (Credential != null)
                    {
                        DSClient = new DatastoreClientBuilder()
                        {
                            ChannelCredentials = Credential.ToChannelCredentials()

                        }.Build();
                    }

                    if (DSClient != null)
                    {
                        DSDB = DatastoreDb.Create(_ProjectID, "", DSClient);

                        bInitializationSucceed = DSDB != null;
                    }
                    else
                    {
                        bInitializationSucceed = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceGC->Constructor: {e.Message}, Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="IFileServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        /// <summary>
        /// Map that holds loaded kind definition instances
        /// </summary>
        private readonly Dictionary<string, KeyFactory> LoadedKindKeyFactories = new Dictionary<string, KeyFactory>();
        private readonly object LoadedKindKeyFactories_DictionaryLock = new object();

        /// <summary>
        /// Searches kind key factories in LoadedKindKeyFactories, if not loaded, loads, stores and returns
        /// </summary>
        private bool LoadStoreAndGetKindKeyFactory(
            string _Kind,
            out KeyFactory _ResultKeyFactory,
            Action<string> _ErrorMessageAction = null)
        {
            bool bResult = true;
            lock (LoadedKindKeyFactories_DictionaryLock)
            {
                if (!LoadedKindKeyFactories.ContainsKey(_Kind))
                {
                    try
                    {
                        _ResultKeyFactory = DSDB.CreateKeyFactory(_Kind);
                    }
                    catch (Exception e)
                    {
                        _ResultKeyFactory = null;
                        _ErrorMessageAction?.Invoke($"DatabaseServiceGC->LoadStoreAndGetKindKeyFactory: Exception: {e.Message}");
                        return false;
                    }

                    if (_ResultKeyFactory != null)
                    {
                        LoadedKindKeyFactories[_Kind] = _ResultKeyFactory;
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("DatabaseServiceGC->LoadStoreAndGetKindKeyFactory: CreateKeyFactory returned null.");
                        bResult = false;
                    }
                }
                else
                {
                    _ResultKeyFactory = LoadedKindKeyFactories[_Kind];
                }
            }
            return bResult;
        }

        private void ChangeExcludeFromIndexes(Value _Value)
        {
            switch (_Value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.ArrayValue:
                    break;
                default:
                    _Value.ExcludeFromIndexes = true;
                    break;
            }
        }
        private Entity FromJsonToEntity(KeyFactory Factory, string _KeyName, PrimitiveType _KeyValue, JObject JsonObject)
        {
            if (JsonObject != null)
            {
                var Result = FromJsonToEntity(JsonObject);
                Result.Key = Factory.CreateKey(GetFinalKeyFromNameValue(_KeyName, _KeyValue));
                return Result;
            }
            return null;
        }
        private Entity FromJsonToEntity(JObject JsonObject)
        {
            var Result = new Entity();
            foreach (var Current in JsonObject)
            {
                var Name = Current.Key;
                var Value = Current.Value;
                Result.Properties[Name] = GetDSValueFromJToken(Value);
                ChangeExcludeFromIndexes(Result.Properties[Name]);
            }
            return Result;
        }
        private Value GetDSValueFromJToken(JToken _Value)
        {
            switch (_Value.Type)
            {
                case JTokenType.Object:
                    return new Value()
                    {
                        EntityValue = FromJsonToEntity((JObject)_Value)
                    };
                case JTokenType.Array:
                    var AsArray = (JArray)_Value;
                    var AsArrayValue = new ArrayValue();
                    foreach (var Current in AsArray)
                    {
                        var CurVal = GetDSValueFromJToken(Current);
                        ChangeExcludeFromIndexes(CurVal);
                        AsArrayValue.Values.Add(CurVal);
                    }
                    return new Value()
                    {
                        ArrayValue = AsArrayValue
                    };
                case JTokenType.Integer:
                    return new Value()
                    {
                        IntegerValue = (long)_Value
                    };
                case JTokenType.Float:
                    return new Value()
                    {
                        DoubleValue = (double)_Value
                    };
                case JTokenType.Boolean:
                    return new Value()
                    {
                        BooleanValue = (bool)_Value
                    };
                case JTokenType.String:
                    return new Value()
                    {
                        StringValue = (string)_Value
                    };
                default:
                    return new Value()
                    {
                        StringValue = _Value.ToString()
                    };
            }
        }
        private JObject FromEntityToJson(Entity _Entity)
        {
            if (_Entity != null && _Entity.Properties != null)
            {
                var Result = new JObject();
                foreach (var Current in _Entity.Properties)
                {
                    Result[Current.Key] = FromValueToJsonToken(Current.Value);
                }
                return Result;
            }
            return null;
        }
        private JToken FromValueToJsonToken(Value _Value)
        {
            switch (_Value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.EntityValue:
                    return FromEntityToJson(_Value.EntityValue);
                case Value.ValueTypeOneofCase.ArrayValue:
                    var AsJArray = new JArray();
                    foreach (var ArrayVal in _Value.ArrayValue.Values)
                    {
                        AsJArray.Add(FromValueToJsonToken(ArrayVal));
                    }
                    return AsJArray;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return _Value.BooleanValue;
                case Value.ValueTypeOneofCase.IntegerValue:
                    return _Value.IntegerValue;
                case Value.ValueTypeOneofCase.DoubleValue:
                    return _Value.DoubleValue;
                case Value.ValueTypeOneofCase.StringValue:
                    return _Value.StringValue;
                default:
                    return _Value.ToString();
            }
        }

        private bool CompareJTokenWithPrimitive(JToken _Token, PrimitiveType _Primitive)
        {
            switch (_Primitive.Type)
            {
                case EPrimitiveTypeEnum.Double:
                    return _Primitive.AsDouble == (double)_Token;
                case EPrimitiveTypeEnum.Integer:
                    return _Primitive.AsInteger == (long)_Token;
                case EPrimitiveTypeEnum.ByteArray:
                    return Convert.ToBase64String(_Primitive.AsByteArray) == (string)_Token;
                default:
                    return _Primitive.AsString == (string)_Token;
            }
        }

        private string GetFinalKeyFromNameValue(string _KeyName, PrimitiveType _KeyValue)
        {
            return $"{_KeyName}:{_KeyValue}";
        }

        /// <summary>
        /// 
        /// <para>DoesItemExistWhichSatisfyOptionalCondition</para>
        /// 
        /// <para>Checks the existence of an item with given key. Also checks if condition is satisfied if given.</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.DoesItemExistWhichSatisfyOptionalCondition"/> for detailed documentation</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        public bool DoesItemExistWhichSatisfyOptionalCondition(
            string _Table, 
            string _KeyName, 
            PrimitiveType _KeyValue, 
            out bool _bExistAndConditionSatisfied, 
            DatabaseAttributeCondition _OptionalConditionExpression = null, 
            Action<string> _ErrorMessageAction = null)
        {
            _bExistAndConditionSatisfied = false;

            if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
            {
                Entity ReturnedEntity = null;
                try
                {
                    ReturnedEntity = DSDB.Lookup(Factory.CreateKey(GetFinalKeyFromNameValue(_KeyName, _KeyValue)));
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"DatabaseServiceGC->DoesItemExistWhichSatisfyOptionalCondition: Exception: {e.Message}");
                    return false;
                }

                if (ReturnedEntity != null & _OptionalConditionExpression != null)
                {
                    var EntityJson = FromEntityToJson(ReturnedEntity);
                    AddKeyToJson(EntityJson, _KeyName, _KeyValue);
                    if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                    {
                        Utility.SortJObject(
                            EntityJson,
                            Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                    }
                    else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                    {
                        Utility.ConvertRoundFloatToIntAllInJObject(EntityJson);
                    }

                    _bExistAndConditionSatisfied = ConditionCheck(EntityJson, _KeyName, _OptionalConditionExpression, _ErrorMessageAction);
                    return true;
                }

                _bExistAndConditionSatisfied = ReturnedEntity != null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// <para>GetItem</para>
        /// 
        /// <para>Gets an item from a table, if _ValuesToGet is null; will retrieve all.</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.GetItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool GetItem(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            string[] _ValuesToGet,
            out JObject _Result,
            Action<string> _ErrorMessageAction = null)
        {
            _Result = null;

            if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
            {
                Entity ReturnedEntity = null;
                try
                {
                    ReturnedEntity = DSDB.Lookup(Factory.CreateKey(GetFinalKeyFromNameValue(_KeyName, _KeyValue)));
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"DatabaseServiceGC->GetItem: Exception: {e.Message}");
                    return false;
                }

                if (ReturnedEntity != null)
                {
                    _Result = FromEntityToJson(ReturnedEntity);
                    AddKeyToJson(_Result, _KeyName, _KeyValue);
                    if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                    {
                        Utility.SortJObject(
                            _Result, 
                            Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                    }
                    else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                    {
                        Utility.ConvertRoundFloatToIntAllInJObject(_Result);
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// <para>GetItems</para>
        /// 
        /// <para>Gets items from a table</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.GetItems"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool GetItems(
            string _Table,
            string _KeyName,
            PrimitiveType[] _KeyValues,
            out List<JObject> _Result,
            Action<string> _ErrorMessageAction = null)
        {
            _Result = null;

            if (_KeyValues == null || _KeyValues.Length == 0)
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceGC->GetItems->_KeyValues is null or empty.");
                return false;
            }

            if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
            {
                IReadOnlyList<Entity> QueryResult = null;
                try
                {
                    var DatastoreKeys = new Key[_KeyValues.Length];

                    int i = 0;
                    foreach (var Value in _KeyValues)
                    {
                        DatastoreKeys[i++] = Factory.CreateKey(GetFinalKeyFromNameValue(_KeyName, Value));
                    }

                    QueryResult = DSDB.Lookup(DatastoreKeys);
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"DatabaseServiceGC->GetItems: Exception: {e.Message}");
                    return false;
                }

                if (QueryResult != null)
                {
                    _Result = new List<JObject>();

                    foreach (var Current in QueryResult)
                    {
                        if (Current != null)
                        {
                            var AsJson = FromEntityToJson(Current);
                            string KeyCombined = Current.Key.Path[0].Name;
                            string[] KeySplitted = KeyCombined.Split(':');
                            if (KeySplitted != null && KeySplitted.Length >= 2)
                            {
                                string KeyName = KeySplitted[0];
                                string KeyValue = "";

                                for (int i = 1; i < KeySplitted.Length; i++)
                                {
                                    KeyValue += KeySplitted[i];
                                }

                                AddKeyToJson(AsJson, KeyName, new PrimitiveType(KeyValue));
                                if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                                {
                                    Utility.SortJObject(
                                        AsJson,
                                        Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                                }
                                else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                                {
                                    Utility.ConvertRoundFloatToIntAllInJObject(AsJson);
                                }

                                _Result.Add(AsJson);
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private bool GetItemInTransaction(
            DatastoreTransaction _Transaction,
            KeyFactory _Factory,
            string _KeyName,
            PrimitiveType _KeyValue,
            out JObject _Result,
            Action<string> _ErrorMessageAction = null)
        {
            _Result = null;

            Entity ReturnedEntity = null;
            try
            {
                ReturnedEntity = _Transaction.Lookup(_Factory.CreateKey(GetFinalKeyFromNameValue(_KeyName, _KeyValue)));
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceGC->GetItemInTransaction: Exception: {e.Message}");
                return false;
            }

            if (ReturnedEntity != null)
            {
                _Result = FromEntityToJson(ReturnedEntity);
                AddKeyToJson(_Result, _KeyName, _KeyValue);
                if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                {
                    Utility.SortJObject(
                        _Result,
                        Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                }
                else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                {
                    Utility.ConvertRoundFloatToIntAllInJObject(_Result);
                }
            }
            return true;
        }

        /// <summary>
        /// 
        /// <para>PutItem</para>
        /// 
        /// <para>Puts an item to a table</para>
        /// <para>Note: Whether _ReturnItemBehaviour set to All or Updated, returns All</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.PutItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool PutItem(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            JObject _Item, 
            out JObject _ReturnItem, 
            EReturnItemBehaviour _ReturnItemBehaviour = EReturnItemBehaviour.DoNotReturn,
            bool _bOverrideIfExist = false,
            Action<string> _ErrorMessageAction = null)
        {
            return PutOrUpdateItem(EPutOrUpdateItemType.PutItem, _Table, _KeyName, _KeyValue, _Item, out _ReturnItem, _ReturnItemBehaviour, null, _bOverrideIfExist, _ErrorMessageAction);
        }

        /// <summary>
        /// 
        /// <para>UpdateItem</para>
        /// 
        /// <para>Updates an item in a table</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.UpdateItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool UpdateItem(
           string _Table,
           string _KeyName,
           PrimitiveType _KeyValue,
           JObject _UpdateItem,
           out JObject _ReturnItem,
           EReturnItemBehaviour _ReturnItemBehaviour = EReturnItemBehaviour.DoNotReturn,
           DatabaseAttributeCondition _ConditionExpression = null,
           Action<string> _ErrorMessageAction = null)
        {
            return PutOrUpdateItem(EPutOrUpdateItemType.UpdateItem, _Table, _KeyName, _KeyValue, _UpdateItem, out _ReturnItem, _ReturnItemBehaviour, _ConditionExpression, false, _ErrorMessageAction);
        }

        private enum EPutOrUpdateItemType
        {
            PutItem,
            UpdateItem
        }
        private bool PutOrUpdateItem(
            EPutOrUpdateItemType _PutOrUpdateItemType,
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            JObject _NewItem, 
            out JObject _ReturnItem, 
            EReturnItemBehaviour _ReturnItemBehaviour = EReturnItemBehaviour.DoNotReturn, 
            DatabaseAttributeCondition _IfUpdate_ConditionExpression = null,
            bool _IfPut_ShouldOverrideIfExist = false,
            Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            var NewItem = new JObject(_NewItem);

            if (NewItem.ContainsKey(_KeyName))
            {
                NewItem.Remove(_KeyName);
            }

            int RetryCount = 0;
            while (++RetryCount <= MAX_RETRY_NUMBER)
            {
                if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
                {
                    JObject ReturnedPreOperationObject = null;
                    using (DatastoreTransaction Transaction = DSDB.BeginTransaction())
                    {
                        if (_PutOrUpdateItemType == EPutOrUpdateItemType.UpdateItem)
                        {
                            if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out ReturnedPreOperationObject, _ErrorMessageAction))
                            {
                                _ErrorMessageAction?.Invoke("DatabaseServiceGC->PutOrUpdateItem: GetItemInTransaction failed (1).");
                                return false;
                            }

                            if (_IfUpdate_ConditionExpression != null)
                            {
                                if (!ConditionCheck(ReturnedPreOperationObject, _KeyName, _IfUpdate_ConditionExpression, _ErrorMessageAction))
                                {
                                    return false;
                                }
                            }

                            if (ReturnedPreOperationObject != null)
                            {
                                var CopyObject = new JObject(ReturnedPreOperationObject);
                                CopyObject.Merge(NewItem, new JsonMergeSettings
                                {
                                    MergeArrayHandling = MergeArrayHandling.Replace
                                });
                                NewItem = CopyObject;
                            }
                        }
                        else //if (_PutOrUpdateItemType == EPutOrUpdateItemType.PutItem)
                        {
                            if (!_IfPut_ShouldOverrideIfExist)
                            {
                                if (GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out ReturnedPreOperationObject, _ErrorMessageAction))
                                {
                                    _ErrorMessageAction?.Invoke("DatabaseServiceGC->PutOrUpdateItem: GetItemInTransaction failed (2).");
                                    return false;
                                }
                                if (ReturnedPreOperationObject != null)
                                {
                                    return false;
                                }
                            }
                        }

                        if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllOld)
                        {
                            if (ReturnedPreOperationObject == null)
                            {
                                if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out _ReturnItem, _ErrorMessageAction))
                                {
                                    _ErrorMessageAction?.Invoke("DatabaseServiceGC->PutOrUpdateItem: GetItemInTransaction failed (3).");
                                    return false;
                                }
                                if (_ReturnItem == null)
                                {
                                    _ReturnItem = new JObject();
                                }
                            }
                            else
                            {
                                _ReturnItem = ReturnedPreOperationObject;
                            }
                        }

                        var ItemAsEntity = FromJsonToEntity(Factory, _KeyName, _KeyValue, NewItem);

                        try
                        {
                            Transaction.Upsert(ItemAsEntity);
                        }
                        catch (Exception e)
                        {
                            _ErrorMessageAction?.Invoke($"DatabaseServiceGC->PutOrUpdateItem->Transaction.Upsert: Exception: {e.Message}, Trace: { e.StackTrace}");
                            return false;
                        }

                        try
                        {
                            Transaction.Commit();
                        }
                        catch (Exception e)
                        {
                            if (CheckForRetriability(e)) continue;
                            _ErrorMessageAction?.Invoke($"DatabaseServiceGC->PutOrUpdateItem->Transaction.Commit: Table: {_Table}, Key: {_KeyValue}, Exception: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }

                        if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllNew)
                        {
                            GetItem(_Table, _KeyName, _KeyValue, null, out _ReturnItem, _ErrorMessageAction);
                        }
                    }
                    return true;
                }
            }
            if (RetryCount > MAX_RETRY_NUMBER)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceGC->PutOrUpdateItem: Too much contention on these datastore entities or service unavailable; tried 5 times and exhausted. Key: { _KeyValue.AsString}");
            }
            return false;
        }

        /// <summary>
        /// 
        /// <para>AddElementsToArrayItem</para>
        /// 
        /// <para>Adds element to the array item</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.AddElementsToArrayItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool AddElementsToArrayItem(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            string _ElementName,
            PrimitiveType[] _ElementValueEntries,
            out JObject _ReturnItem,
            EReturnItemBehaviour _ReturnItemBehaviour,
            DatabaseAttributeCondition _ConditionExpression,
            Action<string> _ErrorMessageAction)
        {
            _ReturnItem = null;

            if (_ElementValueEntries == null || _ElementValueEntries.Length == 0)
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->AddElementsToArrayItem: ElementValueEntries must contain values.");
                return false;
            }
            var ExpectedType = _ElementValueEntries[0].Type;
            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (_ElementValueEntry.Type != ExpectedType)
                {
                    _ErrorMessageAction?.Invoke("DatabaseServiceAWS->AddElementsToArrayItem: ElementValueEntries must contain elements with the same type.");
                    return false;
                }
            }

            int RetryCount = 0;
            while (++RetryCount <= MAX_RETRY_NUMBER)
            {
                if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
                {
                    JObject ReturnedPreOperationObject = null;
                    using (DatastoreTransaction Transaction = DSDB.BeginTransaction())
                    {
                        if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out ReturnedPreOperationObject, _ErrorMessageAction))
                        {
                            _ErrorMessageAction?.Invoke("DatabaseServiceGC->AddElementsToArrayItem: GetItemInTransaction failed.");
                            return false;
                        }

                        JArray ItemAsArray = null;

                        if (ReturnedPreOperationObject != null && ReturnedPreOperationObject.ContainsKey(_ElementName))
                        {
                            if (ReturnedPreOperationObject[_ElementName].Type != JTokenType.Array)
                            {
                                _ErrorMessageAction?.Invoke("DatabaseServiceGC->AddElementsToArrayItem: Item is not an array.");
                                return false;
                            }
                            ItemAsArray = (JArray)ReturnedPreOperationObject[_ElementName];
                        }

                        if (!ConditionCheck(ReturnedPreOperationObject, _KeyName, _ConditionExpression, _ErrorMessageAction))
                        {
                            return false;
                        }

                        if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllOld)
                        {
                            if (ReturnedPreOperationObject != null)
                            {
                                _ReturnItem = new JObject(ReturnedPreOperationObject);
                            }
                            else
                            {
                                _ReturnItem = new JObject();
                            }
                        }

                        if (ItemAsArray == null)
                        {
                            ItemAsArray = new JArray();
                            foreach (var _ElementValueEntry in _ElementValueEntries)
                            {
                                ItemAsArray.Add(FromPrimitiveTypeToJToken(_ElementValueEntry));
                            }
                        }
                        else
                        {
                            foreach (var _ElementValueEntry in _ElementValueEntries)
                            {
                                ItemAsArray.Add(FromPrimitiveTypeToJToken(_ElementValueEntry));
                            }
                        }

                        if (ReturnedPreOperationObject == null)
                        {
                            ReturnedPreOperationObject = new JObject()
                            {
                                [_ElementName] = ItemAsArray
                            };
                        }
                        else
                        {
                            ReturnedPreOperationObject[_ElementName] = ItemAsArray;
                        }

                        //Key will be recreated by FromJsonToEntity
                        if (ReturnedPreOperationObject.ContainsKey(_KeyName))
                        {
                            ReturnedPreOperationObject.Remove(_KeyName);
                        }

                        var ItemAsEntity = FromJsonToEntity(Factory, _KeyName, _KeyValue, ReturnedPreOperationObject);
                        try
                        {
                            Transaction.Upsert(ItemAsEntity);
                        }
                        catch (Exception e)
                        {
                            _ErrorMessageAction?.Invoke($"DatabaseServiceGC->AddElementsToArrayItem: Exception: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }

                        try
                        {
                            Transaction.Commit();
                        }
                        catch (Exception e)
                        {
                            if (CheckForRetriability(e)) continue;
                            _ErrorMessageAction?.Invoke($"DatabaseServiceGC->AddElementsToArrayItem: Exception: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }

                        if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllNew)
                        {
                            GetItem(_Table, _KeyName, _KeyValue, null, out _ReturnItem, _ErrorMessageAction);
                        }
                    }
                    return true;
                }
            }
            if (RetryCount > MAX_RETRY_NUMBER)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceGC->AddElementsToArrayItem: Too much contention on these datastore entities or service unavailable; tried 5 times and exhausted. Key: { _KeyValue.AsString}");
            }
            return false;
        }

        /// <summary>
        /// 
        /// <para>RemoveElementsFromArrayItem</para>
        /// 
        /// <para>Removes element from the array item</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.RemoveElementsFromArrayItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool RemoveElementsFromArrayItem(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            string _ElementName,
            PrimitiveType[] _ElementValueEntries,
            out JObject _ReturnItem,
            EReturnItemBehaviour _ReturnItemBehaviour,
            DatabaseAttributeCondition _ConditionExpression,
            Action<string> _ErrorMessageAction)
        {
            _ReturnItem = null;

            if (_ElementValueEntries == null || _ElementValueEntries.Length == 0)
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->AddElementsToArrayItem: ElementValueEntries must contain values.");
                return false;
            }
            var ExpectedType = _ElementValueEntries[0].Type;
            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (_ElementValueEntry.Type != ExpectedType)
                {
                    _ErrorMessageAction?.Invoke("DatabaseServiceAWS->AddElementsToArrayItem: ElementValueEntries must contain elements with the same type.");
                    return false;
                }
            }

            int RetryCount = 0;
            while (++RetryCount <= MAX_RETRY_NUMBER)
            {
                if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
                {
                    JObject ReturnedPreOperationObject = null;
                    using (DatastoreTransaction Transaction = DSDB.BeginTransaction())
                    {
                        if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out ReturnedPreOperationObject, _ErrorMessageAction))
                        {
                            _ErrorMessageAction?.Invoke("DatabaseServiceGC->RemoveElementsFromArrayItem: GetItemInTransaction failed.");
                            return false;
                        }

                        if (ReturnedPreOperationObject == null)
                        {
                            //Does not exist
                            return true;
                        }

                        JArray ItemAsArray = null;

                        if (ReturnedPreOperationObject.ContainsKey(_ElementName))
                        {
                            if (ReturnedPreOperationObject[_ElementName].Type != JTokenType.Array)
                            {
                                _ErrorMessageAction?.Invoke("DatabaseServiceGC->RemoveElementsFromArrayItem: Item is not an array.");
                                return false;
                            }
                            ItemAsArray = (JArray)ReturnedPreOperationObject[_ElementName];
                            if (ItemAsArray == null)
                            {
                                //Does not exist as an array
                                return true;
                            }
                        }

                        if (!ConditionCheck(ReturnedPreOperationObject, _KeyName, _ConditionExpression, _ErrorMessageAction))
                        {
                            return false;
                        }

                        if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllOld)
                        {
                            if (ReturnedPreOperationObject != null)
                            {
                                _ReturnItem = new JObject(ReturnedPreOperationObject);
                            }
                        }

                        var NewArray = new JArray();
                        foreach (var CurToken in ItemAsArray)
                        {
                            bool bFound = false;
                            foreach (var _ElementValueEntry in _ElementValueEntries)
                            {
                                if (CompareJTokenWithPrimitive(CurToken, _ElementValueEntry))
                                {
                                    bFound = true;
                                    break;
                                }
                            }
                            if (!bFound)
                            {
                                NewArray.Add(CurToken);
                            }
                        }
                        ItemAsArray = NewArray;
                        ReturnedPreOperationObject[_ElementName] = ItemAsArray;

                        //Key will be recreated by FromJsonToEntity
                        if (ReturnedPreOperationObject.ContainsKey(_KeyName))
                        {
                            ReturnedPreOperationObject.Remove(_KeyName);
                        }

                        var ItemAsEntity = FromJsonToEntity(Factory, _KeyName, _KeyValue, ReturnedPreOperationObject);
                        try
                        {
                            Transaction.Upsert(ItemAsEntity);
                        }
                        catch (Exception e)
                        {
                            _ErrorMessageAction?.Invoke($"DatabaseServiceGC->RemoveElementsFromArrayItem: Exception: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }

                        try
                        {
                            Transaction.Commit();
                        }
                        catch (Exception e)
                        {
                            if (CheckForRetriability(e)) continue;
                            _ErrorMessageAction?.Invoke($"DatabaseServiceGC->RemoveElementsFromArrayItem: Exception: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }

                        if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllNew)
                        {
                            GetItem(_Table, _KeyName, _KeyValue, null, out _ReturnItem, _ErrorMessageAction);
                        }
                    }
                    return true;
                }
            }
            if (RetryCount > MAX_RETRY_NUMBER)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceGC->RemoveElementsFromArrayItem: Too much contention on these datastore entities or service unavailable; tried 5 times and exhausted. Key: { _KeyValue.AsString}");
            }
            return false;
        }

        /// <summary>
        /// 
        /// <para>IncrementOrDecrementItemValue</para>
        /// 
        /// <para>Updates an item in a table, if item does not exist, creates a new one with only increment/decrement value</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.IncrementOrDecrementItemValue"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool IncrementOrDecrementItemValue(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            out double _NewValue,
            string _ValueAttribute,
            double _IncrementOrDecrementBy,
            bool _bDecrement = false,
            DatabaseAttributeCondition _ConditionExpression = null,
            Action<string> _ErrorMessageAction = null)
        {
            _NewValue = _IncrementOrDecrementBy;

            int RetryCount = 0;
            while (++RetryCount <= MAX_RETRY_NUMBER)
            {
                if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
                {
                    using (DatastoreTransaction Transaction = DSDB.BeginTransaction())
                    {
                        if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out JObject ReturnItem, _ErrorMessageAction))
                        {
                            _ErrorMessageAction?.Invoke("DatabaseServiceGC->IncrementOrDecrementItemValue: GetItemInTransaction failed.");
                            return false;
                        }

                        if (!ConditionCheck(ReturnItem, _KeyName, _ConditionExpression, _ErrorMessageAction))
                        {
                            return false;
                        }

                        if (ReturnItem != null
                            && ReturnItem.ContainsKey(_ValueAttribute))
                        {
                            _NewValue = (double)ReturnItem[_ValueAttribute];
                            if (_bDecrement)
                            {
                                _NewValue -= _IncrementOrDecrementBy;
                            }
                            else
                            {
                                _NewValue += _IncrementOrDecrementBy;
                            }
                            ReturnItem[_ValueAttribute] = _NewValue;
                        }
                        else
                        {
                            if (ReturnItem == null)
                            {
                                ReturnItem = new JObject()
                                {
                                    [_ValueAttribute] = _IncrementOrDecrementBy
                                };
                            }
                            else
                            {
                                ReturnItem[_ValueAttribute] = _IncrementOrDecrementBy;
                            }
                        }

                        if (ReturnItem.ContainsKey(_KeyName))
                        {
                            ReturnItem.Remove(_KeyName);
                        }

                        try
                        {
                            Transaction.Upsert(FromJsonToEntity(Factory, _KeyName, _KeyValue, ReturnItem));
                        }
                        catch (Exception e)
                        {
                            _ErrorMessageAction?.Invoke($"DatabaseServiceGC->IncrementOrDecrementItemValue: Exception: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }

                        try
                        {
                            Transaction.Commit();
                        }
                        catch (Exception e)
                        {
                            if (CheckForRetriability(e)) continue;
                            _ErrorMessageAction?.Invoke($"DatabaseServiceGC->IncrementOrDecrementItemValue: Exception: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }
                    }
                    return true;
                }
            }
            if (RetryCount > MAX_RETRY_NUMBER)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceGC->IncrementOrDecrementItemValue: Too much contention on these datastore entities or service unavailable; tried 5 times and exhausted. Key: { _KeyValue.AsString}");
            }
            return false;
        }

        /// <summary>
        /// 
        /// <para>DeleteItem</para>
        /// 
        /// <para>Deletes an item from a table</para>
        /// <para>Note: Whether _ReturnItemBehaviour set to All or Updated, returns All</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.DeleteItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool DeleteItem(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            out JObject _ReturnItem,
            EReturnItemBehaviour _ReturnItemBehaviour = EReturnItemBehaviour.DoNotReturn,
            DatabaseAttributeCondition _ConditionExpression = null,
            Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            int RetryCount = 0;
            while (++RetryCount <= MAX_RETRY_NUMBER)
            {
                if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
                {
                    using (DatastoreTransaction Transaction = DSDB.BeginTransaction())
                    {
                        if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllOld || _ConditionExpression != null)
                        {
                            if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out _ReturnItem, _ErrorMessageAction))
                            {
                                _ErrorMessageAction?.Invoke("DatabaseServiceGC->DeleteItem: GetItemInTransaction failed.");
                                return false;
                            }

                            if (!ConditionCheck(_ReturnItem, _KeyName, _ConditionExpression, _ErrorMessageAction))
                            {
                                return false;
                            }
                        }

                        try
                        {
                            Transaction.Delete(Factory.CreateKey(GetFinalKeyFromNameValue(_KeyName, _KeyValue)));
                        }
                        catch (Exception e)
                        {
                            _ErrorMessageAction?.Invoke($"DatabaseServiceGC->DeleteItem: Exception: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }

                        try
                        {
                            Transaction.Commit();
                        }
                        catch (Exception e)
                        {
                            if (CheckForRetriability(e)) continue;
                            _ErrorMessageAction?.Invoke($"DatabaseServiceGC->DeleteItem: Exception: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }
                    }
                    return true;
                }
            }
            if (RetryCount > MAX_RETRY_NUMBER)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceGC->DeleteItem: Too much contention on these datastore entities or service unavailable; tried 5 times and exhausted. Key: { _KeyValue.AsString}");
            }
            return false;
        }

        /// <summary>
        /// 
        /// <para>ScanTable</para>
        /// 
        /// <para>Scans the table for attribute specified by _Key</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.ScanTable"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool ScanTable(
            string _Table,
            string[] _PossibleKeyNames,
            out List<JObject> _ReturnItem, 
            Action<string> _ErrorMessageAction = null)
        {
            return Internal_ScanTable(
                _Table,
                (JObject _Param) => { return true; },
                out _ReturnItem,
                _ErrorMessageAction);
        }

        /// <summary>
        /// 
        /// <para>ScanTable_Paginated - NOT IMPLEMENTED YET</para>
        /// 
        /// <para>Scans the table for attribute specified by _Key with pagination</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.ScanTable_Paginated"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool ScanTable_Paginated(
            string _Table,
            string[] _PossibleKeyNames,
            int _PageNumber,
            int _PageSize,
            out List<JObject> _ReturnItem,
            bool _RetrieveTotalElementsFound,
            out long _TotalElementFound,
            Action<string> _ErrorMessageAction = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// <para>ScanTableFilterBy</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.ScanTable"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool ScanTableFilterBy(
            string _Table,
            string[] _PossibleKeyNames,
            DatabaseAttributeCondition _FilterBy,
            out List<JObject> _ReturnItem,
            Action<string> _ErrorMessageAction = null)
        {
            if (_FilterBy == null)
            {
                return ScanTable(_Table, _PossibleKeyNames, out _ReturnItem, _ErrorMessageAction);
            }

            if (!Internal_ScanTable(
                _Table,
                (JObject _Param) => 
                {
                    var bSucceeded = false;
                    foreach (var PossibleKeyName in _PossibleKeyNames)
                    {
                        if (ConditionCheck(_Param, PossibleKeyName, _FilterBy, _ErrorMessageAction))
                        {
                            bSucceeded = true; 
                            break;
                        }
                    }
                    return bSucceeded;
                },
                out _ReturnItem,
                _ErrorMessageAction))
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceGC->ScanTableFilterBy: ScanTable operation has failed.");
                _ReturnItem = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// <para>ScanTableFilterBy_Paginated - NOT IMPLEMENTED YET</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.ScanTableFilterBy_Paginated"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool ScanTableFilterBy_Paginated(
            string _Table,
            string[] _PossibleKeyNames,
            DatabaseAttributeCondition _FilterBy,
            int _PageNumber,
            int _PageSize,
            out List<JObject> _ReturnItem,
            bool _RetrieveTotalElementsFound,
            out long _TotalElementFound,
            Action<string> _ErrorMessageAction = null)
        {
            throw new NotImplementedException();
        }

        private bool Internal_ScanTable(string _Table, Func<JObject, bool> _FilterCallback, out List<JObject> _ReturnItem, Action<string> _ErrorMessageAction = null)
        {
            DatastoreQueryResults QueryResult = null;
            do
            {
                try
                {
                    var NewQuery = new Query(_Table);
                    if (QueryResult != null)
                    {
                        NewQuery.StartCursor = QueryResult.EndCursor;
                    }
                    QueryResult = DSDB.RunQuery(NewQuery);
                }
                catch (Exception e)
                {
                    _ReturnItem = null;
                    _ErrorMessageAction?.Invoke($"DatabaseServiceGC->ScanTable: Exception: {e.Message}");
                    return false;
                }
                _ReturnItem = new List<JObject>();

                if (QueryResult != null)
                {
                    foreach (var Current in QueryResult.Entities)
                    {
                        if (Current != null)
                        {
                            var AsJson = FromEntityToJson(Current);
                            string KeyCombined = Current.Key.Path[0].Name;
                            string[] KeySplitted = KeyCombined.Split(':');
                            if (KeySplitted != null && KeySplitted.Length >= 2)
                            {
                                string KeyName = KeySplitted[0];
                                string KeyValue = "";

                                for (int i = 1; i < KeySplitted.Length; i++)
                                {
                                    KeyValue += KeySplitted[i];
                                }

                                AddKeyToJson(AsJson, KeyName, new PrimitiveType(KeyValue));
                                if (_FilterCallback(AsJson))
                                {
                                    if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                                    {
                                        Utility.SortJObject(
                                            AsJson,
                                            Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                                    }
                                    else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                                    {
                                        Utility.ConvertRoundFloatToIntAllInJObject(AsJson);
                                    }

                                    _ReturnItem.Add(AsJson);
                                }
                            }
                        }
                    }
                }
            }
            while (QueryResult != null && QueryResult.MoreResults != QueryResultBatch.Types.MoreResultsType.NoMoreResults);
            
            return true;
        }

        private bool ConditionCheck(
            JObject _JObjectToCheck,
            string _KeyName,
            DatabaseAttributeCondition _ConditionExpression,
            Action<string> _ErrorMessageAction = null)
        {
            if (_ConditionExpression == null) return true;
            if (_JObjectToCheck == null) return true;

            var BuiltCondition = _ConditionExpression.GetBuiltCondition();

            if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeExists
                || _ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeNotExist)
            {
                bool bConditionSatisfied = false;

                if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeNotExist)
                {
                    bConditionSatisfied = true;

                    if (BuiltCondition.Item1 == _KeyName || _JObjectToCheck.ContainsKey(BuiltCondition.Item1))
                    {
                        bConditionSatisfied = false;
                    }
                }
                else //if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeExists)
                {
                    if (BuiltCondition.Item1 == _KeyName || _JObjectToCheck.ContainsKey(BuiltCondition.Item1))
                    {
                        bConditionSatisfied = true;
                    }
                }

                if (!bConditionSatisfied)
                {
                    return false;
                }
            }
            else
            {
                bool bConditionSatisfied = false;
                if (_JObjectToCheck.ContainsKey(BuiltCondition.Item1))
                {
                    switch (BuiltCondition.Item2.Item2.Type)
                    {
                        case EPrimitiveTypeEnum.Double:
                            if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeEquals)
                            {
                                bConditionSatisfied = (double)_JObjectToCheck[BuiltCondition.Item1] == BuiltCondition.Item2.Item2.AsDouble;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeNotEquals)
                            {
                                bConditionSatisfied = (double)_JObjectToCheck[BuiltCondition.Item1] != BuiltCondition.Item2.Item2.AsDouble;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeGreater)
                            {
                                bConditionSatisfied = (double)_JObjectToCheck[BuiltCondition.Item1] > BuiltCondition.Item2.Item2.AsDouble;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeGreaterOrEqual)
                            {
                                bConditionSatisfied = (double)_JObjectToCheck[BuiltCondition.Item1] >= BuiltCondition.Item2.Item2.AsDouble;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeLess)
                            {
                                bConditionSatisfied = (double)_JObjectToCheck[BuiltCondition.Item1] < BuiltCondition.Item2.Item2.AsDouble;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.ArrayElementExist
                                || _ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.ArrayElementNotExist)
                            {
                                var AsArr = (JArray)_JObjectToCheck[BuiltCondition.Item1];
                                bool bExist = false;
                                foreach (var Tok in AsArr)
                                {
                                    if (Tok.Type == JTokenType.Float)
                                    {
                                        if (((double)Tok) == BuiltCondition.Item2.Item2.AsDouble)
                                        {
                                            bExist = true;
                                            break;
                                        }
                                    }
                                }
                                bConditionSatisfied = _ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.ArrayElementExist ? bExist : !bExist;
                            }
                            else
                            {
                                bConditionSatisfied = (double)_JObjectToCheck[BuiltCondition.Item1] <= BuiltCondition.Item2.Item2.AsDouble;
                            }
                            break;
                        case EPrimitiveTypeEnum.Integer:
                            if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeEquals)
                            {
                                bConditionSatisfied = (long)_JObjectToCheck[BuiltCondition.Item1] == BuiltCondition.Item2.Item2.AsInteger;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeNotEquals)
                            {
                                bConditionSatisfied = (long)_JObjectToCheck[BuiltCondition.Item1] != BuiltCondition.Item2.Item2.AsInteger;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeGreater)
                            {
                                bConditionSatisfied = (long)_JObjectToCheck[BuiltCondition.Item1] > BuiltCondition.Item2.Item2.AsInteger;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeGreaterOrEqual)
                            {
                                bConditionSatisfied = (long)_JObjectToCheck[BuiltCondition.Item1] >= BuiltCondition.Item2.Item2.AsInteger;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeLess)
                            {
                                bConditionSatisfied = (long)_JObjectToCheck[BuiltCondition.Item1] < BuiltCondition.Item2.Item2.AsInteger;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.ArrayElementExist
                                || _ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.ArrayElementNotExist)
                            {
                                var AsArr = (JArray)_JObjectToCheck[BuiltCondition.Item1];
                                bool bExist = false;
                                foreach (var Tok in AsArr)
                                {
                                    if (Tok.Type == JTokenType.Integer)
                                    {
                                        if (((int)Tok) == BuiltCondition.Item2.Item2.AsInteger)
                                        {
                                            bExist = true;
                                            break;
                                        }
                                    }
                                }
                                bConditionSatisfied = _ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.ArrayElementExist ? bExist : !bExist;
                            }
                            else
                            {
                                bConditionSatisfied = (long)_JObjectToCheck[BuiltCondition.Item1] <= BuiltCondition.Item2.Item2.AsInteger;
                            }
                            break;
                        case EPrimitiveTypeEnum.ByteArray:
                            var First = (string)_JObjectToCheck[BuiltCondition.Item1];
                            var Second = Convert.ToBase64String(BuiltCondition.Item2.Item2.AsByteArray);

                            if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeEquals)
                            {
                                bConditionSatisfied = First == Second;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeNotEquals)
                            {
                                bConditionSatisfied = First != Second;
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("DatabaseServiceGC->ConditionCheck: Invalid condition expression.");
                                return false;
                            }
                            break;
                        default:
                            First = (string)_JObjectToCheck[BuiltCondition.Item1];
                            Second = BuiltCondition.Item2.Item2.AsString;

                            if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeEquals)
                            {
                                bConditionSatisfied = First == Second;
                            }
                            else if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.AttributeNotEquals)
                            {
                                bConditionSatisfied = First != Second;
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("DatabaseServiceGC->ConditionCheck: Invalid condition expression.");
                                return false;
                            }
                            break;
                    }
                }

                if (!bConditionSatisfied)
                {
                    return false;
                }
            }
            
            return true;
        }

        private static bool SearchJsonText(JToken _Token, string _SearchText)
        {
            if (_Token.Type == JTokenType.String)
            {
                return ((string)_Token).Contains(_SearchText, StringComparison.OrdinalIgnoreCase);
            }
            else if (_Token.Type == JTokenType.Object || _Token.Type == JTokenType.Array)
            {
                foreach (var Child in _Token.Children())
                {
                    if (SearchJsonText(Child, _SearchText))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private class AttributeEqualsConditionDatastore : DatabaseAttributeCondition
        {
            public AttributeEqualsConditionDatastore(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeEquals)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, PrimitiveType>("Value", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeEqualsCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeEqualsConditionDatastore(Attribute, Value);
        }

        private class AttributeNotEqualsConditionDatastore : DatabaseAttributeCondition
        {
            public AttributeNotEqualsConditionDatastore(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeNotEquals)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, PrimitiveType>("Value", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeNotEqualsCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeNotEqualsConditionDatastore(Attribute, Value);
        }

        private class AttributeGreaterConditionDatastore : DatabaseAttributeCondition
        {
            public AttributeGreaterConditionDatastore(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeGreater)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, PrimitiveType>("Value", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeGreaterCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeGreaterConditionDatastore(Attribute, Value);
        }

        private class AttributeGreaterOrEqualConditionDatastore : DatabaseAttributeCondition
        {
            public AttributeGreaterOrEqualConditionDatastore(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeGreaterOrEqual)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, PrimitiveType>("Value", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeGreaterOrEqualCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeGreaterOrEqualConditionDatastore(Attribute, Value);
        }

        private class AttributeLessConditionDatastore : DatabaseAttributeCondition
        {
            public AttributeLessConditionDatastore(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeLess)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, PrimitiveType>("Value", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeLessCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeLessConditionDatastore(Attribute, Value);
        }

        private class AttributeLessOrEqualConditionDatastore : DatabaseAttributeCondition
        {
            public AttributeLessOrEqualConditionDatastore(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeLessOrEqual)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, PrimitiveType>("Value", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeLessOrEqualCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeLessOrEqualConditionDatastore(Attribute, Value);
        }

        private class AttributeExistsConditionDatastore : DatabaseAttributeCondition
        {
            public AttributeExistsConditionDatastore(string Attribute) : base(EDatabaseAttributeConditionType.AttributeExists)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>(Attribute, null);
            }
        }
        public DatabaseAttributeCondition BuildAttributeExistsCondition(string Attribute)
        {
            return new AttributeExistsConditionDatastore(Attribute);
        }

        private class AttributeNotExistConditionDatastore : DatabaseAttributeCondition
        {
            public AttributeNotExistConditionDatastore(string Attribute) : base(EDatabaseAttributeConditionType.AttributeNotExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>(Attribute, null);
            }
        }
        public DatabaseAttributeCondition BuildAttributeNotExistCondition(string Attribute)
        {
            return new AttributeNotExistConditionDatastore(Attribute);
        }

        private class ArrayElementExistConditionDatastore : DatabaseAttributeCondition
        {
            public ArrayElementExistConditionDatastore(string Attribute, PrimitiveType ArrayElement) : base(EDatabaseAttributeConditionType.ArrayElementExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, PrimitiveType>("Value", ArrayElement)
                );
            }
        }
        public DatabaseAttributeCondition BuildArrayElementExistCondition(string Attribute, PrimitiveType ArrayElement)
        {
            return new ArrayElementExistConditionDatastore(Attribute, ArrayElement);
        }

        private class ArrayElementNotExistConditionDatastore : DatabaseAttributeCondition
        {
            public ArrayElementNotExistConditionDatastore(string Attribute, PrimitiveType ArrayElement) : base(EDatabaseAttributeConditionType.ArrayElementNotExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, PrimitiveType>("Value", ArrayElement)
                );
            }
        }
        public DatabaseAttributeCondition BuildArrayElementNotExistCondition(string Attribute, PrimitiveType ArrayElement)
        {
            return new ArrayElementNotExistConditionDatastore(Attribute, ArrayElement);
        }

        private const int MAX_RETRY_NUMBER = 5;
        private bool CheckForRetriability(Exception e)
        {
            if (e is Grpc.Core.RpcException)
            {
                var AsRpcException = e as Grpc.Core.RpcException;
                if (AsRpcException.StatusCode == Grpc.Core.StatusCode.Aborted 
                    || AsRpcException.StatusCode == Grpc.Core.StatusCode.Unavailable)
                {
                    Thread.Sleep(5000);
                    return true;
                }
            }
            return false;
        }
    }
}