using HtmlAgilityPack;
using Mmosoft.Facebook.Utils;
using System;
using System.Collections.Generic;
using System.Net;

namespace Mmosoft.Facebook.Sdk
{
    public abstract class FbClient : IDisposable
    {
        protected HttpRequestHandler _requestHandler;
        
        public ILogger Logger { get; private set; }
        
        public string Username { get; set; }
        public string Password { get; set; }

        protected FbClient(string user, string password)
        {
            Logger = LogCreator.Create();
            _requestHandler = new HttpRequestHandler();
            Username = user;
            Password = password;           
            Authorize();
        }

        private bool Authorize()
        {
            Logger.WriteLine("Authorize");
            
            // load html content to document node
            HtmlNode document = this.__BuildDomFromUrl("https://m.facebook.com");

            // Get login form Dom object
            HtmlNode loginForm = document.SelectSingleNode("//form[@id='login_form']");
            IEnumerable<HtmlNode> inputs = loginForm.ParentNode.Elements("input");

            // create postData (payload)
            List<string> postData = __ExtractHidenInputNodes(loginForm.ParentNode);
            postData.Add("email=" + Username);
            postData.Add("pass=" + Password);            
            using (HttpWebResponse response = _requestHandler.SendPOSTRequest("https://m.facebook.com/login.php", __CreatePayload(postData)))
            {
                if (response.Cookies["c_user"] == null)
                    throw new Exception("FbClient:Authorize:c_user not exist");
            }
            return true;
        }
        
        protected HtmlNode __BuildDomFromUrl(string url)
        {
            string htmlContent = _requestHandler.DownloadContent(url);
            return __BuildDomFromHtmlContent(htmlContent);
        }

        /// <summary>
        /// Parse input string to DOM object.
        /// </summary>
        /// <param name="content">Input string</param>
        /// <returns>DOM object</returns>
        protected HtmlNode __BuildDomFromHtmlContent(string content)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);
            return htmlDoc.DocumentNode;
        }
        
        /// <summary>
        /// Extract hidden input node of form then build list string with format of each item is name=value
        /// </summary>
        /// <param name="formNode"></param>
        /// <returns></returns>
        protected List<string> __ExtractHidenInputNodes(HtmlNode formNode)
        {
            var kvp = new List<string>();
            foreach (var inputNode in formNode.SelectNodes("input"))
            {
                if (inputNode.GetAttributeValue("type", string.Empty) != "hidden")
                    continue;

                var name = WebUtility.UrlDecode(inputNode.GetAttributeValue("name", string.Empty));
                var value = WebUtility.UrlDecode(inputNode.GetAttributeValue("value", string.Empty));

                if (name != string.Empty && value != string.Empty)
                    kvp.Add(name + "=" + value);
            }
            return kvp;
        }

        protected string __CreatePayload(List<string> kvp)
        {
            return string.Join("&", kvp.ToArray());
        }

        public void Dispose()
        {
            Logger.Dispose();
            Logger = null;
            _requestHandler = null;
        }
    }
}