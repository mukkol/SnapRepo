using System;
using System.Data.SqlClient;
using System.IO;
using SnapRepo.Common;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SnapRepo.Backups
{
    public class DbBackupService
    {
        private readonly LogService _logService;

        public DbBackupService(LogService logService)
        {
            _logService = logService;
        }

        public void RestoreDbBackupToSqlServer(ManagerSettings settings, string dbName, string packageFolderName, string databaseFileName)
        {
            string dbBackupFileLocalRepositoryPath = settings.LocalRepositoryPath + packageFolderName + databaseFileName;

            string dbBackupFolderFullPath = GetDbBackupFolderFullPath(settings);
            bool useSharedRestoreFolder = dbBackupFolderFullPath != settings.LocalRepositoryPath;

            string dbBackupFileRestoreFullPath = useSharedRestoreFolder
                                            ? dbBackupFolderFullPath + databaseFileName
                                            : dbBackupFileLocalRepositoryPath;
            if (useSharedRestoreFolder)
            {   //Backup folder is not in local repository folder
                _logService.WriteLog($"Moving DB to Restore Folder ({dbBackupFileRestoreFullPath})");
                if(File.Exists(dbBackupFileRestoreFullPath))
                    File.Delete(dbBackupFileRestoreFullPath);
                File.Move(dbBackupFileLocalRepositoryPath, dbBackupFileRestoreFullPath);
            }
            _logService.WriteLog($"Restoring DB from {dbBackupFileRestoreFullPath}");
            string restoreInfo;
            RestoreDb(settings.DbConnectionString, settings.DatabaseName, dbBackupFileRestoreFullPath, out restoreInfo);
            _logService.WriteLog(restoreInfo);
            File.Delete(dbBackupFileRestoreFullPath);
        }

        public string BackupDbToLocalRepository(ManagerSettings settings)
        {
            string infoMessage;
            string dbBackupFolderFullPath = GetDbBackupFolderFullPath(settings);
            string dbBackupFileName = GetDbBackupFileName(settings.DatabaseName);
            string dbBackupFileFullPath = dbBackupFolderFullPath + dbBackupFileName;
            _logService.WriteLog($"Backing up DB to {dbBackupFileFullPath}");
            BackupDb(settings.DbConnectionString, settings.DatabaseName, dbBackupFileFullPath, out infoMessage);
            _logService.WriteLog(infoMessage);
            if (dbBackupFolderFullPath != settings.LocalRepositoryPath)
            {
                string dbBackupFileLocalRepositoryPath = settings.LocalRepositoryPath + dbBackupFileName;
                _logService.WriteLog($"Moving DB to LocalRepositoryPath ({dbBackupFileLocalRepositoryPath})");
                File.Move(dbBackupFileFullPath, dbBackupFileLocalRepositoryPath);
            }
            return dbBackupFileName;
        }

        public void BackupDb(string dbConnectionString, string dbName, string dbBackupFileFullPath, out string infoMessage)
        {
            string info = "";
            var scsb = new SqlConnectionStringBuilder(dbConnectionString);
            scsb.InitialCatalog = "master";
            using (var connection = new SqlConnection(scsb.ConnectionString))
            {
                connection.InfoMessage += (sender, args) => info += args.Message + Environment.NewLine;
                var query = $"BACKUP DATABASE {dbName} TO DISK='{dbBackupFileFullPath}' WITH COPY_ONLY";

                using (var command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = 0;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            infoMessage = info;
        }

        public void RestoreDb(string dbConnectionString, string dbName, string dbBackupFileFullPath, out string infoMessage)
        {
            string info = "";
            var scsb = new SqlConnectionStringBuilder(dbConnectionString);
            scsb.InitialCatalog = "master";
            using (var connection = new SqlConnection(scsb.ConnectionString))
            {
                connection.InfoMessage += (sender, args) => info += args.Message + Environment.NewLine;
                connection.Open();
                bool exists = (int)(new SqlCommand($"SELECT count(*) FROM master.dbo.sysdatabases where name = '{dbName}'", connection).ExecuteScalar()) > 0;
                //kick all users out (alias close connections) before restore.
                if (exists) { new SqlCommand($"ALTER DATABASE {dbName} SET Single_User WITH Rollback Immediate", connection).ExecuteNonQuery(); }
                string moveDbFolder = exists ? null : GetServerDefaultDatabaseFolderPath(connection);
                try
                {
                    string withCmd= moveDbFolder == null ? "RECOVERY" : $"MOVE '{dbName}' TO '{moveDbFolder}{dbName}.mdf', MOVE '{dbName}_Log' TO '{moveDbFolder}{dbName}_Log.LDF', RECOVERY";
                    var command = new SqlCommand( $"RESTORE DATABASE {dbName} FROM DISK='{dbBackupFileFullPath}' WITH {withCmd}",connection) {CommandTimeout = 0};
                    _logService.WriteLog("Restore command:" + command.CommandText);
                    command.ExecuteNonQuery();
                }
                finally
                {
                    if (exists) { new SqlCommand($"ALTER DATABASE {dbName} SET Multi_User", connection).ExecuteNonQuery(); }
                }
                connection.Close();
            }
            infoMessage = info;
        }

        public void SetDbOwner(ManagerSettings settings, string dbName)
        {
            if (string.IsNullOrEmpty(settings.DatabaseOwner))
                return;
            Server server = new Server(new ServerConnection(new SqlConnection(settings.DbConnectionString)));
            Database database = server.Databases[dbName];
            try
            {
                database.SetOwner(settings.DatabaseOwner, true);
                database.Refresh();
            }
            catch (SmoException) { _logService.WriteLog($"User ({settings.DatabaseOwner}) does not exists in SQL Server!"); }
        }

        private static string GetDbBackupFolderFullPath(ManagerSettings settings)
        {
            return !string.IsNullOrEmpty(settings.DbSharedBackupFolder)
                ? settings.DbSharedBackupFolder
                : settings.LocalRepositoryPath;
        }

        private static string GetServerDefaultDatabaseFolderPath(SqlConnection connection)
        {
            return new SqlCommand("SELECT SUBSTRING(physical_name, 1, CHARINDEX(N'master.mdf', LOWER(physical_name)) - 1) FROM master.sys.master_files WHERE database_id = 1 AND file_id = 1", connection).ExecuteScalar() as string;
        }

        private static string GetDbBackupFileName(string dbName)
        {
            return $"{dbName}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{Environment.MachineName}_db.bak";
        }
    }
}