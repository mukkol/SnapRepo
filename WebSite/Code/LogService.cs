using System;
using System.IO;
using System.Text;

namespace AzureBackupManager.Code
{
    public class LogService
    {
        private readonly string _localFolderPath;

        public LogService(string localFolderPath)
        {
            _localFolderPath = localFolderPath;
        }
        public void WriteLog(string message)
        {
            StreamWriter file = new StreamWriter(GetFilename(), true, Encoding.UTF8);
            file.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {message}");
            file.Close();

        }
        public string ReadLog()
        {
            using (StreamReader file = new StreamReader(GetFilename(), Encoding.UTF8))
            {
                var s = file.ReadToEnd();
                file.Close();
                return s;
            }
        }
        public string GetFilename()
        {
            return _localFolderPath + $"{DateTime.Now.ToString("yyyy-MM-dd")}_log.txt";
        }
    }
}