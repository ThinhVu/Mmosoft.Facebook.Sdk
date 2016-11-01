using Mmosoft.Facebook.Sdk.Models;
using System.Runtime.InteropServices;

namespace Mmosoft.Facebook.Sdk
{
    public interface IFacebookClient
    {
        /// <summary>
        /// Join specified group
        /// </summary>
        /// <param name="groupId"></param>
        void JoinGroup(string groupId);        
        /// <summary>
        /// Like specified page
        /// </summary>
        /// <param name="pageId"></param>
        void LikePage(string pageId);
        /// <summary>
        /// Post message to you wall
        /// </summary>
        /// <param name="message"></param>
        void PostToWall(string message);
        /// <summary>
        /// Post message to group you joined in
        /// </summary>
        /// <param name="message"></param>
        /// <param name="groupId"></param>
        void PostToGroup(string message, string groupId);
        /// <summary>
        /// Get id, name of your friend or other's friends
        /// </summary>
        /// <param name="userIdAlias"></param>
        /// <returns></returns>
        FriendInfo GetFriendInfo([Optional] string userIdAlias);
        /// <summary>
        /// Get group, member. Member info contain user is administrator of this group or not
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        GroupInfo GetGroupInfo(string groupId);
        /// <summary>
        /// Get page review
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        ReviewInfo GetReviewInfo(string pageId);
        /// <summary>
        /// Get username from user alias
        /// </summary>
        /// <param name="userAlias"></param>
        /// <returns></returns>
        string GetUserId(string userAlias);
    }
}
