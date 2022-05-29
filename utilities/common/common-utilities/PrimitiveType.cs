/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CommonUtilities
{
    public enum EPrimitiveTypeEnum
    {
        String,
        Integer,
        Double,
        ByteArray
    };
    public sealed class PrimitiveType
    {
        public EPrimitiveTypeEnum Type { get; }

        public PrimitiveType(PrimitiveType _Other)
        {
            Type = _Other.Type;
            switch (Type)
            {
                case EPrimitiveTypeEnum.Double:
                    AsDouble = _Other.AsDouble;
                    break;
                case EPrimitiveTypeEnum.Integer:
                    AsInteger = _Other.AsInteger;
                    break;
                case EPrimitiveTypeEnum.ByteArray:
                    AsByteArray = _Other.AsByteArray;
                    break;
                default:
                    AsString = _Other.AsString;
                    break;
            }
        }

        public string AsString { get; }
        public PrimitiveType(string _Str)
        {
            Type = EPrimitiveTypeEnum.String;
            AsString = _Str;
        }

        public long AsInteger { get; }
        public PrimitiveType(long _Int)
        {
            Type = EPrimitiveTypeEnum.Integer;
            AsInteger = _Int;
        }

        public double AsDouble { get; }
        public PrimitiveType(double _Double)
        {
            Type = EPrimitiveTypeEnum.Double;
            AsDouble = _Double;
        }

        public byte[] AsByteArray { get; }
        public PrimitiveType(byte[] _ByteArray)
        {
            Type = EPrimitiveTypeEnum.ByteArray;
            AsByteArray = _ByteArray;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case EPrimitiveTypeEnum.Double:
                    return AsDouble.ToString();
                case EPrimitiveTypeEnum.Integer:
                    return AsInteger.ToString();
                case EPrimitiveTypeEnum.ByteArray:
                    return Convert.ToBase64String(AsByteArray);
                default:
                    return AsString;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                PrimitiveType Casted = (PrimitiveType)obj;
                if (Casted != null && Casted.Type == Type)
                {
                    return (Casted.Type == EPrimitiveTypeEnum.Double && Casted.AsDouble == AsDouble) ||
                        (Casted.Type == EPrimitiveTypeEnum.Integer && Casted.AsInteger == AsInteger) ||
                        (Casted.Type == EPrimitiveTypeEnum.String && Casted.AsString == AsString) ||
                        (Casted.Type == EPrimitiveTypeEnum.ByteArray && Casted.ToString() == ToString());
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            var HashCode = 674144506;
            HashCode = HashCode * -1521134295 + Type.GetHashCode();
            HashCode = HashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AsString);
            HashCode = HashCode * -1521134295 + AsInteger.GetHashCode();
            HashCode = HashCode * -1521134295 + AsDouble.GetHashCode();
            if (AsByteArray != null)
            {
                HashCode = HashCode * -1521134295 + AsByteArray.ToString().GetHashCode();
            }
            return HashCode;
        }
    }

    public sealed class PrimitiveType_JStringified
    {
        public const string KEY_VALUE_PRIMITIVE_TYPE_PROPERTY = "keyValuePrimitiveType";
        public const string KEY_VALUE_STRINGIFIED_PROPERTY = "keyValueStringified";

        [JsonProperty(KEY_VALUE_PRIMITIVE_TYPE_PROPERTY)]
        public int KeyValuePrimitiveType;

        [JsonProperty(KEY_VALUE_STRINGIFIED_PROPERTY)]
        public string KeyValueStringified;

        public PrimitiveType GetKeyValuePrimitiveReference()
        {
            switch ((EPrimitiveTypeEnum)KeyValuePrimitiveType)
            {
                case EPrimitiveTypeEnum.ByteArray:
                    return new PrimitiveType(Convert.FromBase64String(KeyValueStringified));
                case EPrimitiveTypeEnum.Double:
                    return new PrimitiveType(double.Parse(KeyValueStringified));
                case EPrimitiveTypeEnum.Integer:
                    return new PrimitiveType(long.Parse(KeyValueStringified));
                case EPrimitiveTypeEnum.String:
                    return new PrimitiveType(KeyValueStringified);
            }
            return null;
        }
        public void SetKeyValuePrimitiveReference(PrimitiveType _PrimitiveRef)
        {
            KeyValuePrimitiveType = (int)_PrimitiveRef.Type;
            switch (_PrimitiveRef.Type)
            {
                case EPrimitiveTypeEnum.ByteArray:
                    KeyValueStringified = Convert.ToBase64String(_PrimitiveRef.AsByteArray);
                    break;
                case EPrimitiveTypeEnum.Double:
                    KeyValueStringified = $"{_PrimitiveRef.AsDouble}";
                    break;
                case EPrimitiveTypeEnum.Integer:
                    KeyValueStringified = $"{_PrimitiveRef.AsInteger}";
                    break;
                case EPrimitiveTypeEnum.String:
                    KeyValueStringified = _PrimitiveRef.AsString;
                    break;
            }
        }

        public PrimitiveType_JStringified() { }
        public PrimitiveType_JStringified(PrimitiveType _PrimitiveRef)
        {
            SetKeyValuePrimitiveReference(_PrimitiveRef);
        }

        public static PrimitiveType_JStringified[] ConvertPrimitivesToPrimitiveTypeStructs(PrimitiveType[] _Array)
        {
            if (_Array == null || _Array.Length == 0) return null;

            var Result = new PrimitiveType_JStringified[_Array.Length];

            var i = 0;
            foreach (var Element in _Array)
            {
                Result[i] = new PrimitiveType_JStringified(Element);
                i++;
            }
            return Result;
        }
    }
}