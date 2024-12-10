using CommandLine;
using Deref;
using Deref.Options;
using Deref.Programs;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.UseCases;

// TODO: Figure out why the console app is not respecting the launchSettings.json environment variable
//Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
Environment.CurrentDirectory = AppContext.BaseDirectory;
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Add appsettings.json settings
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureInfrastructureLogging()
    .ConfigureServices((context, services) =>
    {
        var assembly = typeof(Program).Assembly;

        services
            .AddInfrastructure(assembly)
            .AddUseCases(assembly) 
            .AddDerefServices(context);
    });

using var host = builder.Build();
var parser = host.Services.GetRequiredService<Parser>();
var mediator = host.Services.GetRequiredService<IMediator>();

// parse the command line arguments and call appropriate handler
parser.ParseArguments<InitializeOptions, BranchOptions, ProjectOptions, WixOptions>(args)
    .WithParsed<InitializeOptions>(RunInitProgram)
    .WithParsed<BranchOptions>(RunBranchProgram)
    .WithParsed<ProjectOptions>(RunProjectProgram)
    .WithParsed<WixOptions>(RunWixProgram);

return;

// helper methods
void RunInitProgram(InitializeOptions options)
{
    var request = new Initialize.Request
    {
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

void RunProjectProgram(ProjectOptions options)
{
    var request = new Project.Request
    {
        BranchFilter = ConvertToResultFilter(options),
        ProjectName = options.ProjectName,
        IsListReferences = options.IsListReferences,
        IsListReferencedBy = options.IsListReferencedBy,
        IsListWixProjects = options.IsListWixProjects,
        IsListSolutions = options.IsListSolutions,
        IsListBuildDefinitions = options.IsListBuildDefinitions,
        IsList = options.IsList,
        SearchTerm = options.SearchTerm,
        ShowListCounts = options.ShowListCounts,
        ShowListTodos = options.ShowListTodos,
        IsRecursive = options.IsRecursive,
        IsExcludeTests = options.IsExcludeTests,
    };

    SendRequest(request);
}

void RunWixProgram(WixOptions options)
{
    var request = new Wix.Request();
    SendRequest(request);
}

FilterType ConvertToResultFilter(IListOptions options)
{
    return FilterType.OnlyRequired;
    /*
    return options switch
    {
        {IsIncludeAll: true} => FilterType.All,
        {IsIncludeOnlyRequired: true} => FilterType.OnlyRequired,
        {IsIncludeOnlyNonRequired: true} => FilterType.OnlyNonRequired,
        _ => FilterType.OnlyRequired // meaningful default!
    };
*/
}

void SendRequest<TRequest>(TRequest request) where TRequest : IRequest<string>
{
    var result = mediator.Send(request)
        .ConfigureAwait(false)
        .GetAwaiter()
        .GetResult();

    Console.WriteLine(result);
}
