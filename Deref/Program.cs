using CommandLine;
using Deref;
using Deref.Options;
using Deref.Programs;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

// TODO: Figure out why the console app is not respecting the launchSettings.json environment variable
//Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Add appsettings.json settings
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .UseSerilog((context, services, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}");
    })
    .ConfigureServices((context, services) =>
    {
        services.AddDerefServices(context);
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(Program).Assembly));
    });

using var host = builder.Build();
var mediator = host.Services.GetRequiredService<IMediator>();

// parse the command line arguments and call appropriate handler
// Common Arguments:
// 0: The name of the branch which has the solutions you want to analyze.
// -f, --force: Set this to true to overwrite existing files when the directory already exists.
// -s, --solution: The name of the solution file you want to parse 
// -p, --project: The name of the project file you want to parse
// -r, --report: The name of the report file you want to generate
// -x, --open: Open the report file after it is generated
Parser.Default.ParseArguments<InitializeOptions, BranchOptions>(args)
    .WithParsed<InitializeOptions>(RunInitProgram)
    .WithParsed<BranchOptions>(RunBranchProgram);

return;

// helper methods
void RunInitProgram(InitializeOptions options)
{
    var request = new Initialize.Request
    {
        BranchName = options.BranchName,
        ResultsDirectoryPath = options.ResultsDirectoryPath,
        ForceOverwrite = options.ForceOverwrite
    };

    SendRequest(request);
}

void RunBranchProgram(BranchOptions options)
{
    var request = new Branch.Request
    {
        BranchName = options.BranchName,
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
