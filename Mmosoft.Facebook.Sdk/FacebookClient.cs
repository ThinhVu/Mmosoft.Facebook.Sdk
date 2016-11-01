using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

namespace Mmosoft.Facebook.Sdk
{
    public class FacebookClient : IFacebookClient
    {
        /// <summary>
        /// Facebook email
        /// </summary>
        private string email;
        /// <summary>
        /// Facebook password
        /// </summary>
        private string password;
        /// <summary>
        /// Cookie container contain cookies for later request
        /// </summary>
        private CookieContainer cookies;

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

            this.email = email;
            this.password = password;
            this.cookies = new CookieContainer();

            if (!LogOn())
                throw new LogOnFailureException("email=" + this.email + "&password=" + this.password);
        }

        /// <summary>
        /// Login method to get cookies
        /// </summary>
        /// <returns>Bool value indicate that this user logged-in or not</returns>
        private bool LogOn()
        {
            var documentNode = Http.LoadDOM("https://m.facebook.com", ref cookies);

            // Get login form Dom object
            var logInFormNode = documentNode.SelectSingleNode("/html/body/div/div/div[2]/div/table/tbody/tr/td/div[2]/div/form");
            // get input collection
            var inputCollection = new List<string> { "email=" + email + "&pass=" + password };
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
                if (loginResponse.Cookies["c_user"] == null) return false;
                cookies.Add(loginResponse.Cookies);
                return true;
            }
        }

        /// <summary>
        /// Send request to join or cancel group
        /// </summary>
        /// <param name="groupId">Id's target group</param>
        public void JoinGroup(string groupId)
        {
            var htmlNode = Http.LoadDOM("https://m.facebook.com/groups/" + groupId, ref cookies);
            var formNode = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/form");

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
            }
        }

        /// <summary>
        /// Send request to like or dislike target page
        /// </summary>
        /// <param name="pageId">Id's target page</param>
        public void LikePage(string pageId)
        {
            var htmlNode = Http.LoadDOM("https://m.facebook.com/" + pageId, ref cookies);
            // get like link            
            var likeAnchor = htmlNode.SelectSingleNode("/html/body/div/div/div[2]/div/div/div[1]/div[2]/div/div[2]/table/tbody/tr/td[1]/a");                        
            // Decode url
            var href = "https://m.facebook.com" + WebUtility.HtmlDecode(likeAnchor.Attributes["href"]?.Value);
            // Post like page request
            using (var likeResponse = Http.Get(new Uri(href), cookies))
            {
                cookies.Add(likeResponse.Cookies);
            }
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
            var htmlNode = Http.LoadDOM(loadDOMUrl, ref cookies);

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
            using (var joinGroupResponse = Http.Post(new Uri( actionUrl), content, cookies))
            {
                cookies.Add(joinGroupResponse.Cookies);
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
        public FriendInfo GetFriendInfo([Optional] string userIdAlias)
        {
            // See XPath for clear explanation
            // XPath for you
            // 1. Current page friends :    /html/body/div/div/div[2]/div/div[1]/div[2]
            // 2. Next page url :           /html/body/div/div/div[2]/div/div[1]/div[3]

            // XPath for other people
            // 1. Current page friends :    /html/body/div/div/div[2]/div/div[1]/div[1]
            // 2. Next page url :           /html/body/div/div/div[2]/div/div[1]/div[2]

            var friendInfo = new Models.FriendInfo();
            if (userIdAlias == null || userIdAlias.Length == 0 || userIdAlias == email)
            {                
                friendInfo.UserId = email;
                friendInfo.Friends = GetFriends("https://m.facebook.com/" + email + "/friends?startindex=1", 2);
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
        private List<UserInfo> GetFriends(string url, int type)
        {
            var friends = new List<UserInfo>();

            var htmlNode = Http.LoadDOM(url, ref cookies);

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
                        id = CompiledRegex.GetUserId2(href);
                    }
                }
                else
                {
                    // if id href not null then get from this
                    id = CompiledRegex.GetUserId1(idHref);
                }

                friends.Add(new UserInfo
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

        /// <summary>
        /// Get group info
        /// </summary>
        /// <param name="groupId">Group id you want to get info</param>
        /// <exception cref="NodeNotFoundException">Exception when select DOM query fail</exception>
        /// <returns>Group Info object</returns>
        public GroupInfo GetGroupInfo(string groupId)
        {            
            var documentNode = Http.LoadDOM("https://m.facebook.com/groups/" + groupId + "?view=info", ref cookies);
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
            var memberNodes = Http.LoadDOM(groupMemberUrl, ref cookies).SelectNodes("/html/body/div/div/div[2]/div/div[1]/div/table");                      
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
        /// <param name="pageId"></param>
        /// <returns></returns>
        public ReviewInfo GetReviewInfo(string pageId)
        {
            var htmlNode = Http.LoadDOM("https://m.facebook.com/page/reviews.php?id=" + pageId, ref cookies);
         
            if (LocalizationData.PageNotFound.Any(text => htmlNode.InnerHtml.Contains(text)))
                throw new MissingReviewPageException(pageId);

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
                var htmlRateContentNode = Http.LoadDOM("https://m.facebook.com" + rateContentAnchorLink, ref cookies);

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
            // load profile page of this user with alias string
            var htmlDOM = Http.LoadDOM("https://m.facebook.com/" + userAlias, ref cookies);

            // get More button href
            // if td[3] does not exists then choose td[2]
            var href = htmlDOM.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[1]/div[3]/table/tr/td[3]/a")?.Attributes["href"]?.Value ??
                    htmlDOM.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[1]/div[3]/table/tr/td[2]/a")?.Attributes["href"]?.Value;
            // If MORE button exists then get id from it
            if (href != null)
            {
                // Parse More button to get user id
                return CompiledRegex.GetUserId2(href);
            }
            else
            {
                // Select another anchor
                var r = htmlDOM.SelectSingleNode("/html/body/div/div/div[2]/div/div[1]/div[1]/div[2]/div[1]/div[1]/a")?.Attributes["href"]?.Value;

                if (r != null)
                    return Regex.Match(r, @"\D+[^(fb)]id=(?<id>\d+)").Groups["id"].Value;
                // if another anchor does not exist then choose another anchor :v
                else
                    throw new Exception("Not found anchor to detect user id -- coming up");
            }
        }       
    }
}