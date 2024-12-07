using Microsoft.Extensions.DependencyInjection;
using Visavi.Quantis.Data;

namespace Visavi.Quantis
{
    public static class CoreQuantisExtensions
    {
        public static void AddQuantisCoreServices(this IServiceCollection services)
        {
            services.AddTransient<IDataServices, DataServices>();
        }
    }
}
