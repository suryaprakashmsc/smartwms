using System.Net;
using Inventory.API.Health;
using Xunit;

namespace Inventory.Tests;

public class ContainerReadinessProbeTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK, 0)]
    [InlineData(HttpStatusCode.ServiceUnavailable, 1)]
    [InlineData(HttpStatusCode.Redirect, 1)]
    [InlineData(HttpStatusCode.Unauthorized, 1)]
    public async Task ReportsReadyOnlyForAnOkResponse(HttpStatusCode status, int expected)
    {
        using var client = new HttpClient(new StubHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("http://127.0.0.1:8080/api/health/ready", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(status));
        }));
        Assert.Equal(expected, await ContainerReadinessProbe.RunAsync(client));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReportsUnreadyWhenConnectionFailsOrTimesOut(bool timeout)
    {
        using var client = new HttpClient(new StubHandler(_ => Task.FromException<HttpResponseMessage>(
            timeout ? new TaskCanceledException() : new HttpRequestException())));
        Assert.Equal(1, await ContainerReadinessProbe.RunAsync(client));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => respond(request);
    }
}
