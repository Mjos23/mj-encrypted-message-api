using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MessageApi.Tests;

public sealed class MessageEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>, IDisposable
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task AcceptsIndependentPythonAesGcmVector()
    {
        // Generated using Python cryptography, not this API or its C# sender.
        var envelope = new Dictionary<string, string>
        {
            ["nonce"] = "AAECAwQFBgcICQoL",
            ["ciphertext"] = "D2e6d6rFj3LmJA==",
            ["tag"] = "OZnzruUWkmdtB/nzyh4gIw=="
        };
        await AssertEnvelope(envelope, HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(65536)]
    public async Task AcceptsValidMessagesWithinSizeLimit(int size)
    {
        await AssertEnvelope(Encrypt(new byte[size]), HttpStatusCode.OK);
    }

    [Fact]
    public async Task AcceptsUnicodeWithoutReturningPlaintext()
    {
        await AssertEnvelope(Encrypt(Encoding.UTF8.GetBytes("Hello Mike — 🌊")), HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("nonce")]
    [InlineData("ciphertext")]
    [InlineData("tag")]
    public async Task RejectsOneBitChange(string field)
    {
        var envelope = Encrypt("Hello Mike"u8.ToArray());
        var bytes = Convert.FromBase64String(envelope[field]);
        bytes[0] ^= 1;
        envelope[field] = Convert.ToBase64String(bytes);
        await AssertEnvelope(envelope);
    }

    [Fact]
    public async Task RejectsWrongKey()
    {
        await AssertEnvelope(Encrypt("Hello"u8.ToArray(), key: RandomNumberGenerator.GetBytes(32)));
    }

    [Fact]
    public async Task RejectsWrongAssociatedData()
    {
        await AssertEnvelope(Encrypt("Hello"u8.ToArray(), aad: "another:endpoint"));
    }

    [Theory]
    [InlineData("nonce")]
    [InlineData("ciphertext")]
    [InlineData("tag")]
    public async Task RejectsMissingField(string field)
    {
        var envelope = Encrypt("Hello"u8.ToArray());
        envelope.Remove(field);
        await AssertEnvelope(envelope);
    }

    [Theory]
    [InlineData("nonce")]
    [InlineData("ciphertext")]
    [InlineData("tag")]
    public async Task RejectsInvalidBase64(string field)
    {
        var envelope = Encrypt("Hello"u8.ToArray());
        envelope[field] = "!not-base64!";
        await AssertEnvelope(envelope);
    }

    [Theory]
    [InlineData("nonce")]
    [InlineData("ciphertext")]
    [InlineData("tag")]
    public async Task RejectsNonStringField(string field)
    {
        var envelope = Encrypt("Hello"u8.ToArray()).ToDictionary(x => x.Key, x => (object)x.Value);
        envelope[field] = 123;
        await AssertEnvelope(envelope);
    }

    [Theory]
    [InlineData("nonce", 11)]
    [InlineData("nonce", 13)]
    [InlineData("tag", 15)]
    [InlineData("tag", 17)]
    [InlineData("ciphertext", 0)]
    [InlineData("ciphertext", 65537)]
    public async Task RejectsWrongFieldSize(string field, int size)
    {
        var envelope = Encrypt("Hello"u8.ToArray());
        envelope[field] = Convert.ToBase64String(new byte[size]);
        await AssertEnvelope(envelope);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("\"Hello\"")]
    [InlineData("{}")]
    public async Task RejectsInvalidJsonEnvelope(string json)
    {
        await AssertRaw(json);
    }

    [Fact]
    public async Task RejectsDuplicateFields()
    {
        var envelope = Encrypt("Hello"u8.ToArray());
        var json = JsonSerializer.Serialize(envelope);
        await AssertRaw(json[..^1] + ",\"nonce\":\"" + envelope["nonce"] + "\"}");
    }

    [Fact]
    public async Task RejectsAdditionalFields()
    {
        var envelope = Encrypt("Hello"u8.ToArray());
        envelope["unexpected"] = "value";
        await AssertEnvelope(envelope);
    }

    [Fact]
    public async Task RejectsWrongContentType()
    {
        await AssertRaw(JsonSerializer.Serialize(Encrypt("Hello"u8.ToArray())), "text/plain");
    }

    [Fact]
    public async Task RejectsOversizedHttpBody()
    {
        await AssertRaw(new string(' ', 100000));
    }

    [Fact]
    public async Task RejectsOversizedChunkedHttpBody()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/message");
        request.Headers.TransferEncodingChunked = true;
        request.Content = new StringContent(new string(' ', 100000), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        await AssertResponse(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReplaysRemainAcceptedUntilFreshnessProtectionIsAdded()
    {
        var envelope = Encrypt("Hello"u8.ToArray());
        await AssertEnvelope(envelope, HttpStatusCode.OK);
        await AssertEnvelope(envelope, HttpStatusCode.OK);
    }

    [Fact]
    public async Task MessageRouteRequiresPost()
    {
        using var response = await client.GetAsync("/message");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private async Task AssertEnvelope(object envelope, HttpStatusCode expected = HttpStatusCode.BadRequest)
    {
        using var response = await client.PostAsJsonAsync("/message", envelope);
        await AssertResponse(response, expected);
    }

    private async Task AssertRaw(string json, string contentType = "application/json")
    {
        using var content = new StringContent(json, Encoding.UTF8, contentType);
        using var response = await client.PostAsync("/message", content);
        await AssertResponse(response, HttpStatusCode.BadRequest);
    }

    private static async Task AssertResponse(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Single(body.RootElement.EnumerateObject());
        Assert.Equal(expected == HttpStatusCode.OK ? "accepted" : "rejected",
            body.RootElement.GetProperty("status").GetString());
    }

    private static Dictionary<string, string> Encrypt(byte[] plaintext, byte[]? key = null,
        string aad = "message-api:v1:POST:/message")
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key ?? ApiFactory.TestKey, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, Encoding.UTF8.GetBytes(aad));
        return new Dictionary<string, string>
        {
            ["nonce"] = Convert.ToBase64String(nonce),
            ["ciphertext"] = Convert.ToBase64String(ciphertext),
            ["tag"] = Convert.ToBase64String(tag)
        };
    }

    public void Dispose() => client.Dispose();
}
