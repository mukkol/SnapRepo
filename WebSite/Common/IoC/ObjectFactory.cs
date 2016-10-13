using System;
using System.Threading;
using System.Web.Mvc;
using SnapRepo.Azure;
using SnapRepo.Backups;
using SnapRepo.Scheduling;
using StructureMap;
using StructureMap.Graph;

namespace SnapRepo.Common.IoC
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
                x.For<JobPersistor>().Use<JobPersistor>().Singleton();
                x.For<ScheduledJobRegistry>().Use<ScheduledJobRegistry>();
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