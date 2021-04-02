using System;
using System.Reflection;
using System.Threading.Tasks;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Fusion;
using Stl.Fusion.Client;
using Stl.OS;
using Stl.CommandR;
using Stl.DependencyInjection;
using Stl.Fusion.Blazor;
using Stl.Fusion.Extensions;
using Cortex.Net;
using Cortex.Net.Api;
using System.Threading;
using Templates.Blazor2.UI.Stores;
using Templates.Blazor2.Abstractions;

namespace Templates.Blazor2.UI
{
    public static class Program
    {
        public const string ClientSideScope = nameof(ClientSideScope);

        public static Task Main(string[] args)
        {
            if (OSInfo.Kind != OSKind.WebAssembly)
                throw new ApplicationException("This app runs only in browser.");

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            ConfigureServices(builder.Services, builder);
            builder.RootComponents.Add<App>("#app");
            var host = builder.Build();

            host.Services.HostedServices().Start();
            return host.RunAsync();
        }

        public static void ConfigureServices(IServiceCollection services, WebAssemblyHostBuilder builder)
        {
            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
            var apiBaseUri = new Uri($"{baseUri}api/");

            services.AddFusion(fusion => {
                fusion.AddRestEaseClient(
                    (c, o) => {
                        o.BaseUri = baseUri;
                        o.MessageLogLevel = LogLevel.Information;
                    }).ConfigureHttpClientFactory(
                    (c, name, o) => {
                        var isFusionClient = (name ?? "").StartsWith("Stl.Fusion");
                        var clientBaseUri = isFusionClient ? baseUri : apiBaseUri;
                        o.HttpClientActions.Add(client => client.BaseAddress = clientBaseUri);
                    });
                fusion.AddAuthentication(fusionAuth => {
                    fusionAuth.AddRestEaseClient().AddBlazor();
                });
            });

            // This method registers services marked with any of ServiceAttributeBase descendants, including:
            // [Service], [ComputeService], [RestEaseReplicaService], [LiveStateUpdater]
            services.UseAttributeScanner(ClientSideScope).AddServicesFrom(Assembly.GetExecutingAssembly());
            ConfigureSharedServices(services);
            ConfigureCortexServices(services);
        }

        public static IServiceCollection AddCortexService<TService>(
            this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            if (OSInfo.IsWebAssembly) {
                return services.AddSingleton<TService>(implementationFactory);
            }
            else {
                return services.AddScoped<TService>(implementationFactory);
            }
        }


        public static IServiceCollection AddCortexService<TService>(this IServiceCollection services) where TService : class
        {
            if (OSInfo.IsWebAssembly) {
                return services.AddSingleton<TService>();
            }
            else {
                return services.AddScoped<TService>();
            }
        }

        public static void ConfigureSharedServices(IServiceCollection services)
        {
            services.AddBlazorise().AddBootstrapProviders().AddFontAwesomeIcons();

            // Default delay for update delayers
            services.AddSingleton(c => new UpdateDelayer.Options() {
                DelayDuration = TimeSpan.FromSeconds(0.1),
            });

            // Extensions
            services.AddFusion(fusion => {
                fusion.AddLiveClock();
            });

            // https://jspuij.github.io/Cortex.Net.Docs/pages/sharedstate.html
            services.AddCortexService<ISharedState>(sp => {
                // create an instance using the configuration
                var sharedState = new SharedState(new CortexConfiguration() {
                    // enforce that state mutation always happens inside an action.
                    EnforceActions = EnforceAction.Always,
                    AutoscheduleActions = true,
                    SynchronizationContext = SynchronizationContext.Current,
                });
                // spy event handler should be in the object itself to prevent mem-leak as there is no dispose
                //sharedState.SpyEvent += SharedState_SpyEvent; 
                return sharedState;
            });

            // This method registers services marked with any of ServiceAttributeBase descendants, including:
            // [Service], [ComputeService], [RestEaseReplicaService], [LiveStateUpdater]
            services.UseAttributeScanner().AddServicesFrom(Assembly.GetExecutingAssembly());
        }

        public static void ConfigureCortexServices(IServiceCollection services)
        {
            services.AddCortexService<AppStore>(s => {
                var sharedState = s.GetRequiredService<ISharedState>();
                var appStore = sharedState.Observable(() => new AppStore { });
                appStore.OnCreate();
                return appStore;
            });
            services.AddCortexService<TodoPageStore>(s => {
                var todoService = s.GetRequiredService<ITodoService>();
                var sharedState = s.GetRequiredService<ISharedState>();
                var stateFactory = s.GetRequiredService<IStateFactory>();
                var commander = s.GetRequiredService<ICommander>();
                var session = s.GetRequiredService<Stl.Fusion.Authentication.Session>();
                return sharedState.Observable(() => {
                    var s = new TodoPageStore(sharedState, stateFactory, session, todoService, commander);
                    return s;
                });
            });
        }
    }
}
