using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml;
using System.Text;
using System.Diagnostics;
// ----------------------------------
using HtmlAgilityPack;
using Mmosoft.Facebook.Sdk.Exceptions;
using Mmosoft.Facebook.Sdk.Models;
using Mmosoft.Facebook.Sdk.Utilities;

namespace Mmosoft.Facebook.Sdk
{
    public class FacebookClient : IDisposable
    {
        // For logging
        ILogger _logger;
        // for create http request
        HttpHandler _http;

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
        /// <exception cref="ArgumentException">Occur when email or password did not provided</exception>
        /// <exception cref="NodeNotFoundException">Login form DOM not found.</exception>
        public FacebookClient(string user, string password, ILogger log = null)
        {
            _http = new HttpHandler();

            if (log == null)
                _logger = new SimpleConsoleLogger();
            else
                _logger = log;

            Username = user;
            Password = password;

            Authorize();
        }

        /// <summary>
        /// Login with credentials and store cookie for later using
        /// </summary>
        /// <returns>Bool value indicate that this user logged-in or not</returns>
        void Authorize()
        {
            // load html content to document node
            HtmlNode document = this.BuildDom("https://m.facebook.com");

            // Get login form Dom object
            HtmlNode loginForm = document.SelectSingleNode("//form[@id='login_form']");
            IEnumerable<HtmlNode> inputs = loginForm.ParentNode.Elements("input");

            // create content payload
            string credential = string.Format("email={0}&pass={1}", Username, Password);
            string payload = HtmlHelper.BuildPayload(inputs, additionKeyValuePair: credential);

            using (HttpWebResponse response = _http.SendPostRequest("https://m.facebook.com/login.php", payload))
            {
                if (response.Cookies["c_user"] == null)
                    throw new UnAuthorizedException(FacebookClientErrors.CookieKeyCUserNotFound);
            }
        }

        // ----------- Group ---------- //
        /// <summary>
        /// Send request to join or cancel group
        /// </summary>
        /// <param name="groupId">Id's target group</param>
        public bool JoinGroup(string groupId)
        {
            return JoinCancelJoinGroup(groupId, "join");
        }
        /// <summary>
        /// Cancel join group
        /// </summary>
        /// <param name="groupId">facebook group id</param>
        /// <returns>true if join success, otherwhile false</returns>
        public bool CancelJoinGroup(string groupId)
        {
            return JoinCancelJoinGroup(groupId, "canceljoin");
        }
        /// <summary>
        /// Do join, cancel join action
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        bool JoinCancelJoinGroup(string groupId, string action)
        {
            HtmlNode form = this.BuildDom("https://m.facebook.com/groups/" + groupId)
                .SelectSingleNode("//form[contains(@action, '/a/group/')]");
            if (form == null) return false;
            string actionUrlPath = form.GetAttributeValue("action", null);
            if (actionUrlPath == null) return false;
            string payload = HtmlHelper.BuildPayload(form.ParentNode.Elements("input"), null);
            using (var response = _http.SendPostRequest("https://m.facebook.com" + actionUrlPath, payload))
            {                
                return true;
            }
        }
        /// <summary>
        /// Leave group
        /// </summary>
        /// <param name="groupId">facebook group id</param>
        /// <param name="preventReAdd">prevent group member re-add you again</param>
        /// <returns>bool value if leave group request send success</returns>
        public bool LeaveGroup(string groupId, bool preventReAdd)
        {
            HtmlNodeCollection formNodes = this.BuildDom("https://m.facebook.com/group/leave/?group_id=" + groupId)
                .SelectNodes("//form");

            foreach (var formNode in formNodes)
            {
                if (formNode.GetAttributeValue("action", string.Empty).StartsWith("/a/group/leave/"))
                {
                    // because payload depend on some specified key so we can not using BuildPayload method.
                    // Code below is implementtation of BuildPayLoad method

                    // name = fb_dtsg , type = hidden
                    // name = charset_test, type = hidden
                    // name = prevent_readd, type = checkbox
                    // name = group_id, type = hidden
                    // name = confirm, type = submit
                    string[] requiredInputNames = new string[] { "fb_dtsg", "charset_test", "group_id", "confirm" };
                    string preventReAddKeyValue = "prevent_readd=" + (preventReAdd ? "on" : "off");
                    List<string> inputs = new List<string> { preventReAddKeyValue };

                    foreach (HtmlNode input in formNode.SelectNodes("//input"))
                    {
                        string inputName = null;
                        if ((input == null) ||
                            (inputName = input.GetAttributeValue("name", null)) == null ||
                            (!requiredInputNames.Contains(inputName)))
                            continue;

                        string name = input.GetAttributeValue("name", null);
                        string value = input.GetAttributeValue("value", null);

                        inputs.Add(name + "=" + value);
                    }

                    string payload = string.Join("&", inputs);
                    string actionUrl = "https://m.facebook.com" + formNode.GetAttributeValue("action", string.Empty);

                    using (HttpWebResponse joinGroupResponse = _http.SendPostRequest(actionUrl, payload))
                    {
                        return true;
                    }
                }
            }

            return false;
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
        /// Get group info
        /// </summary>
        /// <param name="groupId">Group id you want to get info</param>
        /// <exception cref="NodeNotFoundException">Exception when select DOM query fail</exception>
        /// <returns>Group Info object</returns>
        public GroupInfo GetGroupInfo(string groupId)
        {            
            HtmlNode docNode = this.BuildDom("https://m.facebook.com/groups/" + groupId + "?view=info");
            HtmlNode groupNameNode = docNode.SelectSingleNode("//a[@href='#groupMenuBottom']").SelectSingleNode("//h3");
            if (groupNameNode == null) return null;
            var groupInfo = new GroupInfo
            {
                Id = groupId,
                Name = WebUtility.HtmlDecode(groupNameNode.InnerText)
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

            string groupMemberUrl = "https://m.facebook.com/browse/group/members/?id=" + groupId + "&start=" + page + "&listType=list_nonfriend";

            HtmlNode document = this.BuildDom(groupMemberUrl);
            HtmlNodeCollection members = document.SelectNodes("/html/body/div/div/div[2]/div/div[1]/div/table");

            if (members == null || members.Count == 0)
                return groupMembers;

            foreach (HtmlNode member in members)
            {
                var groupMember = new GroupMember();

                // member id or alias
                groupMember.UserId = CompiledRegex.Match("Digit", member.GetAttributeValue("id", string.Empty)).Value;

                // is admin
                HtmlNode isAdminNode = member.SelectSingleNode("tr/td[2]/div/h3[2]");
                if (isAdminNode != null)
                    groupMember.IsAdmin = isAdminNode.InnerText.Contains(ConstString.IsGroupAdmin);

                // display name
                HtmlNode nameNode = member.SelectSingleNode("tr/td[2]/div/h3[1]/a") ?? member.SelectSingleNode("tr/td[2]/div/h3[1]");
                groupMember.DisplayName = (nameNode == null) ? string.Empty : nameNode.InnerText;

                groupMembers.Add(groupMember);
            }

            // TODO (ThinhVu) : Reduce recursion
            List<GroupMember> nextPageMembers = GetGroupMembers(groupId, page + 30);
            groupMembers.AddRange(nextPageMembers);

            return groupMembers;
        }

        /// <summary>
        /// Get groups which user has joined in
        /// At the moment, this method just get first page of all page (if user join so much group)
        /// Base on my code, you can fix it for your use. Sorry for that inconvenience
        /// </summary>
        /// <returns></returns>
        public List<UserGroup> GetUserGroups()
        {
            var xpath = string.Format("//div/h3[.='{0}']", Utilities.ConstString.GroupYouAreIn);
            var userGroups = new List<UserGroup>();
            var header = BuildDom("https://m.facebook.com/groups/?seemore").SelectSingleNode(xpath);
            var groupUl = header.NextSibling;
            foreach (var li in groupUl.SelectNodes("li"))
            {
                var userGroup = GetUserGroupFrom(li);
                if (userGroup != null)
                    userGroups.Add(userGroup);
            }
            return userGroups;
        }
        private UserGroup GetUserGroupFrom(HtmlNode liNode)
        {
            UserGroup userGroup = null;
            var aTag = liNode.SelectSingleNode("./table/tbody/tr/td/a");            
            if (aTag != null)
            {
                userGroup = new UserGroup();
                userGroup.Name = WebUtility.HtmlDecode(aTag.InnerText);
                userGroup.Link = RemoveQueryString(aTag.GetAttributeValue("href", ""));
            }
            return userGroup;
        }
        private string RemoveQueryString(string link)
        {
            return link.Substring(0, link.IndexOf('?'));
        }

        // ----------- Page ----------- //
        /// <summary>
        /// Send request to like or dislike target page
        /// </summary>
        /// <param name="pageIdOrAlias">Id's target page</param>
        public bool LikePage(string pageIdOrAlias)
        {
            // TODO (ThinhVu): Replace bool with meaningful response data.
            HtmlNode likeAnchor = this.GetLikePageAnchor(pageIdOrAlias);
            if (likeAnchor == null) return false;
            var href = likeAnchor.GetAttributeValue("href", string.Empty);
            using (HttpWebResponse response = _http.SendGetRequest("http://m.facebook.com" + href))
            {
                return true;
            }
        }
        /// <summary>
        /// Get page id from page alias
        /// </summary>
        /// <param name="pageAlias"></param>
        /// <returns></returns>
        public string GetPageId(string pageAlias)
        {
            // When we get likeAnchor HtmlNode, 2 case happened
            // 1. likeAnchor is null
            // 2. likeAnchor not null => we can get page Id from like anchor href.
            HtmlNode likeAnchor = this.GetLikePageAnchor(pageAlias);
            if (likeAnchor == null) return "-1";
            var compiledRegex = new Regex(@"fan&amp;id=(?<pid>\d+)");
            Match match = compiledRegex.Match(likeAnchor.GetAttributeValue("href", string.Empty));
            return match.Groups["pid"].Value;
        }
        /// <summary>
        /// Get page reviews
        /// </summary>
        /// <param name="pageIdOrAlias"></param>
        /// <returns></returns>
        public PageReviewInfo GetPageReviewInfo(string pageIdOrAlias)
        {
            var pageId = string.Empty;

            // Checking if pageIdOrAlias is pageAlias or PageId            
            MatchCollection matches = Regex.Matches(pageIdOrAlias, "\\d+");

            // param passed is page alias, we need to get page id from it.
            if (matches.Count != 1)
                pageId = GetPageId(pageIdOrAlias);

            if (pageId.Length == 0)
                throw new ArgumentException("Can not detect page id of " + pageIdOrAlias);

            HtmlNode htmlNode = this.BuildDom("https://m.facebook.com/page/reviews.php?id=" + pageIdOrAlias);

            if (ConstString.PageNotFound.Any(text => htmlNode.InnerHtml.Contains(text)))
                throw new ReviewPageNotFoundException(pageIdOrAlias + " not contains review page");

            // get review nodes -- review node contain user's review
            // TODO : Replace with more safely xpath
            HtmlNodeCollection reviewNodes = htmlNode.SelectNodes("/html/body/div/div/div[2]/div[2]/div[1]/div/div[3]/div/div/div/div");

            // Create page review
            var pageReview = new PageReviewInfo();

            // loop through DOM reviewNodes
            foreach (var reviewNode in reviewNodes)
            {
                // create new instance of review info
                var reviewInfo = new PageReview();

                // Get avatar
                HtmlNode imgAvatarNode = reviewNode.SelectSingleNode("div/div/div[1]/a/div/img");
                if (imgAvatarNode != null)
                    reviewInfo.UserAvatarUrl = WebUtility.HtmlDecode(imgAvatarNode.GetAttributeValue("src", string.Empty));

                // User name and id                
                HtmlNode userNameIdNode = reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[1]");
                if (userNameIdNode != null)
                {
                    // Get urlink and parse
                    string urlLink = userNameIdNode.GetAttributeValue("href", null);
                    if (urlLink != null)
                    {
                        if (urlLink.Contains("/profile.php?id="))
                            reviewInfo.UserId = urlLink.Substring(16); // /profile.php?id=100012141183155
                        else
                            reviewInfo.UserId = urlLink.Substring(1); // /kakarotto.pham.9
                    }

                    HtmlNode nameNode = userNameIdNode.SelectSingleNode("span");
                    if (nameNode != null)
                        reviewInfo.UserDisplayName = WebUtility.HtmlDecode(nameNode.InnerText + string.Empty);
                }

                // Get rate score
                HtmlNode rateScoreNode = reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[2]/img");
                // return -1 indicate that can not recognize value
                if (rateScoreNode != null)
                    reviewInfo.RateScore = int.Parse(rateScoreNode.GetAttributeValue("alt", "-1"), CultureInfo.CurrentCulture);

                // Get fully rate content page
                HtmlNode rateContentNode = reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[2]");
                if (rateContentNode != null)
                {
                    string rateContentAnchorLink = rateContentNode.GetAttributeValue("href", null);
                    if (rateContentAnchorLink != null)
                    {
                        HtmlNode htmlRateContentNode = this.BuildDom("https://m.facebook.com" + rateContentAnchorLink);
                        // TODO : Replace with more safely
                        HtmlNode contentNode = htmlRateContentNode.SelectSingleNode("html/body/div/div/div[2]/div/div[1]/div/div[1]/div/div[1]/div[2]/p");

                        if (contentNode != null)
                            reviewInfo.Content = contentNode.InnerText;
                    }
                }

                pageReview.Reviews.Add(reviewInfo);
            }

            return pageReview;
        }
        /// <summary>
        /// Get likepage anchor from specified page.
        /// </summary>
        /// <param name="pageAlias">Page alias name</param>
        /// <returns>Anchor element if exist or null if not</returns>
        public HtmlNode GetLikePageAnchor(string pageAlias)
        {
            var pageId = string.Empty;
            HtmlNode htmlNode = this.BuildDom("https://m.facebook.com/" + pageAlias);
            HtmlNode pagesMbasicContextItemsId = htmlNode.SelectSingleNode("//div[@id='pages_mbasic_context_items_id']");
            if (pagesMbasicContextItemsId != null)
            {
                // This div contain Like, Message, .. action
                HtmlNode actionDiv = pagesMbasicContextItemsId.PreviousSibling;
                if (actionDiv == null) return null;

                HtmlNodeCollection anchors = actionDiv.SelectNodes("//table/tbody/tr/td[1]/a");
                var compiledRegex = new Regex(@"fan&amp;id=(?<pid>\d+)");

                foreach (HtmlNode anchor in anchors)
                {
                    if (anchor == null) continue;

                    if (anchor.InnerText == ConstString.Like)
                        return anchor;
                }
            }

            return null;
        }

        // ----------- User ----------- //
        /// <summary>
        /// Post to wall
        /// </summary>
        /// <param name="message">Content you want to post</param>                  
        public void PostToWall(string message)
        {
            Post(message, string.Empty);
        }
        /// <summary>
        /// Get friend's id of someone -- heavy method -- need refactor
        /// </summary>
        /// <param name="userIdOrAlias">
        /// If userId passed is blank then you will get your friend list.
        /// Else you will get friend list of this id.
        /// </param>
        /// <returns>List id of friends</returns>
        public UserInfo GetUserInfo(string userIdOrAlias, bool includeUserAbout = false, bool includedFriendList = false)
        {
            // TODO : messy method - we need refactor it.

            if (string.IsNullOrWhiteSpace(userIdOrAlias))
                throw new ArgumentException("userIdOrAlias must not null or empty.");

            UserInfo userInfo = new UserInfo();

            // TODO : Reduce check
            // The first time we passed userIdOrAlias
            // We don't know it is userIdOrAlias so we need to check
            // The second time when we known passed param is user id or alias
            // But we still need check again. it make perf decrease.

            string userAboutUrl = string.Empty;
            bool isUserId = !CompiledRegex.Match("NonDigit", userIdOrAlias).Success;
            if (isUserId)
            {
                userAboutUrl = "https://m.facebook.com/profile.php?v=info&id=" + userIdOrAlias;
                userInfo.Id = userIdOrAlias;
                userInfo.Alias = string.Empty;
            }
            else
            {
                userAboutUrl = "https://m.facebook.com/" + userIdOrAlias + "/about";
                userInfo.Id = string.Empty;
                userInfo.Alias = userIdOrAlias;
            }

            HtmlNode htmlDom = this.BuildDom(userAboutUrl);

            // Get avatar anchor tag :

            // avatarAnchorElem contain avatar image source, user display name and maybe contain id.
            // if userIdOrAlias is current user or other user with animated avatar then 1st xpath is wrong
            // pick another anchor
            HtmlNode avatarAnchor = htmlDom.SelectSingleNode("//div[@id='root']/div/div/div/div/div/a");
            if (avatarAnchor == null
                || avatarAnchor.InnerText == ConstString.EditProfilePicture // for current user
                || avatarAnchor.InnerText == ConstString.AddProfilePicture) // for animate avatar
            {
                avatarAnchor = htmlDom.SelectSingleNode("//div[@id='root']/div/div/div/div/div/div/a");
            }
            else if (avatarAnchor.SelectSingleNode("div/a/img") != null
                || (avatarAnchor.PreviousSibling != null && avatarAnchor.PreviousSibling.SelectSingleNode("/a/img") != null))
            {
                HtmlNodeCollection anchors = avatarAnchor.SelectNodes("div/a");
                if (anchors != null)
                {
                    foreach (HtmlNode anchor in anchors)
                    {
                        if (anchor.SelectSingleNode("img") != null)
                        {
                            avatarAnchor = anchor;
                            break;
                        }
                    }
                }
            }
            else
            {
                // Support another xpath to get user id
            }

            // get user id
            if (!isUserId && avatarAnchor != null)
            {
                Match idMatch = Match.Empty;

                // trying get id from avatar href
                var avatarHref = avatarAnchor.GetAttributeValue("href", null);

                // If we found avatar href, we might using it to detect user id
                if (avatarHref != null)
                {
                    // There is 3 pattern to detect user id
                    // If we get another url format, return this url for detect later.                    
                    // /photo.php?fbid=704517456378829&id=100004617430839&...
                    // /profile/picture/view/?profile_id=100003877944061&...
                    // /story.php\?story_fbid=\d+&amp;id=(?<id>\d+) for animate avatar                    
                    if ((idMatch = CompiledRegex.Match("UserIdFromAvatar1", avatarHref)).Success
                        || (idMatch = CompiledRegex.Match("UserIdFromAvatar2", avatarHref)).Success
                        || (idMatch = CompiledRegex.Match("UserIdFromAvatar3", avatarHref)).Success)
                    {
                        userInfo.Id = idMatch.Groups["id"].Value;
                    }
                }

                // try another way
                if (string.IsNullOrEmpty(userInfo.Id))
                {                    
                    // Trying to detect user id from hyperlink : 
                    // Timeline · Friends · Photos · Likes · Followers · Following · [Activity Log]
                    // NOTE :
                    //      - Activity log only show in current user about page
                    //
                    // Important : /div/div/div/a must select before /div/div/a. Do not swap SelectNodes order.
                    HtmlNodeCollection anchors = htmlDom.SelectNodes("//div[@id='root']/div/div/div/a")
                        ?? htmlDom.SelectNodes("//div[@id='root']/div/div/a");

                    if (anchors != null && anchors.Count > 0)
                    {                        
                        foreach (HtmlNode anchor in anchors)
                        {
                            // Get and check hrefAttr and innerText
                            // If both of them have value then we can detect it using compiled pattern
                            // NOTE : Check pattern if you think it's incorrect.
                            string hrefAttr = anchor.GetAttributeValue("href", string.Empty);
                            string innerText = anchor.InnerText;

                            if (!string.IsNullOrWhiteSpace(innerText)
                                && (idMatch = CompiledRegex.Match(innerText, hrefAttr)).Success)
                            {
                                userInfo.Id = idMatch.Groups["id"].Value;
                                break;
                            }
                        }
                    }
                }

                // Try another way if id still empty
                if (string.IsNullOrEmpty(userInfo.Id))
                {
                    // Step 3 : 
                    // trying get uid from action button : Add Friend, Message, Follow, More
                    // I only select the 1st xpath,the second xpath will be check in the future.                                               
                    HtmlNodeCollection btnHrefts = htmlDom.SelectNodes("//div[@id='root']/div/div/div/table/tr/td/a");
                    // ??htmlDom.SelectNodes("//div[@id='root']/div/div/div");

                    // If we found some button nodes :
                    //      - Add Friend node if we does not add friend with this user
                    //      - Message if this user allow we can send message to him/her
                    //      - Follow if this user allow we can follow him/her and we not follow him/her before
                    //      - More if we can see more about user - i think that, maybe is incorrect.
                    if (btnHrefts != null && btnHrefts.Count > 0)
                    {
                        foreach (var btnHreft in btnHrefts)
                        {                            
                            // if href and innertext not null then we can trying detect user id by compiled regex
                            // NOTE :
                            //      - Check CompiledRegex if you think it not correct anymore
                            //      - Edit Key if you use another Language rather than English to access FB
                            string hrefAttr = btnHreft.GetAttributeValue("href", string.Empty);
                            string innerText = btnHreft.InnerText;

                            if (!string.IsNullOrWhiteSpace(innerText)
                                && (idMatch = CompiledRegex.Match(innerText, hrefAttr)).Success)
                            {
                                userInfo.Id = idMatch.Groups["id"].Value;
                                break;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(userInfo.Id))
            {
                _logger.WriteLine("Could not detect id from " + userAboutUrl);
                return null;
            }

            // if user id has been detected, check if we want to get user about too
            if (includeUserAbout && avatarAnchor != null)
            {
                HtmlNode avatar = avatarAnchor.SelectSingleNode("img");
                if (avatar != null)
                {
                    // Get name and avatar
                    userInfo.DispayName = WebUtility.HtmlDecode(avatar.GetAttributeValue("alt", string.Empty));
                    userInfo.AvatarUrl = WebUtility.HtmlDecode(avatar.GetAttributeValue("src", string.Empty));
                }
                else
                {
                    _logger.WriteLine("Img tag in avatar is null. Addition info : " + userAboutUrl);
                }
            }

            // get user friend list if included
            // at this step we do not need to check user id anymore
            // if user id is null then we had return before.
            if (includedFriendList)
            {
                var firstFriendPageUrl = "https://m.facebook.com/profile.php?v=friends&startindex=0&id=" + userInfo.Id;
                userInfo.Friends = GetFriends(firstFriendPageUrl);
            }

            // all step have done
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
        List<string> GetFriends(string firstFriendPageUrl)
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
                string currentHtmlContent = _http.DownloadContent(currentFriendPageUrl);
                HtmlNode docNode = HtmlHelper.BuildDom(currentHtmlContent);

                // select root node which is div element with id is root, does not real root node
                HtmlNode rootNode = docNode.SelectSingleNode("//div[@id='root']");

                // Because selectNodes and SelectSingleNode method working incorrect.
                // Both method working in entire document node rather than current node when we pass relative xpath expression.
                // So, we maybe get more element than expect.

                // E.g : Trying select table element with role=presentation in rootNode will search in entire document
                // var friendTables1 = rootNode.SelectNodes("//table[@role='presentation']");            

                // If we only want to search in InnerHtml of current node, just load it to another HtmlDocument object
                // Maybe isn't the best way to do our job. But at the moment, i think it still good.                
                docNode = HtmlHelper.BuildDom(rootNode.InnerHtml);

                // Now search table in new document
                HtmlNodeCollection friendAnchors =
                    docNode.SelectNodes("//table[@role='presentation']/tr/td[2]/a") ??
                    docNode.SelectNodes("//table[@role='presentation']/tbody/tr/td[2]/a");
                if (friendAnchors == null) return friends;

                // Loop through all node and trying to get user alias or id
                foreach (HtmlNode friendAnchor in friendAnchors)
                {
                    string id = string.Empty;
                    string userProfileHref = friendAnchor.GetAttributeValue("href", null);

                    if (userProfileHref != null)
                    {
                        if (!userProfileHref.Contains("profile.php"))
                        {
                            // if userProfileHref does't contain "profile.php", userProfileHref contain user alias.
                            // E.g : https://m.facebook.com:443/user.alias.here?fref=fr_tab&amp;refid=17/about
                            int questionMarkIndex = userProfileHref.IndexOf("?");

                            if (questionMarkIndex > -1)
                                userProfileHref = userProfileHref.Substring(1, questionMarkIndex - 1);
                            else
                                userProfileHref = userProfileHref.Substring(1);

                            friends.Add(userProfileHref);
                        }
                        else
                        {
                            // Extract user id from href profile.php?id=user_id&fre...
                            // If extract not success then we need to log this error
                            Match match = CompiledRegex.Match("UserId", userProfileHref);
                            if (match.Success)
                                friends.Add(match.Groups["id"].Value);
                            else
                                _logger.WriteLine("Match user id by CompiledRege.Match(UserId) is fail. Addition info : url=" + firstFriendPageUrl + " and user profile is " + userProfileHref);
                        }
                    }
                    else
                    {
                        // If we go to this code block, there are some case happend :
                        // - Our bot has been block by this user or facebook.
                        // - This is deleted user.
                        // - We need provide more pattern to detect user id

                        // now i will log it for later fix
                        _logger.WriteLine("Maybe " + friendAnchor.InnerText + " has been banned. Access this link from browser to check again.");
                    }
                }

                HtmlNode moreFriend = rootNode.SelectSingleNode("//div[@id='m_more_friends']/a");
                if (moreFriend == null) continue;

                var nextUrl = WebUtility.HtmlDecode(moreFriend.GetAttributeValue("href", null));

                if (nextUrl != null)
                    friendPages.Enqueue("https://m.facebook.com" + nextUrl);
                else
                    _logger.WriteLine("This is last page.");
            }

            return friends;
        }
        /// <summary>
        /// Post message to targetId
        /// </summary>
        /// <param name="message">Content you want to post</param>
        /// <param name="targetId">Target id is group or wall</param>                      
        bool Post(string message, string targetId)
        {
            var loadDomUrl = string.Empty;
            switch (targetId)
            {
                case "":
                    // Post wall
                    loadDomUrl = "https://m.facebook.com/";
                    break;
                default:
                    // post to group
                    loadDomUrl = "https://m.facebook.com/groups/" + targetId;
                    break;
            }
            HtmlNode document = this.BuildDom(loadDomUrl);
            HtmlNode postForm = document.SelectSingleNode("//form[contains(@action, '/composer')]");
            if (postForm != null)
            {
                IEnumerable<HtmlNode> inputs = postForm.ParentNode.Elements("input");
                string payload = HtmlHelper.BuildPayload(inputs, "view_post=Post&xc_message=" + Uri.EscapeDataString(message));
                string actionUrl = postForm.GetAttributeValue("action", null);
                if (actionUrl == null) return false;
                using (var joinGroup = _http.SendPostRequest("https://m.facebook.com" + actionUrl, payload))
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        // ---------- Dispose -------- //
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (_logger != null)
            {
                _logger.Dispose();
            }
        }        
        /// <summary>
        /// Download content from url and build DOM from it.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        HtmlNode BuildDom(string url)
        {
            string htmlContent = _http.DownloadContent(url);
            return HtmlHelper.BuildDom(htmlContent);
        }
    }
}
