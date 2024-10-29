// TODO: Figure out why the console app is not respecting the launchSettings.json environment variable
//Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

using CommandLine;
using Deref;
using Deref.Options;
using Deref.Programs;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Add appsettings.json settings
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddDerefServices(context);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(Program).Assembly));
    });

using var host = builder.Build();
var mediator = host.Services.GetRequiredService<IMediator>();

// parse the command line arguments and call appropriate handler
Parser.Default.ParseArguments<InitializeOptions>(args)
    .WithParsed<InitializeOptions>(InitializeSettingsFile);

return;

// helper methods
void InitializeSettingsFile(InitializeOptions options)
{
    var request = new Initialize.Request
    {
        BranchName = options.BranchName,
        ResultsDirectoryPath = options.ResultsDirectoryPath,
        ForceOverwrite = options.ForceOverwrite
    };

    SendRequest(request);
}

void SendRequest<TRequest>(TRequest request) where TRequest : IRequest<string>
{
    var result = mediator.Send(request)
        .ConfigureAwait(false)
        .GetAwaiter()
        .GetResult();

    Console.WriteLine(result);
}
