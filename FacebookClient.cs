namespace Mmosoft
{        
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net;    
    using System.Text.RegularExpressions;
    using HtmlAgilityPack;

    public class FacebookClient
    {
        private string email;
        private string password;
        private CookieContainer cookies;

        public FacebookClient(string email, string password)
        {
            // Init data
            this.email = email;
            this.password = password;
            this.cookies = new CookieContainer();

            // Trying to login
            this.Login();
        }

        // Tested -- worked
        private void Login()
        {
            // store html content
            var html = string.Empty;

            // Get bootstrap cookies                
            using (var httpWebResponse = HttpRequestBuilder.RequestGet("https://m.facebook.com").GetResponse() as HttpWebResponse)
            using (var gzipStream = new GZipStream(httpWebResponse.GetResponseStream(), CompressionMode.Decompress))
            using (var responseStreamReader = new StreamReader(gzipStream))
            {
                // store cookies to cookieContainer
                cookies.Add(httpWebResponse.Cookies);
                // read html
                html = responseStreamReader.ReadToEnd();
            }
            // load html to dom object
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);                        
            // Get login form Dom object
            var loginForm = htmlDocument.GetElementbyId("login_form");
            var inputCollection = new List<string>();
            // Loop through login form and retreive data
            foreach (HtmlNode htmlNode in loginForm.ParentNode.Elements("input"))
            {
                inputCollection.Add(htmlNode.GetAttributeValue("name", "") + "=" + htmlNode.GetAttributeValue("value", ""));                
            }
            // JoinString to make data for login request
            inputCollection.Insert(1, "email=" + this.email + "&pass=" + this.password);
            var data = string.Join("&", inputCollection);

            // Post login request and store logged-in cookies.
            using (var response2 = HttpRequestBuilder.PostData("https://m.facebook.com/login.php", data, this.cookies))
            {
                cookies.Add(response2.Cookies);
            }
        }

        // Tested -- worked
        public void JoinOrCancelGroup(string groupId)
        {
            // send request with logged cookies
            var httpResponse = HttpRequestBuilder.RequestGet("https://m.facebook.com/groups/" + groupId, cookies);
            // variable to store html content
            var html = string.Empty;
            // get response stream
            using (var responseStream = httpResponse.GetResponse().GetResponseStream())
            using (var gzipStream = new GZipStream(responseStream, CompressionMode.Decompress))
            using (var responseStreamReader = new StreamReader(gzipStream))
            {
                // store html content
                html = responseStreamReader.ReadToEnd();
            }
            // load html content to DOM
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            // extract form data
            string value = Regex.Match(htmlDocument.GetElementbyId("root")
                                                    .InnerHtml
                                                    .Replace("\"", "'")
                                                    .Replace(@"\r\n|\t|\v|\s+", @"\s"), "<form.*?</form>")
                                                    .Value;            
            var inputCollections = new List<string>();
            foreach (Match match in Regex.Matches(value, "<input type='hidden' name='(?<name>.*?)' value='(?<value>.*?)'.*?>"))
            {
                inputCollections.Add(match.Groups["name"].Value + "=" + match.Groups["value"].Value);                
            }
            var data = string.Join("&", inputCollections);
            var actionUrl = "https://m.facebook.com" + Regex.Match(value, "action='(?<url>.*?)'").Groups["url"];            
            // send post request and store cookies
            using (var response2 = HttpRequestBuilder.PostData(actionUrl, data, cookies))
            {
                cookies.Add(response2.Cookies);
            }
        }

        // Tested -- worked
        public void LikeOrDislikePage(string pageId)
        {
            var html = string.Empty;
            // access to mobile page of pageId
            using (var response = HttpRequestBuilder.RequestGet("https://m.facebook.com/" + pageId, cookies).GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var gzipStream = new GZipStream(responseStream, CompressionMode.Decompress))
            using (var responseStreamReader = new StreamReader(gzipStream))
            {
                // store html content
                html = responseStreamReader.ReadToEnd();
            }
            // load html content to DOM
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            // get like link            
            HtmlNode htmlNode = htmlDocument.DocumentNode
                                            .SelectSingleNode("/html/body/div/div/div[2]/div/div/div[1]/div[2]/div/div[2]/table/tbody/tr/td[1]/a");
            // if like link exist then send simulate mouse click to this link by send request.
            if (htmlNode != null && htmlNode.Attributes.Contains("href"))
            {
                // decode url
                var href = WebUtility.HtmlDecode(htmlNode.Attributes["href"].Value);
                // Post like page request
                using (var response = (HttpRequestBuilder.RequestGet("https://m.facebook.com" + href, cookies).GetResponse() as HttpWebResponse))
                {
                    this.cookies.Add(response.Cookies);
                }
            }                        
        }

        // Tested-worked
        // Does not support attachment
        public void PostWall(string message)
        {
            // /html/body/div/div/div[2]/div/div[2]/div/form
            var html = string.Empty;

            using (var response = HttpRequestBuilder.RequestGet("https://m.facebook.com/", cookies).GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var gzipStream = new GZipStream(responseStream, CompressionMode.Decompress))
            using (var responseStreamReader = new StreamReader(gzipStream))
            {
                // store html content
                html = responseStreamReader.ReadToEnd();
            }
            // load html content to DOM
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            HtmlNode postForm = htmlDocument.DocumentNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[2]/div");
            var innerHtml = postForm.InnerHtml.Replace("\"", "\'").Replace(@"\r\n|\t|\v|\s+", @"\s");
            var inputCollection = new List<string>();
            foreach (Match match in Regex.Matches(innerHtml, "<input type='hidden' name='(?<name>.*?)' value='(?<value>.*?)'.*?>"))
            {
                var name = match.Groups["name"].Value;
                var value = match.Groups["value"].Value;
                //if (name == "charset_test")
                //    value = WebUtility.UrlEncode(WebUtility.HtmlDecode(value));
                inputCollection.Add(name + "=" + value);
            }
            inputCollection.Add("rst_icv=");
            inputCollection.Add("view_post=Post");
            inputCollection.Add("xc_message=" + message);
            var actionUrl = "https://m.facebook.com" + postForm.SelectSingleNode("form").Attributes["action"].Value;
            var data = string.Join("&", inputCollection);
            using (var response2 = HttpRequestBuilder.PostData(actionUrl, data, cookies))
            {
                cookies.Add(response2.Cookies);
            }
        }

        // Tested - ok
        // You can modify this method to post multiple messages.
        public void PostGroup(string groupId, string message)
        {            
            var html = string.Empty;

            using (var response = HttpRequestBuilder.RequestGet("https://m.facebook.com/groups/" + groupId, cookies).GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var gzipStream = new GZipStream(responseStream, CompressionMode.Decompress))
            using (var responseStreamReader = new StreamReader(gzipStream))
            {
                // store html content
                html = responseStreamReader.ReadToEnd();
            }
            // load html content to DOM
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);            
            HtmlNode postForm = htmlDocument.DocumentNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[3]");
            var innerHtml = postForm.InnerHtml.Replace("\"", "\'").Replace(@"\r\n|\t|\v|\s+", @"\s");
            var inputCollection = new List<string>();
            foreach (Match match in Regex.Matches(innerHtml, "<input type='hidden' name='(?<name>.*?)' value='(?<value>.*?)'.*?>"))
            {
                var name = match.Groups["name"].Value;
                var value = match.Groups["value"].Value;
                inputCollection.Add(name + "=" + value);
            }
            inputCollection.Add("view_post=Post");
            inputCollection.Add("xc_message=" + message);
            var actionUrl = "https://m.facebook.com" + postForm.SelectSingleNode("form").Attributes["action"].Value;
            var data = string.Join("&", inputCollection);
            using (var response2 = HttpRequestBuilder.PostData(actionUrl, data, cookies))
            {
                cookies.Add(response2.Cookies);
            }
        }
    }
}