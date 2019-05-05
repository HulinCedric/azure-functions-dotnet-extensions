using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Functions.Extensions.Configuration
{
    public static class ConfigurationBuilderExtension
    {
        /// <summary>
        /// Create configuration with settings files.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        /// <remarks>
        /// Local settings and environment variables are already in <c>IConfiguration</c> service descriptor.
        /// Configuration order:
        /// * existing configuration
        /// * configuration file appsettings.json
        /// * configuration file appsettings.{environment}.json
        /// </remarks>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2"/>
        public static IConfigurationBuilder CreateConfigurationBuilder(this IFunctionsHostBuilder builder)
        {
            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddExistingConfiguration(builder);

            configurationBuilder.SetBasePath(GetFunctionBasePath());

            configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ??
                              Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
            if (!string.IsNullOrWhiteSpace(environment))
            {
                configurationBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
            }

            configurationBuilder.RegisterConfiguration(builder);

            return configurationBuilder;
        }

        private static void AddExistingConfiguration(this ConfigurationBuilder configurationBuilder, IFunctionsHostBuilder builder)
        {
            var existingConfigurationDescriptor = GetConfigurationDescriptor(builder);
            if (existingConfigurationDescriptor?.ImplementationInstance is IConfigurationRoot)
            {
                configurationBuilder.AddConfiguration(existingConfigurationDescriptor.ImplementationInstance as IConfigurationRoot);
            }
        }

        private static ServiceDescriptor GetConfigurationDescriptor(IFunctionsHostBuilder builder)
            => builder.Services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IConfiguration));

        private static string GetFunctionBasePath()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            var segments = location.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var basePath = string.Join(Path.DirectorySeparatorChar.ToString(), segments.Take(segments.Count - 2));

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                basePath = $"/{basePath}";
            }

            return basePath;
        }

        private static void RegisterConfiguration(this ConfigurationBuilder configurationBuilder, IFunctionsHostBuilder builder)
        {
            var existingConfigurationDescriptor = GetConfigurationDescriptor(builder);
            if (existingConfigurationDescriptor?.ImplementationInstance is IConfigurationRoot)
            {
                builder.Services.Replace(new ServiceDescriptor(typeof(IConfiguration), _ => configurationBuilder.Build(), existingConfigurationDescriptor.Lifetime));
            }
            else
            {
                builder.Services.AddScoped<IConfiguration>(sp => configurationBuilder.Build());
            }
        }
    }
}