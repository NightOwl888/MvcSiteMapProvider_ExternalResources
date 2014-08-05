using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Hosting;
using System.Reflection;
using StructureMap.Configuration.DSL;
using StructureMap.Pipeline;
using MvcSiteMapProvider;
using MvcSiteMapProvider.Builder;
using MvcSiteMapProvider.Caching;
using MvcSiteMapProvider.Security;
using MvcSiteMapProvider.Visitor;
using MvcSiteMapProvider.Web.Mvc;
using MvcSiteMapProvider.Web.UrlResolver;
using MvcSiteMapProvider.Xml;

using MvcSiteMapProvider.Globalization;
using System.Resources;

namespace MvcSiteMapProvider_ExternalResources.DI.StructureMap.Registries
{
    public class MvcSiteMapProviderRegistry
        : Registry
    {
        public MvcSiteMapProviderRegistry()
        {
            bool enableLocalization = true;
            string absoluteFileName = HostingEnvironment.MapPath("~/Mvc.sitemap");
            TimeSpan absoluteCacheExpiration = TimeSpan.FromMinutes(5);
            bool visibilityAffectsDescendants = true;
            bool useTitleIfDescriptionNotProvided = true;





            bool securityTrimmingEnabled = false;
            string[] includeAssembliesForScan = new string[] { "MvcSiteMapProvider_ExternalResources" };


            var currentAssembly = this.GetType().Assembly;
            var siteMapProviderAssembly = typeof(SiteMaps).Assembly;
            var allAssemblies = new Assembly[] { currentAssembly, siteMapProviderAssembly };
            var excludeTypes = new Type[] {
// Use this array to add types you wish to explicitly exclude from convention-based  
// auto-registration. By default all types that either match I[TypeName] = [TypeName] or 
// I[TypeName] = [TypeName]Adapter will be automatically wired up as long as they don't 
// have the [ExcludeFromAutoRegistrationAttribute].
//
// If you want to override a type that follows the convention, you should add the name 
// of either the implementation name or the interface that it inherits to this list and 
// add your manual registration code below. This will prevent duplicate registrations 
// of the types from occurring. 

// Example:
// typeof(SiteMap),
// typeof(SiteMapNodeVisibilityProviderStrategy)
                typeof(IStringLocalizer)
            };
            var multipleImplementationTypes = new Type[] {
                typeof(ISiteMapNodeUrlResolver),
                typeof(ISiteMapNodeVisibilityProvider),
                typeof(IDynamicNodeProvider)
            };

// Matching type name (I[TypeName] = [TypeName]) or matching type name + suffix Adapter (I[TypeName] = [TypeName]Adapter)
// and not decorated with the [ExcludeFromAutoRegistrationAttribute].
            CommonConventions.RegisterDefaultConventions(
                (interfaceType, implementationType) => this.For(interfaceType).Singleton().Use(implementationType),
                new Assembly[] { siteMapProviderAssembly },
                allAssemblies,
                excludeTypes,
                string.Empty);

// Multiple implementations of strategy based extension points (and not decorated with [ExcludeFromAutoRegistrationAttribute]).
            CommonConventions.RegisterAllImplementationsOfInterface(
                (interfaceType, implementationType) => this.For(interfaceType).Singleton().Use(implementationType),
                multipleImplementationTypes,
                allAssemblies,
                excludeTypes,
                string.Empty);

// Visibility Providers
            this.For<ISiteMapNodeVisibilityProviderStrategy>().Use<SiteMapNodeVisibilityProviderStrategy>()
                .Ctor<string>("defaultProviderName").Is(string.Empty);

// Pass in the global controllerBuilder reference
            this.For<ControllerBuilder>()
                .Use(x => ControllerBuilder.Current);

            this.For<IControllerTypeResolverFactory>().Use<ControllerTypeResolverFactory>()
                .Ctor<string[]>("areaNamespacesToIgnore").Is(new string[0]);

// Configure Security
            this.For<IAclModule>().Use<CompositeAclModule>()
                .EnumerableOf<IAclModule>().Contains(x =>
                {
                    x.Type<AuthorizeAttributeAclModule>();
                    x.Type<XmlRolesAclModule>();
                });

            // Configure localization

            // Fully qualified assembly.namespace.resourcefile (.resx) name without the extension
            string resourceBaseName = "SomeAssembly.Resources.Resource1";

            // A reference to the assembly where your resources reside.
            Assembly resourceAssembly = typeof(SomeAssembly.Class1).Assembly;

            // Register the ResourceManager (note that this is application wide - if you are 
            // using ResourceManager in your DI setup already you may need to use a named instance or SmartInstance)
            var resourceManager = this.For<ResourceManager>().Use(() => new ResourceManager(resourceBaseName, resourceAssembly));

            // Register the ResourceManagerStringLocalizer
            this.For<IStringLocalizer>().Use<ResourceManagerStringLocalizer>()
                .Ctor<ResourceManager>("resourceManager").Is(resourceManager);

// Setup cache
            SmartInstance<CacheDetails> cacheDetails;


            this.For<System.Runtime.Caching.ObjectCache>()
                .Use(s => System.Runtime.Caching.MemoryCache.Default);

            this.For(typeof(ICacheProvider<>)).Use(typeof(RuntimeCacheProvider<>));

            var cacheDependency =
                this.For<ICacheDependency>().Use<RuntimeFileCacheDependency>()
                    .Ctor<string>("fileName").Is(absoluteFileName);

            cacheDetails =
                this.For<ICacheDetails>().Use<CacheDetails>()
                    .Ctor<TimeSpan>("absoluteCacheExpiration").Is(absoluteCacheExpiration)
                    .Ctor<TimeSpan>("slidingCacheExpiration").Is(TimeSpan.MinValue)
                    .Ctor<ICacheDependency>().Is(cacheDependency);

// Configure the visitors
            this.For<ISiteMapNodeVisitor>()
                .Use<UrlResolvingSiteMapNodeVisitor>();


// Prepare for our node providers
            var xmlSource = this.For<IXmlSource>().Use<FileXmlSource>()
                           .Ctor<string>("fileName").Is(absoluteFileName);

            this.For<IReservedAttributeNameProvider>().Use<ReservedAttributeNameProvider>()
                .Ctor<IEnumerable<string>>("attributesToIgnore").Is(new string[0]);

// Register the sitemap node providers
            var siteMapNodeProvider = this.For<ISiteMapNodeProvider>().Use<CompositeSiteMapNodeProvider>()
                .EnumerableOf<ISiteMapNodeProvider>().Contains(x =>
                {
                    x.Type<XmlSiteMapNodeProvider>()
                        .Ctor<bool>("includeRootNode").Is(true)
                        .Ctor<bool>("useNestedDynamicNodeRecursion").Is(false)
                        .Ctor<IXmlSource>().Is(xmlSource);
                    x.Type<ReflectionSiteMapNodeProvider>()
                        .Ctor<IEnumerable<string>>("includeAssemblies").Is(includeAssembliesForScan)
                        .Ctor<IEnumerable<string>>("excludeAssemblies").Is(new string[0]);
                });

// Register the sitemap builders
            var builder = this.For<ISiteMapBuilder>().Use<SiteMapBuilder>()
                .Ctor<ISiteMapNodeProvider>().Is(siteMapNodeProvider);

// Configure the builder sets
            this.For<ISiteMapBuilderSetStrategy>().Use<SiteMapBuilderSetStrategy>()
                .EnumerableOf<ISiteMapBuilderSet>().Contains(x =>
                {
                    x.Type<SiteMapBuilderSet>()
                        .Ctor<string>("instanceName").Is("default")
                        .Ctor<bool>("securityTrimmingEnabled").Is(securityTrimmingEnabled)
                        .Ctor<bool>("enableLocalization").Is(enableLocalization)
                        .Ctor<bool>("visibilityAffectsDescendants").Is(visibilityAffectsDescendants)
                        .Ctor<bool>("useTitleIfDescriptionNotProvided").Is(useTitleIfDescriptionNotProvided)
                        .Ctor<ISiteMapBuilder>().Is(builder)
                        .Ctor<ICacheDetails>().Is(cacheDetails);
                });
        }
    }
}
