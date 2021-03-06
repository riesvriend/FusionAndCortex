using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Extensions;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests.Extensions
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class LiveTimeTest : FusionTestBase
    {
        public LiveTimeTest(ITestOutputHelper @out)
            : base(@out, new FusionTestOptions() { UseTestClock = true })
        { }

        [Fact]
        public async Task BasicTest()
        {
            var liveClock = Services.GetRequiredService<ILiveClock>();

            var cTime = await Computed.Capture(_ => liveClock.GetUtcNow());
            cTime.IsConsistent().Should().BeTrue();
            (DateTime.UtcNow - cTime.Value).Should().BeLessThan(TimeSpan.FromSeconds(1.1));
            await Delay(1.3);
            cTime.IsConsistent().Should().BeFalse();

            cTime = await Computed.Capture(_ => liveClock.GetUtcNow(TimeSpan.FromMilliseconds(200)));
            cTime.IsConsistent().Should().BeTrue();
            await Delay(0.25);
            cTime.IsConsistent().Should().BeFalse();

            var now = DateTime.UtcNow;
            var ago = await liveClock.GetMomentsAgo(now);
            ago.Should().Be("just now");
            await Delay(1.8);
            ago = await liveClock.GetMomentsAgo(now);
            ago.Should().Be("1 second ago");
        }
    }
}
