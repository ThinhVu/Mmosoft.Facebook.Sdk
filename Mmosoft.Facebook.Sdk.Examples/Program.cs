using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mmosoft.Facebook.Sdk;

namespace Mmosoft.Facebook.Sdk.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            // Define fb client
            var fb = new FacebookClient(user: "", password: "");
            // And invoke method
            //fb.PostToWall("Send from Facebook SDK");
            //fb.PostToGroup("Send from Facebook SDK-Group", "529073513939720");        
            var g = fb.GetJoinedGroups();

            var someone = fb.GetUserInfo("beciuu94", new Models.User.UserInfoOption
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

            Console.WriteLine();
        }
    }
}
