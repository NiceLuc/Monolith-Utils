using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using Microsoft.Extensions.Configuration;
using MonoUtils.Infrastructure;
using MonoUtils.UseCases;
using MediatR;
using MonoUtils.Delinq;
using MonoUtils.Delinq.Options;
using MonoUtils.Delinq.Programs;

// TODO: Figure out why the console app is not respecting the launchSettings.json environment variable
//Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Add appsettings.json settings
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        /*
        // Add Machine.config
        var machineConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            @"..\Microsoft.NET\Framework\v4.0.30319\Config\Machine.config");

        if (File.Exists(machineConfigPath)) 
            config.AddXmlFile(machineConfigPath, optional: false, reloadOnChange: true);

        // Add environment variables
        config.AddEnvironmentVariables();
        */

        // Add user secrets (only in local development)
        // TODO: if (context.HostingEnvironment.IsDevelopment())
            config.AddUserSecrets<Program>();
    })
    .ConfigureInfrastructureLogging()
    .ConfigureServices((context, services) =>
    {
        var assembly = typeof(Program).Assembly;

        services
            .AddInfrastructure(assembly)
            .AddUseCases(assembly)
            .AddDelinqServices(context);
    });

using var host = builder.Build();
var parser = host.Services.GetRequiredService<Parser>();
var mediator = host.Services.GetRequiredService<IMediator>();

// parse the command line arguments and call appropriate handler
parser.ParseArguments<InitializeOptions, CreateRepositoryOptions, CreateUnitTestsOptions, VerifyRepositoryMethodOptions, VerifyReportOptions>(args)
    .WithParsed<InitializeOptions>(InitializeSettingsFile)
    .WithParsed<CreateRepositoryOptions>(GenerateRepositoryFiles)
    .WithParsed<CreateUnitTestsOptions>(GenerateUnitTestFile)
    .WithParsed<VerifyRepositoryMethodOptions>(InitializeVerificationFile)
    .WithParsed<VerifyReportOptions>(GenerateVerificationReport);

return;

// helper methods
void InitializeSettingsFile(InitializeOptions options)
{
    var request = new Initialize.Request
    {
        ContextName = options.ContextName,
        BranchName = options.BranchName,
        DbmlFilePath = options.DbmlFilePath,
        DesignerFilePath = options.DesignerFilePath,
        SettingsFilePath = options.SettingsFilePath,
        ForceOverwrite = options.ForceOverwrite
    };

    SendRequest(request);
}

void GenerateRepositoryFiles(CreateRepositoryOptions options)
{
    var request = new CreateRepositoryFiles.Request
    {
        ContextName = options.ContextName,
        SettingsFilePath = options.SettingsFilePath,
        OutputDirectory = options.OutputDirectory,
        MethodName = options.MethodName
    };

    SendRequest(request);
}

void GenerateUnitTestFile(CreateUnitTestsOptions options)
{
    var request = new CreateUnitTests.Request
    {
        ContextName = options.ContextName,
        SettingsFilePath = options.SettingsFilePath,
        OutputDirectory = options.OutputDirectory,
        MethodName = options.MethodName
    };

    SendRequest(request);
}

void InitializeVerificationFile(VerifyRepositoryMethodOptions options)
{
    var request = new VerifyRepositoryMethods.Request
    {
        ContextName = options.ContextName,
        BranchName = options.BranchName,
        RepositoryFilePath = options.RepositoryFilePath,
        ConnectionString = options.ConnectionString,
        ValidationFilePath = options.ValidationFilePath,
        MethodName = options.MethodName,
        IsGenerateReport = options.IsGenerateReport,
        IsOpenReport = options.IsOpenReport
    };

    SendRequest(request);
}

void GenerateVerificationReport(VerifyReportOptions options)
{
    var request = new VerificationReport.Request
    {
        ContextName = options.ContextName,
        ValidationFilePath = options.ValidationFilePath,
        ReportFilePath = options.ReportFilePath,
        IsOpenReport = options.IsOpenReport
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