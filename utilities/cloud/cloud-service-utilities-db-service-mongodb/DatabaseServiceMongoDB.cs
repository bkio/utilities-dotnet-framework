/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using CommonUtilities;

namespace CloudServiceUtilities.DatabaseServices
{
    public class DatabaseServiceMongoDB : DatabaseServiceBase, IDatabaseServiceInterface
    {
        /// <summary>
        /// <para>Holds initialization success</para>
        /// </summary>
        private readonly bool bInitializationSucceed;

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

        private bool TableExists(string _TableName)
        {
            var filter = new BsonDocument("name", _TableName);
            var options = new ListCollectionNamesOptions { Filter = filter };

            return MongoDB.ListCollectionNames(options).Any();
        }

        /// <summary>
        /// 
        /// <para>TryCreateTable</para>
        /// 
        /// <para>If given table (collection) does not exist in the database, it will create a new table (collection)</para>
        /// 
        /// 
        /// </summary>
        private bool TryCreateTable(string _TableName, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                if (!TableExists(_TableName))
                {
                    MongoDB.CreateCollection(_TableName);
                }
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceMongoDB->TryCreateTable: Given table(collection) couldn't create. Error: {ex.Message} \n Trace: {ex.StackTrace}");
                return false;
            }
        }

        private readonly IMongoDatabase MongoDB = null;

        private readonly Dictionary<string, IMongoCollection<BsonDocument>> TableMap = new Dictionary<string, IMongoCollection<BsonDocument>>();
        private readonly object TableMap_DictionaryLock = new object();
        private IMongoCollection<BsonDocument> GetTable(string _TableName, Action<string> _ErrorMessageAction = null)
        {
            lock (TableMap_DictionaryLock)
            {
                if (!TableMap.ContainsKey(_TableName))
                {
                    if (!TryCreateTable(_TableName, _ErrorMessageAction))
                    {
                        return null;
                    }

                    var TableObj = MongoDB.GetCollection<BsonDocument>(_TableName);
                    if (TableObj != null)
                    {
                        TableMap.Add(_TableName, TableObj);
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("DatabaseServiceMongoDB->GetTable: Given table(collection) does not exist.");
                        return null;
                    }
                }
            }
            return TableMap[_TableName];
        }

        /// <summary>
        /// 
        /// <para>DatabaseServiceGC: Parametered Constructor for Managed Service by Google</para>
        ///
        /// <para><paramref name="_MongoHost"/>                     MongoDB Host</para>
        /// <para><paramref name="_MongoPort"/>                     MongoDB Port</para>
        /// <para><paramref name="_MongoDatabase"/>                 MongoDB Database Name</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public DatabaseServiceMongoDB(
            string _MongoHost,
            int _MongoPort,
            string _MongoDatabase,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                var Client = new MongoClient($"mongodb://{_MongoHost}:{_MongoPort}");
                MongoDB = Client.GetDatabase(_MongoDatabase);
                bInitializationSucceed = MongoDB != null;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceMongoDB->Constructor: {e.Message} \n Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        public DatabaseServiceMongoDB(
            string _ConnectionString,
            string _MongoDatabase,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                var Client = new MongoClient(_ConnectionString);
                MongoDB = Client.GetDatabase(_MongoDatabase);
                bInitializationSucceed = MongoDB != null;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceMongoDB->Constructor: {e.Message} \n Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        public DatabaseServiceMongoDB(
            string _MongoClientConfigJson,
            string _MongoPassword,
            string _MongoDatabase,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                var _ClientConfigString = _MongoClientConfigJson;
                // Parse the Client Config Json if it's a base64 encoded (for running on local environment with launchSettings.json) 
                Span<byte> buffer = new Span<byte>(new byte[_ClientConfigString.Length]);
                if(Convert.TryFromBase64String(_ClientConfigString, buffer, out int bytesParsed))
                {
                    if(bytesParsed > 0)
                    {
                        _ClientConfigString = Encoding.UTF8.GetString(buffer);
                    }
                }

                var _ClientConfigJObject = JObject.Parse(_ClientConfigString);
                
                var _HostTokens = _ClientConfigJObject.SelectTokens("$...hostname");
                var _Hosts = new List<string>();
                foreach (var item in _HostTokens)
                {
                    _Hosts.Add(item.ToObject<string>());
                }
                
                var _PortTokens = _ClientConfigJObject.SelectTokens("$....port");
                var _Ports = new List<int>();
                foreach (var item in _PortTokens)
                {
                    _Ports.Add(item.ToObject<int>());
                }

                var _ReplicaSetName = _ClientConfigJObject.SelectToken("replicaSets[0]._id").ToObject<string>();
                var _DatabaseName = _ClientConfigJObject.SelectToken("auth.usersWanted[0].db").ToObject<string>();
                var _UserName = _ClientConfigJObject.SelectToken("auth.usersWanted[0].user").ToObject<string>();
                var _AuthMechnasim = _ClientConfigJObject.SelectToken("auth.autoAuthMechanism").ToObject<string>();
                int _MongoDBPort = 27017;

                var _ServerList = new List<MongoServerAddress>();
                for (int i = 0; i < _Hosts.Count; i++)
                {
                    if (i < _Ports.Count)
                        _MongoDBPort = _Ports[i];

                    _ServerList.Add(new MongoServerAddress(_Hosts[i], _MongoDBPort));
                }

                MongoInternalIdentity _InternalIdentity = new MongoInternalIdentity(_DatabaseName, _UserName);
                PasswordEvidence _PasswordEvidence = new PasswordEvidence(_MongoPassword);
                MongoCredential _MongoCredential = new MongoCredential(_AuthMechnasim, _InternalIdentity, _PasswordEvidence);

                var Client = new MongoClient(new MongoClientSettings
                {
                    Servers = _ServerList.ToArray(),
                    ConnectionMode = ConnectionMode.ReplicaSet,
                    ReplicaSetName = _ReplicaSetName,
                    Credential = _MongoCredential
                });
                MongoDB = Client.GetDatabase(_MongoDatabase);
                bInitializationSucceed = MongoDB != null;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceMongoDB->Constructor: {e.Message} \n Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        private FilterDefinition<BsonDocument> BuildEqFilter(string _KeyName, PrimitiveType _KeyValue)
        {
            switch (_KeyValue.Type)
            {
                case EPrimitiveTypeEnum.Double:
                    return Builders<BsonDocument>.Filter.Eq(_KeyName, _KeyValue.AsDouble);
                case EPrimitiveTypeEnum.Integer:
                    return Builders<BsonDocument>.Filter.Eq(_KeyName, _KeyValue.AsInteger);
                case EPrimitiveTypeEnum.ByteArray:
                    return Builders<BsonDocument>.Filter.Eq(_KeyName, _KeyValue.AsByteArray);
                case EPrimitiveTypeEnum.String:
                    return Builders<BsonDocument>.Filter.Eq(_KeyName, _KeyValue.AsString);
            }
            return Builders<BsonDocument>.Filter.Eq(_KeyName, _KeyValue.ToString());
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

            var Table = GetTable(_Table);
            if (Table == null) return false;

            try
            {
                var Filter = BuildEqFilter(_KeyName, _KeyValue);
                var Document = FindOne(Table, Filter);

                if (Document != null)
                {
                    _Result = BsonToJObject(Document);
                    AddKeyToJson(_Result, _KeyName, _KeyValue);
                }
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceMongoDB->GetItem: {ex.Message} \n {ex.StackTrace}");
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
                _ErrorMessageAction?.Invoke("DatabaseServiceMongoDB->GetItems->_KeyValues is null or empty.");
                return false;
            }

            var Table = GetTable(_Table);
            if (Table == null) return false;

            bool bFirst = true;
            FilterDefinition<BsonDocument> Filter = null;

            foreach (var Value in _KeyValues)
            {
                if (bFirst)
                {
                    bFirst = false;
                    Filter = BuildEqFilter(_KeyName, Value);
                }
                else
                {
                    Filter = Builders<BsonDocument>.Filter.Or(Filter, BuildEqFilter(_KeyName, Value));
                }
            }

            try
            {
                using (var FindTask = Table.FindAsync(Filter))
                {
                    FindTask.Wait();

                    using (var ToListTask = FindTask.Result.ToListAsync())
                    {
                        ToListTask.Wait();

                        var AsList = ToListTask.Result;

                        _Result = new List<JObject>();

                        foreach (var Document in AsList)
                        {
                            var CreatedJson = BsonToJObject(Document);

                            if (Document.TryGetElement(_KeyName, out BsonElement _Value))
                            {
                                AddKeyToJson(CreatedJson, _KeyName, BsonValueToPrimitiveType(_Value.Value));
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke($"[WARNING!] DatabaseServiceMongoDB->GetItems: TryGetElement {_KeyName} failed.");
                                continue;
                            }
                            Utility.SortJObject(CreatedJson, true);
                            _Result.Add(CreatedJson);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceMongoDB->GetItems-> {e.Message}, {e.StackTrace}");
                return false;
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
            JObject _PutItem, 
            out JObject _ReturnItem,
            EReturnItemBehaviour _ReturnItemBehaviour = EReturnItemBehaviour.DoNotReturn, 
            DatabaseAttributeCondition _ConditionExpression = null,
            Action<string> _ErrorMessageAction = null)
        {
            return UpdateItem(_Table, _KeyName, _KeyValue, _PutItem, out _ReturnItem, _ReturnItemBehaviour, _ConditionExpression, _ErrorMessageAction);
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
            _ReturnItem = null;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            try
            {
                var Filter = BuildEqFilter(_KeyName, _KeyValue);

                if (_ConditionExpression != null)
                {
                    var FirstFilter = (_ConditionExpression as DatabaseAttributeConditionMongo).Filter;
                    var FinalFilter = Builders<BsonDocument>.Filter.And(Filter, FirstFilter);

                    if (!HasTableMatchingResultWithFilter(Table, FinalFilter))
                    {
                        //Condition failed.
                        return false;
                    }
                }

                if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllOld)
                {
                    var GetFilter = BuildEqFilter(_KeyName, _KeyValue);
                    var Document = FindOne(Table, GetFilter);
                    if (Document != null)
                    {
                        _ReturnItem = BsonToJObject(Document);
                        AddKeyToJson(_ReturnItem, _KeyName, _KeyValue);
                    }
                }

                JObject NewObject = (JObject)_UpdateItem.DeepClone();
                AddKeyToJson(NewObject, _KeyName, _KeyValue);

                //use $set for preventing to get element name is not valid exception. more info https://stackoverflow.com/a/35441075
                BsonDocument UpdatedDocument = new BsonDocument { { "$set", JObjectToBson(NewObject) } };

                using (var UpResultTask = Table.UpdateOneAsync(Filter, UpdatedDocument, new UpdateOptions() { IsUpsert = true }))
                {
                    UpResultTask.Wait();
                }

                if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllNew)
                {
                    _ReturnItem = NewObject;
                    //Key added already
                }

                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceMongoDB->UpdateItem: {ex.Message} : \n {ex.StackTrace}");
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

            var Table = GetTable(_Table);
            if (Table == null) return false;

            try
            {
                var Filter = BuildEqFilter(_KeyName, _KeyValue);

                if (_ConditionExpression != null)
                {
                    var FirstFilter = (_ConditionExpression as DatabaseAttributeConditionMongo).Filter;
                    var FinalFilter = Builders<BsonDocument>.Filter.And(Filter, FirstFilter);

                    if (!HasTableMatchingResultWithFilter(Table, FinalFilter))
                    {
                        //Condition failed.
                        return false;
                    }
                }

                if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllOld)
                {
                    var Document = FindOne(Table, Filter);
                    if (Document != null)
                    {
                        _ReturnItem = BsonToJObject(Document);
                        AddKeyToJson(_ReturnItem, _KeyName, _KeyValue);
                    }
                }

                using (var DeleteTask = Table.DeleteOneAsync(Filter))
                {
                    DeleteTask.Wait();
                }
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceMongoDB->DeleteItem: {ex.Message} : \n {ex.StackTrace}");
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
                _ErrorMessageAction?.Invoke("DatabaseServiceMongoDB->AddElementsToArrayItem: ElementValueEntries must contain values.");
                return false;
            }
            var ExpectedType = _ElementValueEntries[0].Type;

            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (_ElementValueEntry.Type != ExpectedType)
                {
                    _ErrorMessageAction?.Invoke("DatabaseServiceMongoDB->AddElementsToArrayItem: ElementValueEntries must contain elements with the same type.");
                    return false;
                }
            }

            if (_KeyValue == null)
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceMongoDB->AddElementsToArrayItem: Key is null.");
                return false;
            }

            var Table = GetTable(_Table);
            if (Table == null) return false;

            var Filter = BuildEqFilter(_KeyName, _KeyValue);

            try
            {
                if (_ConditionExpression != null)
                {
                    var FirstFilter = (_ConditionExpression as DatabaseAttributeConditionMongo).Filter;
                    var FinalFilter = Builders<BsonDocument>.Filter.And(Filter, FirstFilter);

                    if (!HasTableMatchingResultWithFilter(Table, FinalFilter))
                    {
                        //Condition failed.
                        return false;
                    }
                }

                List<object> TempList = new List<object>();

                foreach (var Element in _ElementValueEntries)
                {
                    switch (Element.Type)
                    {
                        case EPrimitiveTypeEnum.String:
                            TempList.Add(Element.AsString);
                            break;
                        case EPrimitiveTypeEnum.Integer:
                            TempList.Add(Element.AsInteger);
                            break;
                        case EPrimitiveTypeEnum.Double:
                            TempList.Add(Element.AsDouble);
                            break;
                        case EPrimitiveTypeEnum.ByteArray:
                            TempList.Add(Element.AsByteArray);
                            break;
                    }
                }

                UpdateDefinition<BsonDocument> Update = Builders<BsonDocument>.Update.PushEach(_ElementName, TempList);

                if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllOld)
                {
                    var GetFilter = BuildEqFilter(_KeyName, _KeyValue);
                    var Document = FindOne(Table, GetFilter);
                    if (Document != null)
                    {
                        _ReturnItem = BsonToJObject(Document);
                        AddKeyToJson(_ReturnItem, _KeyName, _KeyValue);
                    }
                }

                using (var UpResultTask = Table.UpdateOneAsync(Filter, Update, new UpdateOptions() { IsUpsert = true }))
                {
                    UpResultTask.Wait();
                }

                if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllNew)
                {
                    var GetFilter = BuildEqFilter(_KeyName, _KeyValue);
                    var Document = FindOne(Table, GetFilter);
                    if (Document != null)
                    {
                        _ReturnItem = BsonToJObject(Document);
                        AddKeyToJson(_ReturnItem, _KeyName, _KeyValue);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"{ex.Message} : \n {ex.StackTrace}");
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
                _ErrorMessageAction?.Invoke("DatabaseServiceMongoDB->AddElementsToArrayItem: ElementValueEntries must contain values.");
                return false;
            }
            var ExpectedType = _ElementValueEntries[0].Type;

            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (_ElementValueEntry.Type != ExpectedType)
                {
                    _ErrorMessageAction?.Invoke("DatabaseServiceMongoDB->AddElementsToArrayItem: ElementValueEntries must contain elements with the same type.");
                    return false;
                }
            }

            if (_KeyValue == null)
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceMongoDB->AddElementsToArrayItem: Key is null.");
                return false;
            }

            var Table = GetTable(_Table);
            if (Table == null) return false;

            var Filter = BuildEqFilter(_KeyName, _KeyValue);

            if (_ConditionExpression != null)
            {
                var FirstFilter = (_ConditionExpression as DatabaseAttributeConditionMongo).Filter;
                var FinalFilter = Builders<BsonDocument>.Filter.And(Filter, FirstFilter);

                if (!HasTableMatchingResultWithFilter(Table, FinalFilter))
                {
                    //Condition failed.
                    return false;
                }
            }

            List<object> TempList = new List<object>();

            foreach (var Element in _ElementValueEntries)
            {
                switch (Element.Type)
                {
                    case EPrimitiveTypeEnum.String:
                        TempList.Add(Element.AsString);
                        break;
                    case EPrimitiveTypeEnum.Integer:
                        TempList.Add(Element.AsInteger);
                        break;
                    case EPrimitiveTypeEnum.Double:
                        TempList.Add(Element.AsDouble);
                        break;
                    case EPrimitiveTypeEnum.ByteArray:
                        TempList.Add(Element.AsByteArray);
                        break;
                }
            }

            UpdateDefinition<BsonDocument> Update = Builders<BsonDocument>.Update.PullAll(_ElementName, TempList);

            try
            {
                if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllOld)
                {
                    var GetFilter = BuildEqFilter(_KeyName, _KeyValue);
                    var Document = FindOne(Table, GetFilter);
                    if (Document != null)
                    {
                        _ReturnItem = BsonToJObject(Document);
                        AddKeyToJson(_ReturnItem, _KeyName, _KeyValue);
                    }
                }

                using (var UpdateTask = Table.UpdateOneAsync(Filter, Update))
                {
                    UpdateTask.Wait();
                }

                if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllNew)
                {
                    var GetFilter = BuildEqFilter(_KeyName, _KeyValue);
                    var Document = FindOne(Table, GetFilter);
                    if (Document != null)
                    {
                        _ReturnItem = BsonToJObject(Document);
                        AddKeyToJson(_ReturnItem, _KeyName, _KeyValue);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"{ex.Message} : \n {ex.StackTrace}");
                return false;
            }
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
            _NewValue = 0.0f;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            var Filter = BuildEqFilter(_KeyName, _KeyValue);

            if (_ConditionExpression != null)
            {
                var FirstFilter = (_ConditionExpression as DatabaseAttributeConditionMongo).Filter;
                var FinalFilter = Builders<BsonDocument>.Filter.And(Filter, FirstFilter);

                if (!HasTableMatchingResultWithFilter(Table, FinalFilter))
                {
                    //Condition failed.
                    return false;
                }
            }

            UpdateDefinition<BsonDocument> Update;

            if (_bDecrement)
            {
                Update = Builders<BsonDocument>.Update.Inc(_ValueAttribute, -_IncrementOrDecrementBy);
            }
            else
            {
                Update = Builders<BsonDocument>.Update.Inc(_ValueAttribute, _IncrementOrDecrementBy);
            }

            try
            {
                using (var UpdateTask = Table.FindOneAndUpdateAsync(Filter, Update, new FindOneAndUpdateOptions<BsonDocument, BsonDocument>() 
                { 
                    ReturnDocument = ReturnDocument.After,
                    IsUpsert = true
                }))
                {
                    UpdateTask.Wait();
                    BsonDocument Document = UpdateTask.Result;
                    _NewValue = Document.GetValue(_ValueAttribute).AsDouble;
                }
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"{ex.Message} : \n {ex.StackTrace}");
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
            _ReturnItem = null;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            List<JObject> Results = new List<JObject>();

            var Filter = Builders<BsonDocument>.Filter.Empty;

            var ReturnedSearch = new List<BsonDocument>();

            try
            {
                using (var ScanTask = Table.FindAsync(Filter))
                {
                    ScanTask.Wait();

                    do
                    {
                        if (ScanTask.Result.Current != null)
                        {
                            ReturnedSearch.AddRange(ScanTask.Result.Current.ToList());
                        }
                    }
                    while (ScanTask.Result.MoveNext());
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceMongoDB->ScanTable: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }

            return Internal_ScanTable(_PossibleKeyNames, ReturnedSearch, out _ReturnItem, _ErrorMessageAction);
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

            _ReturnItem = null;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            var Filter = (_FilterBy as DatabaseAttributeConditionMongo).Filter;

            List<JObject> Results = new List<JObject>();

            var ReturnedSearch = new List<BsonDocument>();

            try
            {
                using (var ScanTask = Table.FindAsync(Filter))
                {
                    ScanTask.Wait();
                    do
                    {
                        if (ScanTask.Result.Current != null)
                        {
                            ReturnedSearch.AddRange(ScanTask.Result.Current.ToList());
                        }
                    }
                    while (ScanTask.Result.MoveNext());
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceMongoDB->ScanTableFilterBy: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }

            return Internal_ScanTable(_PossibleKeyNames, ReturnedSearch, out _ReturnItem, _ErrorMessageAction);
        }

        private bool Internal_ScanTable(
            string[] _PossibleKeyNames, 
            List<BsonDocument> _ReturnedSearch,
            out List<JObject> _ReturnItem,
            Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            if (_ReturnedSearch != null)
            {
                try
                {
                    var TempResults = new ConcurrentBag<JObject>();

                    Parallel.ForEach(_ReturnedSearch, (Document) =>
                    {
                        var CreatedJson = BsonToJObject(Document);
                        foreach (var _KeyName in _PossibleKeyNames)
                        {
                            if (Document.TryGetElement(_KeyName, out BsonElement _Value))
                            {
                                AddKeyToJson(CreatedJson, _KeyName, BsonValueToPrimitiveType(_Value.Value));
                                break;
                            }
                        }
                        Utility.SortJObject(CreatedJson, true);
                        TempResults.Add(CreatedJson);
                    });

                    _ReturnItem = TempResults.ToList();
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    _ErrorMessageAction?.Invoke($"DatabaseServiceMongoDB->Internal_ScanTable: JsonReaderException: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }
                return true;
            }
            else
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceMongoDB->Internal_ScanTable: TableObject.ScanTable returned null.");
            }

            return false;
        }

        private static PrimitiveType BsonValueToPrimitiveType(BsonValue _BsonValue)
        {
            switch (_BsonValue.BsonType)
            {
                case BsonType.String:
                    return new PrimitiveType(_BsonValue.AsString);
                case BsonType.Int32:
                case BsonType.Int64:
                    return new PrimitiveType(_BsonValue.AsInt64);
                case BsonType.Double:
                    return new PrimitiveType(_BsonValue.AsDouble);
                case BsonType.Binary:
                    return new PrimitiveType(_BsonValue.AsByteArray);
            }
            return new PrimitiveType(_BsonValue.ToJson());
        }

        private class DatabaseAttributeConditionMongo : DatabaseAttributeCondition
        {
            public FilterDefinition<BsonDocument> Filter;
            public DatabaseAttributeConditionMongo(EDatabaseAttributeConditionType _ConditionType) : base(_ConditionType)
            {
            }
        }

        private class AttributeArrayElementExistConditionMongoDb : DatabaseAttributeConditionMongo
        {
            public AttributeArrayElementExistConditionMongoDb(string _Attribute, PrimitiveType _ArrayElement) : base(EDatabaseAttributeConditionType.ArrayElementExist)
            {
                switch (_ArrayElement.Type)
                {
                    case EPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.AnyIn(_Attribute, new double[] { _ArrayElement.AsDouble });
                        break;
                    case EPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.AnyIn(_Attribute, new long[] { _ArrayElement.AsInteger });
                        break;
                    case EPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.AnyIn(_Attribute, new byte[][] { _ArrayElement.AsByteArray });
                        break;
                    case EPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.AnyIn(_Attribute, new string[] { _ArrayElement.AsString });
                        break;
                }
            }
        }

        public DatabaseAttributeCondition BuildArrayElementExistCondition(string _Attribute, PrimitiveType ArrayElement)
        {
            return new AttributeArrayElementExistConditionMongoDb(_Attribute, ArrayElement);
        }

        private class AttributeArrayElementNotExistConditionMongoDb : DatabaseAttributeConditionMongo
        {
            public AttributeArrayElementNotExistConditionMongoDb(string _Attribute, PrimitiveType _ArrayElement) : base(EDatabaseAttributeConditionType.ArrayElementNotExist)
            {
                switch (_ArrayElement.Type)
                {
                    case EPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.AnyNin(_Attribute, new double[] { _ArrayElement.AsDouble });
                        break;
                    case EPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.AnyNin(_Attribute, new long[] { _ArrayElement.AsInteger });
                        break;
                    case EPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.AnyNin(_Attribute, new byte[][] { _ArrayElement.AsByteArray });
                        break;
                    case EPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.AnyNin(_Attribute, new string[] { _ArrayElement.AsString });
                        break;
                }
            }
        }

        public DatabaseAttributeCondition BuildArrayElementNotExistCondition(string _Attribute, PrimitiveType ArrayElement)
        {
            return new AttributeArrayElementNotExistConditionMongoDb(_Attribute, ArrayElement);
        }

        private class AttributeEqualsConditionMongoDb : DatabaseAttributeConditionMongo
        {
            public AttributeEqualsConditionMongoDb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeEquals)
            {
                switch (Value.Type)
                {
                    case EPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Eq(Attribute, Value.AsDouble);
                        break;
                    case EPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Eq(Attribute, Value.AsInteger);
                        break;
                    case EPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Eq(Attribute, Value.AsByteArray);
                        break;
                    case EPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Eq(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>(Attribute, null);
            }
        }

        public DatabaseAttributeCondition BuildAttributeEqualsCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeEqualsConditionMongoDb(Attribute, Value);
        }

        private class AttributeExistConditionMongoDb : DatabaseAttributeConditionMongo
        {
            public AttributeExistConditionMongoDb(string Attribute) : base(EDatabaseAttributeConditionType.AttributeExists)
            {
                Filter = Builders<BsonDocument>.Filter.Exists(Attribute, true);
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>(Attribute, null);
            }
        }

        public DatabaseAttributeCondition BuildAttributeExistsCondition(string Attribute)
        {
            return new AttributeExistConditionMongoDb(Attribute);
        }

        private class AttributeGreaterMongoDb : DatabaseAttributeConditionMongo
        {
            public AttributeGreaterMongoDb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeGreater)
            {
                switch (Value.Type)
                {
                    case EPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Gt(Attribute, Value.AsDouble);
                        break;
                    case EPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Gt(Attribute, Value.AsInteger);
                        break;
                    case EPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Gt(Attribute, Value.AsByteArray);
                        break;
                    case EPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Gt(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>(Attribute, null);
            }
        }

        public DatabaseAttributeCondition BuildAttributeGreaterCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeGreaterMongoDb(Attribute, Value);
        }

        private class AttributeGreaterOrEqualMongoDb : DatabaseAttributeConditionMongo
        {
            public AttributeGreaterOrEqualMongoDb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeGreaterOrEqual)
            {
                switch (Value.Type)
                {
                    case EPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Gte(Attribute, Value.AsDouble);
                        break;
                    case EPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Gte(Attribute, Value.AsInteger);
                        break;
                    case EPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Gte(Attribute, Value.AsByteArray);
                        break;
                    case EPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Gte(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>(Attribute, null);
            }
        }

        public DatabaseAttributeCondition BuildAttributeGreaterOrEqualCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeGreaterOrEqualMongoDb(Attribute, Value);
        }

        private class AttributeLessMongoDb : DatabaseAttributeConditionMongo
        {
            public AttributeLessMongoDb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeLess)
            {
                switch (Value.Type)
                {
                    case EPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Lt(Attribute, Value.AsDouble);
                        break;
                    case EPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Lt(Attribute, Value.AsInteger);
                        break;
                    case EPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Lt(Attribute, Value.AsByteArray);
                        break;
                    case EPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Lt(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>(Attribute, null);
            }
        }

        public DatabaseAttributeCondition BuildAttributeLessCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeLessMongoDb(Attribute, Value);
        }

        private class AttributeLessOrEqualMongoDb : DatabaseAttributeConditionMongo
        {
            public AttributeLessOrEqualMongoDb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeLessOrEqual)
            {
                switch (Value.Type)
                {
                    case EPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Lte(Attribute, Value.AsDouble);
                        break;
                    case EPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Lte(Attribute, Value.AsInteger);
                        break;
                    case EPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Lte(Attribute, Value.AsByteArray);
                        break;
                    case EPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Lte(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>(Attribute, null);
            }
        }

        public DatabaseAttributeCondition BuildAttributeLessOrEqualCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeLessOrEqualMongoDb(Attribute, Value);
        }

        private class AttributeNotEqualsConditionMongoDb : DatabaseAttributeConditionMongo
        {
            public AttributeNotEqualsConditionMongoDb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeNotEquals)
            {
                switch (Value.Type)
                {
                    case EPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Ne(Attribute, Value.AsDouble);
                        break;
                    case EPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Ne(Attribute, Value.AsInteger);
                        break;
                    case EPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Ne(Attribute, Value.AsByteArray);
                        break;
                    case EPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Ne(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>(Attribute, null);
            }
        }

        public DatabaseAttributeCondition BuildAttributeNotEqualsCondition(string Attribute, PrimitiveType Value)
        {
            return new AttributeNotEqualsConditionMongoDb(Attribute, Value);
        }

        private class AttributeNotExistConditionMongoDb : DatabaseAttributeConditionMongo
        {
            public AttributeNotExistConditionMongoDb(string Attribute) : base(EDatabaseAttributeConditionType.AttributeNotExist)
            {
                Filter = Builders<BsonDocument>.Filter.Exists(Attribute, false/*Not*/);
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>(Attribute, null);
            }
        }

        public DatabaseAttributeCondition BuildAttributeNotExistCondition(string Attribute)
        {
            return new AttributeNotExistConditionMongoDb(Attribute);
        }

        private JObject BsonToJObject(BsonDocument _Document)
        {
            //remove database id as it is not part of what we store
            _Document.Remove("_id");

            //Set strict mode to convert numbers to valid json otherwise it generates something like NumberLong(5) where you expect a 5
            var JsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };

            return JObject.Parse(_Document.ToJson(JsonWriterSettings));
        }

        private BsonDocument JObjectToBson(JObject _JsonObject)
        {
            // https://stackoverflow.com/a/62104268
            //Write JObject to MemoryStream
            using var stream = new MemoryStream();
            using (var writer = new BsonDataWriter(stream) { CloseOutput = false })
            {
                _JsonObject.WriteTo(writer);
            }
            stream.Position = 0; //for reading the steam immediately 

            //Read the object from MemoryStream
            BsonDocument bsonData;
            using (var reader = new BsonBinaryReader(stream))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                bsonData = BsonDocumentSerializer.Instance.Deserialize(context);
            }
            return bsonData;
        }

        private static BsonDocument FindOne(IMongoCollection<BsonDocument> _Table, FilterDefinition<BsonDocument> _Filter)
        {
            try
            {
                using (var FindTask = _Table.FindAsync(_Filter))
                {
                    FindTask.Wait();

                    using (var FirstOrDefTask = FindTask.Result.FirstOrDefaultAsync())
                    {
                        FirstOrDefTask.Wait();
                        return FirstOrDefTask.Result;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool HasTableMatchingResultWithFilter(IMongoCollection<BsonDocument> _Table, FilterDefinition<BsonDocument> _Filter)
        {
            try
            {
                using (var FindTask = _Table.FindAsync(_Filter))
                {
                    FindTask.Wait();

                    using (var AnyTask = FindTask.Result.AnyAsync())
                    {
                        AnyTask.Wait();
                        return AnyTask.Result;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}