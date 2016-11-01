using System.Collections.Generic;

namespace Mmosoft.Facebook.Sdk.Models
{
    public class UserInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class FriendInfo
    {
        public string UserId { get; set; }
        public List<UserInfo> Friends { get; set; }
    }
}
