using System.Collections.Generic;

namespace Mmosoft.Facebook.Sdk.Models
{
    public class UserInfo
    {       
        public string Id { get; set; }
        public string Alias { get; set; }
        public string DispayName { get; set; }
        public string AvatarUrl { get; set; }
        public List<string> Friends { get; set; }

        public UserInfo()
        {
            Friends = new List<string>(0);
        }
    }    
}
