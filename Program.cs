namespace Mmosoft
{
    using System;

    public class Program
    {
        public static void Main()
        {
            var fc = new FacebookClient(Account.Username, Account.Password);

            // Join or cancel request to group HocVienYT
            fc.JoinOrCancelGroup("HocVienYT");

            // Like or Dislike fanpage Takeit.me
            fc.LikeOrDislikePage("takeit.me");

            // Post to wall
            fc.PostWall("Hello from api - Demo test with special character !@#$%%^^&*(())");

            // Post to group
            fc.PostGroup(groupId : "1580910895550572", message : "Send from API - 093146");

            Console.WriteLine("Done");

            Console.ReadLine();
        }
    }
}
