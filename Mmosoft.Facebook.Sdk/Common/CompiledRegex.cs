using System.Text.RegularExpressions;

namespace Mmosoft.Facebook.Sdk.Common
{
    public static class CompiledRegex
    {
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
        public static string GetUserId2(string input)
        {
            if (null == _friendListRegex2)
                _friendListRegex2 = new Regex(@"\D+(?<id>\d+)\D+", RegexOptions.Compiled);

            return _friendListRegex2.Match(input).Groups["id"].Value;
        }
    }
}
