using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mmosoft.Facebook.Sdk
{
    /// <summary>
    /// This static class will contain const strings for some common pattern
    /// which doesn't depend on user language
    /// </summary>
    public static class Pattern
    {
        // Common pattern
        public const string Digits = "Digits";
        public const string NonDigit = "NonDigit";
        public const string Words = "Words";

        // User id pattern
        public const string UserId = "UserId";
        public const string UserIdFromAvatar1 = "UserIdFromAvatar1";
        public const string UserIdFromAvatar2 = "UserIdFromAvatar2";
        public const string UserIdFromAvatar3 = "UserIdFromAvatar3";

        // page id
        public const string PageId = "PageId";
    }

    /// <summary>
    /// Only English support
    /// For other language, change english value of key
    /// </summary>
    public static class CompiledRegex
    {
        static Regex Compile(string pattern)
        {
            return new Regex(pattern, RegexOptions.Compiled);
        }
        static IDictionary<string, Regex> _map;

        static CompiledRegex()
        {
            _map = new Dictionary<string, Regex> 
            {
                // english - anchor href
                { Localization.TimeLine, Compile(@"/profile.php\?v=timeline&id=(?<id>\d+)") },
                { Localization.Friends, Compile(@"/profile.php\?v=friends&id=(?<id>\d+)") },
                { Localization.Photos, Compile(@"/profile.php\?v=photos&id=(?<id>\d+)") },
                { Localization.Likes, Compile(@"/profile.php\?v=likes&id=(?<id>\d+)") },
                { Localization.Followers, Compile(@"not contain pattern for that") },
                { Localization.Following, Compile(@"/profile.php\?v=following&id=(?<id>\d+)") },
                { Localization.ActivityLog, Compile(@"/(?<id>\d+)/allactivity") },
            
                // english- button href
                { Localization.AddFriend, Compile(@"profile_add_friend.php\?subjectid=(?<id>\d+)") },
                { Localization.Message, Compile(@"/messages/thread/(?<id>\d+)/") },
                { Localization.Follow, Compile(@"/a/subscribe.php\?id=(?<id>\d+)")},
                { Localization.More, Compile(@"/mbasic/more/\?owner_id=(?<id>\d+)") },

                // common
                { Pattern.Digits, Compile(@"\d+")},            
                { Pattern.NonDigit, Compile(@"\D+")},
                { Pattern.Words, Compile(@"\w+") },

                // User id
                { Pattern.UserId, Compile(@"\D+(?<id>\d+)\D+")},
                { Pattern.UserIdFromAvatar1, Compile(@"/photo.php\?fbid=\d+&amp;id=(?<id>\d+)")},
                { Pattern.UserIdFromAvatar2, Compile(@"/profile/picture/view/\?profile_id=(?<id>\d+)")},
                { Pattern.UserIdFromAvatar3, Compile(@"/story.php\?story_fbid=\d+&amp;id=(?<id>\d+)")},

                // Page id
                { Pattern.PageId, Compile(@"fan&amp;id=(?<pid>\d+)")}
            };
        }



        /// <summary>
        /// Match content from pattern provided by key in regex map.
        /// </summary>
        /// <param name="pattern">Match pattern</param>
        /// <param name="content">Input content</param>
        /// <returns>Match.Empty if match fail or Match object if success</returns>
        public static Match Match(string pattern, string content)
        {
            if (_map.ContainsKey(pattern))
                return _map[pattern].Match(content);
            else
                return System.Text.RegularExpressions.Match.Empty;
        }
    }
}
