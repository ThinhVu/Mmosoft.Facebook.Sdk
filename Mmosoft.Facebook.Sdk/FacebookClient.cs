using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.InteropServices;
// ----------------------------------
using HtmlAgilityPack;
using Mmosoft.Facebook.Sdk.Exceptions;
using Mmosoft.Facebook.Sdk.Models;
using Mmosoft.Facebook.Sdk.Common;
using System.Xml;
using System.Text;
using System.Diagnostics;

namespace Mmosoft.Facebook.Sdk
{
    public class FacebookClient : IFacebookClient
    {
        /// <summary>
        /// Cookie container contain cookies for later request
        /// </summary>
        private CookieContainer cookies;
        public string Id { get; private set; }
        /// <summary>
        /// Facebook email
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Facebook password
        /// </summary>        
        public string Password { get; set; }
        /// <summary>
        /// Get group user joined
        /// </summary>
        public GroupInfo Group
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Create new instance of Facebook client class
        /// </summary>
        /// <param name="username">Facebook email or phone number</param>
        /// <param name="password">Facebook password</param>
        /// <exception cref="ArgumentException">Appear if email or password did not provided</exception>
        /// <exception cref="NodeNotFoundException">Exception raise if Login form DOM not found</exception>
        public FacebookClient(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("email does must not null or empty or contains only whitespace.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("password does must not null or empty or contains only whitespace.");
            }

            Username = username;
            Password = password;
            cookies = new CookieContainer();

            if (!LogOn())
            {
                throw new LogOnFailureException("email=" + this.Username + "&password=" + this.Password);
            }

            Id = GetUserId(Username);
        }

        /// <summary>
        /// Login method to get cookies
        /// </summary>
        /// <returns>Bool value indicate that this user logged-in or not</returns>
        private bool LogOn()
        {
            var documentNode = Http.LoadDom("https://m.facebook.com", ref cookies);

            // Get login form Dom object
            var logInFormNode = documentNode.SelectSingleNode("//form[@id='login_form']");

            // get input collection
            var inputCollection = new List<string> { "email=" + Username + "&pass=" + Password };

            foreach (HtmlNode input in logInFormNode.ParentNode.Elements("input"))
            {
                // if input not hidden then step over
                if (input.Attributes["type"].Value != "hidden") continue;
                // else we need to get info and add key-value pair to collection
                var name = input.Attributes["name"].Value;
                var value = input.Attributes["value"].Value;
                inputCollection.Add(name + "=" + value);
            }
            // now join to get content for Post request
            var content = string.Join("&", inputCollection);
            using (var loginResponse = Http.Post(new Uri("https://m.facebook.com/login.php"), content, cookies))
            {
                if (loginResponse.Cookies["c_user"] != null)
                {
                    cookies.Add(loginResponse.Cookies);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Send request to join or cancel group
        /// </summary>
        /// <param name="groupId">Id's target group</param>
        public bool JoinGroup(string groupId)
        {
            var htmlNode = Http.LoadDom("https://m.facebook.com/groups/" + groupId, ref cookies);
            var formNodes = htmlNode.SelectNodes("//form");

            foreach (var formNode in formNodes)
            {
                if (formNode.Attributes["action"].Value.StartsWith("/a/group/join/"))
                {
                    var inputs = new List<string>();
                    foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
                    {
                        if (input.Attributes["type"].Value != "hidden") continue;
                        var name = input.Attributes["name"].Value;
                        var value = input.Attributes["value"].Value;
                        inputs.Add(name + "=" + value);
                    }
                    var content = string.Join("&", inputs);
                    // post
                    var actionUrl = "https://m.facebook.com" + formNode.Attributes["action"].Value;
                    // send post request and store cookies               
                    using (var joinGroupResponse = Http.Post(new Uri(actionUrl), content, cookies))
                    {
                        cookies.Add(joinGroupResponse.Cookies);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Cancel join group
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public bool CancelJoinGroup(string groupId)
        {
            var htmlNode = Http.LoadDom("https://m.facebook.com/groups/" + groupId, ref cookies);
            var formNodes = htmlNode.SelectNodes("//form");

            foreach (var formNode in formNodes)
            {
                if (formNode.Attributes["action"].Value.StartsWith("/a/group/canceljoin"))
                {
                    var inputs = new List<string>();

                    foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
                    {
                        if (input.Attributes["type"].Value == "hidden")
                        {
                            var name = input.Attributes["name"].Value;
                            var value = input.Attributes["value"].Value;
                            inputs.Add(name + "=" + value);
                        }
                    }

                    var content = string.Join("&", inputs);

                    var actionUrl = "https://m.facebook.com" + formNode.Attributes["action"].Value;

                    using (var joinGroupResponse = Http.Post(new Uri(actionUrl), content, cookies))
                    {
                        cookies.Add(joinGroupResponse.Cookies);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Leave group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="preventReAdd"></param>
        /// <returns></returns>
        public bool LeaveGroup(string groupId, bool preventReAdd)
        {
            var htmlNode = Http.LoadDom("https://m.facebook.com/group/leave/?group_id=" + groupId, ref cookies);

            var formNodes = htmlNode.SelectNodes("//form");

            foreach (var formNode in formNodes)
            {
                if (formNode.Attributes["action"].Value.StartsWith("/a/group/leave/"))
                {
                    var inputElements = formNode.SelectNodes("//input");
                    // name = fb_dtsg , type = hidden
                    // name = charset_test, type = hidden
                    // name = prevent_readd, type = checkbox
                    // name = group_id, type = hidden
                    // name = confirm, type = submit
                    var requiredInputNames = new string[] { "fb_dtsg", "charset_test", "group_id", "confirm" };
                    var preventReAddValue = preventReAdd ? "on" : "off";
                    var inputs = new List<string> { "prevent_readd=" + preventReAddValue };

                    foreach (HtmlNode input in inputElements)
                    {
                        var inputName = input.Attributes["name"]?.Value;
                        if (requiredInputNames.Contains(inputName))
                        {
                            var name = input.Attributes["name"].Value;
                            var value = input.Attributes["value"].Value;
                            inputs.Add(name + "=" + value);
                        }
                    }

                    var content = string.Join("&", inputs);

                    var actionUrl = "https://m.facebook.com" + formNode.Attributes["action"].Value;

                    using (var joinGroupResponse = Http.Post(new Uri(actionUrl), content, cookies))
                    {
                        cookies.Add(joinGroupResponse.Cookies);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Send request to like or dislike target page
        /// </summary>
        /// <param name="pageIdOrAlias">Id's target page</param>
        public bool LikePage(string pageIdOrAlias)
        {
            var htmlNode = Http.LoadDom("https://m.facebook.com/" + pageIdOrAlias, ref cookies);
            var pagesMbasicContextItemsId = htmlNode.SelectSingleNode("//div[@id='pages_mbasic_context_items_id']");
            if (pagesMbasicContextItemsId != null)
            {
                // This div contain Like, Message, .. action
                var actionDiv = pagesMbasicContextItemsId.PreviousSibling;
                var anchors = actionDiv.SelectNodes("//table/tbody/tr/td[1]/a");
                var compiledRegex = new Regex(@"fan&amp;id=(?<pid>\d+)");

                foreach (var anchor in anchors)
                {
                    var href = anchor.Attributes["href"]?.Value;
                    if (href != null && compiledRegex.Match(href).Success)
                    {
                        using (var likeResponse = Http.Get(new Uri("https://m.facebook.com" + href), cookies))
                        {
                            cookies.Add(likeResponse.Cookies);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Post to wall
        /// </summary>
        /// <param name="message">Content you want to post</param>                  
        public void PostToWall(string message)
        {
            Post(message, string.Empty);
        }

        /// <summary>
        /// Post to group
        /// </summary>
        /// <param name="message">Message you want to post</param>
        /// <param name="groupId">Id's target group</param>
        public void PostToGroup(string message, string groupId)
        {
            Post(message, groupId);
        }

        /// <summary>
        /// Post message to targetId
        /// </summary>
        /// <param name="message">Content you want to post</param>
        /// <param name="targetId">Target id is group or wall</param>                      
        private void Post(string message, string targetId)
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
            var htmlNode = Http.LoadDom(loadDOMUrl, ref cookies);

            // get form node
            var formNode = htmlNode.SelectSingleNode(formNodeXPath);

            var inputs = new List<string> { "view_post=Post", "xc_message=" + message };
            foreach (HtmlNode input in formNode.ParentNode.Elements("input"))
            {
                if (input.Attributes["type"].Value != "hidden") continue;

                var name = input.Attributes["name"].Value;
                var value = input.Attributes["value"].Value;

                inputs.Add(name + "=" + value);
            }
            var content = string.Join("&", inputs);

            // Post action
            var actionUrl = "https://m.facebook.com" + formNode.Attributes["action"].Value;
            using (var joinGroupResponse = Http.Post(new Uri(actionUrl), content, cookies))
            {
                cookies.Add(joinGroupResponse.Cookies);
            }
        }

        /// <summary>
        /// Get friend's id of someone
        /// </summary>
        /// <param name="userIdOrAlias">
        /// If userId passed is blank then you will get your friend list.
        /// Else you will get friend list of this id.
        /// </param>
        /// <returns>List id of friends</returns>
        public FriendInfo GetFriendInfo([Optional] string userIdOrAlias)
        {           
            var friendInfo = new FriendInfo();
            var userId = string.Empty;

            if (userIdOrAlias == null || userIdOrAlias.Length == 0 || userIdOrAlias == Username)
            {
                userId = Id;                
            }
            else
            {
                var matchNonDigit = Regex.Match(userIdOrAlias, @"\D+");
                if (matchNonDigit.Success) userId = GetUserId(userIdOrAlias);
                else userId = userIdOrAlias;                         
            }
            
            friendInfo.UserId = userId;
            friendInfo.Friends = GetFriends("https://m.facebook.com/profile.php?v=friends&startindex=1&id=" + userId);

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
        private List<UserInfo> GetFriends(string url)
        {
            var friends = new List<UserInfo>();
            HtmlNode htmlNode = null;

            try
            {
                 htmlNode = Http.LoadDom(url, ref cookies);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            if (htmlNode == null) return friends;

            var rootNode = htmlNode.SelectSingleNode("//div[@id='root']");
            // There is so much error in HtmlAgilityPack 
            // or we can say it error from XPath library from Microsoft.
            // This method select all node in entire document rather than current node
            // var friendTables1 = rootNode.SelectNodes("//table[@role='presentation']");            
            var hDoc = new HtmlDocument();
            hDoc.LoadHtml(rootNode.InnerHtml);
            var friendTables = hDoc.DocumentNode.SelectNodes("//table[@role='presentation']");
            if (friendTables == null)
            {
                return new List<UserInfo>();
            }
            foreach (var friendTable in friendTables)
            {
                string id = string.Empty,
                    name = string.Empty,
                    avatar = string.Empty;

                var imgAvatar = friendTable.SelectSingleNode("tbody/tr/td/img");
                if (imgAvatar != null)
                {
                    avatar = WebUtility.HtmlDecode(imgAvatar.GetAttributeValue("src", "").ToString());
                    name = WebUtility.HtmlDecode(imgAvatar.Attributes["alt"]?.Value);
                }
                var friendInfoLink = friendTable.SelectSingleNode("tbody/tr/td[2]/a")?.Attributes["href"]?.Value;
                if (friendInfoLink != null)
                {
                    if (!friendInfoLink.Contains("profile.php"))
                    {
                        try
                        {
                            // Get User Id from alias
                            id = GetUserId(friendInfoLink.Substring(1));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    else
                    {
                        // Extract user id from href profile.php?id=user_id&fre...
                        id = CompiledRegex.GetUserId2(friendInfoLink);
                    }
                }
                else
                {
                    // Blocked user -- banned user -- 
                    //else
                    //{
                    //    // if id href not null then get from this
                    //    id = CompiledRegex.GetUserId1(idHref);
                    //}
                }

                if (id.Length != 0)
                {
                    friends.Add(new UserInfo() { Id = id, Name = name, Avatar = avatar });
                }
            }

            // Next page
            var nextUrl = rootNode.SelectSingleNode("//div[@id='m_more_friends']/a")?.Attributes["href"]?.Value;
            if (nextUrl != null && nextUrl.Length != 0)
            {
                var frs = GetFriends("https://m.facebook.com" + nextUrl);
                friends.AddRange(frs);
            }

            return friends;
        }

        /// <summary>
        /// Get group info
        /// </summary>
        /// <param name="groupId">Group id you want to get info</param>
        /// <exception cref="NodeNotFoundException">Exception when select DOM query fail</exception>
        /// <returns>Group Info object</returns>
        public GroupInfo GetGroupInfo(string groupId)
        {
            var documentNode = Http.LoadDom("https://m.facebook.com/groups/" + groupId + "?view=info", ref cookies);
            var groupNameNode = documentNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[2]/table/tbody/tr/td[1]/a/table/tbody/tr/td[2]/h3");
            var groupInfo = new GroupInfo
            {
                Id = groupId,
                Name = groupNameNode.InnerText
            };
            groupInfo.Members.AddRange(GetGroupMembers(groupId, 0));
            return groupInfo;
        }

        /// <summary>
        /// Get member info in specified group.
        /// </summary>
        /// <param name="groupId">Id of target group. Note that this method only support id of group, not for alias name</param>
        /// <param name="page">List friends will be paged, to get specified page, pass page value.</param>
        /// <returns>List of group member</returns>
        private List<GroupMember> GetGroupMembers(string groupId, int page)
        {
            var groupMembers = new List<GroupMember>();

            var groupMemberUrl = "https://m.facebook.com/browse/group/members/?id=" + groupId + "&start=" + page + "&listType=list_nonfriend";
            var memberNodes = Http.LoadDom(groupMemberUrl, ref cookies).SelectNodes("/html/body/div/div/div[2]/div/div[1]/div/table");
            if (memberNodes.Count == 0) return groupMembers;

            foreach (var memberNode in memberNodes)
            {
                var userId = Regex.Match(memberNode.Attributes["id"].Value, @"\d+").Value;
                var isAdminNodeHtml = memberNode.SelectSingleNode("tr/td[2]/div/h3[2]").InnerHtml;
                var isAdmin = LocalizationData.IsGroupAdministrator.Any(adminText => isAdminNodeHtml.Contains(adminText));
                var nameNode = memberNode.SelectSingleNode("tr/td[2]/div/h3[1]/a") ?? memberNode.SelectSingleNode("tr/td[2]/div/h3[1]");
                var displayName = (nameNode == null) ? string.Empty : nameNode.InnerText;

                groupMembers.Add(new GroupMember
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

        /// <summary>
        /// Get page reviews
        /// </summary>
        /// <param name="pageIdOrAlias"></param>
        /// <returns></returns>
        public ReviewInfo GetReviewInfo(string pageIdOrAlias)
        {
            var pageId = string.Empty;

            // Checking if pageId is pageAlias or PageId
            var matches = Regex.Matches(pageIdOrAlias, "\\d+");
            if (matches.Count != 1)
            {
                // param passed is page alias, we need to get page id from it.
                pageId = GetPageId(pageIdOrAlias);
            }

            if (pageId.Length == 0)
            {
                throw new ArgumentException("can not detect page id of " + pageIdOrAlias);
            }

            var htmlNode = Http.LoadDom("https://m.facebook.com/page/reviews.php?id=" + pageIdOrAlias, ref cookies);

            if (LocalizationData.PageNotFound.Any(text => htmlNode.InnerHtml.Contains(text)))
            {
                throw new MissingReviewPageException(pageIdOrAlias + " not contains review page");
            }

            // get review nodes -- review node contain user's review
            var reviewNodes = htmlNode.SelectNodes("/html/body/div/div/div[2]/div[2]/div[1]/div/div[3]/div/div/div/div");

            // Create page review
            var pageReview = new ReviewInfo();

            // loop through DOM reviewNodes
            foreach (var reviewNode in reviewNodes)
            {
                // create new instance of review info
                var reviewInfo = new Review();

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
                var htmlRateContentNode = Http.LoadDom("https://m.facebook.com" + rateContentAnchorLink, ref cookies);

                reviewInfo.Content = htmlRateContentNode.SelectSingleNode("html/body/div/div/div[2]/div/div[1]/div/div[1]/div/div[1]/div[2]/p")?.InnerText;

                pageReview.Reviews.Add(reviewInfo);
            }

            return pageReview;
        }

        /// <summary>
        /// Get user id from alias
        /// </summary>
        /// <param name="userAlias">User alias</param>
        /// <returns>user id</returns>
        public string GetUserId(string userAlias)
        {
            string id = string.Empty;
            // load profile page of this user with alias string
            var htmlDOM = Http.LoadDom("https://m.facebook.com/" + userAlias, ref cookies);

            // trying get from avatar href
            var avatarHref = htmlDOM.SelectSingleNode("//div[@id='m-timeline-cover-section']/div/div/div/a")?.Attributes["href"]?.Value;
            if (avatarHref != null)
            {
                // /profile/picture/view/?profile_id=100006909485444&refid=17
                var match = Regex.Match(avatarHref, @"/profile/picture/view/?profile_id=(?<id>\d+)");
                if (match.Success) return match.Groups["id"].Value;
            }

            // trying to get from root div if above way is not success
            avatarHref = htmlDOM.SelectSingleNode("//div[@id='root']/div/div/div/div/div/a")?.Attributes["href"]?.Value;
            if (avatarHref != null)
            {
                var match = Regex.Match(avatarHref, @"/photo.php\?fbid=\d+&amp;id=(?<id>\d+)");
                if (match.Success)
                {
                    return match.Groups["id"].Value;
                }
                else
                {
                    match = Regex.Match(avatarHref, @"/profile/picture/view/\?profile_id=(?<id>\d+)");
                    if (match.Success)
                    {
                        return match.Groups["id"].Value;
                    }
                }
            }

            // trying get uid from action button : Add Friend, Message, Follow, More
            var actionTds = htmlDOM.SelectNodes("//div[@id='m-timeline-cover-section']/div/table/tbody/tr/td") ??
                 htmlDOM.SelectNodes("//div[@id='m-timeline-cover-section']/div/table/tr/td") ??
                 htmlDOM.SelectNodes("//div[@id='root']/div/div/div/table/tbody/tr/td") ??
                 htmlDOM.SelectNodes("//div[@id='root']/div/div/div/table/tr/td");

            if (actionTds != null)
            {
                // For English only
                foreach (var td in actionTds)
                {
                    switch (td.InnerText)
                    {
                        case "Add Friend":
                            var addFriendHref = td.SelectSingleNode("a")?.Attributes["href"]?.Value;
                            if (addFriendHref != null)
                            {
                                var match = Regex.Match(addFriendHref, @"profile_add_friend.php\?subjectid=(?<id>\d+)");
                                if (match.Success) return match.Groups["id"].Value;
                            }
                            break;
                        case "Message":
                            var messageHref = td.SelectSingleNode("a")?.Attributes["href"]?.Value;
                            if (messageHref != null)
                            {
                                var match = Regex.Match(messageHref, @"/messages/thread/(?<id>\d+)/");
                                if (match.Success) return match.Groups["id"].Value;
                            }
                            break;
                        case "Follow":
                            var followHref = td.SelectSingleNode("a")?.Attributes["href"]?.Value;
                            if (followHref != null)
                            {
                                var match = Regex.Match(followHref, @"/a/subscribe.php?id=(?<id>\d+)");
                                if (match.Success) return match.Groups["id"].Value;
                            }
                            break;
                        case "More":
                            var moreHref = td.SelectSingleNode("a")?.Attributes["href"]?.Value;
                            if (moreHref != null)
                            {
                                var match = Regex.Match(moreHref, @"/mbasic/more/?owner_id=(?<id>\d+)");
                                if (match.Success) return match.Groups["id"].Value;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            // Get current user id
            // About | Friends | Photos | Followers | Activity Log
            var userEdges = htmlDOM.SelectNodes("//div[@id='m-timeline-cover-section']/div/a");
            foreach (var userEdge in userEdges)
            {
                if (userEdge.InnerText == "Activity Log")
                {
                    var href = userEdge?.Attributes["href"]?.Value;
                    if (href != null)
                    {
                        var match = Regex.Match(href, @"/(?<id>\d+)/allactivity");
                        if (match.Success) return match.Groups["id"].Value;
                    }
                }
            }

            // We need another way to detect user
            throw new Exception("Could not find user id by current method, try another way.");
        }

        /// <summary>
        /// Get page id from page alias
        /// </summary>
        /// <param name="pageAlias"></param>
        /// <returns></returns>
        public string GetPageId(string pageAlias)
        {
            var pageId = string.Empty;

            var htmlNode = Http.LoadDom("https://m.facebook.com/" + pageAlias, ref cookies);
            var pagesMbasicContextItemsId = htmlNode.SelectSingleNode("//div[@id='pages_mbasic_context_items_id']");
            if (pagesMbasicContextItemsId != null)
            {
                // This div contain Like, Message, .. action
                var actionDiv = pagesMbasicContextItemsId.PreviousSibling;
                var anchors = actionDiv.SelectNodes("//table/tbody/tr/td[1]/a");
                var compiledRegex = new Regex(@"fan&amp;id=(?<pid>\d+)");

                foreach (var anchor in anchors)
                {
                    var href = anchor.Attributes["href"]?.Value;
                    if (href != null)
                    {
                        var match = compiledRegex.Match(href);
                        if (match.Success)
                        {
                            pageId = match.Groups["pid"].Value;
                            return pageId;
                        }
                    }
                }
            }

            return pageId;
        }
    }
}