using System;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Mmosoft.Facebook.Sdk.Common;

namespace Mmosoft.Facebook.Sdk.Test
{
    class Program
    {
        public static string UserId = "";
        public static string Password = "";

        public static void Main()
        {
            // Setup
            Console.OutputEncoding = Encoding.UTF8;

            // Test function
            GetFriendTest();

            // Done
            Console.WriteLine(" -- Done-- ");
            Console.ReadLine();
        }

        public static void JoinGroupTest()
        {
            try
            {
                var fc = new FacebookClient(UserId, Password);
                // Join or cancel request to group HocVienYT
                fc.JoinGroup("HocVienYT");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Message : " + ex.Message);
                Console.WriteLine("StackTrace : " + ex.StackTrace);
                Console.WriteLine("TargetSite : " + ex.TargetSite);
                Console.WriteLine("Source : " + ex.Source);
            }

        }

        public static void LeaveGroupTest()
        {
            try
            {
                var fc = new FacebookClient(UserId, Password);                
                fc.LeaveGroup("426581264196328", false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Message : " + ex.Message);
                Console.WriteLine("StackTrace : " + ex.StackTrace);
                Console.WriteLine("TargetSite : " + ex.TargetSite);
                Console.WriteLine("Source : " + ex.Source);
            }
        }

        public static void LikePageTest()
        {
            // Like or Dislike fanpage Takeit.me
            try
            {
                var fc = new FacebookClient(UserId, Password);
                fc.LikePage("takeit.me");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Message : " + ex.Message);
                Console.WriteLine("StackTrace : " + ex.StackTrace);
                Console.WriteLine("TargetSite : " + ex.TargetSite);
                Console.WriteLine("Source : " + ex.Source);
            }

        }

        public static void PostToWallTest()
        {
            try
            {
                var fc = new FacebookClient(UserId, Password);
                fc.PostToWall(DateTime.Now.ToString() + "\r\nhttps://news.google.com\r\nPost to wall test.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Message : " + ex.Message);
                Console.WriteLine("StackTrace : " + ex.StackTrace);
                Console.WriteLine("TargetSite : " + ex.TargetSite);
                Console.WriteLine("Source : " + ex.Source);
            }
        }

        public static void PostToGroupTest()
        {
            try
            {
                var fc = new FacebookClient(UserId, Password);
                // Post to group
                fc.PostToGroup(groupId: "1580910895550572", message: "Send from API - 093146");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Message : " + ex.Message);
                Console.WriteLine("StackTrace : " + ex.StackTrace);
                Console.WriteLine("TargetSite : " + ex.TargetSite);
                Console.WriteLine("Source : " + ex.Source);
            }
        }

        public static void TestGetReview()
        {
            try
            {
                var fc = new FacebookClient(UserId, Password);
                var reviewInfo = fc.GetReviewInfo("ArduinoCommunityVN");
                Console.WriteLine("page id :" + reviewInfo.PageId);
                foreach (var item in reviewInfo.Reviews)
                {
                    Console.WriteLine("user id :" +  item.UserId);
                    Console.WriteLine("user name :"  +item.UserDisplayName);
                    Console.WriteLine("content :" + item.Content);
                    Console.WriteLine("rate sccore :" + item.RateScore);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Message : " + ex.Message);
                Console.WriteLine("StackTrace : " + ex.StackTrace);
                Console.WriteLine("TargetSite : " + ex.TargetSite);
                Console.WriteLine("Source : " + ex.Source);
            }

        }

        public static void GetFriendTest()
        {
            try
            {
                var facebookClient = new FacebookClient(UserId, Password, new FileLog());

                var friends = facebookClient.GetUserInfo("100000792505718", true, true);
                // object -> json
                var result = new StringBuilder();
                // Open bracket
                result.Append("{");
                // append id
                result.Append("\"id\" : \"" + friends._id + "\"");
                // append friends
                if (friends.Friends != null)
                {
                    // append count
                    result.Append(", \"counts\" : " + friends.Friends.Count + ", \"friends\" : [");
                    foreach (var friend in friends.Friends)
                    {
                        result.Append("{ \"id\" : \"" + friend + "\"},");
                    }                        
                    result.Remove(result.Length - 1, 1);
                    result.Append("]");
                }
                // append close tag
                result.Append("}");

                // write to file data.txt
                File.WriteAllText("data.txt", result.ToString());
                Process.Start("notepad.exe", "data.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Message : " + ex.Message);
                Console.WriteLine("StackTrace : " + ex.StackTrace);
                Console.WriteLine("TargetSite : " + ex.TargetSite);
                Console.WriteLine("Source : " + ex.Source);
            }
        }

        public static void GetGroupInfoTest()
        {
            try
            {
                var fc = new FacebookClient(UserId, Password);
                var gi = fc.GetGroupInfo("529073513939720");
                Console.WriteLine("Group id : " + gi.Id);
                Console.WriteLine("Group name : " + gi.Name);
                Console.WriteLine("Group members : ");
                Console.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0,-20} | {1,-10} | {2, -30} |", "User Id", "Is admin", "Display name"));
                gi.Members.ToList().ForEach(member =>
                {
                    var display = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0,-20} | {1,-10} | {2, -30} |", member.UserId, member.IsAdmin, member.DisplayName);
                    Console.WriteLine(display);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Message : " + ex.Message);
                Console.WriteLine("StackTrace : " + ex.StackTrace);
                Console.WriteLine("TargetSite : " + ex.TargetSite);
                Console.WriteLine("Source : " + ex.Source);
            }
        }

        public static void GetPageIdFromAlias()
        {
            try
            {
                var fc = new FacebookClient(UserId, Password);                
                Console.WriteLine(fc.GetPageId("ArduinoCommunityVN"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Message : " + ex.Message);
                Console.WriteLine("StackTrace : " + ex.StackTrace);
                Console.WriteLine("TargetSite : " + ex.TargetSite);
                Console.WriteLine("Source : " + ex.Source);
            }

        }
    }
}