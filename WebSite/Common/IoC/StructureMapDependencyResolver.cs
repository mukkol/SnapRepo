using System;
using System.Collections.Generic;
using System.Web.Mvc;
using StructureMap;

namespace AzureBackupManager.Common.IoC
{
    public class StructureMapDependencyResolver : IDependencyResolver
    {
        private readonly IContainer _container;
        public StructureMapDependencyResolver(IContainer container)
        {
            _container = container;
        }
        public object GetService(Type serviceType)
        {
            if (serviceType.IsAbstract || serviceType.IsInterface)
                return _container.TryGetInstance(serviceType);

            return _container.GetInstance(serviceType);
        }
        public IEnumerable<object> GetServices(Type serviceType)
        {
            foreach (object obj in _container.GetAllInstances(serviceType))
                yield return obj;
        }
        public void Dispose()
        {
            _container.Dispose();
        }
    }
}