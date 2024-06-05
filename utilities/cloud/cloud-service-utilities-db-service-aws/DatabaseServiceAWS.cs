/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using CommonUtilities;

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

            var Request = new QueryRequest
            {
                TableName = _Table,
                KeyConditionExpression = $"{_KeyName} = :key_val",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>(),
                ProjectionExpression = _KeyName, //What to get
                ConsistentRead = true
            };
            if (_OptionalConditionExpression != null)
            {
                var BuiltCondition = _OptionalConditionExpression.GetBuiltCondition();

                Request.KeyConditionExpression += $" and {BuiltCondition.Item1}";
                if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.Integer)
                {
                    Request.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = new AttributeValue { N = BuiltCondition.Item2.Item2.AsInteger.ToString() };
                }
                else if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.Double)
                {
                    Request.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = new AttributeValue { N = BuiltCondition.Item2.Item2.AsDouble.ToString() };
                }
                else if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.String)
                {
                    Request.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = new AttributeValue { S = BuiltCondition.Item2.Item2.AsString };
                }
                else if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.ByteArray)
                {
                    Request.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = new AttributeValue { S = BuiltCondition.Item2.Item2.ToString() };
                }
            }

            if (_KeyValue.Type == EPrimitiveTypeEnum.Integer)
            {
                Request.ExpressionAttributeValues[":key_val"] = new AttributeValue { N = _KeyValue.AsInteger.ToString() };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.Double)
            {
                Request.ExpressionAttributeValues[":key_val"] = new AttributeValue { N = _KeyValue.AsDouble.ToString() };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.String)
            {
                Request.ExpressionAttributeValues[":key_val"] = new AttributeValue { S = _KeyValue.AsString };
            }
            else if (_KeyValue.Type == EPrimitiveTypeEnum.ByteArray)
            {
                Request.ExpressionAttributeValues[":key_val"] = new AttributeValue { S = _KeyValue.ToString() };
            }

            try
            {
                using (var _Query = DynamoDBClient.QueryAsync(Request))
                {
                    _Query.Wait();

                    var ReturnedDocument = _Query.Result;
                    _bExistAndConditionSatisfied = ReturnedDocument != null && ReturnedDocument.Count > 0;
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->DoesItemExistWhichSatisfyOptionalCondition: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
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
                                if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                                {
                                    Utility.SortJObject(
                                        AsJObject,
                                        Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                                }
                                else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                                {
                                    Utility.ConvertRoundFloatToIntAllInJObject(AsJObject);
                                }

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
            bool _bOverrideIfExist = false,
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
                    if (!_bOverrideIfExist)
                    {
                        Config.ConditionalExpression = BuildConditionalExpression(new AttributeNotExistConditionDynamodb(_KeyName));
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
                                if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                                {
                                    Utility.SortJObject(
                                        _ReturnItem,
                                        Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                                }
                                else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                                {
                                    Utility.ConvertRoundFloatToIntAllInJObject(_ReturnItem);
                                }
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
                    Config.ConditionalExpression = BuildConditionalExpression(_ConditionExpression);

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
                                if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                                {
                                    Utility.SortJObject(
                                        _ReturnItem,
                                        Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                                }
                                else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                                {
                                    Utility.ConvertRoundFloatToIntAllInJObject(_ReturnItem);
                                }
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

            if (BuildConditionalExpression(_ConditionExpression, Request.ExpressionAttributeValues, out string FinalConditionExpression))
                Request.ConditionExpression = FinalConditionExpression;

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
                            if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                            {
                                Utility.SortJObject(
                                    _ReturnItem,
                                    Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                            }
                            else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                            {
                                Utility.ConvertRoundFloatToIntAllInJObject(_ReturnItem);
                            }
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

            if (BuildConditionalExpression(_ConditionExpression, Request.ExpressionAttributeValues, out string FinalConditionExpression))
                Request.ConditionExpression = FinalConditionExpression;

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
                            if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                            {
                                Utility.SortJObject(
                                    _ReturnItem,
                                    Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                            }
                            else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                            {
                                Utility.ConvertRoundFloatToIntAllInJObject(_ReturnItem);
                            }
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
            DatabaseAttributeCondition _ConditionExpression = null,
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

            if (BuildConditionalExpression(_ConditionExpression, Request.ExpressionAttributeValues, out string FinalConditionExpression))
                Request.ConditionExpression = FinalConditionExpression;

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
            DatabaseAttributeCondition _ConditionExpression = null,
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

                //Set condition expression
                Config.ConditionalExpression = BuildConditionalExpression(_ConditionExpression);

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
                            if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                            {
                                Utility.SortJObject(
                                    _ReturnItem,
                                    Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                            }
                            else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                            {
                                Utility.ConvertRoundFloatToIntAllInJObject(_ReturnItem);
                            }
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
            return Internal_ScanTable(_Table, _PossibleKeyNames, out _ReturnItem, null, _ErrorMessageAction);
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
            bool _bPaginateBackwards,
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
            return Internal_ScanTable(_Table, _PossibleKeyNames, out _ReturnItem, BuildConditionalExpression(_FilterBy), _ErrorMessageAction);
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
            bool _bPaginateBackwards,
            out List<JObject> _ReturnItem,
            bool _RetrieveTotalElementsFound,
            out long _TotalElementFound,
            Action<string> _ErrorMessageAction = null)
        {
            throw new NotImplementedException();
        }

        private bool Internal_ScanTable(
            string _Table,
            string[] _PossibleKeyNames,
            out List<JObject> _ReturnItem,
            Expression _ConditionalExpression = null,
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
                if (_ConditionalExpression != null)
                {
                    Config.FilterExpression = _ConditionalExpression;
                }

                //Scan the table
                Search ReturnedSearch = null;

                try
                {
                    ReturnedSearch = TableObject.Scan(Config);
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->Internal_ScanTable: {e.Message}, Trace: {e.StackTrace}");
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
                                    if (Options.AutoSortArrays == EAutoSortArrays.Yes)
                                    {
                                        Utility.SortJObject(
                                            CreatedJson,
                                            Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes);
                                    }
                                    else if (Options.AutoConvertRoundableFloatToInt == EAutoConvertRoundableFloatToInt.Yes)
                                    {
                                        Utility.ConvertRoundFloatToIntAllInJObject(CreatedJson);
                                    }
                                    TempResults.Add(CreatedJson);
                                }
                            }
                        }
                        while (!ReturnedSearch.IsDone);

                        _ReturnItem = TempResults;
                    }
                    catch (Newtonsoft.Json.JsonReaderException e)
                    {
                        _ErrorMessageAction?.Invoke($"DatabaseServiceAWS->Internal_ScanTable: JsonReaderException: {e.Message}, Trace: {e.StackTrace}");
                        return false;
                    }
                    return true;
                }
                else
                {
                    _ErrorMessageAction?.Invoke("DatabaseServiceAWS->Internal_ScanTable: TableObject.ScanTable returned null.");
                }
            }
            return false;
        }
        
        private Expression BuildConditionalExpression(DatabaseAttributeCondition _ConditionExpression)
        {
            if (_ConditionExpression == null) return null;

            var BuiltCondition = _ConditionExpression.GetBuiltCondition();
            if (BuiltCondition != null)
            {
                var ConditionalExpression = new Expression
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
                return ConditionalExpression;
            }
            return null;
        }
        private bool BuildConditionalExpression(DatabaseAttributeCondition _ConditionExpression, Dictionary<string, AttributeValue> _ExpressionAttributeValues, out string _FinalConditionExpression)
        {
            _FinalConditionExpression = null;
            if (_ConditionExpression == null) return false;

            var BuiltCondition = _ConditionExpression.GetBuiltCondition();

            if (BuiltCondition.Item2 != null)
            {
                if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.Integer)
                {
                    _ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { NS = new List<string>() { BuiltCondition.Item2.Item2.AsInteger.ToString() } });
                }
                else if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.Double)
                {
                    _ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { NS = new List<string>() { BuiltCondition.Item2.Item2.AsDouble.ToString() } });
                }
                else if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.String)
                {
                    _ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { SS = new List<string>() { BuiltCondition.Item2.Item2.AsString } });
                }
                else if (BuiltCondition.Item2.Item2.Type == EPrimitiveTypeEnum.ByteArray)
                {
                    _ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { SS = new List<string>() { BuiltCondition.Item2.Item2.ToString() } });
                }
            }
            _FinalConditionExpression = BuiltCondition.Item1;
            return true;
        }

        private class AttributeEqualsConditionDynamodb : DatabaseAttributeCondition
        {
            public AttributeEqualsConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeEquals)
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
            return new AttributeEqualsConditionDynamodb(Attribute, Value);
        }

        private class AttributeNotEqualsConditionDynamodb : DatabaseAttributeCondition
        {
            public AttributeNotEqualsConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeNotEquals)
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
            return new AttributeNotEqualsConditionDynamodb(Attribute, Value);
        }

        private class AttributeGreaterConditionDynamodb : DatabaseAttributeCondition
        {
            public AttributeGreaterConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeGreater)
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
            return new AttributeGreaterConditionDynamodb(Attribute, Value);
        }

        private class AttributeGreaterOrEqualConditionDynamodb : DatabaseAttributeCondition
        {
            public AttributeGreaterOrEqualConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeGreaterOrEqual)
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
            return new AttributeGreaterOrEqualConditionDynamodb(Attribute, Value);
        }

        private class AttributeLessConditionDynamodb : DatabaseAttributeCondition
        {
            public AttributeLessConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeLess)
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
            return new AttributeLessConditionDynamodb(Attribute, Value);
        }

        private class AttributeLessOrEqualConditionDynamodb : DatabaseAttributeCondition
        {
            public AttributeLessOrEqualConditionDynamodb(string Attribute, PrimitiveType Value) : base(EDatabaseAttributeConditionType.AttributeLessOrEqual)
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
            return new AttributeLessOrEqualConditionDynamodb(Attribute, Value);
        }

        private class AttributeExistsConditionDynamodb : DatabaseAttributeCondition
        {
            public AttributeExistsConditionDynamodb(string Attribute) : base(EDatabaseAttributeConditionType.AttributeExists)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>($"attribute_exists({Attribute})", null);
            }
        }
        public DatabaseAttributeCondition BuildAttributeExistsCondition(string Attribute)
        {
            return new AttributeExistsConditionDynamodb(Attribute);
        }

        private class AttributeNotExistConditionDynamodb : DatabaseAttributeCondition
        {
            public AttributeNotExistConditionDynamodb(string Attribute) : base(EDatabaseAttributeConditionType.AttributeNotExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>($"attribute_not_exists({Attribute})", null);
            }
        }
        public DatabaseAttributeCondition BuildAttributeNotExistCondition(string Attribute)
        {
            return new AttributeNotExistConditionDynamodb(Attribute);
        }

        private class ArrayElementExistConditionDynamodb : DatabaseAttributeCondition
        {
            public ArrayElementExistConditionDynamodb(string Attribute, PrimitiveType ArrayElement) : base(EDatabaseAttributeConditionType.ArrayElementExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    $"CONTAINS {Attribute} :cond_val",
                    new Tuple<string, PrimitiveType>(":cond_val", ArrayElement)
                );
            }
        }
        public DatabaseAttributeCondition BuildArrayElementExistCondition(string Attribute, PrimitiveType ArrayElement)
        {
            return new ArrayElementExistConditionDynamodb(Attribute, ArrayElement);
        }

        private class ArrayElementNotExistConditionDynamodb : DatabaseAttributeCondition
        {
            public ArrayElementNotExistConditionDynamodb(string Attribute, PrimitiveType ArrayElement) : base(EDatabaseAttributeConditionType.ArrayElementNotExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, PrimitiveType>>
                (
                    $"NOT CONTAINS {Attribute} :cond_val",
                    new Tuple<string, PrimitiveType>(":cond_val", ArrayElement)
                );
            }
        }
        public DatabaseAttributeCondition BuildArrayElementNotExistCondition(string Attribute, PrimitiveType ArrayElement)
        {
            return new ArrayElementNotExistConditionDynamodb(Attribute, ArrayElement);
        }
    }
}