using System.Web.Helpers;

namespace AzureBackupManager.Code
{
    public class ManagerSettings
    {
        public string LocalFolderPath { get; set; }
        public string DatabaseName { get; set; }
        public string AppDataFolder { get; set; }
        public string DatabaseUser { get; set; }
        public string ContainerName { get; set; }
        public string DatabaseOwner { get; set; }
        public string DbConnectionString { get; set; }
        public string DatabaseServerName { get; set; }
        public bool DbExists { get; set; }

        public override string ToString()
        {
            return Json.Encode(this);
        }
    }
}