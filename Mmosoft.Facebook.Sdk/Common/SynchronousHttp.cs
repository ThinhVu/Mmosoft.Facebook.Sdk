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
    public class SynchronousHttp
    {
        /// <summary>
        /// Get or set interval time per each request
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// store last request time
        /// </summary>
        private DateTime lastRequestTime;

        /// <summary>
        /// Init new instance of SynchronousHttp request
        /// </summary>
        /// <param name="interval"></param>
        public SynchronousHttp(TimeSpan interval)
        {
            lastRequestTime = DateTime.Now;
            Interval = Interval;
        }

        private void Block()
        {
            while (lastRequestTime > DateTime.Now.Subtract(Interval))
            {
                Thread.Sleep(Interval.Milliseconds / 2);
            }
            lastRequestTime = DateTime.Now;
        }

        public HttpWebResponse Get(Uri requestUri, [Optional] CookieContainer cookies)
        {
            Block();
            return Http.Get(requestUri, cookies);
        }

        public HttpWebResponse Post(Uri requestUri, string content, [Optional] CookieContainer cookies)
        {
            Block();
            return Http.Post(requestUri, content, cookies);
        }

        public HtmlNode LoadDom(string url, ref CookieContainer cookieContainer)
        {
            Block();
            return Http.LoadDom(url, ref cookieContainer);
        }
    }
}
