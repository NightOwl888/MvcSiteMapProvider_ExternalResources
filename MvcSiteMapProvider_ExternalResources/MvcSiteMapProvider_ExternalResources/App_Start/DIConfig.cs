using System;
using System.Web.Mvc;
using MvcSiteMapProvider_ExternalResources.DI;


[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(DIConfig), "Register")]



public class DIConfig
{
    public static void Register()
    {


        var container = CompositionRoot.Compose();

#if DependencyResolver
// ************************************************************************************** //
//  Dependency Resolver
//
//  You may use this option if you are using MVC 3 or higher and you have other code
//  that references DependencyResolver.Current.GetService() or DependencyResolver.Current.GetServices()
//
// ************************************************************************************** //

// Reconfigure MVC to use Service Location
        var dependencyResolver = new InjectableDependencyResolver(container, DependencyResolver.Current);
        DependencyResolver.SetResolver(dependencyResolver);
#else
// ************************************************************************************** //
//  Controller Factory
//
//  It is recommended to use Controller Factory unless you are getting errors due to a conflict.
//
// ************************************************************************************** //

// Reconfigure MVC to use DI
        var controllerFactory = new InjectableControllerFactory(container);
        ControllerBuilder.Current.SetControllerFactory(controllerFactory);
#endif

        MvcSiteMapProviderConfig.Register(container);

    }
}

