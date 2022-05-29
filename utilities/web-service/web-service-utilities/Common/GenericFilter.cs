/// Copyright 2022- Burak Kara, All rights reserved.

using Newtonsoft.Json;

namespace WebServiceUtilities.Common
{
    public class GenericFilter
    {
        public const string FILTER_KEY_PROPERTY = "filterKey";
        public const string FILTER_VALUE_PROPERTY = "filterValue";

        [JsonProperty(FILTER_KEY_PROPERTY)]
        public string FilterKey = "";

        [JsonProperty(FILTER_VALUE_PROPERTY)]
        public string FilterValue = "";
    }
}
