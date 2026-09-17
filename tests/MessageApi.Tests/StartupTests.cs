using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MessageApi.Tests;

public sealed class StartupTests
{
    [Theory]
    [InlineData("")]
    [InlineData("!invalid-base64!")]
    [InlineData("eHh4eHh4eHh4eHh4eHh4eA==")]
    public void RefusesToStartWithMissingOrInvalidKey(string value)
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("MESSAGE_API_KEY_BASE64", value));

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("must encode a 32-byte key", error.Message);
    }
}
