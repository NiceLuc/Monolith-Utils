using MediatR;
using MonoUtils.Domain.Data;
using MonoUtils.Domain.Data.Queries;
using SharedKernel;

namespace MonoUtils.UseCases.LocalProjects;

public static class ProjectList
{
    public class Query : ListQuery, IRequest<Result<IEnumerable<ProjectDTO>>>
    {
        public TodoFilterType TodoFilter { get; set; }
    }

    public class Handler : IRequestHandler<Query, Result<IEnumerable<ProjectDTO>>>
    {
        public Task<Result<IEnumerable<ProjectDTO>>> Handle(Query query, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}