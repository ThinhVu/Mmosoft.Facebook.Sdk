using System;
using System.Text;
using System.Linq;
using FacebookAPI;

namespace Test
{
    class Program
    {
        public static string UserId = "your facebook username, email or phone number";
        public static string Password = "your facebook password";
        public static FacebookClient fc = new FacebookClient(UserId, Password);

        public static void Main()
        {
            SetUpEnvironment();

            GetReviewsTest();

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
            fc.PostToWall(DateTime.Now.ToString() + " - https://news.google.com\r\nPost to wall test\r\nMultiline.");
        }

        public static void PostToGroupTest()
        {
            // Post to group
            fc.PostToGroup(groupId: "1580910895550572", message: "Send from API - 093146");
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

        public static void GetGroupInfoTest()
        {
            var gi = fc.GetGroupInfo("184155705105717");
            Console.WriteLine();
        }        

        public static void GetReviewsTest()
        {
            try
            {
                var reviews = fc.GetReviews("Phieukyyeu");
                foreach (var rv in reviews)
                {
                    Console.WriteLine(rv.DisplayName);
                    Console.WriteLine(rv.RateScore);
                    Console.WriteLine(rv.Content);
                    Console.WriteLine(new string('-', 40));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }                       
        }
    }
}
