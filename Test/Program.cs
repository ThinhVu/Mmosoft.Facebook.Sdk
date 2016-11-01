using System;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;

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

        public static void GetFriendTest()
        {
            try
            {
                var facebookClient = new FacebookClient(UserId, Password);                      

                var friends = facebookClient.GetFriendInfo();
                // object -> json
                var result = new StringBuilder("{ \"id\" : \"" + friends.UserId + "\", \"friends\" : [");
                foreach (var friend in friends.Friends)
                    result.Append("{ \"id\" : \"" + friend.Id + "\", \"name\" : \"" + friend.Name + "\"},");
                result.Remove(result.Length - 1, 1);
                result.Append("], \"counts\" : " + friends.Friends.Count + "}");

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
    }
}