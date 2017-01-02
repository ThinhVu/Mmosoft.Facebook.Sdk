using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mmosoft.Facebook.Sdk.Utilities
{
    /// <summary>
    /// Only English support
    /// For other language, change english value of key
    /// </summary>
    public static class CompiledRegex
    {
        static IDictionary<string, Regex> Map = new Dictionary<string, Regex> 
        {
            // english - anchor href
            {"Timeline", new Regex(@"/profile.php\?v=timeline&id=(?<id>\d+)", RegexOptions.Compiled) },
            {"Friends", new Regex(@"/profile.php\?v=friends&id=(?<id>\d+)", RegexOptions.Compiled) },
            {"Photos", new Regex(@"/profile.php\?v=photos&id=(?<id>\d+)", RegexOptions.Compiled) },
            {"Likes", new Regex(@"/profile.php\?v=likes&id=(?<id>\d+)", RegexOptions.Compiled) },
            {"Followers", new Regex(@"not contain pattern for that", RegexOptions.Compiled) },
            {"Following", new Regex(@"/profile.php\?v=following&id=(?<id>\d+)", RegexOptions.Compiled) },
            {"Activity Log", new Regex(@"/(?<id>\d+)/allactivity", RegexOptions.Compiled) },
            
            // english- button href
            { "Add Friend", new Regex(@"profile_add_friend.php\?subjectid=(?<id>\d+)", RegexOptions.Compiled) },
            { "Message", new Regex(@"/messages/thread/(?<id>\d+)/", RegexOptions.Compiled) },
            { "Follow",  new Regex(@"/a/subscribe.php?id=(?<id>\d+)", RegexOptions.Compiled)},
            { "More", new Regex(@"/mbasic/more/?owner_id=(?<id>\d+)", RegexOptions.Compiled) },

            // common
            {"Digit", new Regex(@"\d+", RegexOptions.Compiled)},
            {"NonDigit", new Regex(@"\D+", RegexOptions.Compiled)},

            // User id
            {"UserId", new Regex(@"\D+(?<id>\d+)\D+", RegexOptions.Compiled)},
            {"UserIdFromAvatar1", new Regex(@"/photo.php\?fbid=\d+&amp;id=(?<id>\d+)", RegexOptions.Compiled)},
            {"UserIdFromAvatar2", new Regex(@"/profile/picture/view/\?profile_id=(?<id>\d+)", RegexOptions.Compiled)},
            {"UserIdFromAvatar3", new Regex(@"/story.php\?story_fbid=\d+&amp;id=(?<id>\d+)", RegexOptions.Compiled)},

        };

        /// <summary>
        /// Match content from pattern provided by key in regex map.
        /// </summary>
        /// <param name="key">Regex map key</param>
        /// <param name="content">Content want to match</param>
        /// <returns>Match.Empty if match fail or Match object if success</returns>
        public static Match Match(string key, string content)
        {
            if (Map.ContainsKey(key))
                return Map[key].Match(content);
            else return System.Text.RegularExpressions.Match.Empty;
        }
    }
}
