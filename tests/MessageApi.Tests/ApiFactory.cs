using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MessageApi.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    // Public fixture key (bytes 00..1f), never a deployment secret.
    public static byte[] TestKey => Enumerable.Range(0, 32).Select(x => (byte)x).ToArray();

    public ApiFactory()
    {
        // Exercise the actual HTTP server, including its request-body limit.
        UseKestrel(0);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("MESSAGE_API_KEY_BASE64", Convert.ToBase64String(TestKey));
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
    }
}
