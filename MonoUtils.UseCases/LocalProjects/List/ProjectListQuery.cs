using MediatR;
using MonoUtils.Domain.Data;
using MonoUtils.Domain.Data.Queries;
using SharedKernel;

namespace MonoUtils.UseCases.LocalProjects.List;

public class ProjectListQuery : ListQuery, IRequest<Result<IEnumerable<ProjectDTO>>>
{
    public TodoFilterType TodoFilter { get; set; }
}