using System.IO;
using System.Text;
using System.Web.Helpers;
using AzureBackupManager.Common;

namespace AzureBackupManager.Scheduling
{
    public class ScheduledJobPersistor
    {
        private readonly string _localFolderPath;
        
        public ScheduledJobPersistor()
        {
            _localFolderPath = SettingsFactory.GetStaticLocalFolderPath();
        }

        public void Store(BackupJobSettings[] backupJobs)
        {
            StreamWriter file = new StreamWriter(_localFolderPath + "ScheduledJobsRepository.json", false, Encoding.UTF8);
            file.WriteLine(Json.Encode(backupJobs));
            file.Close();
        }

        public BackupJobSettings[] GetAll()
        {
            try
            {
                StreamReader file = new StreamReader(_localFolderPath + "ScheduledJobsRepository.json", Encoding.UTF8);
                var str = file.ReadToEnd();
                var s = Json.Decode<BackupJobSettings[]>(str);
                file.Close();
                return s ?? new BackupJobSettings[] {};
            }
            catch (FileNotFoundException) { return new BackupJobSettings[] { }; }
        }

    }

}