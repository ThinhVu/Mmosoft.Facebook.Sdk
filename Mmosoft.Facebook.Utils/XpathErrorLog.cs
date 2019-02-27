namespace Mmosoft.Facebook.Utils
{
    using System.IO;

    /// <summary>
    /// This class aim to log detail info about error to later investigate
    /// </summary>
    public class XpathErrorLog
    {
        private int mErrCounter;
        private string mLogFolder;

        /// <summary>
        /// Folder store xpath log
        /// </summary>
        /// <param name="logFolder"></param>
        public XpathErrorLog(string logFolder)
        {
            if (logFolder.EndsWith("\\"))
                mLogFolder = logFolder;
            else
                mLogFolder = logFolder + "\\";

            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
        }

        public string Log(string html, string xpath)
        {
            mErrCounter++;
            var content = string.Format("XPath: {0}{1}Html : {2}", xpath, System.Environment.NewLine, html);            
            var logFilePath = mLogFolder + mErrCounter + ".txt";
            File.WriteAllText(logFilePath, content);
            return logFilePath;
        }
    }
}
