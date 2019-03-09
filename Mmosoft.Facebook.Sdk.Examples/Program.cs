using System;

namespace Mmosoft.Facebook.Sdk.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            // user end-point
            var user = new UserEndpoint(username: "", password: "");
            user.Post("Send from Facebook SDK"); // post to logged users wall
            // Missing some stuff as like, comment, block another user, etc
            user.GetUserInfo("beciuu94", new Models.User.UserInfoOption // gather another user information
            {
                FbInfoOption = new Models.User.FacebookInfoOption
                {
                    IncludeAvatarUrl = true,
                    IncludeFbFriends = true,
                    IncludeFbUrl = true,
                    IncludeUserDisplayName = true,
                    IncludeUserId = true
                },
                IncludeAddressInfo = true,
                IncludeBasicInfo = true,
                IncludeContactInfo = false,
                IncludeEduInfo = false,
                IncludeRelationshipInfo = false,
                IncludeWorkInfo = false
            });

            // Page end-point
            var page = new PageEndpoint(username: "", password: "");            
            page.GetPageId(pageAlias: "FHNChallengingTheImpossible"); // get id from page alias
            page.GetPageAlbums(pageAlias: "FHNChallengingTheImpossible"); // get album list of a page using page alias
            page.GetPageReviewInfo(pageIdOrAlias: "FHNChallengingTheImpossible"); // get review of pages
            page.LikePage(pageIdOrAlias: "FHNChallengingTheImpossible"); // like a page            
            page.GetPageAlbumImages(pageAlias: "FHNChallengingTheImpossible", albumId: "568853546917720");

            // messy stuff
            // I'm not sure we can use this method for user or group posts. Not tested yet.
            page.CommentToPageAlbumImage("https://m.facebook.com/photo.php?fbid=569190373550704&id=533880033748405", "Hi");
            page.LikePhoto(targetId: "target id is photo picture in a page."); // hmm
            
            // Group end-point
            var group = new GroupEndpoint(username: "", password: "");
            group.GetJoinedGroups();            
            group.Post(message: "hello I'm new", groupId: "1234567");
            group.JoinGroup(groupId: "1234567");
            group.CancelJoinGroup(groupId: "1234567"); // incase your join group is pending for processing
            group.LeaveGroup(groupId: "1234567", preventReAdd: true);
            group.GetGroupInfo(groupId: "1234567"); // get group information, member list, admin, etc...

            Console.WriteLine();
        }
    }
}
