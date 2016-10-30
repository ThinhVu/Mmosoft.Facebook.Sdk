using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

using HtmlAgilityPack;

using Mmosoft.Utility;
using Mmosoft.Facebook.Sdk.Exceptions;
using System.Runtime.InteropServices;

namespace Mmosoft.Facebook.Sdk
{
    public class FacebookClient
    {
        /// <summary>
        /// Facebook email
        /// </summary>
        private string _email;
        /// <summary>
        /// Facebook password
        /// </summary>
        private string _password;
        /// <summary>
        /// Cookie container contain cookies for later request
        /// </summary>
        private CookieContainer _cookies;

        /// <summary>
        /// Create new instance of Facebook client class
        /// </summary>
        /// <param name="email">Facebook email or phone number</param>
        /// <param name="password">Facebook password</param>
        /// <exception cref="ArgumentException">Appear if email or password did not provided</exception>
        /// <exception cref="NodeNotFoundException">Exception raise if Login form DOM not found</exception>
        public FacebookClient(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("email does must not null or empty or contains only whitespace.");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("password does must not null or empty or contains only whitespace.");

            _email = email;
            _password = password;
            _cookies = new CookieContainer();

            try
            {
                if (!LogOn()) throw new LogOnException("LogOn failure with email=" + _email + "&password=" + _password);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Login method to get cookies
        /// </summary>
        /// <returns>Bool value indicate that this user logged-in or not</returns>
        private bool LogOn()
        {
            try
            {
                var htmlNode = LoadDOM("https://m.facebook.com", ref _cookies);

                // Get login form Dom object
                var formNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/table/tbody/tr/td/div[2]/div/form");
                // get input collection
                var inputCollection = new List<string> { "email=" + _email + "&pass=" + _password };
                foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
                {
                    if (input.Attributes["type"]?.Value != "hidden") continue;
                    var name = input.Attributes["name"]?.Value;
                    var value = input.Attributes["value"]?.Value;
                    inputCollection.Add(name + "=" + value);
                }
                var content = string.Join("&", inputCollection);

                using (var loginResponse = HttpRequester.Post(new Uri("https://m.facebook.com/login.php"), content, _cookies))
                {
                    if (loginResponse.Cookies["c_user"] == null) return false;
                    _cookies.Add(loginResponse.Cookies);
                    return true;
                }
            }
            catch
            {
                // rethrow
                throw;
            }
        }

        /// <summary>
        /// Send request to join or cancel group
        /// </summary>
        /// <param name="groupId">Id's target group</param>
        public void JoinGroup(string groupId)
        {
            try
            {
                var htmlNode = LoadDOM("https://m.facebook.com/groups/" + groupId, ref _cookies);
                var formNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/form");

                var inputCollection = new List<string>();               

                foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
                {
                    if (input.Attributes["type"]?.Value != "hidden") continue;
                    var name = input.Attributes["name"]?.Value;
                    var value = input.Attributes["value"]?.Value;
                    inputCollection.Add(name + "=" + value);
                }
                var content = string.Join("&", inputCollection);

                // post
                var actionUrl = "https://m.facebook.com" + formNode.Attributes["action"]?.Value;
                if (actionUrl.Length != 0)
                {
                    // send post request and store cookies
                    using (var joinGroupResponse = HttpRequester.Post(new Uri(actionUrl), content, _cookies))
                    {
                        _cookies.Add(joinGroupResponse.Cookies);
                    }
                }
            }
            catch
            {

                throw;
            }
        }

        /// <summary>
        /// Send request to like or dislike target page
        /// </summary>
        /// <param name="pageId">Id's target page</param>
        public void LikePage(string pageId)
        {
            try
            {
                var htmlNode = LoadDOM("https://m.facebook.com/" + pageId, ref _cookies);
                // get like link            
                var likeAnchor = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div/div[1]/div[2]/div/div[2]/table/tbody/tr/td[1]/a");
                // if like link exist then send simulate mouse click to this link by send request.
                if (likeAnchor == null) throw new NodeNotFoundException("LikePageOrDislike : Like button node not found");
                // Decode url
                var href = "https://m.facebook.com" + WebUtility.HtmlDecode(likeAnchor.Attributes["href"]?.Value);
                // Post like page request
                using (var likeResponse = HttpRequester.Get(new Uri(href), _cookies))
                {
                    _cookies.Add(likeResponse.Cookies);
                }
            }
            catch
            {

                throw;
            }
        }

        /// <summary>
        /// Post to wall
        /// </summary>
        /// <param name="message">Content you want to post</param>                  
        public void PostToWall(string message)
        {
            try
            {
                Post(message, string.Empty);
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Post to group
        /// </summary>
        /// <param name="message">Message you want to post</param>
        /// <param name="groupId">Id's target group</param>
        public void PostToGroup(string message, string groupId)
        {
            try
            {
                Post(message, groupId);
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Post message to targetId
        /// </summary>
        /// <param name="message">Content you want to post</param>
        /// <param name="targetId">Target id is group or wall</param>                      
        private void Post(string message, string targetId)
        {
            try
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
                        // post to group
                        loadDOMUrl = "https://m.facebook.com/groups/" + targetId;
                        formNodeXPath = "/html/body/div/div/div[2]/div/div[1]/div[3]/form";
                        break;
                }

                // load html node
                var htmlNode = LoadDOM(loadDOMUrl, ref _cookies);

                // get form node
                var formNode = htmlNode.SelectSingleNode(formNodeXPath);

                var inputCollection = new List<string> { "view_post=Post", "xc_message=" + message };
                foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
                {
                    if (input.Attributes["type"]?.Value != "hidden") continue;
                    var name = input.Attributes["name"]?.Value;
                    var value = input.Attributes["value"]?.Value;
                    inputCollection.Add(name + "=" + value);
                }
                var content = string.Join("&", inputCollection);

                // Post action
                var actionUrl = formNode.Attributes["action"]?.Value;
                if (actionUrl.Length != 0)
                {
                    // send post request and store cookies
                    // TODO : Check to get result
                    using (var joinGroupResponse = HttpRequester.Post(new Uri("https://m.facebook.com" + actionUrl), content, _cookies))
                    {
                        _cookies.Add(joinGroupResponse.Cookies);
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Get friend's id of someone
        /// </summary>
        /// <param name="userIdAlias">
        /// If userId passed is blank then you will get your friend list.
        /// Else you will get friend list of this id.
        /// </param>
        /// <returns>List id of friends</returns>
        public Common.FriendInfo GetFriendInfo([Optional] string userIdAlias)
        {
            // See XPath for clear explanation
            // XPath for you
            // 1. Current page friends :    /html/body/div/div/div[2]/div/div[1]/div[2]
            // 2. Next page url :           /html/body/div/div/div[2]/div/div[1]/div[3]

            // XPath for other people
            // 1. Current page friends :    /html/body/div/div/div[2]/div/div[1]/div[1]
            // 2. Next page url :           /html/body/div/div/div[2]/div/div[1]/div[2]

            var friendInfo = new Common.FriendInfo();
            if (userIdAlias == null || userIdAlias.Length == 0 || userIdAlias == _email)
            {                
                friendInfo.UserId = _email;
                friendInfo.Friends = GetFriends("https://m.facebook.com/" + _email + "/friends?startindex=1", 2);
            }
            else
            {                
                friendInfo.UserId = userIdAlias;
                friendInfo.Friends = GetFriends("https://m.facebook.com/" + userIdAlias + "/friends?startindex=1", 1);
            }
            return friendInfo;
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
        private List<Common.UserInfo> GetFriends(string url, int type)
        {
            try
            {
                var friends = new List<Common.UserInfo>();

                var htmlNode = LoadDOM(url, ref _cookies);

                var friendParentNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[" + type + "]");
                if (friendParentNode == null) return friends;

                foreach (var friendNode in friendParentNode.ChildNodes)
                {
                    HtmlNode dataNode = friendNode.SelectSingleNode("./table/tbody/tr/td[2]");
                    if (dataNode == null) continue;

                    string name, id;

                    // User name
                    name = dataNode.SelectSingleNode("./a")?.InnerText;

                    // id href
                    string idHref = dataNode.SelectSingleNode("./div[2]/a")?.Attributes["href"]?.Value;


                    if (idHref == null)
                    {
                        // Get href of this person
                        string href = dataNode.SelectSingleNode("./a")?.Attributes["href"]?.Value;
                        if (href == null) continue;

                        // If href does not contain profile.php mean href link contains alias string
                        if (!href.Contains("profile.php"))
                        {
                            // Get User Id from alias
                            id = GetUserId(href.Substring(1));
                        }
                        else
                        {
                            // Extract user id from href profile.php?id=user_id&fre...
                            id = Common.CompiledRegex.GetUserId2(href);
                        }
                    }
                    else
                    {
                        // if id href not null then get from this
                        id = Common.CompiledRegex.GetUserId1(idHref);
                    }

                    friends.Add(new Common.UserInfo
                    {
                        Id = id,
                        Name = name
                    });
                }

                // get next page div
                var divNext = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[" + (type + 1) + "]/a");
                if (divNext != null)
                {
                    var nextUrl = divNext.Attributes["href"]?.Value;
                    if (nextUrl != null || nextUrl.Length != 0)
                    {
                        var frs = GetFriends("https://m.facebook.com" + nextUrl, type);
                        friends.AddRange(frs);
                    }
                }

                return friends;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Get group info
        /// </summary>
        /// <param name="groupId">Group id you want to get info</param>
        /// <exception cref="NodeNotFoundException">Exception when select DOM query fail</exception>
        /// <returns>Group Info object</returns>
        public Common.GroupInfo GetGroupInfo(string groupId)
        {
            try
            {
                var groupInfo = new Common.GroupInfo();
                var htmlNode = LoadDOM("https://m.facebook.com/groups/" + groupId + "?view=info", ref _cookies);
                var groupNameNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[2]/table/tbody/tr/td[1]/a/table/tbody/tr/td[2]/h3");
                groupInfo.Id = groupId;
                groupInfo.Name = groupNameNode?.InnerText;
                ((List<Common.GroupMember>)groupInfo.Members).AddRange(GetGroupMembers(groupId, 0));
                return groupInfo;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get member info in specified group.
        /// </summary>
        /// <param name="groupId">Id of target group. Note that this method only support id of group, not for alias name</param>
        /// <param name="page">List friends will be paged, to get specified page, pass page value.</param>
        /// <returns>List of group member</returns>
        private List<Common.GroupMember> GetGroupMembers(string groupId, int page)
        {
            try
            {
                var groupMemberUrl = "https://m.facebook.com/browse/group/members/?id=" + groupId + "&start=" + page + "&listType=list_nonfriend";
                var htmlNode = LoadDOM(groupMemberUrl, ref _cookies);
                var parentNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div");
                var groupMembers = new List<Common.GroupMember>();

                // Check if datanode exist            
                var memberNodes = parentNode.SelectNodes("table");
                // TODO: Check null condition
                if (memberNodes == null || memberNodes.Count == 0) return groupMembers;

                // loop through data node and add member
                foreach (var memberNode in memberNodes)
                {
                    var userId = Regex.Match(memberNode.Attributes["id"]?.Value, @"\d+").Value;
                    var isAdminNodeHtml = memberNode.SelectSingleNode("tr/td[2]/div/h3[2]")?.InnerHtml;
                    var isAdmin = Common.LocalizationData.IsGroupAdministrator.Any(adminText => isAdminNodeHtml.Contains(adminText));
                    var nameNode = memberNode.SelectSingleNode("tr/td[2]/div/h3[1]/a") ?? memberNode.SelectSingleNode("tr/td[2]/div/h3[1]");
                    var displayName = (nameNode == null) ? string.Empty : nameNode.InnerText;

                    groupMembers.Add(new Common.GroupMember
                    {
                        UserId = userId,
                        IsAdmin = isAdmin,
                        DisplayName = displayName
                    });
                }

                // recursive -> next page
                var nextPageMembers = GetGroupMembers(groupId, page + 30);
                groupMembers.AddRange(nextPageMembers);

                return groupMembers;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get page reviews
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        public Common.PageReview GetReview(string pageId)
        {
            try
            {
                var htmlNode = LoadDOM("https://m.facebook.com/page/reviews.php?id=" + pageId, ref _cookies);

                // TODO : Check Any
                if (Common.LocalizationData.PageNotFound.Any(text => htmlNode.InnerHtml.Contains(text)))
                    throw new MissingReviewPageException(pageId);

                // get review nodes -- review node contain user's review
                var reviewNodes = htmlNode.SelectNodes("/html/body/div/div/div[2]/div[2]/div[1]/div/div[3]/div/div/div/div");

                // Create page review
                var pageReview = new Common.PageReview();

                // loop through DOM reviewNodes
                foreach (var reviewNode in reviewNodes)
                {
                    // create new instance of review info
                    var reviewInfo = new Common.Review();

                    // Get avatar
                    reviewInfo.UserAvatarUrl = WebUtility.HtmlDecode(reviewNode.SelectSingleNode("div/div/div[1]/a/div/img")?.Attributes["src"]?.Value);

                    // User name and id                
                    var userNameIdNode = reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[1]");
                    if (userNameIdNode != null)
                    {
                        // Get urlink and parse
                        var urlLink = userNameIdNode.Attributes["href"]?.Value;
                        if (urlLink.Contains("/profile.php?id="))
                            reviewInfo.UserId = urlLink.Substring(16); // /profile.php?id=100012141183155
                        else
                            reviewInfo.UserId = urlLink.Substring(1); // /kakarotto.pham.9

                        // Display name
                        reviewInfo.UserDisplayName = WebUtility.HtmlDecode(userNameIdNode.SelectSingleNode("span")?.InnerText);
                    }

                    // Get rate score
                    reviewInfo.RateScore = int.Parse(reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[2]/img").Attributes["alt"]?.Value?[0].ToString(), CultureInfo.CurrentCulture);

                    // Get fully rate content page
                    var rateContentAnchorLink = reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[2]")?.GetAttributeValue("href", string.Empty);

                    // Request to get fully rate content
                    var htmlRateContentNode = LoadDOM("https://m.facebook.com" + rateContentAnchorLink, ref _cookies);

                    reviewInfo.Content = htmlRateContentNode.SelectSingleNode("html/body/div/div/div[2]/div/div[1]/div/div[1]/div/div[1]/div[2]/p")?.InnerText;

                    pageReview.Reviews.Add(reviewInfo);
                }

                return pageReview;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get user id from alias
        /// </summary>
        /// <param name="userAlias">User alias</param>
        /// <returns>user id</returns>
        public string GetUserId(string userAlias)
        {
            // load profile page of this user with alias string
            var htmlDOM = LoadDOM("https://m.facebook.com/" + userAlias, ref _cookies);

            // get More button href
            // if td[3] does not exists then choose td[2]
            var href = htmlDOM.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[1]/div[3]/table/tr/td[3]/a")?.Attributes["href"]?.Value ??
                    htmlDOM.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[1]/div[3]/table/tr/td[2]/a")?.Attributes["href"]?.Value;
            // If MORE button exists then get id from it
            if (href != null)
            {
                // Parse More button to get user id
                return Common.CompiledRegex.GetUserId2(href);
            }
            else
            {
                // Select another anchor
                var r = htmlDOM.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[1]/div[2]/div[1]/div[1]/a")?.Attributes["href"]?.Value;

                if (r != null)
                {
                    return Regex.Match(r, @"\D+[^(fb)]id=(?<id>\d+)").Groups["id"].Value;
                }
                // if another anchor does not exist then choose another anchor :v
                else
                {
                    throw new Exception("Not found anchor to detect user id -- coming up");
                }
            }
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
            try
            {
                var html = string.Empty;

                using (var resp = HttpRequester.Get(new Uri(url), cookieContainer))
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
            catch
            {
                throw;
            }
        }
    }
}