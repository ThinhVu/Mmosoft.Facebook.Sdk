using System;
using System.Collections.Generic;
using System.IO;

namespace Mmosoft.Facebook.Sdk
{    
    public static class Localization
    {
        private static Dictionary<string, string> _languageMap;

        static Localization()
        {
            _languageMap = new Dictionary<string, string>();
            // init dictionary <language, dictionary<key, value>>            
            var lines = File.ReadAllLines("Localization.txt");
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("//"))
                    continue;
                var pair = line.Split(',');
                if (pair.Length == 2)
                    _languageMap[pair[0]] = pair[1];
            }
        }
                
        // Anchor string -- pattern depend on user language
        public static string EditProfilePicture
        {
            get 
            {
                return _languageMap["EditProfilePicture"];
            }
        }
        public static string AddProfilePicture
        {
            get
            {
                return _languageMap["AddProfilePicture"];
            }
        }
        public static string IsGroupAdmin
        {
            get
            {
                return _languageMap["AddProfilePicture"];
            }
        }
        public static string PageNotFound
        {
            get
            {
                return _languageMap["PageNotFound"];
            }
        }
        public static string Like
        {
            get
            {
                return _languageMap["Like"];
            }
        }
        public static string GroupYouAreIn
        {
            get
            {
                return _languageMap["GroupYouAreIn"];                
            }
        }
        public static string TimeLine
        {
            get 
            {
                return _languageMap["TimeLine"];
            }
        }
        public static string Friends
        {
            get 
            {
                return _languageMap["Friends"];
            }
        }
        public static string Photos
        {
            get 
            {
                return _languageMap["Photos"];
            }
        }
        public static string Likes
        {
            get 
            {
                return _languageMap["Likes"];
            }
        }
        public static string Followers
        {
            get 
            {
                return _languageMap["Followers"];
            }
        }
        public static string Following
        {
            get 
            {
                return _languageMap["Following"];
            }
        }
        public static string ActivityLog
        {
            get 
            {
                return _languageMap["Activity Log"];
            }
        }
        public static string AddFriend
        {
            get 
            {
                return _languageMap["Add Friend"];
            }
        }
        public static string Message
        {
            get
            {
                return _languageMap["Message"];
            }
        }
        public static string Follow
        {
            get
            {
                return _languageMap["Follow"];
            }
        }
        public static string More
        {
            get
            {
                return _languageMap["More"];
            }
        }
        public static string CurrentCity
        {
            get
            {
                return _languageMap["Current City"];
            }
        }
        public static string HomeTown
        {
            get
            {
                return _languageMap["Hometown"];
            }
        }
    }
}

