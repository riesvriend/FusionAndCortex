using System;
using Xunit;
using Templates.Blazor2.Host;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Tests
{
    public class HostTest
    {
        [Fact]
        public async Task RunHost() // use FusionTestWebHost
        {
            // run at http://localhost:5000/ ?

            var host = await Program.CreateHost();
            await host.RunAsync();
        }
    }
}
