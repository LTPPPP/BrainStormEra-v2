using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogicLayer.Extensions
{
    /// <summary>
    /// Extension methods for configuring Hashids services
    /// </summary>
    public static class HashidsServiceExtensions
    {
        /// <summary>
        /// Add Hashids services to the dependency injection container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddHashidsServices(this IServiceCollection services)
        {
            // Register HashidsService as singleton since it's stateless and thread-safe
            services.AddSingleton<IHashidsService, HashidsService>();

            return services;
        }
    }
}
