using HtmlAgilityPack;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Mmosoft.Facebook.Sdk.Common
{
    public static class Http
    {
        /// <summary>
        /// Create a request
        /// </summary>
        /// <param name="requestUri">A System.Uri containing the URI of the requested resource</param>
        /// <param name="cookieContainer">A System.Net.CookieContainer containing cookies for this request</param>              
        /// <returns>A System.Net.HttpWebRequest for the specified URI scheme.</returns>
        private static HttpWebRequest Create(Uri requestUri, [Optional] CookieContainer cookieContainer)
        {
            try
            {
                var request = WebRequest.Create(requestUri) as HttpWebRequest;
                request.Accept = "*/*";
                request.AllowAutoRedirect = false;
                if (cookieContainer != null) request.CookieContainer = cookieContainer;
                request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
                request.Headers.Add("Accept-Encoding", "gzip");
                request.Host = requestUri.Host + ":" + requestUri.Port;
                request.KeepAlive = true;
                request.ProtocolVersion = HttpVersion.Version11;
                request.ServicePoint.Expect100Continue = false;
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:46.0) Gecko/20100101 Firefox/46.0";

                return request;
            }
            catch
            {
                // rethrow exception
                throw;
            }
        }

        /// <summary>
        /// Send GET request to requestUri and recv response
        /// </summary>
        /// <param name="requestUri">A string containing the URI of the requested resource</param>
        /// <param name="cookies">A System.Net.CookieContainer containing cookies for this request</param>        
        /// <returns>A System.Net.HttpWebResponse for the specified uri scheme</returns>
        public static HttpWebResponse Get(Uri requestUri, [Optional] CookieContainer cookies)
        {
            try
            {
                var request = Create(requestUri, cookies);
                request.Method = WebRequestMethods.Http.Get;
                return request.GetResponse() as HttpWebResponse;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Send POST request to requestUri and recv response
        /// </summary>
        /// <param name="requestUrl">A System.String containing the URI of the requested resource</param>
        /// <param name="content">A System.String containing the content to send</param>
        /// <param name="cookies">A System.Net.CookieContainer containing cookies for this request</param>        
        /// <returns>A System.Net.HttpWebResponse for the specified uri scheme</returns>
        public static HttpWebResponse Post(Uri requestUri, string content, [Optional] CookieContainer cookies)
        {
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(content);
                var request = Create(requestUri, cookies);
                request.Method = WebRequestMethods.Http.Post;
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = buffer.Length;
                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(buffer, 0, buffer.Length);
                }
                return request.GetResponse() as HttpWebResponse;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Load DOM method. This method get url and download html content then parse to DOM object
        /// using HtmlAgilityPack library.
        /// </summary>
        /// <param name="url">Url want to get DOM</param>
        /// <param name="cookieContainer">cookie you want to pass for this method</param>        
        /// <returns>DOM object parsed from html content</returns>
        public static HtmlNode LoadDom(string url, ref CookieContainer cookieContainer)
        {
            var html = string.Empty;

            using (var resp = Get(new Uri(url), cookieContainer))
            using (var respReader = new StreamReader(new GZipStream(resp.GetResponseStream(), CompressionMode.Decompress)))
            {
                if (cookieContainer != null) cookieContainer.Add(resp.Cookies);
                html = respReader.ReadToEnd();
            }

            // load html content to DOM
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            return htmlDocument.DocumentNode;
        }
    }   
}
