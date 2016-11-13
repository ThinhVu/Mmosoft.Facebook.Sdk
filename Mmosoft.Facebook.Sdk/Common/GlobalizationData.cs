using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mmosoft.Facebook.Sdk.Common
{
    /// <summary>
    /// Class contain localization data
    /// </summary>
    public static class GlobalData
    {
        public static string EditProfilePicture;
        public static ICollection<string> IsGroupAdministrator;
        public static ICollection<string> PageNotFound;
        public static IDictionary<string, Regex> HrefRegexes;
        public static IDictionary<string, Regex> BtnHrefRegexes;

        // Init data
        static GlobalData()
        {
            EditProfilePicture = "Edit Profile Picture";

            IsGroupAdministrator = new List<string>
            {
                "Admin",        // English
                "Quản trị viên" // Vietnamese
            };

            PageNotFound = new List<string>
            {
                "The page you requested cannot be displayed right now." // English
            };

            HrefRegexes = new Dictionary<string, Regex> {
                            {"Timeline", CompiledRegex.TimeLine },
                            {"Friends", CompiledRegex.Friend },
                            {"Photos", CompiledRegex.Photos },
                            {"Likes", CompiledRegex.Likes },
                            {"Followers", CompiledRegex.Followers },
                            {"Following", CompiledRegex.Following },
                            {"Activity Log", CompiledRegex.ActivityLog }
                        };

            BtnHrefRegexes = new Dictionary<string, Regex> {
                            { "Add Friend", CompiledRegex.AddFriend },
                            { "Message", CompiledRegex.Message },
                            { "Follow",  CompiledRegex.Follow},
                            { "More", CompiledRegex.More }
                        };
        }


    }
}
