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

        public static DateOnly ToDateOnly(this DateTime dateTime)
        {
            return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
        }
    }
}
