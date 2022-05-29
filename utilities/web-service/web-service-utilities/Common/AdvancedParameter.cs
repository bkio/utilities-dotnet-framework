/// Copyright 2022- Burak Kara, All rights reserved.

using Newtonsoft.Json;

namespace WebServiceUtilities.Common
{
    public class AdvancedParameter
    {
        public const string PARAMETER_NAME_PROPERTY = "paramName";
        public const string PARAMETER_TYPE_PROPERTY = "paramType";
        public const string PARAMETER_VALUE_PROPERTY = "paramValue";

        [JsonProperty(PARAMETER_NAME_PROPERTY)]
        public string ParameterName = "";

        [JsonProperty(PARAMETER_TYPE_PROPERTY)]
        public string ParameterType = "";

        [JsonProperty(PARAMETER_VALUE_PROPERTY)]
        public string ParameterValue = "";
    }
}
