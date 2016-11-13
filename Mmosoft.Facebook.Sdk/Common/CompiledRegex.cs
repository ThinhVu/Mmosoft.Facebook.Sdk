using System.Text.RegularExpressions;

namespace Mmosoft.Facebook.Sdk.Common
{
    public static class CompiledRegex
    {
        // Common Regex
        public static Regex MatchNonDigit = new Regex(@"\D+", RegexOptions.Compiled);

        // Compiled Regex for HrefPatterns in user about page
        public static Regex TimeLine = new Regex(@"/profile.php\?v=timeline&id=(?<id>\d+)", RegexOptions.Compiled);
        public static Regex Friend = new Regex(@"/profile.php\?v=friends&id=(?<id>\d+)", RegexOptions.Compiled);
        public static Regex Photos = new Regex(@"/profile.php\?v=photos&id=(?<id>\d+)", RegexOptions.Compiled);
        public static Regex Likes = new Regex(@"/profile.php\?v=likes&id=(?<id>\d+)", RegexOptions.Compiled);
        public static Regex Followers = new Regex(@"not contain pattern for that", RegexOptions.Compiled);
        public static Regex Following = new Regex(@"/profile.php\?v=following&id=(?<id>\d+)", RegexOptions.Compiled);
        public static Regex ActivityLog = new Regex(@"/(?<id>\d+)/allactivity", RegexOptions.Compiled);

        // Compiled Regex for BtnHrefPatterns in user about page
        public static Regex AddFriend = new Regex(@"profile_add_friend.php\?subjectid=(?<id>\d+)", RegexOptions.Compiled);
        public static Regex Message = new Regex(@"/messages/thread/(?<id>\d+)/", RegexOptions.Compiled);
        public static Regex Follow = new Regex(@"/a/subscribe.php?id=(?<id>\d+)", RegexOptions.Compiled);
        public static Regex More = new Regex(@"/mbasic/more/?owner_id=(?<id>\d+)", RegexOptions.Compiled);

        /// <summary>
        /// Compiled Regex to get friend list
        /// </summary>
        private static Regex _friendListRegex1;
        private static Regex _friendListRegex2;

        /// <summary>
        /// Get user id from add friend button
        /// </summary>
        public static string GetUserId1(string input)
        {
            if (null == _friendListRegex1)
                _friendListRegex1 = new Regex(@"add_friend.php\?id=(?<id>\d+)", RegexOptions.Compiled);

            return _friendListRegex1.Match(input).Groups["id"].Value;
        }

        /// <summary>
        /// Get friend id from profile link
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool GetUserIdFromRawProfileUrl(string input, out string id)
        {
            // lazy init
            if (null == _friendListRegex2)
            {
                _friendListRegex2 = new Regex(@"\D+(?<id>\d+)\D+", RegexOptions.Compiled);
            }

            var match = _friendListRegex2.Match(input);
            if (match.Success)
            {
                id = match.Groups["id"].Value;
            }
            else
            {
                id = string.Empty;
            }

            return match.Success;
        }
    }
}
