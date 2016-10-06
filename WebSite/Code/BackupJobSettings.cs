using System.Web.Helpers;

namespace AzureBackupManager.Code
{
    public class BackupJobSettings
    {
        public string Name { get; set; }
        public int Interval { get; set; }
        public int AtHours { get; set; }
        public int AtMins { get; set; }
        public ManagerSettings ManagerSettins { get; set; }

        public override string ToString()
        {
            return Json.Encode(this);
        }
    }
}