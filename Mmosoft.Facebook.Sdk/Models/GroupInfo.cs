using System.Collections.Generic;

namespace Mmosoft.Facebook.Sdk.Models
{
    public class GroupInfo
    {      
        public string Id { get; set; }
        public string Name { get; set; }
        public List<GroupMember> Members { get; set; }

        public GroupInfo()
        {
            Id = string.Empty;
            Name = string.Empty;
            Members = new List<GroupMember>();
        }
    }

    public class GroupMember
    {
        public bool IsAdmin { get; set; }
        public string UserId { get; set; }
        public string DisplayName { get; set; }
    }
}
