namespace FacebookAPI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;
    using FacebookAPI.Models.Exception;

    public class FacebookClient
    {
        /// <summary>
        /// Facebook email
        /// </summary>
        private string _Email;
        /// <summary>
        /// Facebook password
        /// </summary>
        private string _Password;
        /// <summary>
        /// Cookie container contain cookies for later request
        /// </summary>
        private CookieContainer _Cookies;

        /// <summary>
        /// Create new instance of Facebook client class
        /// </summary>
        /// <param name="email">Facebook email or phone number</param>
        /// <param name="password">Facebook password</param>
        public FacebookClient(string email, string password)
        {
            // Init data
            _Email = email;
            _Password = password;
            _Cookies = new CookieContainer();

            // Login -- bootstrap cookie
            Login();
        }

        /// <summary>
        /// Send request to join or cancel group
        /// </summary>
        /// <param name="groupId">Id's target group</param>
        public void JoinGroupOrCancel(string groupId)
        {
            var htmlNode = LoadDOM($"https://m.facebook.com/groups/{groupId}", ref _Cookies);
            // extract form data
            var formNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/form");
            var inputCollection = new List<string>();
            foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
            {
                if (input.GetAttributeValue("type", string.Empty) == "hidden")
                {
                    var name = input.GetAttributeValue("name", string.Empty);
                    var value = input.GetAttributeValue("value", string.Empty);
                    inputCollection.Add($"{name}={value}");
                }
            }
            var content = string.Join("&", inputCollection);
            var actionUrl = formNode.GetAttributeValue("action", string.Empty);
            if (actionUrl.Length != 0)
            {
                // send post request and store cookies
                using (var joinGroupResponse = HttpRequester.Post($"https://m.facebook.com{actionUrl}", content, _Cookies))
                {
                    _Cookies.Add(joinGroupResponse.Cookies);
                }
            }
        }

        /// <summary>
        /// Send request to like or dislike target page
        /// </summary>
        /// <param name="pageId">Id's target page</param>
        public void LikePageOrDislike(string pageId)
        {
            var htmlNode = LoadDOM($"https://m.facebook.com/{pageId}", ref _Cookies);
            // get like link            
            var likeAnchor = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div/div[1]/div[2]/div/div[2]/table/tbody/tr/td[1]/a");
            // if like link exist then send simulate mouse click to this link by send request.
            if (likeAnchor != null && likeAnchor.Attributes.Contains("href"))
            {
                // Decode url
                var href = WebUtility.HtmlDecode(likeAnchor.Attributes["href"].Value);
                // Post like page request
                using (var likeResponse = HttpRequester.Get($"https://m.facebook.com{href}", _Cookies))
                {
                    _Cookies.Add(likeResponse.Cookies);
                }
            }
        }
        
        /// <summary>
        /// Post to wall
        /// </summary>
        /// <param name="message">Content you want to post</param>                  
        public void PostToWall(string message)
        {
            PostHelper(message);
        }

        /// <summary>
        /// Post to group
        /// </summary>
        /// <param name="message">Message you want to post</param>
        /// <param name="groupId">Id's target group</param>
        public void PostToGroup(string message, string groupId)
        {
            PostHelper(message, groupId);
        }

        /// <summary>
        /// Get friend's id of someone
        /// </summary>
        /// <param name="userId">
        /// If userId passed is blank then you will get your friend list.
        /// Else you will get friend list of this id.
        /// </param>
        /// <returns>List id of friends</returns>
        public List<string> GetFriends(string userId = "")
        {
            // See XPath for clear explanation
            // XPath for you
            // 1. Current page friends :    /html/body/div/div/div[2]/div/div[1]/div[2]
            // 2. Next page url :           /html/body/div/div/div[2]/div/div[1]/div[3]

            // XPath for other people
            // 1. Current page friends :    /html/body/div/div/div[2]/div/div[1]/div[1]
            // 2. Next page url :           /html/body/div/div/div[2]/div/div[1]/div[2]
            if (userId == _Email || userId == string.Empty)
                return GetFriendHelper($"/{_Email}/friends?startindex=1", 2);
            else
                return GetFriendHelper($"/{userId}/friends?startindex=1", 1);
        }

        /// <summary>
        /// Get group info
        /// </summary>
        /// <param name="groupId">Group id you want to get info</param>
        /// <returns>Group Info object</returns>
        public Models.Group.GroupInfo GetGroupInfo(string groupId)
        {
            var groupInfo = new Models.Group.GroupInfo();
            var htmlNode = LoadDOM($"https://m.facebook.com/groups/{groupId}?view=info", ref _Cookies);
            var groupNameNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[2]/table/tbody/tr/td[1]/a/table/tbody/tr/td[2]/h3");
            groupInfo.Id = groupId;
            groupInfo.Name = groupNameNode.InnerText;
            groupInfo.Members = GetGroupMembers(groupId);

            return groupInfo;
        }

        public List<Models.Page.ReviewInfo> GetReviews(string pageId)
        {            
            // load content to DOM
            var reviewUrl = $"https://m.facebook.com/{pageId}/reviews";
            var htmlNode = LoadDOM(reviewUrl, ref _Cookies);

            // page not found detect. -- localization
            var pageNotFounds = new Dictionary<string, string> 
            {
                // English
                { "English", "The page you requested cannot be displayed right now." },
                
            };
                       
            foreach (var pageNotFound in pageNotFounds)
            {
                if (htmlNode.InnerHtml.Contains(pageNotFound.Value))
                    throw new ContentNotFoundException($"Review page not found in {pageNotFound.Key}");
            }
            
            // get review nodes -- review node contain user's review
            var reviewNodes = htmlNode.SelectNodes("/html/body/div/div/div[2]/div[2]/div[1]/div/div[3]/div/div/div/div");

            // Create review info to contain user review info
            var reviewInfos = new List<Models.Page.ReviewInfo>();

            // loop through DOM reviewNodes
            foreach (var reviewNode in reviewNodes)
            {
                // create new instance of review info
                var reviewInfo = new Models.Page.ReviewInfo();
                
                // Get avatar
                reviewInfo.AvatarUrl = WebUtility.HtmlDecode(reviewNode.SelectSingleNode("div/div/div[1]/a/div/img")?.Attributes["src"]?.Value);

                // User name and id                
                var userNameIdNode = reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[1]");
                if (userNameIdNode != null)
                {                                        
                    // Get urlink and parse
                    var urlLink = userNameIdNode.Attributes["href"]?.Value;
                    if (urlLink.Contains("/profile.php?id="))
                        reviewInfo.Id = urlLink.Substring(16); // /profile.php?id=100012141183155
                    else
                        reviewInfo.Id = urlLink.Substring(1); // /kakarotto.pham.9

                    // Display name
                    reviewInfo.DisplayName = WebUtility.HtmlDecode(userNameIdNode.SelectSingleNode("span")?.InnerText);
                }

                // Get rate score
                reviewInfo.RateScore = int.Parse(reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[2]/img").Attributes["alt"]?.Value?[0].ToString());                

                // Get fully rate content page
                var rateContentAnchorLink = reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[2]")?.GetAttributeValue("href",string.Empty);

                // Request to get fully rate content
                var htmlRateContentNode = LoadDOM($"https://m.facebook.com{rateContentAnchorLink}", ref _Cookies);

                reviewInfo.Content = htmlRateContentNode.SelectSingleNode("html/body/div/div/div[2]/div/div[1]/div/div[1]/div/div[1]/div[2]/p")?.InnerText;

                reviewInfos.Add(reviewInfo);
            }

            return reviewInfos;
        }

        /// <summary>
        /// Get member info in specified group.
        /// </summary>
        /// <param name="groupId">Id of target group. Note that this method only support id of group, not for alias name</param>
        /// <param name="page">List friends will be paged, to get specified page, pass page value.</param>
        /// <returns>List of group member</returns>
        private List<Models.Group.GroupMember> GetGroupMembers(string groupId = "", int page = 0)
        {
            var htmlNode = LoadDOM($"https://m.facebook.com/browse/group/members/?id={groupId}&start={page}&listType=list_nonfriend", ref _Cookies);
            var parentNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div");
            if (parentNode == null) throw new NullReferenceException("Parent node does not exist.");

            // get data nodes
            var dataNodes = parentNode.SelectNodes("table");

            // Create collection
            var groupMembers = new List<Models.Group.GroupMember>();

            // Check if datanode exist
            if (dataNodes == null || dataNodes.Count == 0) return groupMembers;

            // loop through data node and add member
            foreach (var dataNode in dataNodes)
            {
                var userId = Regex.Match(dataNode.Attributes["id"].Value, @"\d+").Value;
                var isAdminNode = dataNode.SelectSingleNode("tr/td[2]/div/h3[2]");
                if (isAdminNode == null) continue;
                var isAdminNodeHtml = isAdminNode.InnerHtml;
                var isAdmin = isAdminNodeHtml.Contains("Admin") ||   // For English
                    isAdminNodeHtml.Contains("Quản trị viên");       // For Vietnamese
                var nameNode = dataNode.SelectSingleNode("tr/td[2]/div/h3[1]/a") ?? dataNode.SelectSingleNode("tr/td[2]/div/h3[1]");
                var displayName = nameNode == null ? string.Empty : nameNode.InnerText;

                groupMembers.Add(new Models.Group.GroupMember
                                    {
                                        UserId = userId,
                                        IsAdmin = isAdmin,
                                        DisplayName = displayName
                                    });
            }

            // next page
            groupMembers.AddRange(GetGroupMembers(groupId, page + 30));

            return groupMembers;
        }

        /// <summary>
        /// Login method to get cookies
        /// </summary>
        private void Login()
        {
            var htmlNode = LoadDOM("https://m.facebook.com", ref _Cookies);

            // Get login form Dom object
            var formNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/table/tbody/tr/td/div[2]/div/form");
            var inputCollection = new List<string> { $"email={_Email}&pass={_Password}" };
            // Loop through login form and retreive data
            foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
            {
                if (input.GetAttributeValue("type", string.Empty) == "hidden")
                {
                    var name = input.GetAttributeValue("name", string.Empty);
                    var value = input.GetAttributeValue("value", string.Empty);
                    inputCollection.Add($"{name}={value}");
                }
            }
            var content = string.Join("&", inputCollection);
            // Post login request and store logged-in cookies.
            using (var loginResponse = HttpRequester.Post("https://m.facebook.com/login.php", content, _Cookies))
            {
                _Cookies.Add(loginResponse.Cookies);
            }
        }

        /// <summary>
        /// Post message to targetId
        /// </summary>
        /// <param name="message">Content you want to post</param>
        /// <param name="targetId">Target id is group or wall</param>                      
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

            var htmlNode = LoadDOM(loadDOMUrl, ref _Cookies);
            var formNode = htmlNode.SelectSingleNode(formNodeXPath);

            if (formNode == null) throw new NullReferenceException();

            var inputCollection = new List<string> { "view_post=Post", "xc_message=" + message };
            foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
            {
                if (input.GetAttributeValue("type", string.Empty) == "hidden")
                {
                    var name = input.GetAttributeValue("name", string.Empty);
                    var value = input.GetAttributeValue("value", string.Empty);
                    inputCollection.Add($"{name}={value}");
                }
            }
            var content = string.Join("&", inputCollection);
            var actionUrl = formNode.GetAttributeValue("action", string.Empty);
            if (actionUrl.Length != 0)
            {
                // send post request and store cookies
                using (var joinGroupResponse = HttpRequester.Post("https://m.facebook.com" + actionUrl, content, _Cookies))
                {
                    _Cookies.Add(joinGroupResponse.Cookies);
                }
            }
        }
    
        /// <summary>
        /// Get friend helper method. This method support for GetFriend
        /// </summary>
        /// <param name="url">Url</param>
        /// <param name="type">
        /// Type = 1 if you want to get other friends.
        /// Type = 2 if you want to get your friend.
        /// </param>
        /// <returns>List of facebook user id</returns>                  
        private List<string> GetFriendHelper(string url, int type)
        {
            var friends = new List<string>();
            var htmlNode = LoadDOM($"https://m.facebook.com{url}", ref _Cookies);
            var friendsNode = htmlNode.SelectSingleNode($"/html/body/div/div/div[2]/div/div[1]/div[{type}]");
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

            var divNext = htmlNode.SelectSingleNode($"/html/body/div/div/div[2]/div/div[1]/div[{type + 1}]/a");
            if (divNext != null)
            {
                var nextUrl = divNext.GetAttributeValue("href", string.Empty);
                if (nextUrl.Length != 0) friends.AddRange(GetFriendHelper(nextUrl, type));
            }

            return friends;
        }
        
        /// <summary>
        /// Load DOM method. This method get url and download html content then parse to DOM object
        /// using HtmlAgilityPack library.
        /// </summary>
        /// <param name="url">Url want to get DOM</param>
        /// <param name="cookieContainer">cookie you want to pass for this method</param>
        /// <returns>DOM object parsed from html content</returns>
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