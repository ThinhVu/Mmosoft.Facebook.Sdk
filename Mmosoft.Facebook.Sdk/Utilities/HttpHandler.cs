using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace Mmosoft.Facebook.Sdk.Utilities
{
    public class HttpHandler
    {
        /// <summary>
        /// Contain cookie for making authorized request 
        /// </summary>
        public virtual CookieContainer CookieContainer { get; set; }

        /// <summary>
        /// User-agent
        /// </summary>
        public virtual string UserAgent { get; set; }

        /// <summary>
        /// Initialize new instance of BaseHttp request with default user agent
        /// </summary>
        public HttpHandler()
        {
            CookieContainer = new CookieContainer();
            
            // default user-agent
            // TODO (ThinhVu) : Fix hard-coded user agent
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:46.0) Gecko/20100101 Firefox/46.0";
        }

        /// <summary>
        /// Initialize new instance of BaseHttp request.
        /// </summary>
        /// <param name="userAgent">User agent</param>
        public HttpHandler(string userAgent) 
            : this()
        {
            UserAgent = userAgent;
        }

       
        /// <summary>
        /// Make GET request
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public virtual HttpWebRequest MakeGetRequest(Uri requestUri)
        {
            var request = this.CreateRequest(requestUri);

            request.Method = WebRequestMethods.Http.Get;

            return request;
        }

        /// <summary>
        /// Make POST request
        /// </summary>
        /// <param name="requestUri"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public virtual HttpWebRequest MakePostRequest(Uri requestUri, byte[] content)
        {
            var request = this.CreateRequest(requestUri);

            request.Method = WebRequestMethods.Http.Post;
            request.ContentLength = content.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            return request;
        }

        /// <summary>
        /// Send GET request to requestUri and recv response
        /// </summary>
        /// <param name="requestUri">A string containing the URI of the requested resource</param>
        /// <param name="cookies">A System.Net.CookieContainer containing cookies for this request</param>        
        /// <returns>A System.Net.HttpWebResponse for the specified uri scheme</returns>
        public virtual HttpWebResponse SendGetRequest(string requestUrl)
        {
            var response = MakeGetRequest(new Uri(requestUrl)).GetResponse() as HttpWebResponse;

            this.StoreCookie(response.Cookies);

            return response;
        }

        /// <summary>
        /// Send POST request to requestUri and recv response
        /// </summary>
        /// <param name="requestUrl">A System.String containing the URI of the requested resource</param>
        /// <param name="content">A System.String containing the content to send</param>
        /// <param name="cookies">A System.Net.CookieContainer containing cookies for this request</param>        
        /// <returns>A System.Net.HttpWebResponse for the specified uri scheme</returns>
        public virtual HttpWebResponse SendPostRequest(string requestUrl, string content)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(content);

            var postRequest = MakePostRequest(new Uri(requestUrl), buffer);

            using (var postRequestStream = postRequest.GetRequestStream())
                postRequestStream.Write(buffer, 0, buffer.Length);

            var response = postRequest.GetResponse() as HttpWebResponse;

            this.StoreCookie(response.Cookies);

            return response;
        }

        /// <summary>
        /// Download Html content
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public virtual string DownloadContent(string requestUrl)
        {
            // cause Accept-Encoding allow gzip so request use gzip stream to decompress content.
            using (var response = this.SendGetRequest(requestUrl))
            using (var responseStreamReader = new StreamReader(new GZipStream(response.GetResponseStream(), CompressionMode.Decompress)))
            {
                return responseStreamReader.ReadToEnd();
            }
        }

        /// <summary>
        /// Create a request
        /// </summary>
        /// <param name="requestUri">A System.Uri containing the URI of the requested resource</param>
        /// <param name="cookieContainer">A System.Net.CookieContainer containing cookies for this request</param>              
        /// <returns>A System.Net.HttpWebRequest for the specified URI scheme.</returns>
        HttpWebRequest CreateRequest(Uri requestUri)
        {
            var request = WebRequest.Create(requestUri) as HttpWebRequest;

            request.Accept = "*/*";
            request.AllowAutoRedirect = false;
            request.CookieContainer = this.CookieContainer;
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            request.Headers.Add("Accept-Encoding", "gzip");
            request.Host = requestUri.Host + ":" + requestUri.Port;
            request.KeepAlive = true;
            request.ProtocolVersion = HttpVersion.Version11;
            request.ServicePoint.Expect100Continue = false;
            request.UserAgent = this.UserAgent;

            return request;
        }

        /// <summary>
        /// Store new cookie to cookie containter
        /// </summary>
        /// <param name="cookies"></param>
        void StoreCookie(CookieCollection cookies)
        {
            if (cookies != null && cookies.Count > 0)
                this.CookieContainer.Add(cookies);
        }
    }   
}
