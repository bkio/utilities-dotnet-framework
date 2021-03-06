/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using CommonUtilities;
using Newtonsoft.Json.Linq;

namespace CloudServiceUtilities.DatabaseServices
{
    public class DatabaseServiceAWS : DatabaseServiceBase, IDatabaseServiceInterface
    {
        /// <summary>
        /// <para>AWS Dynamodb Client that is responsible to serve to this object</para>
        /// </summary>
        private readonly AmazonDynamoDBClient DynamoDBClient;

        /// <summary>
        /// <para>Holds initialization success</para>
        /// </summary>
        private readonly bool bInitializationSucceed;

        /// <summary>
        /// 
        /// <para>DatabaseServiceAWS: Parametered Constructor for Managed Service by Amazon</para>
        /// 
        /// <para><paramref name="_AccessKey"/>                     AWS Access Key</para>
        /// <para><paramref name="_SecretKey"/>                     AWS Secret Key</para>
        /// <para><paramref name="_Region"/>                        AWS Region that DynamoDB Client will connect to (I.E. eu-west-1)</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>        
        /// 
        /// </summary>
        public DatabaseServiceAWS(
            string _AccessKey,
            string _SecretKey,
            string _Region,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                DynamoDBClient = new AmazonDynamoDBClient(new Amazon.Runtime.BasicAWSCredentials(_AccessKey, _SecretKey), Amazon.RegionEndpoint.GetBySystemName(_Region));
                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->Constructor: {e.Message}, Trace: { e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        /// <summary>
        /// 
        /// <para>DatabaseServiceAWS: Parametered Constructor for Local DynamoDB Edition</para>
        /// 
        /// <para><paramref name="_ServiceURL"/>                     Service URL for DynamoDB</para>
        /// <para><paramref name="_ErrorMessageAction"/>             Error messages will be pushed to this action</para>           
        /// 
        /// </summary>
        public DatabaseServiceAWS(
            string _ServiceURL,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                DynamoDBClient = new AmazonDynamoDBClient("none", "none", new AmazonDynamoDBConfig
                {
                    ServiceURL = _ServiceURL
                });
                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->Constructor: {e.Message}, Trace: { e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        ~DatabaseServiceAWS()
        {
            DynamoDBClient?.Dispose();
        }

        /// <summary>
        /// <para>Map that holds loaded table definition instances</para>
        /// </summary>
        private readonly Dictionary<string, Table> LoadedTables = new Dictionary<string, Table>();
        private readonly object LoadedTables_DictionaryLock = new object();

        /// <summary>
        /// <para>Searches table definition in LoadedTables, if not loaded, loads, stores and returns</para>
        /// </summary>
        private bool LoadStoreAndGetTable(
            string _Table, 
            out Table _ResultTable, 
            Action<string> _ErrorMessageAction = null)
        {
            bool bResult = true;
            lock (LoadedTables_DictionaryLock)
            {
                if (!LoadedTables.ContainsKey(_Table))
                {
                    if (Table.TryLoadTable(DynamoDBClient, _Table, out _ResultTable))
                    {
                        LoadedTables[_Table] = _ResultTable;
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("DatabaseServiceAWS->LoadStoreAndGetTable: Table has not been found.");
                        bResult = false;
                    }
                }
                else
                {
                    _ResultTable = LoadedTables[_Table];
                }
            }
            return bResult;
        }

        /// <summary>
        /// 
        /// <para>HasInitializationSucceed</para>
        /// 
        /// <para>Check <seealso cref="IDatabaseServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
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

            bool bGetAll = false;
            if (_ValuesToGet == null || _ValuesToGet.Length == 0)
            {
                bGetAll = true;
            }

            //Try getting table definition
            if (LoadStoreAndGetTable(_Table, out Table TableObject, _ErrorMessageAction))
            {
                GetItemOperationConfig Config;
                if (!bGetAll)
                {
                    List<string> ValuesToGet = new List<string>(_ValuesToGet);
                    Config = new GetItemOperationConfig
                    {
                        AttributesToGet = ValuesToGet,
                        ConsistentRead = true
                    };
                }
                else
                {
                    Config = new GetItemOperationConfig
                    {
                        ConsistentRead = true
                    };
                }

                //Get item from the table
                try
                {               
                    using (var _GetItem = TableObject.GetItemAsync(_KeyValue.ToString(), Config))
                    {
                        _GetItem.Wait();

                        var ReturnedDocument = _GetItem.Result;
                        if (ReturnedDocument != null)
                        {
                            //Convert to string and parse as JObject
                            _Result = JObject.Parse(ReturnedDocument.ToJson());
                            AddKeyToJson(_Result, _KeyName, _KeyValue);
                            Utility.SortJObject(_Result, true);
                        }
                    }
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->GetItem: {e.Message}, Trace: { e.StackTrace}");
                    return false;
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
                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->GetItems->_KeyValues is null or empty.");
                return false;
            }

            //Try getting table definition
            if (LoadStoreAndGetTable(_Table, out Table TableObject, _ErrorMessageAction))
            {
                try
                {
                    TableObject.CreateBatchGet();

                    var _GetItems = TableObject.CreateBatchGet();
                    _GetItems.ConsistentRead = true;

                    foreach (var Value in _KeyValues)
                    {
                        _GetItems.AddKey(Value.ToString());
                    }

                    using (var GetItemsTask = _GetItems.ExecuteAsync())
                    {
                        GetItemsTask.Wait();

                        if (_GetItems.Results.Count != _KeyValues.Length)
                        {
                            _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->GetItems: _GetItems.Results.Count != _KeyValues.Length. {_GetItems.Results.Count} != {_KeyValues.Length}");
                            return false;
                        }

                        _Result = new List<JObject>();

                        int i = 0;
                        foreach (var ReturnedDocument in _GetItems.Results)
                        {
                            if (ReturnedDocument != null)
                            {
                                //Convert to string and parse as JObject
                                var AsJObject = JObject.Parse(ReturnedDocument.ToJson());
                                AddKeyToJson(AsJObject, _KeyName, _KeyValues[i]);
                                Utility.SortJObject(AsJObject, true);

                                _Result.Add(AsJObject);
                            }
                            i++;
                        }
                    }
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->GetItems: {e.Message}, Trace: { e.StackTrace}");
                    return false;
                }
                return true;
            }
            return false;
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
            DatabaseAttributeCondition _ConditionExpression = null, 
            Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            var Item = new JObject(_Item);
            if (Item != null && !Item.ContainsKey(_KeyName))
            {
                switch (_KeyValue.Type)
                {
                    case EPrimitiveTypeEnum.Double:
                        Item[_KeyName] = _KeyValue.AsDouble;
                        break;
                    case EPrimitiveTypeEnum.Integer:
                        Item[_KeyName] = _KeyValue.AsInteger;
                        break;
                    case EPrimitiveTypeEnum.ByteArray:
                        Item[_KeyName] = Convert.ToBase64String(_KeyValue.AsByteArray);
                        break;
                    default:
                        Item[_KeyName] = _KeyValue.AsString;
                        break;
                }
            }

            //First convert JObject to AWS Document
            Document ItemAsDocument = null;
            try
            {
                ItemAsDocument = Document.FromJson(Item.ToString());
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->PutItem->JObject-Document Conversion: {e.Message}, Trace: { e.StackTrace}");
                return false;
            }

            if (ItemAsDocument != null)
            {
                //Try getting table definition
                if (LoadStoreAndGetTable(_Table, out Table TableObject, _ErrorMessageAction))
                {
                    var Config = new PutItemOperationConfig();
                    
                    //Set return value expectation
                    if (_ReturnItemBehaviour == EReturnItemBehaviour.DoNotReturn)
                    {
                        Config.ReturnValues = ReturnValues.None;
                    }
                    else if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllNew)
                    {
                        Config.ReturnValues = ReturnValues.AllNewAttributes;
                    }
                    else
                    {
                        Config.ReturnValues = ReturnValues.AllOldAttributes;
                    }

                    //Set condition expression
                    if (_ConditionExpression != null)
                    {
                        var BuiltCondition = _ConditionExpression.GetBuiltCondition();
                        if (BuiltCondition != null)
                        {
                            Expression ConditionalExpression = new Expression
                            {
                                ExpressionStatement = BuiltCondition.Item1
                            };
                            if (BuiltCondition.Item2 != null)
                            {
                                switch (BuiltCondition.Item2.Item2.Type)
                                {
                                    case EPrimitiveTypeEnum.String:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsString;
                                        break;
                                    case EPrimitiveTypeEnum.Integer:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsInteger;
                                        break;
                                    case EPrimitiveTypeEnum.Double:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsDouble;
                                        break;
                                    case EPrimitiveTypeEnum.ByteArray:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.ToString();
                                        break;
                                }
                            }
                            Config.ConditionalExpression = ConditionalExpression;
                        }
                    }

                    //Put item to the table
                    try
                    {
                        using (var _PutItem = TableObject.PutItemAsync(ItemAsDocument, Config))
                        {
                            _PutItem.Wait();

                            var ReturnedDocument = _PutItem.Result;

                            if (_ReturnItemBehaviour == EReturnItemBehaviour.DoNotReturn)
                            {
                                return true;
                            }
                            else if (ReturnedDocument != null)
                            {
                                //Convert to string and parse as JObject
                                _ReturnItem = JObject.Parse(ReturnedDocument.ToJson());
                                Utility.SortJObject(_ReturnItem, true);
                                return true;
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->PutItem: TableObject.PutItem returned null.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!(e is ConditionalCheckFailedException))
                        {
                            _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->PutItem: {e.Message}, Trace: { e.StackTrace}");
                        }
                        return false;
                    }
                }
            }
            else
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->PutItem->JObject-Document Conversion: ItemAsDocument is null.");
        }
            return false;
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

            var UpdateItem = new JObject(_UpdateItem);
            if (UpdateItem != null && !UpdateItem.ContainsKey(_KeyName))
            {
                switch (_KeyValue.Type)
                {
                    case EPrimitiveTypeEnum.Double:
                        UpdateItem[_KeyName] = _KeyValue.AsDouble;
                        break;
                    case EPrimitiveTypeEnum.Integer:
                        UpdateItem[_KeyName] = _KeyValue.AsInteger;
                        break;
                    case EPrimitiveTypeEnum.ByteArray:
                        UpdateItem[_KeyName] = Convert.ToBase64String(_KeyValue.AsByteArray);
                        break;
                    default:
                        UpdateItem[_KeyName] = _KeyValue.AsString;
                        break;
                }
            }

            //First convert JObject to AWS Document
            Document ItemAsDocument = null;
            try
            {
                ItemAsDocument = Document.FromJson(UpdateItem.ToString());
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->UpdateItem->JObject-Document Conversion: {e.Message}, Trace: { e.StackTrace}");
                return false;
            }

            if (ItemAsDocument != null)
            {
                //Try getting table definition
                if (LoadStoreAndGetTable(_Table, out Table TableObject, _ErrorMessageAction))
                {
                    UpdateItemOperationConfig Config = new UpdateItemOperationConfig();

                    //Set return value expectation
                    if (_ReturnItemBehaviour == EReturnItemBehaviour.DoNotReturn)
                    {
                        Config.ReturnValues = ReturnValues.None;
                    }
                    else if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllOld)
                    {
                        Config.ReturnValues = ReturnValues.AllOldAttributes;
                    }
                    else
                    {
                        Config.ReturnValues = ReturnValues.AllNewAttributes;
                    }

                    //Set condition expression
                    if (_ConditionExpression != null)
                    {
                        var BuiltCondition = _ConditionExpression.GetBuiltCondition();
                        if (BuiltCondition != null)
                        {
                            Expression ConditionalExpression = new Expression
                            {
                                ExpressionStatement = BuiltCondition.Item1
                            };
                            if (BuiltCondition.Item2 != null)
                            {
                                switch (BuiltCondition.Item2.Item2.Type)
                                {
                                    case EPrimitiveTypeEnum.String:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsString;
                                        break;
                                    case EPrimitiveTypeEnum.Integer:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsInteger;
                                        break;
                                    case EPrimitiveTypeEnum.Double:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsDouble;
                                        break;
                                    case EPrimitiveTypeEnum.ByteArray:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.ToString();
                                        break;
                                }
                            }
                            Config.ConditionalExpression = ConditionalExpression;
                        }
                    }

                    //Update item in the table
                    try
                    {
                        using (var ItemTask = TableObject.UpdateItemAsync(ItemAsDocument, Config))
                        {
                            ItemTask.Wait();

                            var ReturnedDocument = ItemTask.Result;

                            if (_ReturnItemBehaviour == EReturnItemBehaviour.DoNotReturn)
                            {
                                return true;
                            }
                            else if (ReturnedDocument != null)
                            {
                                //Convert to string and parse as JObject
                                _ReturnItem = JObject.Parse(ReturnedDocument.ToJson());
                                Utility.SortJObject(_ReturnItem, true);
                                return true;
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->UpdateItem: TableObject.UpdateItem returned null.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!(e is ConditionalCheckFailedException))
                        {
                            _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->UpdateItem: {e.Message}, Trace: { e.StackTrace}");
                        }
                        return false;
                    }
                }
            }
            else
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->UpdateItem->JObject-Document Conversion: ItemAsDocument is null.");
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

            if (DynamoDBClient == null)
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->AddElementsToArrayItem: DynamoDBClient is null.");
                return false;
            }

            if (_KeyValue == null)
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->AddElementsToArrayItem: Key is null.");
                return false;
            }

            UpdateItemRequest Request = new UpdateItemRequest
            {
                TableName = _Table,
                Key = new Dictionary<string, AttributeValue>()
            };

            if (_KeyValue.Type == EPrimitiveTypeEnum.Integer)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsInteger.ToString() };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.Double)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsDouble.ToString() };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.String)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.AsString };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.ByteArray)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.ToString() };
            }

            //Set return value expectation
            if (_ReturnItemBehaviour == EReturnItemBehaviour.DoNotReturn)
            {
                Request.ReturnValues = ReturnValue.NONE;
            }
            else if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllOld)
            {
                Request.ReturnValues = ReturnValue.ALL_OLD;
            }
            else
            {
                Request.ReturnValues = ReturnValue.ALL_NEW;
            }

            var SetAsList = new List<string>();
            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (ExpectedType == EPrimitiveTypeEnum.Integer || ExpectedType == EPrimitiveTypeEnum.Double)
                {
                    if (_ElementValueEntry.Type == EPrimitiveTypeEnum.Integer)
                    {
                        SetAsList.Add(_ElementValueEntry.AsInteger.ToString());
                    }
                    else
                    {
                        SetAsList.Add(_ElementValueEntry.AsDouble.ToString());
                    }
                }
                else
                {
                    if (ExpectedType == EPrimitiveTypeEnum.ByteArray)
                    {
                        SetAsList.Add(_ElementValueEntry.ToString());
                    }
                    else
                    {
                        SetAsList.Add(_ElementValueEntry.AsString);
                    }
                }
            }

            Request.ExpressionAttributeNames = new Dictionary<string, string>()
            {
                {
                    "#V", _ElementName
                }
            };
            Request.UpdateExpression = "ADD #V :vals";

            if (ExpectedType == EPrimitiveTypeEnum.Integer || ExpectedType == EPrimitiveTypeEnum.Double)
            {
                Request.ExpressionAttributeValues.Add(":vals", new AttributeValue { NS = SetAsList });
            }
            else
            {
                Request.ExpressionAttributeValues.Add(":vals", new AttributeValue { SS = SetAsList });
            }

            if (_ConditionExpression != null)
            {
                if (_ConditionExpression.AttributeConditionType == EDatabaseAttributeConditionType.ArrayElementNotExist)
                {
                    var BuiltCondition = _ConditionExpression.GetBuiltCondition();

                    if (BuiltCondition.Item1 == null || BuiltCondition.Item2 == null || BuiltCondition.Item2.Item2 == null)
                    {
                        _ErrorMessageAction?.Invoke("DatabaseServiceAWS->AddElementsToArrayItem: Invalid condition expression.");
                        return false;
                    }
                    
                    if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.Integer)
                    {
                        Request.ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { NS = new List<string>() { BuiltCondition.Item2.Item2.AsInteger.ToString() } });
                    }
                    else if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.Double)
                    {
                        Request.ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { NS = new List<string>() { BuiltCondition.Item2.Item2.AsDouble.ToString() } });
                    }
                    else if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.String)
                    {
                        Request.ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { SS = new List<string>() { BuiltCondition.Item2.Item2.AsString } });
                    }
                    else if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.ByteArray)
                    {
                        Request.ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { SS = new List<string>() { BuiltCondition.Item2.Item2.ToString() } });
                    }

                    Request.ConditionExpression = BuiltCondition.Item1.Replace("$ARRAY_NAME$", _ElementName);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("DatabaseServiceAWS->AddElementsToArrayItem: Condition is not valid for this operation.");
                    return false;
                }
            }

            //Update item in the table
            try
            {
                using (var _UpdateItem = DynamoDBClient.UpdateItemAsync(Request))
                {
                    _UpdateItem.Wait();

                    if (_ReturnItemBehaviour != EReturnItemBehaviour.DoNotReturn)
                    {
                        var Response = _UpdateItem.Result;

                        if (Response != null && Response.Attributes != null)
                        {
                            _ReturnItem = JObject.Parse(Document.FromAttributeMap(Response.Attributes).ToJson());
                            Utility.SortJObject(_ReturnItem, true);
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("DatabaseServiceAWS->AddElementsToArrayItem: DynamoDBClient.UpdateItem returned null.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!(e is ConditionalCheckFailedException))
                {
                    _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->AddElementsToArrayItem: {e.Message}, Trace: { e.StackTrace}");
                }
                return false;
            }
            return true;
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

            if (DynamoDBClient == null)
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->RemoveElementsFromArrayItem: DynamoDBClient is null.");
                return false;
            }

            if (_KeyValue == null)
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->RemoveElementsFromArrayItem: Key is null.");
                return false;
            }

            UpdateItemRequest Request = new UpdateItemRequest
            {
                TableName = _Table,
                Key = new Dictionary<string, AttributeValue>()
            };

            if (_KeyValue.Type == EPrimitiveTypeEnum.Integer)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsInteger.ToString() };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.Double)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsDouble.ToString() };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.String)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.AsString };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.ByteArray)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.ToString() };
            }

            //Set return value expectation
            if (_ReturnItemBehaviour == EReturnItemBehaviour.DoNotReturn)
            {
                Request.ReturnValues = ReturnValue.NONE;
            }
            else if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllOld)
            {
                Request.ReturnValues = ReturnValue.ALL_OLD;
            }
            else
            {
                Request.ReturnValues = ReturnValue.ALL_NEW;
            }

            var SetAsList = new List<string>();
            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (ExpectedType == EPrimitiveTypeEnum.Integer || ExpectedType == EPrimitiveTypeEnum.Double)
                {
                    if (_ElementValueEntry.Type == EPrimitiveTypeEnum.Integer)
                    {
                        SetAsList.Add(_ElementValueEntry.AsInteger.ToString());
                    }
                    else
                    {
                        SetAsList.Add(_ElementValueEntry.AsDouble.ToString());
                    }
                }
                else
                {
                    if (ExpectedType == EPrimitiveTypeEnum.ByteArray)
                    {
                        SetAsList.Add(_ElementValueEntry.ToString());
                    }
                    else
                    {
                        SetAsList.Add(_ElementValueEntry.AsString);
                    }
                }
            }

            Request.ExpressionAttributeNames = new Dictionary<string, string>()
            {
                {
                    "#V", _ElementName
                }
            };
            Request.UpdateExpression = "DELETE #V :vals";

            if (ExpectedType == EPrimitiveTypeEnum.Integer || ExpectedType == EPrimitiveTypeEnum.Double)
            {
                Request.ExpressionAttributeValues.Add(":vals", new AttributeValue { NS = SetAsList });
            }
            else
            {
                Request.ExpressionAttributeValues.Add(":vals", new AttributeValue { SS = SetAsList });
            }

            //Update item in the table
            try
            {
                using (var _UpdateItem = DynamoDBClient.UpdateItemAsync(Request))
                {
                    _UpdateItem.Wait();

                    if (_ReturnItemBehaviour != EReturnItemBehaviour.DoNotReturn)
                    {
                        var Response = _UpdateItem.Result;

                        if (Response != null && Response.Attributes != null)
                        {
                            _ReturnItem = JObject.Parse(Document.FromAttributeMap(Response.Attributes).ToJson());
                            Utility.SortJObject(_ReturnItem, true);
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("DatabaseServiceAWS->RemoveElementsFromArrayItem: DynamoDBClient.UpdateItem returned null.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!(e is ConditionalCheckFailedException))
                {
                    _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->RemoveElementsFromArrayItem: {e.Message}, Trace: { e.StackTrace}");
                }
                return false;
            }
            return true;
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
            Action<string> _ErrorMessageAction = null)
        {
            _NewValue = 0.0f;

            if (DynamoDBClient == null)
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->IncrementOrDecrementItemValue: DynamoDBClient is null.");
                return false;
            }

            if (_KeyValue == null)
            {
                _ErrorMessageAction?.Invoke("DatabaseServiceAWS->IncrementOrDecrementItemValue: Key is null.");
                return false;
            }

            UpdateItemRequest Request = new UpdateItemRequest
            {
                TableName = _Table,
                Key = new Dictionary<string, AttributeValue>()
            };

            if (_KeyValue.Type == EPrimitiveTypeEnum.Integer)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsInteger.ToString() };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.Double)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsDouble.ToString() };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.String)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.AsString };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.ByteArray)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.ToString() };
            }

            //Set return value expectation
            Request.ReturnValues = ReturnValue.UPDATED_NEW;

            Request.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":incr"] = new AttributeValue
                {
                    N = (_IncrementOrDecrementBy * (_bDecrement ? -1 : 1)).ToString()
                },
                [":start"] = new AttributeValue
                {
                    N = "0"
                }
            };
            Request.ExpressionAttributeNames = new Dictionary<string, string>()
            {
                {
                    "#V", _ValueAttribute
                }
            };
            Request.UpdateExpression = "SET #V = if_not_exists(#V, :start) + :incr";

            //Update item in the table
            try
            {
                using (var _UpdateItem = DynamoDBClient.UpdateItemAsync(Request))
                {
                    _UpdateItem.Wait();

                    var Response = _UpdateItem.Result;

                    if (Response != null && Response.Attributes != null && Response.Attributes.ContainsKey(_ValueAttribute))
                    {
                        if (!double.TryParse(Response.Attributes[_ValueAttribute].N, out _NewValue))
                        {
                            _ErrorMessageAction?.Invoke("DatabaseServiceAWS->IncrementOrDecrementItemValue: Cast from returned attribute to double has failed.");
                            return false;
                        }
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("DatabaseServiceAWS->IncrementOrDecrementItemValue: DynamoDBClient.UpdateItem returned null.");
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->IncrementOrDecrementItemValue: {e.Message}, Trace: { e.StackTrace}");
                return false;
            }
            return true;
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
            Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            //Try getting table definition
            if (LoadStoreAndGetTable(_Table, out Table TableObject, _ErrorMessageAction))
            {
                DeleteItemOperationConfig Config = new DeleteItemOperationConfig();

                //Set return value expectation
                if (_ReturnItemBehaviour == EReturnItemBehaviour.DoNotReturn)
                {
                    Config.ReturnValues = ReturnValues.None;
                }
                else if (_ReturnItemBehaviour == EReturnItemBehaviour.ReturnAllNew)
                {
                    Config.ReturnValues = ReturnValues.AllNewAttributes;
                }
                else
                {
                    Config.ReturnValues = ReturnValues.AllOldAttributes;
                }

                //Delete item from the table
                try
                {
                    using (var _DeleteItem = TableObject.DeleteItemAsync(_KeyValue.ToString(), Config))
                    {
                        _DeleteItem.Wait();

                        var ReturnedDocument = _DeleteItem.Result;

                        if (_ReturnItemBehaviour == EReturnItemBehaviour.DoNotReturn)
                        {
                            return true;
                        }
                        else if (ReturnedDocument != null)
                        {
                            //Convert to string and parse as JObject
                            _ReturnItem = JObject.Parse(ReturnedDocument.ToJson());
                            Utility.SortJObject(_ReturnItem, true);
                            return true;
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("DatabaseServiceAWS->DeleteItem: TableObject.DeleteItem returned null.");
                        }
                    }
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->DeleteItem: {e.Message}, Trace: { e.StackTrace}");
                    return false;
                }
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

            //Try getting table definition
            if (LoadStoreAndGetTable(_Table, out Table TableObject, _ErrorMessageAction))
            {
                var Config = new ScanOperationConfig()
                {
                    Select = SelectValues.AllAttributes
                };

                //Scan the table
                Search ReturnedSearch = null;

                try
                {
                    ReturnedSearch = TableObject.Scan(Config);
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->ScanTable: {e.Message}, Trace: { e.StackTrace}");
                    return false;
                }

                if (ReturnedSearch != null)
                {
                    List<JObject> TempResults = new List<JObject>();
                    try
                    {
                        do
                        {
                            using (var _GetNextSet = ReturnedSearch.GetNextSetAsync())
                            {
                                _GetNextSet.Wait();
                                List<Document> DocumentList = _GetNextSet.Result;

                                foreach (var Document in DocumentList)
                                {
                                    var CreatedJson = JObject.Parse(Document.ToJson());
                                    Utility.SortJObject(CreatedJson, true);
                                    TempResults.Add(CreatedJson);
                                }
                            }
                        }
                        while (!ReturnedSearch.IsDone);

                        _ReturnItem = TempResults;                        
                    }
                    catch (Newtonsoft.Json.JsonReaderException e)
                    {
                        _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->ScanTable: JsonReaderException: {e.Message}, Trace: { e.StackTrace}");
                        return false;
                    }
                    return true;
                }
                else
                {
                    _ErrorMessageAction?.Invoke("DatabaseServiceAWS->ScanTable: TableObject.ScanTable returned null.");
                }
            }
            return false;
        }

        private class BAttributeEqualsConditionDynamodb : DatabaseAttributeCondition
        {
            public BAttributeEqualsConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeEquals)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    $"{Attribute} = :val",
                    new Tuple<string, PrimitiveType>(":val", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeEqualsCondition(string Attribute, PrimitiveType Value)
        {
            return new BAttributeEqualsConditionDynamodb(Attribute, Value);
        }

        private class BAttributeNotEqualsConditionDynamodb : DatabaseAttributeCondition
        {
            public BAttributeNotEqualsConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeNotEquals)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    $"{Attribute} <> :val",
                    new Tuple<string, PrimitiveType>(":val", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeNotEqualsCondition(string Attribute, PrimitiveType Value)
        {
            return new BAttributeNotEqualsConditionDynamodb(Attribute, Value);
        }

        private class BAttributeGreaterConditionDynamodb : DatabaseAttributeCondition
        {
            public BAttributeGreaterConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeGreater)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    $"{Attribute} > :val",
                    new Tuple<string, PrimitiveType>(":val", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeGreaterCondition(string Attribute, PrimitiveType Value)
        {
            return new BAttributeGreaterConditionDynamodb(Attribute, Value);
        }

        private class BAttributeGreaterOrEqualConditionDynamodb : DatabaseAttributeCondition
        {
            public BAttributeGreaterOrEqualConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeGreaterOrEqual)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    $"{Attribute} >= :val",
                    new Tuple<string, PrimitiveType>(":val", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeGreaterOrEqualCondition(string Attribute, PrimitiveType Value)
        {
            return new BAttributeGreaterOrEqualConditionDynamodb(Attribute, Value);
        }

        private class BAttributeLessConditionDynamodb : DatabaseAttributeCondition
        {
            public BAttributeLessConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeLess)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    $"{Attribute} < :val",
                    new Tuple<string, PrimitiveType>(":val", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeLessCondition(string Attribute, PrimitiveType Value)
        {
            return new BAttributeLessConditionDynamodb(Attribute, Value);
        }

        private class BAttributeLessOrEqualConditionDynamodb : DatabaseAttributeCondition
        {
            public BAttributeLessOrEqualConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeLessOrEqual)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    $"{Attribute} <= :val",
                    new Tuple<string, PrimitiveType>(":val", Value)
                );
            }
        }
        public DatabaseAttributeCondition BuildAttributeLessOrEqualCondition(string Attribute, PrimitiveType Value)
        {
            return new BAttributeLessOrEqualConditionDynamodb(Attribute, Value);
        }

        private class BAttributeExistsConditionDynamodb : DatabaseAttributeCondition
        {
            public BAttributeExistsConditionDynamodb(string Attribute) : base(EDatabaseAttributeConditionType.AttributeExists)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>($"attribute_exists({Attribute})", null);
            }
        }
        public DatabaseAttributeCondition BuildAttributeExistsCondition(string Attribute)
        {
            return new BAttributeExistsConditionDynamodb(Attribute);
        }

        private class BAttributeNotExistConditionDynamodb : DatabaseAttributeCondition
        {
            public BAttributeNotExistConditionDynamodb(string Attribute) : base(EDatabaseAttributeConditionType.AttributeNotExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>($"attribute_not_exists({Attribute})", null);
            }
        }
        public DatabaseAttributeCondition BuildAttributeNotExistCondition(string Attribute)
        {
            return new BAttributeNotExistConditionDynamodb(Attribute);
        }

        private class BArrayElementNotExistConditionDynamodb : DatabaseAttributeCondition
        {
            public BArrayElementNotExistConditionDynamodb(PrimitiveType ArrayElement) : base(EDatabaseAttributeConditionType.ArrayElementNotExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    "NOT CONTAINS $ARRAY_NAME$ :cond_val",
                    new Tuple<string, PrimitiveType>(":cond_val", ArrayElement)
                );
            }
        }
        public DatabaseAttributeCondition BuildArrayElementNotExistCondition(PrimitiveType ArrayElement)
        {
            return new BArrayElementNotExistConditionDynamodb(ArrayElement);
        }
    }
}