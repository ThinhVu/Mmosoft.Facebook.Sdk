namespace Mmosoft.Facebook.Utils
{
    using System;

    public interface ILogger : IDisposable
    {
        void WriteLine(string message);
    }

    public static class LogCreator
    {
        public static ILogger Create()
        {
            return new FileLogger();
        }
    }
}
