using System;
using System.Web.Mvc;
using StructureMap;
using MvcSiteMapProvider_ExternalResources.DI;
using MvcSiteMapProvider_ExternalResources.DI.StructureMap;
using MvcSiteMapProvider_ExternalResources.DI.StructureMap.Registries;

internal class CompositionRoot
{
    public static IDependencyInjectionContainer Compose()
    {
// Create the DI container
        var container = new Container();

// Setup configuration of DI
        container.Configure(r => r.AddRegistry<MvcSiteMapProviderRegistry>());

#if DEBUG
        container.AssertConfigurationIsValid();
#endif

// Return our DI container wrapper instance
        return new StructureMapDependencyInjectionContainer(container);
    }
}
