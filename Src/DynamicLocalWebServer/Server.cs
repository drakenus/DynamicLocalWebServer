using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;

namespace LocalWebServer
{
    public class Server : IDisposable
    {
        private IWebHost webHost;
        private readonly IDictionary<PathString, RouteConfig> routes = new Dictionary<PathString, RouteConfig>();

        public IReadOnlyCollection<HttpRequest> CapturedRequests => new ReadOnlyCollection<HttpRequest>(capturedRequests.ToList());

        private readonly ConcurrentBag<HttpRequest> capturedRequests = new ConcurrentBag<HttpRequest>();

        public Uri ServerUri => new Uri(webHost.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First());

        public Server(int port)
        {
            Configure(port);
        }

        /// <summary>
        /// Start web server on separate thread
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await webHost.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds route endpoint to the web server
        /// </summary>
        /// <returns>Fluent route config</returns>
        public RouteConfig AddRoute(string path)
        {
            var pathString = new PathString(path);
            var routeConfig = new RouteConfig();
            routes[pathString] = routeConfig;
            return routeConfig;
        }

        private void Configure(int port)
        {
            webHost = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.ListenLocalhost(port);
                })
                .Configure(configureApp =>
                {
                    configureApp.Use(async (context, next) =>
                    {
                        capturedRequests.Add(context.Request);

                        if (!routes.TryGetValue(context.Request.Path, out var value) || !string.Equals(value.Method, context.Request.Method, StringComparison.InvariantCultureIgnoreCase))
                        {
                            await next().ConfigureAwait(false);
                            return;
                        }

                        context.Response.StatusCode = (int)value.HttpStatusCode;
                        context.Response.ContentType = value.ContentType;

                        await context.Response.WriteAsync(value.BodyContent).ConfigureAwait(false);
                    });
                })
                .Build();
        }

        public void Dispose()
        {
            webHost?.Dispose();
            webHost = null;
        }
    }
}