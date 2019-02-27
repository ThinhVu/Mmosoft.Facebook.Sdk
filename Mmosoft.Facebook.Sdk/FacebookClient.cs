using HtmlAgilityPack;
using Mmosoft.Facebook.Sdk.Exceptions;
using Mmosoft.Facebook.Sdk.Models.Group;
using Mmosoft.Facebook.Sdk.Models.Page;
using Mmosoft.Facebook.Sdk.Models.User;
using Mmosoft.Facebook.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Mmosoft.Facebook.Sdk
{
    public class FacebookClient : IDisposable
    {
        // Logger support
        private static int mInsCreated;         // support facebook client instance manage
        private static string mSession;         // each time host program run FBClient, one session will be create
        private int mFbClientId;                // support for log work and error for multiple fbc run at the same time
        private ILogger mLogger;                // Log process of FBC
        private XpathErrorLog mExceptionLogger; // Log Xpath for later investigate

        // Transfer data
        private HttpHandler mHttp;

        // credential
        public string Username { get; set; }
        public string Password { get; set; }

        public FacebookClient(string user, string password)
        {
            mInsCreated++;
            mFbClientId = mInsCreated;

            // each time we start host program and create multiple FBClient object
            // we need to supplied its folder for store error and log
            // this folder should not exist, otherwise, old error and log info will be lost because of overwrite.
            // create session folder if not exist
            if (!Directory.Exists("Session"))
                Directory.CreateDirectory("Session");
            
            if (mSession == null)
            {
                var now = DateTime.Now;
                var today = now.Year + "_" + now.Month + "_" + now.Day;
                var dirCount = Directory.GetDirectories("Session", today, SearchOption.TopDirectoryOnly);
                mSession = "Session\\" + today + "_" + dirCount.Length;                
                Directory.CreateDirectory(mSession);            
            }

            // Folder structure
            // - Session
            //     - {FBClientId}
            //         - Log
            //         - Error

            // create specific facebook client folder
            var fbcFolder = mSession + "\\" + mFbClientId;
            Directory.CreateDirectory(fbcFolder);

            // init Log
            var logFolder = fbcFolder + "\\Log";
            Directory.CreateDirectory(logFolder);
            mLogger = new FileLogger(logFolder);

            // init error log
            var eLogFolder = fbcFolder + "\\Error";
            Directory.CreateDirectory(eLogFolder);
            mExceptionLogger = new XpathErrorLog(eLogFolder);

            mHttp = new HttpHandler();

            Username = user;
            Password = password;

            // after login, we need set delay so each request will delay at least x seconds to send next request.
            mHttp.DelayRequest = true;
            mHttp.DelaySeconds = 2;

            this.authorize();
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
            string actionPath = "/a/group/" + action;
            HtmlNode form = this.createDom("https://m.facebook.com/groups/" + groupId)
                .SelectSingleNode(string.Format("//form[contains(@action, '{0}')]", actionPath));
            if (form == null) return false;
            string actionUrlPath = form.GetAttributeValue("action", null);
            if (actionUrlPath == null) return false;
            string payload = HtmlHelper.CreatePayload(form.ParentNode.Elements("input"), null);
            using (var response = mHttp.SendPOSTRequest("https://m.facebook.com" + actionUrlPath, payload))
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
            HtmlNodeCollection formNodes = this.createDom("https://m.facebook.com/group/leave/?group_id=" + groupId)
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

                    using (HttpWebResponse joinGroupResponse = mHttp.SendPOSTRequest(actionUrl, payload))
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
            HtmlNode docNode = this.createDom("https://m.facebook.com/groups/" + groupId + "?view=info");
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
        /// <param name="startMemberIndex">
        /// Index of member in target group.
        /// Note that FB display 30 members per page. 
        /// So the first page begin at startMemberIndex = 0, second page begin at startMemberIndex = 30 </param>
        /// <returns>List of group member</returns>
        private List<GroupMember> GetGroupMembers(string groupId, int startMemberIndex)
        {
            var groupMembers = new List<GroupMember>();

            int p = startMemberIndex;
            int numberOfMemberFacebookDisplayPerPage = 30;

            while (true)
            {
                string groupMemberUrl =
                    "https://m.facebook.com/browse/group/members/?id=" + groupId +
                    "&start=" + startMemberIndex +
                    "&listType=list_nonfriend";

                HtmlNode document = this.createDom(groupMemberUrl);
                HtmlNodeCollection memberNodes = document.SelectNodes("/html/body/div/div/div[2]/div/div[1]/div/table");

                if (memberNodes == null || memberNodes.Count == 0)
                    break;

                foreach (HtmlNode memberNode in memberNodes)
                {
                    var groupMember = new GroupMember();

                    // member id or alias
                    groupMember.UserId = CompiledRegex.Match("Digit", memberNode.GetAttributeValue("id", string.Empty)).Value;

                    // is admin
                    HtmlNode isAdminNode = memberNode.SelectSingleNode("tr/td[2]/div/h3[2]");
                    if (isAdminNode != null)
                        groupMember.IsAdmin = isAdminNode.InnerText.Contains(Localization.IsGroupAdmin);

                    // display name
                    HtmlNode nameNode = memberNode.SelectSingleNode("tr/td[2]/div/h3[1]/a") ?? memberNode.SelectSingleNode("tr/td[2]/div/h3[1]");
                    groupMember.DisplayName = (nameNode == null) ? string.Empty : nameNode.InnerText;

                    groupMembers.Add(groupMember);
                }

                startMemberIndex += numberOfMemberFacebookDisplayPerPage;
            }

            return groupMembers;
        }

        /// <summary>
        /// Get groups which user has joined in
        /// At the moment, this method just get first page of all page (if user join so much group)
        /// Base on my code, you can fix it for your use. Sorry for that inconvenience
        /// </summary>
        /// <returns></returns>
        public List<UserGroup> GetJoinedGroups()
        {
            var userGroups = new List<UserGroup>();
            var dom = this.createDom("https://m.facebook.com/groups/?seemore");
            var header = dom.SelectSingleNode(string.Format("//div/h3[.='{0}']", Localization.GroupYouAreIn));
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
            using (HttpWebResponse response = mHttp.SendGETRequest("http://m.facebook.com" + href))
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

            HtmlNode htmlNode = this.createDom("https://m.facebook.com/page/reviews.php?id=" + pageIdOrAlias);

            if (Localization.PageNotFound.Any(text => htmlNode.InnerHtml.Contains(text)))
                throw new PageNotFoundException(pageIdOrAlias + " not contains review page");

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
                        HtmlNode htmlRateContentNode = this.createDom("https://m.facebook.com" + rateContentAnchorLink);
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
            HtmlNode htmlNode = this.createDom("https://m.facebook.com/" + pageAlias);
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

                    if (anchor.InnerText == Localization.Like)
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
            HtmlNode document = this.createDom(loadDomUrl);
            HtmlNode postForm = document.SelectSingleNode("//form[contains(@action, '/composer')]");
            if (postForm != null)
            {
                IEnumerable<HtmlNode> inputs = postForm.ParentNode.Elements("input");
                string payload = HtmlHelper.CreatePayload(inputs, "view_post=Post&xc_message=" + Uri.EscapeDataString(message));
                string actionUrl = postForm.GetAttributeValue("action", null);
                if (actionUrl == null) return false;
                using (var joinGroup = mHttp.SendPOSTRequest("https://m.facebook.com" + actionUrl, payload))
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        public UserInfo GetUserInfo(string userIdOrAlias, UserInfoOption option)
        {
            var ctx = "GetUserInfo";
            if (string.IsNullOrWhiteSpace(userIdOrAlias))
                throw new ArgumentException("userIdOrAlias must not null or empty.");

            var userInfo = new UserInfo { FBInfo = new FacebookInfo() };
            var userAboutUrl = string.Empty;
            bool isUserId = !CompiledRegex.Match(Pattern.NonDigit, userIdOrAlias).Success;

            if (isUserId)
            {
                userAboutUrl = "https://m.facebook.com/profile.php?v=info&id=" + userIdOrAlias;
                userInfo.FBInfo.Id = userIdOrAlias;
            }
            else
            {
                userAboutUrl = "https://m.facebook.com/" + userIdOrAlias + "/about";
                userInfo.FBInfo.Alias = userIdOrAlias;
            }

            HtmlNode htmlDom = this.createDom(userAboutUrl);

            // Get avatar anchor tag :

            // avatarAnchorElem contain avatar image source, user display name and maybe contain id.
            // if userIdOrAlias is "me user" or "other users with animated avatar" then 1st xpath is wrong
            // so we need to pick another anchor
            HtmlNode avatarAnchor = htmlDom.SelectSingleNode("//div[@id='root']/div/div/div/div/div/a");
            if (avatarAnchor == null ||                                        // FB change structure
                avatarAnchor.InnerText == Localization.EditProfilePicture ||   // Me user
                avatarAnchor.InnerText == Localization.AddProfilePicture)      // Me user
            {
                // pick the second pattern                
                avatarAnchor = htmlDom.SelectSingleNode("//div[@id='root']/div/div/div/div/div/div/a");
            }
            else if ((avatarAnchor.SelectSingleNode("div/a/img") != null) ||
                    (avatarAnchor.PreviousSibling != null && avatarAnchor.PreviousSibling.SelectSingleNode("/a/img") != null))
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
                else
                {
                    mLogger.WriteLine(ctx + ":Empty avatar anchor node-001");
                    mLogger.WriteLine("-------");
                }
            }
            // Update 25-0802917
            else 
            {
                if (avatarAnchor.SelectSingleNode("img") == null)
                {
                    mLogger.WriteLine(ctx + ":Empty avatar anchor node-002");
                    mLogger.WriteLine("-------");
                }
            }

            // require user id
            if (!isUserId && option.FbInfoOption.IncludeUserId)
            {
                Match idMatch = Match.Empty;

                // trying get id from avatar href
                var avatarHref = avatarAnchor.GetAttributeValue("href", null);

                // If we found avatar href, we might using it to detect user id
                if (avatarHref != null)
                {
                    // There is 3 pattern to detect user id
                    // If both 3 pattern can not detect user id, return this url for detect later.                    
                    // /photo.php?fbid=704517456378829&id=100004617430839&...
                    // /profile/picture/view/?profile_id=100003877944061&...
                    // /story.php\?story_fbid=\d+&amp;id=(?<id>\d+) for animate avatar                    
                    if ((idMatch = CompiledRegex.Match(Pattern.UserIdFromAvatar1, avatarHref)).Success ||
                        (idMatch = CompiledRegex.Match(Pattern.UserIdFromAvatar2, avatarHref)).Success ||
                        (idMatch = CompiledRegex.Match(Pattern.UserIdFromAvatar3, avatarHref)).Success)
                    {
                        userInfo.FBInfo.Id = idMatch.Groups["id"].Value;
                    }
                }

                // now avatarHref is null or we cannot detect user id from avatarHref
                // so we need try another way
                if (string.IsNullOrEmpty(userInfo.FBInfo.Id))
                {
                    // Trying to detect user id from hyperlink : 
                    // Timeline · Friends · Photos · Likes · Followers · Following · [Activity Log]
                    // NOTE :
                    //      - Activity log only show in "Me users" about page
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
                            string hrefAttr = anchor.GetAttributeValue("href", string.Empty);
                            string innerText = anchor.InnerText;
                            if (!string.IsNullOrWhiteSpace(innerText)
                                && (idMatch = CompiledRegex.Match(innerText, hrefAttr)).Success)
                            {
                                userInfo.FBInfo.Id = idMatch.Groups["id"].Value;
                                break;
                            }
                        }
                    }
                }

                // Try another way if id still empty
                if (string.IsNullOrEmpty(userInfo.FBInfo.Id))
                {
                    // Step 3 : 
                    // trying get uid from action button : Add Friend, Message, Follow, More
                    // I only select the 1st xpath,the second xpath will be check in the future.
                    HtmlNodeCollection btnHrefs = htmlDom.SelectNodes("//div[@id='root']/div/div/div/table/tr/td/a");
                    // ??htmlDom.SelectNodes("//div[@id='root']/div/div/div");

                    // If we found some button nodes :
                    //      - Add Friend node if we does not add friend with this user
                    //      - Message if this user allow we can send message to him/her
                    //      - Follow if this user allow we can follow him/her and we not follow him/her before
                    //      - More if we can see more about user - i guess
                    if (btnHrefs != null && btnHrefs.Count > 0)
                    {
                        foreach (var btnHref in btnHrefs)
                        {
                            // if href and innertext not null then we can trying detect user id by compiled regex
                            // NOTE :
                            //      - Check CompiledRegex if you think it not correct anymore
                            //      - Edit Key if you use another Language rather than English to access FB
                            string hrefAttr = btnHref.GetAttributeValue("href", string.Empty);
                            string innerText = btnHref.InnerText;
                            if (!string.IsNullOrWhiteSpace(innerText)
                                && (idMatch = CompiledRegex.Match(innerText, hrefAttr)).Success)
                            {
                                userInfo.FBInfo.Id = idMatch.Groups["id"].Value;
                                break;
                            }
                        }
                    }
                }
            }

            #region IncludeUserId
            // id or alias [at least one]
            if (option.FbInfoOption.IncludeUserId && string.IsNullOrWhiteSpace(userInfo.FBInfo.Id))
            {
                // Cause these are so many pattern to detect user id so we won't log each xpath to each file
                // In stead, we need to log specify link and check all xpath later.
                mLogger.WriteLine(ctx + ": Require user id but user id empty");
                mLogger.WriteLine("\tLink: " + userAboutUrl);
                mLogger.WriteLine("-------------");
            }
            #endregion

            #region IncludeAvatarUrl || IncludeUserDisplayName
            // user name and avatar url [optional]
            // check avatarAnchor is null or not -- fault tolerant -- cuz these info doesn't important
            if ((option.FbInfoOption.IncludeAvatarUrl || option.FbInfoOption.IncludeUserDisplayName))
            {
                if (avatarAnchor != null)
                {
                    // TODO: Avater node has been changed into Cover image photo
                    HtmlNode avatar = avatarAnchor.SelectSingleNode("img");
                    if (avatar != null)
                    {
                        // Get name and avatar
                        if (option.FbInfoOption.IncludeUserDisplayName)
                            userInfo.FBInfo.DisplayName = WebUtility.HtmlDecode(avatar.GetAttributeValue("alt", string.Empty));
                        if (option.FbInfoOption.IncludeAvatarUrl)
                            userInfo.FBInfo.AvatarUrl = WebUtility.HtmlDecode(avatar.GetAttributeValue("src", string.Empty));
                    }
                    else
                    {
                        this.logXPathNullNode(ctx, avatarAnchor.InnerHtml, "img");
                    }
                }
                else
                {
                    mLogger.WriteLine(ctx + ":IncludeAvatarUrl||IncludeUserDisplayName:Empty avatar anchor node");
                    mLogger.WriteLine("----");
                }
            }
            #endregion

            #region IncludeAddressInfo
            if (option.IncludeAddressInfo)
            {
                // livingNode contains information about City and HomeTown
                HtmlNode livingNode = htmlDom.SelectSingleNode("//div[@id='living']");
                if (livingNode != null)
                {
                    // Notice: See Note 001
                    HtmlNodeCollection trNodes = HtmlHelper.BuildDom(livingNode.InnerHtml).SelectNodes("//tr");
                    userInfo.Address = new AddressInfo();
                    foreach (var trNode in trNodes)
                    {
                        HtmlNodeCollection tds = trNode.SelectNodes("td");
                        // only get which td have 2 td
                        // td[0] is topic
                        // td[1] is value
                        if (tds == null && tds.Count != 2)
                            continue;

                        string value = string.Empty;
                        HtmlNode valueNode = null;
                        if (((valueNode = tds[1].SelectSingleNode("div/span/span")) != null) ||
                            ((valueNode = tds[1].SelectSingleNode("div/span")) != null) ||
                            ((valueNode = tds[1].SelectSingleNode("div/a")) != null) ||
                            ((valueNode = tds[1].SelectSingleNode("div")) != null))
                        {
                            value = valueNode.InnerText;
                            // store address
                            if (tds[0].InnerText.Contains(Localization.CurrentCity))
                            {
                                userInfo.Address.City = value;
                            }
                            else if (tds[0].InnerText.Contains(Localization.HomeTown))
                            {
                                userInfo.Address.HomeTown = value;
                            }
                        }
                        else
                        {
                            mLogger.WriteLine(ctx + ":IncludeAddressInfo:Could not detect value");
                            mLogger.WriteLine("\tMore details:");
                            this.logXPathNullNode(ctx, tds[1].InnerHtml, "div/span/span || div/span || div/a || div");
                            mLogger.WriteLine("-------------");
                        }
                    }
                }
                else
                {
                    this.logXPathNullNode(ctx, htmlDom.InnerHtml, "//div[@id='living']");
                }
            }
            #endregion

            #region IncludeContactInfo
            if (option.IncludeContactInfo)
            {
                userInfo.Contact = new ContactInfo();

                HtmlNode contactInfo = htmlDom.SelectSingleNode("//div[@id='contact-info']");
                if (contactInfo != null)
                {
                    // extract local DOM
                    HtmlNodeCollection trNodes = HtmlHelper.BuildDom(contactInfo.InnerHtml).SelectNodes("//tr");
                    foreach (var trNode in trNodes)
                    {
                        var tds = HtmlHelper.BuildDom(trNode.InnerHtml).SelectNodes("//td");
                        if (tds == null || tds.Count != 2)
                            continue; // ignore                    

                        // get contact value
                        string value = string.Empty;
                        HtmlNode valueNode = null;
                        if ((valueNode = HtmlHelper.BuildDom(tds[1].InnerHtml).SelectSingleNode("//div/span/span")) != null ||
                            (valueNode = HtmlHelper.BuildDom(tds[1].InnerHtml).SelectSingleNode("//div/span")) != null ||
                            (valueNode = HtmlHelper.BuildDom(tds[1].InnerHtml).SelectSingleNode("//div/a")) != null ||
                            (valueNode = HtmlHelper.BuildDom(tds[1].InnerHtml).SelectSingleNode("//div")) != null)
                        {
                            value = WebUtility.HtmlDecode(valueNode.InnerText);
                            // store contact
                            if (tds[0].InnerText.Contains("Mobile"))
                            {
                                userInfo.Contact.Mobile = value;
                            }
                            else if (tds[0].InnerText.Contains("Email"))
                            {
                                userInfo.Contact.Email = value;
                            }
                            else if (tds[0].InnerText.Contains("Websites"))
                            {
                                userInfo.Contact.Website = value;
                            }
                        }
                        else
                        {
                            mLogger.WriteLine(ctx + ":IncludeContactInfo:Could not detect value");
                            mLogger.WriteLine("\tMore details:");
                            this.logXPathNullNode(ctx, tds[1].InnerHtml, "div/span/span || div/span || div/a || div");
                            mLogger.WriteLine("-------------");
                        }
                    }
                }
                else
                {
                    this.logXPathNullNode(ctx, htmlDom.InnerHtml, "//div[@id='contact-info']");
                }
            }
            #endregion

            #region IncludeBasicInfo
            if (option.IncludeBasicInfo)
            {
                userInfo.BasicInfo = new BasicInfo();

                HtmlNode contactInfo = htmlDom.SelectSingleNode("//div[@id='basic-info']");
                HtmlNodeCollection trNodes = HtmlHelper.BuildDom(contactInfo.InnerHtml).SelectNodes("//tr");
                foreach (var trNode in trNodes)
                {
                    var tds = HtmlHelper.BuildDom(trNode.InnerHtml).SelectNodes("//td");
                    if (tds == null || tds.Count != 2)
                        continue; // ignore                    

                    // get contact value
                    string value = string.Empty;
                    HtmlNode valueNode = null;
                    if ((valueNode = HtmlHelper.BuildDom(tds[1].InnerHtml).SelectSingleNode("//div/span/span")) != null ||
                        (valueNode = HtmlHelper.BuildDom(tds[1].InnerHtml).SelectSingleNode("//div/span")) != null ||
                        (valueNode = HtmlHelper.BuildDom(tds[1].InnerHtml).SelectSingleNode("//div/a")) != null ||
                        (valueNode = HtmlHelper.BuildDom(tds[1].InnerHtml).SelectSingleNode("//div")) != null)
                    {
                        value = valueNode.InnerText;

                        // store contact
                        if (tds[0].InnerText.Contains("Birthday"))
                        {
                            userInfo.BasicInfo.BirthDay = value;
                        }
                        else if (tds[0].InnerText.Contains("Gender"))
                        {
                            userInfo.BasicInfo.Gender = value;
                        }
                        else if (tds[0].InnerText.Contains("Interested In"))
                        {
                            userInfo.BasicInfo.InterestedIn = value;
                        }
                        else if (tds[0].InnerText.Contains("Languages"))
                        {
                            userInfo.BasicInfo.Languages = value;
                        }
                        else if (tds[0].InnerText.Contains("Religious Views"))
                        {
                            userInfo.BasicInfo.ReligiousViews = value;
                        }
                        else if (tds[0].InnerText.Contains("Political Views"))
                        {
                            userInfo.BasicInfo.PolictialViews = value;
                        }
                    }
                    else
                    {
                        mLogger.WriteLine(ctx + ":IncludeBasicInfo:Could not detect value");
                        mLogger.WriteLine("\tMore details:");
                        this.logXPathNullNode(ctx, tds[1].InnerHtml, "div/span/span || div/span || div/a || div");
                        mLogger.WriteLine("-------------");
                    }

                }
            }
            #endregion

            #region IncludeEduInfo
            if (option.IncludeEduInfo)
            {
                // coming not soon
            }
            #endregion

            #region IncludeRelationshipInfo
            if (option.IncludeRelationshipInfo)
            {
                // not current ver
            }
            #endregion

            #region IncludeWorkInfo
            if (option.IncludeWorkInfo)
            {
                // not for current ver
            }
            #endregion

            #region IncludeFbFriends
            // get user friend list if included
            // at this step we do not need to check user id anymore
            // if user id is null then we had return before.
            if (option.FbInfoOption.IncludeFbFriends)
            {
                if (string.IsNullOrWhiteSpace(userInfo.FBInfo.Id))
                {
                    mLogger.WriteLine(".GetUserInfo : Include FB Friends but User Id empty. No friends included.");
                }
                else
                {
                    var friendPageUrl = "https://m.facebook.com/profile.php?v=friends&startindex=0&id=" + userInfo.FBInfo.Id;
                    userInfo.FBInfo.FbFriends = this.getFriends(friendPageUrl);
                }
            }
            #endregion

            // all step have done
            return userInfo;
        }
        List<string> getFriends(string friendPage)
        {
            var ctx = "_GetFriends";
            // Declare list string to store user id
            var friends = new List<string>();

            // friendPage will be update each loop
            // if there is no more friend page, this loop will be terminate.
            while (true)
            {
                HtmlNode docNode = this.createDom(friendPage);
                // Because selectNodes and SelectSingleNode method working incorrect.
                // Both method working in entire document node rather than current node when we pass relative xpath expression.
                // So, we maybe get more element than expect.

                // E.g : Trying select table element with role=presentation in rootNode will search in entire document
                // var friendTables1 = rootNode.SelectNodes("//table[@role='presentation']");            

                // If we only want to search in InnerHtml of current node, just load it to another HtmlDocument object
                // Maybe isn't the best way to do our job. But at the moment, i think it still good.
                HtmlNode rootNode = HtmlHelper.BuildDom(docNode.SelectSingleNode("//div[@id='root']").InnerHtml);
                HtmlNodeCollection friendAnchors =
                    rootNode.SelectNodes("//table[@role='presentation']/tr/td[2]/a") ??
                    rootNode.SelectNodes("//table[@role='presentation']/tbody/tr/td[2]/a");
                if (friendAnchors == null)
                {
                    mLogger.WriteLine(ctx + ":friendAnchors:Maybe last page or xpath error");
                    mLogger.WriteLine("\tMore details:");
                    this.logXPathNullNode(ctx, rootNode.InnerHtml, "//table[@role='presentation']/tr/td[2]/a || //table[@role='presentation']/tbody/tr/td[2]/a");
                    return friends;
                }

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
                            Match match = CompiledRegex.Match(Pattern.UserId, userProfileHref);
                            if (match.Success)
                                friends.Add(match.Groups["id"].Value);
                            else
                                mLogger.WriteLine("Match user id by CompiledRege.Match(UserId) is fail. Addition info : url=" + friendPage + " and user profile is " + userProfileHref);
                        }
                    }
                    else
                    {
                        // If we go to this code block, there are some case happend :
                        // - Our bot has been block by this user or facebook.
                        // - This is deleted user.
                        // - We need provide more pattern to detect user id                       
                        mLogger.WriteLine(ctx + ":userProfileHref:Could not detect.");
                        mLogger.WriteLine("\tMaybe our bot blocked by this user or FB, or this user " + friendAnchor.InnerText + " has been banned or our xpath does not match anymore");
                        mLogger.WriteLine("\tLink : " + friendPage);
                        mLogger.WriteLine("-------------");
                    }
                }

                // get more friend
                HtmlNode moreFriend = rootNode.SelectSingleNode("//div[@id='m_more_friends']/a");
                if (moreFriend == null)
                {
                    mLogger.WriteLine(ctx + ":moreFriend:No more friends page at : " + friendPage);
                    break;
                }

                var nextUrl = WebUtility.HtmlDecode(moreFriend.GetAttributeValue("href", string.Empty));
                if (nextUrl != null)
                {
                    friendPage = "https://m.facebook.com" + nextUrl;
                }
                else
                {
                    mLogger.WriteLine(".GetFriends");
                    mLogger.WriteLine("\t\tNext Url is empty. Maybe this is last page.");
                    mLogger.WriteLine("\t\tLink : " + friendPage);
                    // exit loop
                    break;
                }
            }

            return friends;
        }

        // --------- helper ------------
        HtmlNode createDom(string url)
        {
            string htmlContent = mHttp.DownloadContent(url);
            return HtmlHelper.BuildDom(htmlContent);
        }
        bool authorize()
        {
            mLogger.WriteLine("Authorize");
            // load html content to document node
            HtmlNode document = this.createDom("https://m.facebook.com");

            // Get login form Dom object
            HtmlNode loginForm = document.SelectSingleNode("//form[@id='login_form']");
            IEnumerable<HtmlNode> inputs = loginForm.ParentNode.Elements("input");

            // create content payload
            string credential = string.Format("email={0}&pass={1}", Username, Password);
            string payload = HtmlHelper.CreatePayload(inputs, additionKeyValuePair: credential);

            using (HttpWebResponse response = mHttp.SendPOSTRequest("https://m.facebook.com/login.php", payload))
            {
                if (response.Cookies["c_user"] == null)
                    throw new UnAuthorizedException("CookieKeyCUserNotFound");
            }
            return true;
        }
        void logXPathNullNode(string context, string html, string xpath)
        {
            var xePath = mExceptionLogger.Log(html, xpath);
            mLogger.WriteLine(context + ":Null Node");
            mLogger.WriteLine("\tXpath log file:" + xePath);
            mLogger.WriteLine("-------");
        }

        public void Dispose()
        {
            mLogger.Dispose();
            mLogger = null;
            mExceptionLogger = null;
            mHttp = null;
        }
    }
}
