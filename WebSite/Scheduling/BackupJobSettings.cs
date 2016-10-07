using System.Web.Helpers;
using AzureBackupManager.Common;

namespace AzureBackupManager.Scheduling
{
    public class BackupJobSettings
    {
        public string Name { get; set; }
        public int Interval { get; set; }
        public int AtHours { get; set; }
        public int AtMins { get; set; }
        public ManagerSettings ManagerSettins { get; set; }
        public string Query { get; set; }

        public override string ToString()
        {
            return Json.Encode(this);
        }
    }
}