using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using RestSharp;
using Xunit;

namespace LocalWebServer.Tests
{
    public class LocalWebServerTests : IDisposable
    {
        private const string testServerName = "localhost";
        private const int testServerPort = 45763;
        private readonly Server testServerInstance;

        public LocalWebServerTests()
        {
            testServerInstance = new Server(testServerPort);
        }

        [Fact]
        public async Task StartServerAsyncTest_ShouldBeConfigured()
        {
            await testServerInstance.StartAsync().ConfigureAwait(false);

            var serverUri = testServerInstance.ServerUri;
            testServerInstance.ServerUri.Should().NotBeNull();

            serverUri.Port.Should().Be(testServerPort);
            serverUri.Host.Should().Be(testServerName);

            using var client = new TcpClient();
            await client.ConnectAsync(serverUri.Host, serverUri.Port).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddRoute_RequestForExistingRoute_ShouldCapture()
        {
            await testServerInstance.StartAsync().ConfigureAwait(false);

            testServerInstance.AddRoute("/test");

            var response = await GetRestResponseAsync("test").ConfigureAwait(false);
            response.IsSuccessful.Should().BeTrue();
            testServerInstance.CapturedRequests.Count.Should().Be(1);
        }

        [Fact]
        public async Task AddRoute_RequestForNonExistingRoute_ShouldCapture()
        {
            await testServerInstance.StartAsync().ConfigureAwait(false);

            testServerInstance.AddRoute("/test");

            await GetRestResponseAsync("test").ConfigureAwait(false);

            testServerInstance.CapturedRequests.Count.Should().Be(1);
        }

        [Fact]
        public async Task StartServerAsync_PortTaken_ShouldThrow()
        {
            using var server = new Server(testServerPort);
            await server.StartAsync().ConfigureAwait(false);

            await Assert.ThrowsAsync<IOException>(() => testServerInstance.StartAsync()).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddRoute_ConfiguredBodyCodeAndMethod_ShouldReturnValidResponse()
        {
            const string expectedResponseContent = "hello from route";
            const HttpStatusCode expectedStatusCode = HttpStatusCode.Accepted;
            await testServerInstance.StartAsync().ConfigureAwait(false);

            testServerInstance.AddRoute("/test")
                .WithHttpStatusCode(expectedStatusCode)
                .WithStringBody(expectedResponseContent)
                .WithMethod(WebRequestMethods.Http.Post);

            var response = await GetRestResponseAsync("test", Method.POST).ConfigureAwait(false);
            response.StatusCode.Should().Be(expectedStatusCode);
            response.Content.Should().Be(expectedResponseContent);
        }

        private static Task<IRestResponse> GetRestResponseAsync(string route)
        {
            return GetRestResponseAsync(route, Method.GET);
        }

        private static async Task<IRestResponse> GetRestResponseAsync(string route, Method method)
        {
            var restClient = new RestClient(new Uri($"http://{testServerName}:{testServerPort}"));
            var restRequest = new RestRequest(route, method);
            return await restClient.ExecuteTaskAsync(restRequest).ConfigureAwait(false);
        }

        public void Dispose()
        {
            testServerInstance?.Dispose();
        }
    }
}