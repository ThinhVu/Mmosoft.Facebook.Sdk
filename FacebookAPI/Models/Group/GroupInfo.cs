using System.Collections.Generic;

namespace FacebookAPI.Models.Group
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
}
