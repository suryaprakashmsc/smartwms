namespace Inventory.API.Health;

public static class ContainerReadinessProbe
{
    public static async Task<int> RunAsync(HttpClient client)
    {
        try
        {
            using var response = await client.GetAsync("http://127.0.0.1:8080/api/health/ready");
            return response.StatusCode == System.Net.HttpStatusCode.OK ? 0 : 1;
        }
        catch (HttpRequestException) { return 1; }
        catch (OperationCanceledException) { return 1; }
    }
}
