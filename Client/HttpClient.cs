﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TinyHttp
{
    public static class HttpClient
    {
        public enum RequestTypes
        {
            Get,
            Post,
            Put,
            Delete
        }

        public enum RequestContentTypes
        {
            Json,
            Xml,
            Text,
            Html,
            Form
        }

        internal static string GetContentType(RequestContentTypes requestContent)
        {
            switch (requestContent)
            {
                case HttpClient.RequestContentTypes.Json:
                    return "application/json";
                case HttpClient.RequestContentTypes.Xml:
                    return "application/xml";
                case HttpClient.RequestContentTypes.Text:
                    return "text/plain";
                case HttpClient.RequestContentTypes.Html:
                    return "text/html";
                case RequestContentTypes.Form:
                    return "application/x-www-form-urlencoded; charset=UTF-8";
                default:
                    throw new ArgumentException(string.Format("The content type {0} is not supported", requestContent), "requestContent");
            }
        }

        public static string UserAgent { get; set; } = string.Format("TinyHttp .NET: {0} OS: {1}", Environment.Version.ToString(), Environment.OSVersion.ToString());

        public static HttpResponse<TOAuthToken> OAuthPasswordLogin<TOAuthToken>(string Url, string Username, string Password) where TOAuthToken : IOAuthToken
        {
            return OAuthPasswordLoginAsync<TOAuthToken>(Url, Username, Password).Result;
        }

        public static async Task<HttpResponse<TOAuthToken>> OAuthPasswordLoginAsync<TOAuthToken>(string Url, string Username, string Password) where TOAuthToken : IOAuthToken
        {
            NameValueCollection col = new NameValueCollection();
            col.Add("username", Username);
            col.Add("password", Password);
            return await OAuthTokenAsync<TOAuthToken>(Url, "password", col);
        }


        public static HttpResponse<OAuthAccessToken> OAuthPasswordLogin(string Url, string Username, string Password)
        {
            return OAuthPasswordLoginAsync(Url, Username, Password).Result;
        }
        public static async Task<HttpResponse<OAuthAccessToken>> OAuthPasswordLoginAsync(string Url, string Username, string Password)
        {
            NameValueCollection col = new NameValueCollection();
            col.Add("username", Username);
            col.Add("password", Password);
            return await OAuthTokenAsync(Url, "password", col);
        }

        public static HttpResponse<OAuthAccessToken> OAuthClientLogin(string Url, string clientId, string clientSecret)
        {
            return OAuthClientLoginAsync(Url, clientId, clientSecret).Result;
        }

        public static async Task<HttpResponse<OAuthAccessToken>> OAuthClientLoginAsync(string Url, string clientId, string clientSecret)
        {
            NameValueCollection col = new NameValueCollection();
            col.Add("client_id", clientId);
            col.Add("client_secret", clientSecret);
            return await OAuthTokenAsync(Url, "client_credentials", col);
        }

        private static Task<HttpResponse<OAuthAccessToken>> OAuthTokenAsync(string Url, string grantType, NameValueCollection parameters)
        {
            string s = string.Format("grant_type={0}&", grantType);
            s = parameters.AppendAsQueryString(s);
            return ExecuteAsync<OAuthAccessToken>(Url, RequestTypes.Post, null, null, null, s, "application/x-www-form-urlencoded; charset=UTF-8");
        }

        private static Task<HttpResponse<TOAuthToken>> OAuthTokenAsync<TOAuthToken>(string Url, string grantType, NameValueCollection parameters) where TOAuthToken : IOAuthToken
        {
            string s = string.Format("grant_type={0}&", grantType);
            s = parameters.AppendAsQueryString(s);

            return CreateWriteRequest(Url)
                .WithContent<string>(RequestContentTypes.Form, s)
                .ExecuteAsync<TOAuthToken>();

            // return ExecuteAsync<TOAuthToken>(Url, RequestTypes.Post, null, null, null, s, "application/x-www-form-urlencoded; charset=UTF-8");
        }

        public static Task<HttpResponse<T>> DeleteAsync<T, S>(string Url, S Id, OAuthAccessToken Auth = null)
        {
            return ExecuteAsync<T>(Url + "/" + Id.ToString(), RequestTypes.Delete, null, Auth);
        }

        public static Task<HttpResponse<T>> DetailsAsync<T>(string Url, string Id, OAuthAccessToken Auth = null)
        {

            return ExecuteAsync<T>(Url + "/" + Id, RequestTypes.Get, null, Auth);
        }

        public static HttpResponse<T> Details<T>(string Url, string Id, OAuthAccessToken Auth = null)
        {
            return Execute<T>(Url, RequestTypes.Get, new NameValueCollection() { { "Id", Id } }, Auth);
        }


        public static HttpResponse<T> Get<T>(string Url, OAuthAccessToken Auth = null)
        {
            return Execute<T>(Url, RequestTypes.Get, null, Auth);
        }
        public static Task<HttpResponse<T>> GetAsync<T>(string Url, OAuthAccessToken Auth = null)
        {
            return ExecuteAsync<T>(Url, RequestTypes.Get, null, Auth);
        }
        public static HttpResponse<T> Post<T>(string Url, NameValueCollection Data, OAuthAccessToken Auth = null)
        {
            return Execute<T>(Url, RequestTypes.Post, Data, Auth);
        }
        public static Task<HttpResponse<T>> PostAsync<T>(string Url, NameValueCollection Data, OAuthAccessToken Auth = null)
        {
            return ExecuteAsync<T>(Url, RequestTypes.Post, Data, Auth);
        }


        //public static HttpResponse<T> Execute<T>(string Url, RequestTypes RequestType, NameValueCollection data = null, OAuthAccessToken Auth = null)
        //{
        //    return ExecuteAsync<T>(Url, RequestType, data, Auth).Result;
        //}


        public static HttpResponse<T> Update<T>(string Url, OAuthAccessToken Auth, string Object)
        {
            return UpdateAsync<T>(Url, Auth, Object).Result;
        }
        public static async Task<HttpResponse<T>> UpdateAsync<T>(string Url, OAuthAccessToken Auth, string Object)
        {
            return await ExecuteAsync<T>(Url, RequestTypes.Put, null, Auth, null, Object);
        }
        public static HttpResponse<T> Create<T>(string Url, OAuthAccessToken Auth, string Object)
        {
            return CreateAsync<T>(Url, Auth, Object).Result;
        }
        public static async Task<HttpResponse<T>> CreateAsync<T>(string Url, OAuthAccessToken Auth, string Object)
        {
            return await ExecuteAsync<T>(Url, RequestTypes.Post, null, Auth, null, Object);
        }
        public static HttpResponse<T> Execute<T>(string Url, RequestTypes RequestType, NameValueCollection QueryString = null, OAuthAccessToken Auth = null, ICredentials Credentials = null, string Payload = null, string ContentType = "application/json")
        {
            return ExecuteAsync<T>(Url, RequestType, QueryString, Auth, Credentials, Payload, ContentType).Result;
        }

        public static HttpWebRequest CreateFormDataRequest(string Url, NameValueCollection FormData = null, List<FileUpload> Files = null, OAuthAccessToken Auth = null, ICredentials Credentials = null)
        {
            string Boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");
            string FormDataFormat = "--" + Boundary + Environment.NewLine +
                    "Content-Disposition: form-data; name=\"{0}\"" + Environment.NewLine + Environment.NewLine +
                    "{1}" + Environment.NewLine;

            string FileDataFormat = "--" + Boundary + Environment.NewLine +
                        "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"" + Environment.NewLine +
                        "Content-Type: {2}" + Environment.NewLine + Environment.NewLine + "{3}" + Environment.NewLine;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = WebRequestMethods.Http.Post;
            request.ContentType = "multipart/form-data; boundary=" + Boundary;
            request.ContinueTimeout = request.ReadWriteTimeout = request.Timeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
            //request.AllowReadStreamBuffering = false;
            //request.Accept = "text/plain";
            if (Auth != null)
            {
                request.Headers.Add(Auth.AuthorizationHeader);
            }
            if (Credentials != null)
            {
                request.Credentials = Credentials;
            }

            StringBuilder sb = new StringBuilder();
            StringWriter ss = new StringWriter(sb);


            using (Stream rs = request.GetRequestStream())
            using (StreamWriter sw = new StreamWriter(rs,Encoding.ASCII))
            {
                
                if (FormData != null)
                {
                    foreach (string key in FormData.Keys)
                    {
                        sw.Write(string.Format(FormDataFormat, key, FormData[key]));
                        ss.Write(string.Format(FormDataFormat, key, FormData[key]));
                    }
                }
                sw.Flush();
                if (Files != null)
                {
                    foreach (FileUpload file in Files)
                    {
                        sw.Write(string.Format(FileDataFormat, file.ParameterName, file.FileName, file.ContentType, System.Text.Encoding.UTF8.GetString(file.Content)));
                        ss.Write(string.Format(FileDataFormat, file.ParameterName, file.FileName, file.ContentType, System.Text.Encoding.UTF8.GetString(file.Content)));
                        // sw.Flush();
                        // sw.Write(sw.NewLine);
                        // rs.Write(file.Content, 0, file.Content.Length);

                    }
                }
                sw.Write(string.Format("\r\n--{0}--\r\n", Boundary));
                ss.Write(string.Format("\r\n--{0}--\r\n", Boundary));
                ss.Flush();
                ss.Close();
                Console.WriteLine(sb.ToString());
                sw.Flush();
                rs.Flush();
                sw.Close();
            }

            return request;   

        }

        #region CRUD Request creation

        public static HttpWebRequest CreateReadRequest(string url, NameValueCollection queryString = null)
        {
            return CreateRequest(url, RequestTypes.Get, queryString);
        }

        public static HttpWebRequest CreateWriteRequest(string url, NameValueCollection queryString = null)
        {
            return CreateRequest(url, RequestTypes.Post, queryString);
        }

        public static HttpWebRequest CreateUpdateRequest(string url, NameValueCollection queryString = null)
        {
            return CreateRequest(url, RequestTypes.Put, queryString);
        }

        public static HttpWebRequest CreateDeleteRequest(string url, NameValueCollection queryString = null)
        {
            return CreateRequest(url, RequestTypes.Delete, queryString);
        }

        #endregion  

        public static HttpWebRequest CreateRequest(string url, RequestTypes requestType, NameValueCollection queryString = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (queryString != null)
            {
                url = queryString.AppendAsQueryString(url);
            }
            request.Method = requestType.ToString();
            return request;
        }

        private static HttpWebRequest CreateRequest(string Url, RequestTypes RequestType, NameValueCollection QueryString = null, OAuthAccessToken Auth = null, ICredentials Credentials = null, string Payload = null, string ContentType = "application/json")
        {
            if (QueryString != null)
            {
                Url = QueryString.AppendAsQueryString(Url);
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);

            request.Method = RequestType.ToString().ToUpper();
            request.ContentType = ContentType;
            request.UserAgent = UserAgent;
            if (RequestType != RequestTypes.Get)
            {
                if (!string.IsNullOrEmpty(Payload))
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(request.GetRequestStream()))
                    {
                        sw.Write(Payload);
                        sw.Close();
                    }
                }
                else
                {
                    request.ContentLength = 0;
                }
            }
            if (Auth != null)
            {
                request.Headers.Add(Auth.AuthorizationHeader);
            }
            if (Credentials != null)
            {
                request.Credentials = Credentials;
            }
            return request;
        }

        public static async Task<HttpResponse<T>> ExecuteAsync<T>(HttpWebRequest request)
        {
            string response = null;
            HttpStatusCode responseStatusCode = HttpStatusCode.Unused;
            string responseStatusCodeDescription = null;
            string responseContentType = null;

            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
                {

                    if (resp != null)
                    {
                        System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                        responseStatusCode = resp.StatusCode;
                        responseStatusCodeDescription = resp.StatusDescription;
                        responseContentType = resp.ContentType;
                        response = sr.ReadToEnd().Trim();
                    }
                }
            }
            catch (WebException we)
            {
                System.IO.StreamReader ss = new System.IO.StreamReader(we.Response.GetResponseStream());
                string msg = ss.ReadToEnd();
                if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    return new HttpResponse<T>(default(T), we, ((HttpWebResponse)we.Response).StatusCode, we.Status);
                }
                else
                {
                    // TODO: Uncontrolled exception
                    return new HttpResponse<T>(default(T), we, ((HttpWebResponse)we.Response).StatusCode, we.Status);
                }
            }
            catch (Exception ex)
            {
                // TODO: Handle Unknown exception nicely
                return new HttpResponse<T>(default(T), ex, HttpStatusCode.InternalServerError, WebExceptionStatus.UnknownError);
            }

            return new HttpResponse<T>(ParseResponseData<T>(response, responseContentType), null, responseStatusCode);
        }

        public static async Task<HttpResponse<T>> ExecuteAsync<T>(string Url, RequestTypes RequestType, NameValueCollection QueryString = null, OAuthAccessToken Auth = null, ICredentials Credentials = null, string Payload = null, string ContentType = "application/json")
        {
            HttpWebRequest request = CreateRequest(Url, RequestType, QueryString, Auth, Credentials, Payload, ContentType);
            return await ExecuteAsync<T>(request);
        }

        private static T ParseResponseData<T>(string responseData, string contentType)
        {

            if (string.IsNullOrEmpty(responseData))
            {
                return default(T);
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(responseData, typeof(T));
            }
            else
            {
                if (contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonConvert.DeserializeObject<T>(responseData, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.All, ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                }
                if (new List<string>() { "application/xml", "application/rss+xml", "text/xml" }.Any(x => x.StartsWith(contentType, StringComparison.OrdinalIgnoreCase)))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                    using (StringReader sr = new StringReader(responseData))
                    {
                        return (T)serializer.Deserialize(sr);
                    }
                }
            }
            throw new InvalidOperationException(string.Format("The content type '{0}' is not supported for the HTTP Response object type '{1}'", contentType, typeof(T).FullName));

        }

    }
}
