/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using CommonUtilities;

namespace CloudServiceUtilities
{
    /// <summary>
    /// <para>After performing an operation that causes a change in an item, defines what service shall return</para>
    /// </summary>
    public enum EReturnItemBehaviour
    {
        DoNotReturn,
        ReturnAllOld,
        ReturnAllNew
    };

    public enum EDatabaseAttributeConditionType
    {
        AttributeEquals,
        AttributeNotEquals,
        AttributeGreater,
        AttributeGreaterOrEqual,
        AttributeLess,
        AttributeLessOrEqual,
        AttributeExists,
        AttributeNotExist,
        ArrayElementExist,
        ArrayElementNotExist
    };

    public enum EAutoSortArrays
    {
        No,
        Yes
    };
    public enum EAutoConvertRoundableFloatToInt
    {
        No,
        Yes
    };
    public class DatabaseOptions
    {
        public EAutoSortArrays AutoSortArrays = EAutoSortArrays.No;
        public EAutoConvertRoundableFloatToInt AutoConvertRoundableFloatToInt = EAutoConvertRoundableFloatToInt.No;
    }

    public abstract class DatabaseAttributeCondition
    {
        public readonly EDatabaseAttributeConditionType AttributeConditionType;

        private DatabaseAttributeCondition() {}
        protected DatabaseAttributeCondition(EDatabaseAttributeConditionType _AttributeConditionType)
        {
            AttributeConditionType = _AttributeConditionType;
        }

        protected Tuple<string, Tuple<string, PrimitiveType>> BuiltCondition;
        public Tuple<string, Tuple<string, PrimitiveType>> GetBuiltCondition()
        {
            if (BuiltCondition != null && BuiltCondition.Item1 != null)
            {
                return new Tuple<string, Tuple<string, PrimitiveType>>(BuiltCondition.Item1, BuiltCondition.Item2 != null ? new Tuple<string, PrimitiveType>(BuiltCondition.Item2.Item1, BuiltCondition.Item2.Item2) : null);
            }
            return null;
        }
    };

    public class DatabaseServiceBase
    {
        protected DatabaseServiceBase() {}

        protected Newtonsoft.Json.Linq.JToken FromPrimitiveTypeToJToken(PrimitiveType _Primitive)
        {
            switch (_Primitive.Type)
            {
                case EPrimitiveTypeEnum.Double:
                    return _Primitive.AsDouble;
                case EPrimitiveTypeEnum.Integer:
                    return _Primitive.AsInteger;
                case EPrimitiveTypeEnum.ByteArray:
                    return Convert.ToBase64String(_Primitive.AsByteArray);
                default:
                    return _Primitive.AsString;
            }
        }

        protected void AddKeyToJson(Newtonsoft.Json.Linq.JObject Destination, string _KeyName, PrimitiveType _KeyValue)
        {
            if (Destination != null && !Destination.ContainsKey(_KeyName))
            {
                Destination[_KeyName] = FromPrimitiveTypeToJToken(_KeyValue);
            }
        }

        public void SetOptions(DatabaseOptions _NewOptions)
        {
            Options = _NewOptions;
        }
        protected DatabaseOptions Options = new DatabaseOptions();

    }

    /// <summary>
    /// <para>Interface for abstracting Database Services to make it usable with multiple cloud solutions</para>
    /// </summary>
    public interface IDatabaseServiceInterface
    {
        /// <summary>
        /// 
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>Returns: Initialization succeed or failed</returns>
        /// 
        /// </summary>
        bool HasInitializationSucceed();

        DatabaseAttributeCondition BuildAttributeExistsCondition(string Attribute);
        DatabaseAttributeCondition BuildAttributeNotExistCondition(string Attribute);
        DatabaseAttributeCondition BuildAttributeEqualsCondition(string Attribute, PrimitiveType Value);
        DatabaseAttributeCondition BuildAttributeNotEqualsCondition(string Attribute, PrimitiveType Value);
        DatabaseAttributeCondition BuildAttributeGreaterCondition(string Attribute, PrimitiveType Value);
        DatabaseAttributeCondition BuildAttributeGreaterOrEqualCondition(string Attribute, PrimitiveType Value);
        DatabaseAttributeCondition BuildAttributeLessCondition(string Attribute, PrimitiveType Value);
        DatabaseAttributeCondition BuildAttributeLessOrEqualCondition(string Attribute, PrimitiveType Value);
        DatabaseAttributeCondition BuildArrayElementExistCondition(string Attribute, PrimitiveType ArrayElement);
        DatabaseAttributeCondition BuildArrayElementNotExistCondition(string Attribute, PrimitiveType ArrayElement);

        /// <summary>
        /// 
        /// <para>GetItem</para>
        /// 
        /// <para>Gets an item from a table, if _ValuesToGet is null; will retrieve all.</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                         Table name</para>
        /// <para><paramref name="_KeyName"/>                       Name of the key of item</para>
        /// <para><paramref name="_KeyValue"/>                      Value of the key of item</para>
        /// <para><paramref name="_ValuesToGet"/>                   Defines which values shall be retrieved</para>
        /// <para><paramref name="_Result"/>                        Result as JSON Object</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool GetItem(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            string[] _ValuesToGet,
            out Newtonsoft.Json.Linq.JObject _Result,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>GetItems</para>
        /// 
        /// <para>Gets items from a table</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                         Table name</para>
        /// <para><paramref name="_KeyName"/>                       Name of the key of item</para>
        /// <para><paramref name="_KeyValues"/>                     Values of the key of item that wished to be retrieved</para>
        /// <para><paramref name="_Result"/>                        Result as list of JSON Object</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool GetItems(
            string _Table,
            string _KeyName,
            PrimitiveType[] _KeyValues,
            out List<Newtonsoft.Json.Linq.JObject> _Result,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>PutItem</para>
        /// 
        /// <para>Puts an item to a table</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                         Table name</para>
        /// <para><paramref name="_KeyName"/>                       Name of the key of item</para>
        /// <para><paramref name="_KeyValue"/>                      Value of the key of item</para>
        /// <para><paramref name="_Item"/>                          Item to be put</para>
        /// <para><paramref name="_ReturnItem"/>                    In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ReturnItemBehaviour"/>           In case item exists, defines what service shall return</para>
        /// <para><paramref name="_bOverrideIfExist"/>              If there is a matching key with the value, should it override or fail?</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool PutItem(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            Newtonsoft.Json.Linq.JObject _Item,
            out Newtonsoft.Json.Linq.JObject _ReturnItem,
            EReturnItemBehaviour _ReturnItemBehaviour = EReturnItemBehaviour.DoNotReturn,
            bool _bOverrideIfExist = false,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>UpdateItem</para>
        /// 
        /// <para>Updates an item in a table</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                         Table name</para>
        /// <para><paramref name="_KeyName"/>                       Name of the key of item</para>
        /// <para><paramref name="_KeyValue"/>                      Value of the key of item</para>
        /// <para><paramref name="_UpdateItem"/>                    Item to be updated with</para>
        /// <para><paramref name="_ReturnItem"/>                    In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ReturnItemBehaviour"/>           In case item exists, defines what service shall return</para>
        /// <para><paramref name="_ConditionExpression"/>           Condition expression to be performed remotely</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool UpdateItem(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            Newtonsoft.Json.Linq.JObject _UpdateItem,
            out Newtonsoft.Json.Linq.JObject _ReturnItem,
            EReturnItemBehaviour _ReturnItemBehaviour = EReturnItemBehaviour.DoNotReturn,
            DatabaseAttributeCondition _ConditionExpression = null,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>AddElementsToArrayItem</para>
        /// 
        /// <para>Adds element to the array item</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                         Table name</para>
        /// <para><paramref name="_KeyName"/>                       Name of the key of array item</para>
        /// <para><paramref name="_KeyValue"/>                      Value of the key of array item</para>
        /// <para><paramref name="_ElementName"/>                   Name of the array element</para>
        /// <para><paramref name="_ElementValueEntries"/>           Items to be put into array element</para>
        /// <para><paramref name="_ReturnItem"/>                    In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ReturnItemBehaviour"/>           In case item exists, defines what service shall return</para>
        /// <para><paramref name="_ConditionExpression"/>           Condition expression to be performed remotely</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool AddElementsToArrayItem(
            string _Table, 
            string _KeyName,
            PrimitiveType _KeyValue, 
            string _ElementName,
            PrimitiveType[] _ElementValueEntries, 
            out Newtonsoft.Json.Linq.JObject _ReturnItem, 
            EReturnItemBehaviour _ReturnItemBehaviour = EReturnItemBehaviour.DoNotReturn, 
            DatabaseAttributeCondition _ConditionExpression = null, 
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>RemoveElementsFromArrayItem</para>
        /// 
        /// <para>Removes element from the array item</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                     Table name</para>
        /// <para><paramref name="_KeyName"/>                   Name of the key of array item</para>
        /// <para><paramref name="_KeyValue"/>                  Value of the key of array item</para>
        /// <para><paramref name="_ElementName"/>               Name of the array element</para>
        /// <para><paramref name="_ElementValueEntries"/>       Items to be removed from array element</para>
        /// <para><paramref name="_ReturnItem"/>                In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ReturnItemBehaviour"/>       In case item exists, defines what service shall return</para>
        /// <para><paramref name="_ConditionExpression"/>           Condition expression to be performed remotely</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool RemoveElementsFromArrayItem(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            string _ElementName,
            PrimitiveType[] _ElementValueEntries,
            out Newtonsoft.Json.Linq.JObject _ReturnItem,
            EReturnItemBehaviour _ReturnItemBehaviour = EReturnItemBehaviour.DoNotReturn,
            DatabaseAttributeCondition _ConditionExpression = null,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>IncrementOrDecrementItemValue</para>
        /// 
        /// <para>Updates an item in a table, if item does not exist, creates a new one with only increment/decrement value</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                         Table name</para>
        /// <para><paramref name="_KeyName"/>                       Name of the key of item</para>
        /// <para><paramref name="_KeyValue"/>                      Value of the key of item</para>
        /// <para><paramref name="_NewValue"/>                      New value after increment/decrement</para>
        /// <para><paramref name="_ValueAttribute"/>                Name of the value</para>
        /// <para><paramref name="_IncrementOrDecrementBy"/>        Increment or decrement the value by this</para>
        /// <para><paramref name="_bDecrement"/>                    If true, will be decremented, otherwise incremented</para>
        /// <para><paramref name="_ConditionExpression"/>           Condition expression to be performed remotely</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool IncrementOrDecrementItemValue(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            out double _NewValue,
            string _ValueAttribute,
            double _IncrementOrDecrementBy,
            bool _bDecrement = false,
            DatabaseAttributeCondition _ConditionExpression = null,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>DeleteItem</para>
        /// 
        /// <para>Deletes an item from a table</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                     Table name</para>
        /// <para><paramref name="_KeyName"/>                   Name of the key of item</para>
        /// <para><paramref name="_KeyValue"/>                  Value of the key of item</para>
        /// <para><paramref name="_ReturnItem"/>                In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ReturnItemBehaviour"/>       In case item exists, defines what service shall return</para>
        /// <para><paramref name="_ConditionExpression"/>           Condition expression to be performed remotely</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool DeleteItem(
            string _Table,
            string _KeyName,
            PrimitiveType _KeyValue,
            out Newtonsoft.Json.Linq.JObject _ReturnItem,
            EReturnItemBehaviour _ReturnItemBehaviour = EReturnItemBehaviour.DoNotReturn,
            DatabaseAttributeCondition _ConditionExpression = null,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>ScanTable</para>
        /// 
        /// <para>Scans the table for attribute specified by _Key</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                 Table name</para>
        /// <para><paramref name="_PossibleKeyNames"/>      Names of the keys in table</para>
        /// <para><paramref name="_ReturnItem"/>            In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ErrorMessageAction"/>    Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                              Operation success</returns>
        /// 
        /// </summary>
        bool ScanTable(
            string _Table,
            string[] _PossibleKeyNames,
            out List<Newtonsoft.Json.Linq.JObject> _ReturnItem,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>ScanTableFilterBy</para>
        /// 
        /// <para>Scans the table for attribute specified by _Key, filtered by the _FilterBy condition</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                 Table name</para>
        /// <para><paramref name="_PossibleKeyNames"/>      Names of the keys in table</para
        /// <para><paramref name="_FilterBy"/>              Filter each item to be returned by the scan operation</para>
        /// <para><paramref name="_ReturnItem"/>            In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ErrorMessageAction"/>    Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                              Operation success</returns>
        /// 
        /// </summary>
        bool ScanTableFilterBy(
            string _Table,
            string[] _PossibleKeyNames,
            DatabaseAttributeCondition _FilterBy,
            out List<Newtonsoft.Json.Linq.JObject> _ReturnItem,
            Action<string> _ErrorMessageAction = null);
    }
}