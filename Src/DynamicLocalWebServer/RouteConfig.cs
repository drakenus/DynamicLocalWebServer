using System.Net;
using Newtonsoft.Json;

namespace LocalWebServer
{
    public sealed class RouteConfig
    {
        public HttpStatusCode HttpStatusCode { get; set; } = HttpStatusCode.OK;
        public string BodyContent { get; set; } = string.Empty;

        public string Method { get; set; } = WebRequestMethods.Http.Get;

        public string ContentType { get; set; } = "text/plain";

        internal RouteConfig()
        {
        }

        public RouteConfig WithHttpStatusCode(HttpStatusCode httpStatusCode)
        {
            HttpStatusCode = httpStatusCode;
            return this;
        }

        public RouteConfig WithStringBody(string bodyContent)
        {
            BodyContent = bodyContent;
            return this;
        }

        public RouteConfig WithJsonBody(object jsonObject)
        {
            var json = JsonConvert.SerializeObject(jsonObject);
            ContentType = "application/json";
            return WithStringBody(json);
        }

        public RouteConfig WithMethod(string httpMethod)
        {
            Method = httpMethod;
            return this;
        }

        public RouteConfig WithContentType(string contentType)
        {
            ContentType = contentType;
            return this;
        }
    }
}