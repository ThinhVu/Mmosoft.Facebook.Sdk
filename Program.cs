namespace Mmosoft
{
    public class Program
    {
        public static void Main()
        {
            var fc = new FacebookClient("your email", "your password");

            // Join or cancel request to group HocVienYT
            // fc.JoinOrCancelGroup("HocVienYT");

            // Like or Dislike fanpage Takeit.me
            // fc.LikeOrDislikePage("takeit.me");

            // Post to wall
            // fc.PostWall("Hello from api - Demo test with special character !@#$%%^^&*(())");

            System.Console.WriteLine("Done");
            System.Console.ReadLine();
        }
    }
}
