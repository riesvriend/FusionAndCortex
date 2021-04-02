using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Templates.Blazor2.Host;
using Templates.Blazor2.Services;

namespace Templates.Blazor2.Host
{
    public static class Program
    {
        public static async Task Main()
        {
            var host = await CreateHost();
            await host.RunAsync();
        }

        public static async Task<IHost> CreateHost()
        {
            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureHostConfiguration(builder => {
                    // Looks like there is no better way to set _default_ URL
                    builder.Sources.Insert(0, new MemoryConfigurationSource() {
                        InitialData = new Dictionary<string, string>() {
                        {WebHostDefaults.ServerUrlsKey, "http://localhost:5005"},
                        }
                    });
                })
                .ConfigureWebHostDefaults(builder => builder
                    .UseDefaultServiceProvider((ctx, options) => {
                        if (ctx.HostingEnvironment.IsDevelopment()) {
                            options.ValidateScopes = true;
                            options.ValidateOnBuild = true;
                        }
                    })
                    .UseStartup<Startup>())
                .Build();

            // Ensure the DB is created
            var dbContextFactory = host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var dbContext = dbContextFactory.CreateDbContext();
            // await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            return host;
        }
    }
}
