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
    public class FacebookClient : IDisposable
    {
        /// <summary>
        /// Cookie container contain cookies for later request
        /// </summary>
        private CookieContainer cookies;
        private ILog log;

        /// <summary>
        /// Facebook email
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Facebook password
        /// </summary>        
        public string Password { get; set; }

        /// <summary>
        /// Create new instance of Facebook client class
        /// </summary>
        /// <param name="username">Facebook email or phone number</param>
        /// <param name="password">Facebook password</param>
        /// <exception cref="ArgumentException">Appear if email or password did not provided</exception>
        /// <exception cref="NodeNotFoundException">Exception raise if Login form DOM not found</exception>
        public FacebookClient(string username, string password, ILog log = null)
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

            if (log == null)
            {
                this.log = new ConsoleLog();
            }
            else
            {
                this.log = log;
            }

            cookies = new CookieContainer();

            if (!LogOn())
            {
                throw new LogOnFailureException("email=" + this.Username + "&password=" + this.Password);
            }
        }

        /// <summary>
        /// Login method to get cookies
        /// </summary>
        /// <returns>Bool value indicate that this user logged-in or not</returns>
        private bool LogOn()
        {
            var documentNode = SynchronousHttp.LoadDom("https://m.facebook.com", ref cookies);

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
            using (var loginResponse = SynchronousHttp.Post(new Uri("https://m.facebook.com/login.php"), content, cookies))
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
            var htmlNode = SynchronousHttp.LoadDom("https://m.facebook.com/groups/" + groupId, ref cookies);
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
                    using (var joinGroupResponse = SynchronousHttp.Post(new Uri(actionUrl), content, cookies))
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
            var htmlNode = SynchronousHttp.LoadDom("https://m.facebook.com/groups/" + groupId, ref cookies);
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

                    using (var joinGroupResponse = SynchronousHttp.Post(new Uri(actionUrl), content, cookies))
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
            var htmlNode = SynchronousHttp.LoadDom("https://m.facebook.com/group/leave/?group_id=" + groupId, ref cookies);

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

                    using (var joinGroupResponse = SynchronousHttp.Post(new Uri(actionUrl), content, cookies))
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
            var htmlNode = SynchronousHttp.LoadDom("https://m.facebook.com/" + pageIdOrAlias, ref cookies);
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
                        using (var likeResponse = SynchronousHttp.Get(new Uri("https://m.facebook.com" + href), cookies))
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
            var htmlNode = SynchronousHttp.LoadDom(loadDOMUrl, ref cookies);

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
            using (var joinGroupResponse = SynchronousHttp.Post(new Uri(actionUrl), content, cookies))
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
        public UserInfo GetUserInfo(string userIdOrAlias, bool includeUserAbout = false, bool includedFriendList = false)
        {
            if (string.IsNullOrWhiteSpace(userIdOrAlias))
            {
                throw new ArgumentException("userIdOrAlias must not null or empty");
            }

            UserInfo userInfo = new UserInfo();

            // TODO : Reduce check
            // The first time we passed userIdOrAlias
            // We don't know it is userIdOrAlias so we need to check
            // The second time when we known passed param is user id or alias
            // But we still need check again. it make perf decrease.

            bool isUserId = !CompiledRegex.MatchNonDigit.Match(userIdOrAlias).Success;
            string userAboutUrl = string.Empty;
            if (isUserId)
            {
                userAboutUrl = "profile.php?v=info&id=" + userIdOrAlias;
                userInfo._id = userIdOrAlias;
            }
            else
            {
                userAboutUrl = userIdOrAlias + "/about";
            }   
                     
            HtmlNode htmlDom = SynchronousHttp.LoadDom("https://m.facebook.com/" + userAboutUrl, ref cookies);
            
            // Get avatar href :
            // avatarAnchorElem contain avatar image source, user display name and maybe contain id.
            // if userIdOrAlias is current user then we pick wrong anchor, pick Another anchor
            HtmlNode avatarA = htmlDom.SelectSingleNode("//div[@id='root']/div/div/div/div/div/a");
            if ((avatarA == null) ||
                (avatarA != null && avatarA.InnerText == GlobalData.EditProfilePicture))
            {
                // If 1st is not a tag what we want, select 2nd a tag
                avatarA = htmlDom.SelectSingleNode("//div[@id='root']/div/div/div/div/div/div/a");

                // if 2nd a tag is null then we does not found any a tag to detect user avatar anchor
                // log it for later fix
                if (avatarA == null)
                {
                    log.WriteL("Could not select avatarA with this link : " + userAboutUrl);
                }
            }

            // get user id
            if (!isUserId)
            {
                Match idMatch = null;

                // trying get id from avatar href
                var avatarHref = avatarA?.Attributes["href"]?.Value;

                // If we found avatar href, we might using it to detect user id
                if (avatarHref != null)
                {
                    // There is two pattern to detect user id
                    // If we get another url format, return this url for detect later.
                    // There is 2 pattern i had found.
                    // /photo.php?fbid=704517456378829&id=100004617430839&...
                    // /profile/picture/view/?profile_id=100003877944061&...
                    // We just need to match it from our pattern. If match success then pick this id
                    if ((idMatch = Regex.Match(avatarHref, @"/photo.php\?fbid=\d+&amp;id=(?<id>\d+)")).Success ||
                        (idMatch = Regex.Match(avatarHref, @"/profile/picture/view/\?profile_id=(?<id>\d+)")).Success)
                    {
                        userInfo._id = idMatch.Groups["id"].Value;
                    }
                    // if it does not correct then we need another pattern
                    // just log it for later fix
                    else
                    {
                        log.WriteL(userAboutUrl + " avatarHref pattern does not recognize exactly. Adding another pattern.");
                    }
                }
                else
                {
                    // If avatarHref is null then we need to detect from another element in DOM
                    // In this step, i trying to detect user id from hyperlink : 
                    // Timeline · Friends · Photos · Likes · Followers · Following · [Activity Log]
                    // NOTE :
                    //      - Activity log only show in current user about page
                    //
                    var hrefNodes = htmlDom.SelectNodes("//div[@id='root']/div/div/a");
                    if (hrefNodes != null)
                    {
                        // Loop through all href we found    
                        foreach (var hrefNode in hrefNodes)
                        {
                            // Get and check href and hrefInnerText
                            // If both of them have value then we can detect it using compiled pattern
                            // NOTE : Check pattern if you think it's incorrect.
                            var href = hrefNode?.Attributes["href"]?.Value;
                            var hrefInnerText = hrefNode?.InnerText;
                            if (href != null &&
                                hrefInnerText != null &&
                                GlobalData.HrefRegexes.ContainsKey(hrefInnerText) &&
                                (idMatch = GlobalData.HrefRegexes[hrefInnerText].Match(href)).Success)
                            {
                                // If id has been detected, break
                                userInfo._id = idMatch.Groups["id"].Value;
                                break;
                            }
                        }
                    }
                    // if hrefNode is null then we need to detect user id by another way
                    else
                    {
                        // Step 3 : 
                        // trying get uid from action button : Add Friend, Message, Follow, More
                        // I only select the 1st xpath,the second xpath will be check in the future.                                               
                        var btnHreftNodes = htmlDom.SelectNodes("//div[@id='root']/div/div/div/table/tr/td/a");
                        // ??htmlDom.SelectNodes("//div[@id='root']/div/div/div");

                        // If we found some button nodes :
                        //      - Add Friend node if we does not add friend with this user
                        //      - Message if this user allow we can send message to him/her
                        //      - Follow if this user allow we can follow him/her and we not follow him/her before
                        //      - More if we can see more about user - i think that, maybe is incorrect.
                        if (btnHreftNodes != null)
                        {
                            foreach (var btnHreftNode in btnHreftNodes)
                            {
                                // if href and innertext not null then we can trying detect user id by compiled regex
                                // NOTE :
                                //      - Check CompiledRegex if you think it not correct anymore
                                //      - Edit Key if you use another Language rather than English to access FB
                                var actionHref = btnHreftNode?.Attributes["href"]?.Value;
                                var actionInnerText = btnHreftNode.InnerText;
                                if (actionHref != null &&
                                    actionInnerText != null &&
                                    GlobalData.BtnHrefRegexes.ContainsKey(actionInnerText) &&
                                    (idMatch = GlobalData.BtnHrefRegexes[actionInnerText].Match(actionHref)).Success)
                                {
                                    userInfo._id = idMatch.Groups["id"].Value;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // If btnHref also null then we just log it for later fix
                            log.WriteL("All method to detect user id is fail. Update your code. Addition info : " + userAboutUrl);
                        }
                    }
                }
            }

            // get user about
            if (includeUserAbout)
            {
                // Ensure avatarA not null and exactly what we need..............
                var img = avatarA?.SelectSingleNode("img");
                if (img != null)
                {
                    // Get name and avatar
                    userInfo.Name = WebUtility.HtmlDecode("" + img.Attributes["alt"]?.Value);
                    userInfo.Avatar = WebUtility.HtmlDecode("" + img.Attributes["src"]?.Value);
                }
                else
                {
                    log.WriteL("Img Tag in Avatar is null - Need to check again. Addition info : " + userAboutUrl);
                }
            }

            // get user friend list if included
            if (includedFriendList && !string.IsNullOrWhiteSpace(userInfo._id))
            {
                var firstFriendPageUrl = "https://m.facebook.com/profile.php?v=friends&startindex=1&id=" + userInfo._id;
                userInfo.Friends = GetFriends(firstFriendPageUrl);
            }

            // return user info
            return userInfo;
        }

        /// <summary>
        /// Get friend helper method. This method support for GetFriend
        /// </summary>
        /// <param name="firstFriendPageUrl">Url</param>
        /// <param name="type">
        /// Type = 1 if you want to get other friends.
        /// Type = 2 if you want to get your friend.
        /// </param>
        /// <returns>List of facebook user id</returns>                  
        private List<string> GetFriends(string firstFriendPageUrl)
        {
            // Declare list string to store user id
            var friends = new List<string>();

            // using queue to remove recursive search over pages
            var friendPages = new Queue<string>();
            friendPages.Enqueue(firstFriendPageUrl);

            // we will loop to the end of friend list
            while (friendPages.Count > 0)
            {
                string currentFriendPageUrl = friendPages.Dequeue();

                HtmlNode htmlNode = SynchronousHttp.LoadDom(currentFriendPageUrl, ref cookies);

                // select root node which is div element with id is root, does not real root node
                var rootNode = htmlNode.SelectSingleNode("//div[@id='root']");

                // SelectNodes or SelectSingleNode method working incorrect.
                // Both method working in entire document node rather than current node when we pass relative xpath expression.
                // In this case, we might get more element than we expected.
                // Trying select table element with role=presentation in rootNode will search in entire document
                // var friendTables1 = rootNode.SelectNodes("//table[@role='presentation']");            

                // If we only want to search in InnerHtml of current node, just load it to another HtmlDocument object
                // Maybe isn't the best way to do our job. But at the moment, i think it still good.
                var hDoc = new HtmlDocument();
                hDoc.LoadHtml(rootNode.InnerHtml);

                // Now search table in new document
                var friendsA = 
                    hDoc.DocumentNode.SelectNodes("//table[@role='presentation']/tr/td[2]/a") ??
                    hDoc.DocumentNode.SelectNodes("//table[@role='presentation']/tbody/tr/td[2]/a");
                if (friendsA == null) return friends;

                // Loop through all node and trying to get user alias or id
                foreach (var friendA in friendsA)
                {
                    var id = string.Empty;
                    var userProfileHref = friendA.Attributes["href"]?.Value;
                    if (userProfileHref != null)
                    {
                        // if href does not contain "profile.php" mean we get user alias
                        if (!userProfileHref.Contains("profile.php"))
                        {
                            // Get userid from user alias
                            // In this case we just need user id so 
                            // i don't pass addition info etc : includeUserAbout and includeFriends
                            // https://m.facebook.com:443//hoanghienson.tung?fref=fr_tab&amp;refid=17/about
                            int questionMarkIndex = userProfileHref.IndexOf("?");
                            if (questionMarkIndex > -1)
                            {
                                userProfileHref = userProfileHref.Substring(1, questionMarkIndex - 1);
                            }
                            else
                            {
                                userProfileHref = userProfileHref.Substring(1);
                            }                            
                            id = GetUserInfo(userProfileHref)._id;
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                friends.Add(id);
                            }
                            else
                            {
                                log.WriteL("Could not find id from this alias : " + userProfileHref);
                            }
                        }
                        else
                        {
                            // Extract user id from href profile.php?id=user_id&fre...
                            // If extract not success then we need to log this error
                            if (CompiledRegex.GetUserIdFromRawProfileUrl(userProfileHref, out id))
                            {
                                friends.Add(id);
                            }
                            else
                            {
                                log.WriteL("Match user id by CompiledRegex.GetUserIdFromRawProfileUrl() fail. Addition info : url is " + firstFriendPageUrl + " and user profile is " + userProfileHref);
                            }
                        }
                    }
                    else
                    {
                        // If we go to this code block, there are some case happen :
                        // - our crawl account has been block by this user
                        // - this is deleted user.
                        // - we need provide more pattern to detect user id

                        // now i will log it for later fix
                        log.WriteL("Could not detect user id. " + userProfileHref);
                    }
                }

                // using queue to remove recursive
                var nextUrl = rootNode.SelectSingleNode("//div[@id='m_more_friends']/a")?.Attributes["href"]?.Value;
                if (nextUrl != null && nextUrl.Length != 0)
                {
                    friendPages.Enqueue("https://m.facebook.com" + nextUrl);
                }
                else
                {
                    log.WriteL("Maybe we reach to the last page or we need to review xpath again :S");
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
            var documentNode = SynchronousHttp.LoadDom("https://m.facebook.com/groups/" + groupId + "?view=info", ref cookies);
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
            var memberNodes = SynchronousHttp.LoadDom(groupMemberUrl, ref cookies).SelectNodes("/html/body/div/div/div[2]/div/div[1]/div/table");
            if (memberNodes.Count == 0) return groupMembers;

            foreach (var memberNode in memberNodes)
            {
                var userId = Regex.Match(memberNode.Attributes["id"].Value, @"\d+").Value;
                var isAdminNodeHtml = memberNode.SelectSingleNode("tr/td[2]/div/h3[2]").InnerHtml;
                var isAdmin = GlobalData.IsGroupAdministrator.Any(adminText => isAdminNodeHtml.Contains(adminText));
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

            var htmlNode = SynchronousHttp.LoadDom("https://m.facebook.com/page/reviews.php?id=" + pageIdOrAlias, ref cookies);

            if (GlobalData.PageNotFound.Any(text => htmlNode.InnerHtml.Contains(text)))
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
                var htmlRateContentNode = SynchronousHttp.LoadDom("https://m.facebook.com" + rateContentAnchorLink, ref cookies);

                reviewInfo.Content = htmlRateContentNode.SelectSingleNode("html/body/div/div/div[2]/div/div[1]/div/div[1]/div/div[1]/div[2]/p")?.InnerText;

                pageReview.Reviews.Add(reviewInfo);
            }

            return pageReview;
        }

        /// <summary>
        /// Get page id from page alias
        /// </summary>
        /// <param name="pageAlias"></param>
        /// <returns></returns>
        public string GetPageId(string pageAlias)
        {
            var pageId = string.Empty;

            var htmlNode = SynchronousHttp.LoadDom("https://m.facebook.com/" + pageAlias, ref cookies);
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

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (log != null)
            {
                log.Dispose();
            }                
        }
    }
}