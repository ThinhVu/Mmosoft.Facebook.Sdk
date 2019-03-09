using HtmlAgilityPack;
using Mmosoft.Facebook.Sdk.Models.Group;
using Mmosoft.Facebook.Sdk.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Mmosoft.Facebook.Sdk
{
    public partial class GroupEndpoint : FbClient
    {
        public GroupEndpoint(string username, string password)
            : base (username, password)
        {
        }

        // ----------- Group ---------- //
        /// <summary>
        /// Send request to join or cancel group
        /// </summary>
        /// <param name="groupId">Id's target group</param>
        public void JoinGroup(string groupId)
        {
            JoinCancelJoinGroup(groupId, "join");
        }
        /// <summary>
        /// Cancel join group
        /// </summary>
        /// <param name="groupId">facebook group id</param>
        /// <returns>true if join success, otherwhile false</returns>
        public void CancelJoinGroup(string groupId)
        {
            JoinCancelJoinGroup(groupId, "canceljoin");
        }
        /// <summary>
        /// Do join, cancel join action
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        void JoinCancelJoinGroup(string groupId, string action)
        {
            string actionPath = "/a/group/" + action;
            HtmlNode form = __BuildDomFromUrl("https://m.facebook.com/groups/" + groupId)
                .SelectSingleNode(string.Format("//form[contains(@action, '{0}')]", actionPath));

            if (form == null)
                throw new Exception("GroupEndpoint_JoinCancelJoinGroup: form node is null");

            string actionUrlPath = form.GetAttributeValue("action", null);
            if (actionUrlPath == null)
                throw new Exception("GroupEndpoint_JoinCancelJoinGroup: actionUrlPath is null");

            string payload = __CreatePayload(__ExtractHidenInputNodes(form.ParentNode));
            _requestHandler.SendPOSTRequest("https://m.facebook.com" + actionUrlPath, payload);
        }
        /// <summary>
        /// Leave group
        /// </summary>
        /// <param name="groupId">facebook group id</param>
        /// <param name="preventReAdd">prevent group member re-add you again</param>
        /// <returns>bool value if leave group request send success</returns>
        public void LeaveGroup(string groupId, bool preventReAdd)
        {
            HtmlNodeCollection formNodes = __BuildDomFromUrl("https://m.facebook.com/group/leave/?group_id=" + groupId)
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

                    _requestHandler.SendPOSTRequest(actionUrl, payload);
                }
            }
        }
        /// <summary>
        /// Post to group
        /// </summary>
        /// <param name="message">Message you want to post</param>
        /// <param name="groupId">Id's target group</param>
        public void Post(string message, string groupId)
        {            
            HtmlNode document = __BuildDomFromUrl("https://m.facebook.com/groups/" + groupId);
            HtmlNode postForm = document.SelectSingleNode("//form[contains(@action, '/composer')]");
            if (postForm == null)
                throw new Exception("Group:Post:postForm is null");

            string actionUrl = postForm.GetAttributeValue("action", null);
            if (actionUrl == null)
                throw new Exception("Group:Post:actionUrl is null");

            List<string> postData = __ExtractHidenInputNodes(postForm.ParentNode);
            postData.Add("view_post=Post");
            postData.Add("xc_message="+ Uri.EscapeDataString(message));            
            _requestHandler.SendPOSTRequest("https://m.facebook.com" + actionUrl, __CreatePayload(postData));
        }
        /// <summary>
        /// Get group info
        /// </summary>
        /// <param name="groupId">Group id you want to get info</param>
        /// <exception cref="NodeNotFoundException">Exception when select DOM query fail</exception>
        /// <returns>Group Info object</returns>
        public GroupInfo GetGroupInfo(string groupId)
        {
            HtmlNode docNode = __BuildDomFromUrl("https://m.facebook.com/groups/" + groupId + "?view=info");
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

                HtmlNode document = __BuildDomFromUrl(groupMemberUrl);
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
            var dom = __BuildDomFromUrl("https://m.facebook.com/groups/?seemore");
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
    }
}
