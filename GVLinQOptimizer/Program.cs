using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using GVLinQOptimizer.DependencyInjection;
using GVLinQOptimizer.Options;
using GVLinQOptimizer.Programs;
using MediatR;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;

services.AddCustomDesignerParsers();
services.AddHandlebarsTemplateSupport();

services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(Program).Assembly));

using var host = builder.Build();
var mediator = host.Services.GetRequiredService<IMediator>();

// parse the command line arguments and call appropriate handler
Parser.Default.ParseArguments<InitializeOptions, ExtractDTOOptions, CreateRepositoryOptions, CreateUnitTestsOptions>(args)
    .WithParsed<InitializeOptions>(InitializeSettingsFile)
    .WithParsed<ExtractDTOOptions>(GenerateDTOTypes)
    .WithParsed<CreateRepositoryOptions>(GenerateRepositoryClass)
    .WithParsed<CreateUnitTestsOptions>(GenerateUnitTestFile);

return;

// helper methods
void InitializeSettingsFile(InitializeOptions options)
{
    SendRequest(new Initialize.Request
    {
        DbmlFilePath = options.DbmlFilePath,
        SettingsFilePath = options.SettingsFilePath,
        ForceOverwrite = options.ForceOverwrite
    });
}

void GenerateDTOTypes(ExtractDTOOptions options)
{
    SendRequest(new ExtractDTOs.Request
    {
        SettingsFilePath = options.SettingsFilePath,
        OutputFileOrDirectory = options.OutputFileOrDirectory,
    });
}

void GenerateRepositoryClass(CreateRepositoryOptions options)
{
    SendRequest(new CreateRepository.Request
    {
        SettingsFilePath = options.SettingsFilePath,
        OutputDirectory = options.OutputDirectory,
        MethodName = options.MethodName
    });
}

void GenerateUnitTestFile(CreateUnitTestsOptions options)
{
    SendRequest(new CreateUnitTests.Request
    {
        SettingsFilePath = options.DesignerFilePath,
        OutputDirectory = options.OutputDirectory
    });
}


void SendRequest<TRequest>(TRequest request) where TRequest : IRequest<string>
{
    var result = mediator.Send(request).ConfigureAwait(false).GetAwaiter().GetResult();
    Console.WriteLine(result);
}
