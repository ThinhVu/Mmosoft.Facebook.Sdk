namespace Mmosoft.Facebook.Utils
{
    using System;

    public interface ILogger : IDisposable
    {
        void WriteLine(string message);
    }
}
