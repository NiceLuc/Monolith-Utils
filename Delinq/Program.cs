using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using Delinq.DependencyInjection;
using Delinq.Options;
using Delinq.Programs;
using MediatR;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;

services.AddCustomDesignerParsers();
services.AddHandlebarsTemplateSupport();

services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(Program).Assembly));

using var host = builder.Build();
var mediator = host.Services.GetRequiredService<IMediator>();

// parse the command line arguments and call appropriate handler
Parser.Default.ParseArguments<InitializeOptions, CreateRepositoryOptions, CreateUnitTestsOptions>(args)
    .WithParsed<InitializeOptions>(InitializeSettingsFile)
    .WithParsed<CreateRepositoryOptions>(GenerateRepositoryFiles)
    .WithParsed<CreateUnitTestsOptions>(GenerateUnitTestFile);

return;

// helper methods
void InitializeSettingsFile(InitializeOptions options)
{
    var request = new Initialize.Request
    {
        DbmlFilePath = options.DbmlFilePath,
        SettingsFilePath = options.SettingsFilePath,
        ForceOverwrite = options.ForceOverwrite
    };

    SendRequest(request);
}

void GenerateRepositoryFiles(CreateRepositoryOptions options)
{
    var request = new CreateRepositoryFiles.Request
    {
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
        SettingsFilePath = options.SettingsFilePath,
        OutputDirectory = options.OutputDirectory,
        MethodName = options.MethodName
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