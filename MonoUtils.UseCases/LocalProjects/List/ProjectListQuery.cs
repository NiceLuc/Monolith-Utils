using MediatR;
using SharedKernel;

namespace MonoUtils.UseCases.LocalProjects.List;

internal class ProjectListQuery : IRequest<Result<IEnumerable<ProjectDTO>>>
{

}