﻿using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using AzureBackupManager.Azure;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace AzureBackupManager.Common
{
    public static class SettingsFactory
    {
        public const string BackupManagerConnectionStringName = "BackupManager";
        public const string AzureBlobStorageConnectionStringName = "AzureBackupBlobStorage";

        public static ManagerSettings CreateSettingsFromParamsOrDefault(NameValueCollection requestParams = null)
        {
            if (requestParams == null) requestParams = new NameValueCollection();
            var dBConnectionString = ConfigurationManager.ConnectionStrings[BackupManagerConnectionStringName]?.ConnectionString;
            var blobStorageConnectionString = BlobStorageService.BlobStorageConnectionString;
            var databaseName = requestParams["databaseName"]
                               ?? ConfigurationManager.AppSettings["BackupManager.DatabaseName"]
                               ?? new SqlConnectionStringBuilder(dBConnectionString).InitialCatalog;
            return new ManagerSettings()
            {
                LocalRepositoryPath = StaticLocalRepositoryPath,
                AppDataFolder = requestParams["appDataFolder"]
                                ?? ConfigurationManager.AppSettings["BackupManager.AppDataFolder"]
                                ?? TryGetEpiserverAppDataPath()
                                ?? "C:\\Path\\To\\AppData\\Folder",
                ContainerName = requestParams["containerName"]
                                ?? ConfigurationManager.AppSettings["BackupManager.ContainerName"]
                                ?? "backup-manager-repository",
                DatabaseOwner = requestParams["databaseOwner"]
                                ?? ConfigurationManager.AppSettings["BackupManager.DatabaseOwner"]
                                ?? GetDbOwner(databaseName, dBConnectionString)
                                ?? "DbOwner",
                DatabaseServerName = requestParams["databaseServerName"]
                                ?? ConfigurationManager.AppSettings["BackupManager.DatabaseServerName"]
                                ?? new SqlConnectionStringBuilder(dBConnectionString).DataSource,
                IisSiteName = requestParams["iisSiteName"]
                                ?? ConfigurationManager.AppSettings["BackupManager.IisSiteName"],
                DbSharedBackupFolder = requestParams["dbSharedBackupFolder"]
                                ?? ConfigurationManager.AppSettings["BackupManager.DbSharedBackupFolder"],
                DatabaseName = databaseName,
                DbConnectionString = dBConnectionString,
                BlobStorageConnectionString = blobStorageConnectionString,
                AzureRepositoryUrl = BlobStorageService.GetBlobStorageUri(blobStorageConnectionString),
            };
        }


        public static string StaticLocalRepositoryPath =>  ConfigurationManager.AppSettings["BackupManager.LocalRepositoryPath"]
                                                        ?? (new DirectoryInfo(HttpContext.Current.Server.MapPath("~")).Parent?.Parent?.FullName ?? "C:\\temp") 
                                                            + "\\BackupManagerRepository\\";
        public static bool CheckUserGroups => bool.Parse(ConfigurationManager.AppSettings["BackupManager.CheckUserGroups"] ?? "True");
        public static bool UseBasicAuth => bool.Parse(ConfigurationManager.AppSettings["BackupManager.UseBasicAuth"] ?? "True");


        public static bool DatabaseExists(string dbConnectionString, string databaseName)
        {
            if(string.IsNullOrEmpty(databaseName) || string.IsNullOrEmpty(dbConnectionString))
                return false;
            var connStrBuilder = new SqlConnectionStringBuilder(dbConnectionString);
            connStrBuilder.ConnectTimeout = 3;
            try
            {
                Server server = new Server(new ServerConnection(new SqlConnection(connStrBuilder.ConnectionString)));
                return server.Databases[databaseName] != null;
            }
            catch (ConnectionFailureException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
        }

        public static string GetDbOwner(string databaseName, string dbConnectionString)
        {
            var connStrBuilder = new SqlConnectionStringBuilder(dbConnectionString);
            connStrBuilder.ConnectTimeout = 3;
            try
            {
                Server server = new Server(new ServerConnection(new SqlConnection(connStrBuilder.ConnectionString)));
                var database = server.Databases[databaseName];
                return database?.Owner;
            }
            catch (ConnectionFailureException) { return null; }
            catch (IndexOutOfRangeException) { return null; }
        }

        /// <summary>
        /// Tries to find episerver configuration but if it's nor referenced it will return null;
        /// </summary>
        public static string TryGetEpiserverAppDataPath()
        {
            Type type = Type.GetType("EPiServer.Framework.Configuration.EPiServerFrameworkSection, EPiServer.Framework");
            Type appDataType = Type.GetType("EPiServer.Framework.Configuration.AppDataElement, EPiServer.Framework");
            var instance = type?.GetProperty("Instance").GetValue(null);
            var appDataInstance = type?.GetProperty("AppData")?.GetValue(instance);
            return appDataType?.GetProperty("BasePath")?.GetValue(appDataInstance) as string;
            //return EPiServer.Framework.Configuration.EPiServerFrameworkSection.Instance.AppData.BasePath;
        }
    }
}