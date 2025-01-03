﻿using System.Reflection;
using SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonoUtils.Domain;
using Mustache;
using Serilog;

namespace MonoUtils.Infrastructure;

public static class DependencyInjection
{
    public static IHostBuilder ConfigureInfrastructureLogging(this IHostBuilder host) =>
        host.UseSerilog((context, services, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}");
        });

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, Assembly assembly)
    {
        services.AddTransient<ScopeTracker>();
        services.AddTransient<UniqueNameResolver>();

        // file system persistence utilities
        services.AddSingleton<IFileStorage, FileStorage>();

        // embedded resource utilities
        services.AddSingleton<IEmbeddedResourceProvider>(_
            => new EmbeddedResourceProvider(assembly));

        // handlebars template support
        services.AddSingleton<FormatCompiler>();
        services.AddSingleton<ITemplateEngine, HandlebarsTemplateEngine>();

        return services;
    }
}