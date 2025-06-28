using BusinessLogicLayer.Services.Implementations;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogicLayer.Extensions
{
    /// <summary>
    /// Extension methods for configuring chat URL hashing services
    /// </summary>
    public static class ChatUrlServiceExtensions
    {
        /// <summary>
        /// Add chat URL hashing services to the dependency injection container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddChatUrlServices(this IServiceCollection services)
        {
            // Register ChatUrlHasher as singleton since it's stateless
            services.AddSingleton<ChatUrlHasher>();

            // Register ChatUrlService as scoped since it depends on other scoped services
            services.AddScoped<IChatUrlService, ChatUrlService>();

            return services;
        }
    }
}
