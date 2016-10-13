using System.Web.Helpers;

namespace SnapRepo.Common
{
    public class ManagerSettings
    {
        public string LocalRepositoryPath { get; set; }
        public string DatabaseName { get; set; }
        public string AppDataFolder { get; set; }
        public string AzureRepositoryUrl { get; set; }
        public string ContainerName { get; set; }
        public string DatabaseOwner { get; set; }
        public string DbConnectionString { get; set; }
        public string BlobStorageConnectionString { get; set; }
        public string DatabaseServerName { get; set; }
        public string IisSiteName { get; set; }
        public string DbSharedBackupFolder { get; set; }

        public override string ToString()
        {
            return Json.Encode(this);
        }
    }
}