// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Extensions.Configuration;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using Xunit;

namespace DependencyInjection.Tests
{
    public class FunctionsStartupConfigurationTests
    {

        private static IConfigurationRoot GetConfiguration(IFunctionsHostBuilder builder)
            => builder.Services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IConfiguration)).ImplementationInstance as IConfigurationRoot;

        [Fact]
        public void Configure_ConfiguresServices()
        {
            var builder = new TestStartup();

            var webJobsBuilder = new TestWebJobsBuilder();

            Environment.SetEnvironmentVariable("Bar", "foobar");
            var configurationBuilder = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            webJobsBuilder.Services.AddSingleton<IConfiguration>(configurationBuilder);

            builder.Configure(webJobsBuilder);

            var sp = webJobsBuilder.Services.BuildServiceProvider();

            var fooSettings = sp.GetRequiredService<IOptionsMonitor<FooSettings>>().CurrentValue;

            Assert.Equal("foobar", fooSettings.Bar);
        }

        private class TestWebJobsBuilder : IWebJobsBuilder
        {
            public TestWebJobsBuilder()
            {
                Services = new ServiceCollection();
            }

            public IServiceCollection Services { get; }
        }

        private class TestStartup : FunctionsStartup
        {
            public override void Configure(IFunctionsHostBuilder builder)
            {
                var configuration = builder.CreateConfigurationBuilder().Build();

                builder.Services.Configure<FooSettings>(configuration);
            }
        }

        public class FooSettings
        {
            public string Bar { get; set; }
        }
    }
}
