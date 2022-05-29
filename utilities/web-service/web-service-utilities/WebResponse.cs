/// Copyright 2022- Burak Kara, All rights reserved.

using CommonUtilities;
using Newtonsoft.Json.Linq;

namespace WebServiceUtilities
{
    public static class WebResponse
    {
        public static string GetResponseContentTypeFromFailCode(int _Code)
        {
            switch (_Code)
            {
                case 501:
                    return Error_NotImplemented_ContentType;
                case 500:
                    return Error_InternalError_ContentType;
                case 415:
                    return Error_UnsupportedMediaType_ContentType;
                case 409:
                    return Error_Conflict_ContentType;
                case 406:
                    return Error_NotAcceptable_ContentType;
                case 405:
                    return Error_MethodNotAllowed_ContentType;
                case 404:
                    return Error_NotFound_ContentType;
                case 403:
                    return Error_Forbidden_ContentType;
                case 401:
                    return Error_Unauthorized_ContentType;
                case 400:
                    return Error_BadRequest_ContentType;
                default:
                    return null;
            }
        }
        public static string GetErrorStringWithFailCode(string _Message, int _Code)
        {
            switch (_Code)
            {
                case 501:
                    return Error_NotImplemented_String(_Message);
                case 500:
                    return Error_InternalError_String(_Message);
                case 415:
                    return Error_UnsupportedMediaType_String(_Message);
                case 409:
                    return Error_Conflict_String(_Message);
                case 406:
                    return Error_NotAcceptable_String(_Message);
                case 405:
                    return Error_MethodNotAllowed_String(_Message);
                case 404:
                    return Error_NotFound_String(_Message);
                case 403:
                    return Error_Forbidden_String(_Message);
                case 401:
                    return Error_Unauthorized_String(_Message);
                case 400:
                    return Error_BadRequest_String(_Message);
                default:
                    return null;
            }
        }

        public static readonly string Error_InternalError_ContentType = "application/json";
        public static string Error_InternalError_String(string _Message) { return $"{{\"result\":\"failure\",\"message\":\"Internal Server Error. {_Message}\"}}"; }
        public static readonly int Error_InternalError_Code = 500;
        public static WebServiceResponse InternalError(string _Message) { return new WebServiceResponse(Error_InternalError_Code, new StringOrStream(Error_InternalError_String(_Message)), Error_InternalError_ContentType); }

        //

        public static readonly string Error_NotImplemented_ContentType = "application/json";
        public static string Error_NotImplemented_String(string _Message) { return $"{{\"result\":\"failure\",\"message\":\"Not Implemented. {_Message}\"}}"; }
        public static readonly int Error_NotImplemented_Code = 501;
        public static WebServiceResponse NotImplemented(string _Message) { return new WebServiceResponse(Error_NotImplemented_Code, new StringOrStream(Error_NotImplemented_String(_Message)), Error_NotImplemented_ContentType); }

        //

        public static readonly string Error_ServiceUnavailable_ContentType = "application/json";
        public static string Error_ServiceUnavailable_String(string _Message) { return $"{{\"result\":\"failure\",\"message\":\"Service unavailable. {_Message} Please retry later.\"}}"; }
        public static readonly int Error_ServiceUnavailable_Code = 503;
        public static WebServiceResponse ServiceUnavailable(string _Message) { return new WebServiceResponse(Error_ServiceUnavailable_Code, new StringOrStream(Error_ServiceUnavailable_String(_Message)), Error_ServiceUnavailable_ContentType); }

        //

        public static readonly string Error_UnsupportedMediaType_ContentType = "application/json";
        public static string Error_UnsupportedMediaType_String(string _Message) { return $"{{\"result\":\"failure\",\"message\":\"Unsupported media type. {_Message} \"}}"; }
        public static readonly int Error_UnsupportedMediaType_Code = 415;
        public static WebServiceResponse UnsupportedMediaType(string _Message) { return new WebServiceResponse(Error_UnsupportedMediaType_Code, new StringOrStream(Error_UnsupportedMediaType_String(_Message)), Error_UnsupportedMediaType_ContentType); }

        //

        public static readonly string Error_Conflict_ContentType = "application/json";
        public static string Error_Conflict_String(string _Message) { return $"{{\"result\":\"failure\",\"message\":\"Conflict. {_Message} \"}}"; }
        public static readonly int Error_Conflict_Code = 409;
        public static WebServiceResponse Conflict(string _Message) { return new WebServiceResponse(Error_Conflict_Code, new StringOrStream(Error_Conflict_String(_Message)), Error_Conflict_ContentType); }

        //

        public static readonly string Error_NotAcceptable_ContentType = "application/json";
        public static string Error_NotAcceptable_String(string _Message) { return $"{{\"result\":\"failure\",\"message\":\"Not acceptable. {_Message} \"}}"; }
        public static readonly int Error_NotAcceptable_Code = 406;
        public static WebServiceResponse NotAcceptable(string _Message) { return new WebServiceResponse(Error_NotAcceptable_Code, new StringOrStream(Error_NotAcceptable_String(_Message)), Error_NotAcceptable_ContentType); }

        //

        public static readonly string Error_MethodNotAllowed_ContentType = "application/json";
        public static string Error_MethodNotAllowed_String(string _Message) { return $"{{\"result\":\"failure\",\"message\":\"Method Not Allowed. {_Message} \"}}"; }
        public static readonly int Error_MethodNotAllowed_Code = 405;
        public static WebServiceResponse MethodNotAllowed(string _Message) { return new WebServiceResponse(Error_MethodNotAllowed_Code, new StringOrStream(Error_MethodNotAllowed_String(_Message)), Error_MethodNotAllowed_ContentType); }

        //

        public static readonly string Error_NotFound_ContentType = "application/json";
        public static string Error_NotFound_String(string _Message) { return $"{{\"result\":\"failure\",\"message\":\"Not Found. {_Message} \"}}"; }
        public static readonly int Error_NotFound_Code = 404;
        public static WebServiceResponse NotFound(string _Message) { return new WebServiceResponse(Error_NotFound_Code, new StringOrStream(Error_NotFound_String(_Message)), Error_NotFound_ContentType); }

        //

        public static readonly string Error_Forbidden_ContentType = "application/json";
        public static string Error_Forbidden_String(string _Message) { return $"{{\"result\":\"failure\",\"message\":\"Forbidden. {_Message} \"}}"; }
        public static readonly int Error_Forbidden_Code = 403;
        public static WebServiceResponse Forbidden(string _Message) { return new WebServiceResponse(Error_Forbidden_Code, new StringOrStream(Error_Forbidden_String(_Message)), Error_Forbidden_ContentType); }

        //

        public static readonly string Error_Unauthorized_ContentType = "application/json";
        public static string Error_Unauthorized_String(string _Message) { return $"{{\"result\":\"failure\",\"message\":\"Unauthorized. {_Message} \"}}"; }
        public static readonly int Error_Unauthorized_Code = 401;
        public static WebServiceResponse Unauthorized(string _Message) { return new WebServiceResponse(Error_Unauthorized_Code, new StringOrStream(Error_Unauthorized_String(_Message)), Error_Unauthorized_ContentType); }

        //

        public static readonly string Error_BadRequest_ContentType = "application/json";
        public static string Error_BadRequest_String(string _Message) { return $"{{\"result\":\"failure\",\"message\":\"Bad Request. {_Message} \"}}"; }
        public static readonly int Error_BadRequest_Code = 400;
        public static WebServiceResponse BadRequest(string _Message) { return new WebServiceResponse(Error_BadRequest_Code, new StringOrStream(Error_BadRequest_String(_Message)), Error_BadRequest_ContentType); }

        //
        public static readonly int Status_OK_Code = 200;
        public static WebServiceResponse StatusOK(string _Message, JObject _AdditionalFields = null) { return new WebServiceResponse(Status_OK_Code, new StringOrStream(Status_Success_String(_Message, _AdditionalFields)), Status_Success_ContentType); }

        //

        public static readonly int Status_Created_Code = 201;
        public static WebServiceResponse StatusCreated(string _Message, JObject _AdditionalFields = null) { return new WebServiceResponse(Status_Created_Code, new StringOrStream(Status_Success_String(_Message, _AdditionalFields)), Status_Success_ContentType); }

        //

        public static readonly int Status_Accepted_Code = 202;
        public static WebServiceResponse StatusAccepted(string _Message, JObject _AdditionalFields = null) { return new WebServiceResponse(Status_Accepted_Code, new StringOrStream(Status_Success_String(_Message, _AdditionalFields)), Status_Success_ContentType); }

        // Success common

        public static readonly string Status_Success_ContentType = "application/json";
        public static string Status_Success_String(string _Message, JObject _AdditionalFields = null)
        {
            return Status_Success_JObject(_Message, _AdditionalFields).ToString();
        }
        public static JObject Status_Success_JObject(string _Message, JObject _AdditionalFields = null)
        {
            return new JObject()
            {
                ["result"] = "success",
                ["message"] = _Message
            }.MergeJObjects(_AdditionalFields);
        }
        private static JObject MergeJObjects(this JObject _Input_1, JObject _Input_2)
        {
            if (_Input_2 != null)
            {
                _Input_1.Merge(_Input_2);
            }
            return _Input_1;
        }
    }
}