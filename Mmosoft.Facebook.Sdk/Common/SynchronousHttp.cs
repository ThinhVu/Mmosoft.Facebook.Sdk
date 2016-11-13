using HtmlAgilityPack;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mmosoft.Facebook.Sdk.Common
{
    /// <summary>
    /// Control request speed to facebook
    /// </summary>
    public static class SynchronousHttp
    {
        /// <summary>
        /// Get or set interval time per each request
        /// </summary>
        public static TimeSpan Interval { get; set; }

        /// <summary>
        /// store last request time
        /// </summary>
        private static DateTime lastRequestTime;

        static SynchronousHttp()
        {
            lastRequestTime = DateTime.Now;
            Interval = TimeSpan.FromMilliseconds(200);
        }

        private static void Block()
        {
            while (lastRequestTime > DateTime.Now.Subtract(Interval))
            {
                Thread.Sleep(Interval.Milliseconds / 2);
            }
            lastRequestTime = DateTime.Now;
        }

        public static HttpWebResponse Get(Uri requestUri, [Optional] CookieContainer cookies)
        {
            Block();
            return Http.Get(requestUri, cookies);
        }

        public static HttpWebResponse Post(Uri requestUri, string content, [Optional] CookieContainer cookies)
        {
            Block();
            return Http.Post(requestUri, content, cookies);
        }

        public static HtmlNode LoadDom(string url, ref CookieContainer cookieContainer)
        {
            Block();
            return Http.LoadDom(url, ref cookieContainer);
        }
    }
}
