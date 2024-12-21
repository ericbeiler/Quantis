using Microsoft.Extensions.DependencyInjection;
using Visavi.Quantis.Data;
using Visavi.Quantis.Modeling;

namespace Visavi.Quantis
{
    public static class CoreQuantisExtensions
    {
        public static void AddQuantisCoreServices(this IServiceCollection services)
        {
            services.AddTransient<IDataServices, DataServices>();
            services.AddTransient<IPredictionService, PredictionService>();
        }

        public static DateOnly ToDateOnly(this DateTime dateTime)
        {
            return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
        }
    }
}
