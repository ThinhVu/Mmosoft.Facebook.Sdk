namespace Mmosoft
{
    using System;
    using System.Text;

    public class Program
    {
        static FClient fc = new FClient(YourAccount.UserId, YourAccount.Password);

        public static void Main()
        {            
            SetUpEnvironment();


            PostToWallTest();
            GetGroupMembersTest();


            Console.WriteLine(" -- Done-- ");
            Console.ReadLine();
        }

        public static void SetUpEnvironment()
        {
            Console.OutputEncoding = Encoding.UTF8;            
        }

        public static void JoinGroupTest()
        {
            // Join or cancel request to group HocVienYT
            fc.JoinGroupOrCancel("HocVienYT");
        }

        public static void LikePageTest()
        {
            // Like or Dislike fanpage Takeit.me
            fc.LikePageOrDislike("takeit.me");
        }

        public static void PostToWallTest()
        {
            // Post to wall
            fc.PostToWall("Testing retrieve group members info");
        }

        public static void PostToGroupTest()
        {
            // Post to group
            fc.PostToGroup(groupId : "1580910895550572", message : "Send from API - 093146");
        }

        public static void GetFriendTest()
        {                        
            // Note that this method does not work if you using your userId
            // Get friends of someone
            var friendIds = fc.GetFriends();
            foreach (var item in friendIds)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine(friendIds.Count);
        }

        public static void GetGroupMembersTest()
        {
            // C# developer
            var gms = fc.GetGroupMembers(groupId : "529073513939720");
            Console.WriteLine();           
        }

    }
}
