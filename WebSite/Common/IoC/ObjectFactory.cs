using System;
using System.Threading;
using System.Web.Mvc;
using AzureBackupManager.Azure;
using AzureBackupManager.Backups;
using AzureBackupManager.Scheduling;
using StructureMap;
using StructureMap.Graph;

namespace AzureBackupManager.Common.IoC
{
    public static class ObjectFactory
    {
        private static readonly Lazy<Container> ContainerBuilder = new Lazy<Container>(DefaultContainer,
            LazyThreadSafetyMode.ExecutionAndPublication);

        public static IContainer Container => ContainerBuilder.Value;

        private static Container DefaultContainer()
        {
            return new Container(x =>
            {
                x.For<BackupService>().Use<BackupService>().Singleton();
                x.For<BlobStorageService>().Use<BlobStorageService>().Singleton();
                x.For<LogService>().Use<LogService>().Singleton();
                x.For<ScheduledJobService>().Use<ScheduledJobService>().Singleton();
                x.For<ScheduledJobPersistor>().Use<ScheduledJobPersistor>().Singleton();
                x.For<BackupRegistry>().Use<BackupRegistry>();
            });

        }


        public static void InitIoC()
        {
            Container.Configure(config =>
            {
                config.Scan(scan =>
                {
                    scan.TheCallingAssembly();
                    scan.WithDefaultConventions();
                });
            });
            DependencyResolver.SetResolver(new StructureMapDependencyResolver(Container));
        }
    }
}