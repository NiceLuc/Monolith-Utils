using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel;

namespace Deref
{
    internal static class DependencyInjection
    {
        public static IServiceCollection AddDerefServices(this IServiceCollection services, HostBuilderContext context)
        {
            services.AddSharedServices(typeof(DependencyInjection).Assembly);

            return services;
        }
    }
}
