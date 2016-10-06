namespace FacebookAPI
{
    using System;
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

            // Login -- bootstrap cookie
            this.Login();
        }

        public void JoinGroupOrCancel(string groupId)
        {
            var htmlNode = LoadDOM("https://m.facebook.com/groups/" + groupId, ref cookies);
            // extract form data
            var formNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/form");
            var inputCollection = new List<string>();
            foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
            {
                if (input.GetAttributeValue("type", string.Empty) == "hidden")
                {
                    var name = input.GetAttributeValue("name", string.Empty);
                    var value = input.GetAttributeValue("value", string.Empty);
                    inputCollection.Add(name + "=" + value);
                }
            }

            var content = string.Join("&", inputCollection);
            var actionUrl = formNode.GetAttributeValue("action", string.Empty);
            if (actionUrl.Length != 0)
            {
                // send post request and store cookies
                using (var joinGroupResponse = HttpRequester.Post("https://m.facebook.com" + actionUrl, content, cookies))
                {
                    cookies.Add(joinGroupResponse.Cookies);
                }
            }
        }

        // Tested -- worked
        public void LikePageOrDislike(string pageId)
        {
            var htmlNode = LoadDOM("https://m.facebook.com/" + pageId, ref cookies);
            // get like link            
            var likeAnchor = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div/div[1]/div[2]/div/div[2]/table/tbody/tr/td[1]/a");
            // if like link exist then send simulate mouse click to this link by send request.
            if (likeAnchor != null && likeAnchor.Attributes.Contains("href"))
            {
                // decode url
                var href = WebUtility.HtmlDecode(likeAnchor.Attributes["href"].Value);
                // Post like page request
                using (var likeResponse = HttpRequester.Get("https://m.facebook.com" + href, cookies))
                {
                    this.cookies.Add(likeResponse.Cookies);
                }
            }
        }

        // Tested-worked : Does not support attachment        
        public void PostToWall(string message)
        {
            PostHelper(message);
        }

        // Tested - ok : You can modify this method to post multiple messages.
        public void PostToGroup(string message, string groupId)
        {
            PostHelper(message, groupId);
        }

        // Get friend ids
        public List<string> GetFriends(string userId = "")
        {
            // See XPath for clear explanation
            // XPath for you
            // 1. Current page friends :    /html/body/div/div/div[2]/div/div[1]/div[2]
            // 2. Next page url :           /html/body/div/div/div[2]/div/div[1]/div[3]

            // XPath for other people
            // 1. Current page friends :    /html/body/div/div/div[2]/div/div[1]/div[1]
            // 2. Next page url :           /html/body/div/div/div[2]/div/div[1]/div[2]
            if (userId == email || userId == "")
                return GetFriendHelper("/" + email + "/friends?startindex=1", 2);
            else
                return GetFriendHelper("/" + userId + "/friends?startindex=1", 1);
        }

        // Get members info of group
        public List<Models.GroupMember> GetGroupMembers(string groupId = "", int page = 0)
        {
            var htmlNode = LoadDOM("https://m.facebook.com/browse/group/members/?id=" + groupId + "&start=" + page + "&listType=list_nonfriend", ref cookies);
            var parentNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div");

            if (parentNode == null) throw new NullReferenceException("Parent node does not exist");

            var dataNodes = parentNode.SelectNodes("table");

            // Create collection
            var groupMembers = new List<Models.GroupMember>();

            // Check if datanode exist
            if (dataNodes == null || dataNodes.Count == 0) return groupMembers;

            // loop through data node and add member
            foreach (var dataNode in dataNodes)
            {
                var id = Regex.Match(dataNode.Attributes["id"].Value, @"\d+").Value;
                var isAdminNode = dataNode.SelectSingleNode("tr/td[2]/div/h3[2]").InnerHtml;
                var isAdmin = isAdminNode.Contains("Admin") ||   // For English
                    isAdminNode.Contains("Quản trị viên");       // For Vietnamese
                var nameNode = dataNode.SelectSingleNode("tr/td[2]/div/h3[1]/a") ?? dataNode.SelectSingleNode("tr/td[2]/div/h3[1]");
                var name = nameNode == null ? "" : nameNode.InnerText;

                groupMembers.Add(new Models.GroupMember
                                    {
                                        UserId = id,
                                        IsAdmin = isAdmin,
                                        DisplayName = name
                                    });
            }

            // next page
            groupMembers.AddRange(GetGroupMembers(groupId, page + 30));

            return groupMembers;
        }

        // ===================================================
        private void Login()
        {
            var htmlNode = LoadDOM("https://m.facebook.com", ref cookies);

            // Get login form Dom object
            var formNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/table/tbody/tr/td/div[2]/div/form");
            var inputCollection = new List<string> { "email=" + email + "&pass=" + password };
            // Loop through login form and retreive data
            foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
            {
                if (input.GetAttributeValue("type", string.Empty) == "hidden")
                {
                    var name = input.GetAttributeValue("name", string.Empty);
                    var value = input.GetAttributeValue("value", string.Empty);
                    inputCollection.Add(name + "=" + value);
                }
            }
            var content = string.Join("&", inputCollection);

            // Post login request and store logged-in cookies.
            using (var loginResponse = HttpRequester.Post("https://m.facebook.com/login.php", content, this.cookies))
            {
                cookies.Add(loginResponse.Cookies);
            }
        }

        // post helper method : note that, targetId empty then post wall else post group        
        private void PostHelper(string message, string targetId = "")
        {
            var loadDOMUrl = string.Empty;
            var formNodeXPath = string.Empty;

            switch (targetId)
            {
                case "":
                    // Post wall
                    loadDOMUrl = "https://m.facebook.com/";
                    formNodeXPath = "/html/body/div/div/div[2]/div/div[2]/div/form";
                    break;
                default:
                    loadDOMUrl = "https://m.facebook.com/groups/" + targetId;
                    formNodeXPath = "/html/body/div/div/div[2]/div/div[1]/div[3]/form";
                    break;
            }

            var htmlNode = LoadDOM(loadDOMUrl, ref cookies);
            var formNode = htmlNode.SelectSingleNode(formNodeXPath);

            if (formNode == null) throw new NullReferenceException();

            var inputCollection = new List<string> { "view_post=Post", "xc_message=" + message };
            foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
            {
                if (input.GetAttributeValue("type", string.Empty) == "hidden")
                {
                    var name = input.GetAttributeValue("name", string.Empty);
                    var value = input.GetAttributeValue("value", string.Empty);
                    inputCollection.Add(name + "=" + value);
                }
            }

            var content = string.Join("&", inputCollection);

            var actionUrl = formNode.GetAttributeValue("action", string.Empty);
            if (actionUrl.Length != 0)
            {
                // send post request and store cookies
                using (var joinGroupResponse = HttpRequester.Post("https://m.facebook.com" + actionUrl, content, cookies))
                {
                    cookies.Add(joinGroupResponse.Cookies);
                }
            }
        }

        // Get friend ids helper method
        // type = 1 is the other
        // type = 2 is you        
        private List<string> GetFriendHelper(string url, int type)
        {
            var friends = new List<string>();
            var htmlNode = LoadDOM("https://m.facebook.com" + url, ref cookies);
            var friendsNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[" + type + "]");
            if (friendsNode != null)
            {
                foreach (var friendNode in friendsNode.ChildNodes)
                {
                    var result = Regex.Match(friendNode.InnerHtml, @"<a.*?href=""(?<refLink>.*?)fref=fr_tab"">.*?</a>", RegexOptions.Singleline);
                    var refLink = result.Groups["refLink"].Value;
                    var fbId = refLink.Contains("profile") ?
                        Regex.Match(refLink, @"(?<id>\d+)").Groups["id"].Value :
                        Regex.Match(refLink, @"/(?<id>.*?)\?").Groups["id"].Value;
                    if (fbId.Length != 0) friends.Add(fbId);
                }
            }

            var divNext = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[" + (type + 1) + "]/a");
            if (divNext != null)
            {
                var nextUrl = divNext.GetAttributeValue("href", string.Empty);
                if (nextUrl.Length != 0) friends.AddRange(GetFriendHelper(nextUrl, type));
            }

            return friends;
        }

        // Load DOM helper method
        private static HtmlNode LoadDOM(string url, ref CookieContainer cookieContainer)
        {
            var html = string.Empty;

            using (var response = HttpRequester.Get(url, cookieContainer) as HttpWebResponse)
            using (var responseStream = response.GetResponseStream())
            using (var gzipStream = new GZipStream(responseStream, CompressionMode.Decompress))
            using (var responseStreamReader = new StreamReader(gzipStream))
            {
                // store cookies
                cookieContainer.Add(response.Cookies);
                // store html content
                html = responseStreamReader.ReadToEnd();
            }

            // load html content to DOM
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            return htmlDocument.DocumentNode;
        }
    }
}