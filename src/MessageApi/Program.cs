using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

byte[] key;
try
{
    key = Convert.FromBase64String(
        builder.Configuration["MESSAGE_API_KEY_BASE64"] ?? "");
}
catch (FormatException)
{
    throw new InvalidOperationException("MESSAGE_API_KEY_BASE64 must encode a 32-byte key.");
}
if (key.Length != 32)
    throw new InvalidOperationException("MESSAGE_API_KEY_BASE64 must encode a 32-byte key.");
if (!AesGcm.IsSupported)
    throw new PlatformNotSupportedException("AES-GCM is required.");

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 96 * 1024);
if (string.IsNullOrEmpty(builder.Configuration["urls"]))
    builder.WebHost.UseUrls("http://127.0.0.1:8000");
var app = builder.Build();
app.MapPost("/message", MessageResponse);
try
{
    app.Run();
}
finally
{
    CryptographicOperations.ZeroMemory(key);
}

async Task<IResult> MessageResponse(HttpRequest request)
{
    byte[]? plaintext = null;
    try
    {
        if (!request.HasJsonContentType() || request.ContentLength is > 96 * 1024)
            return Results.BadRequest(new { status = "rejected" });

        using var document = await JsonDocument.ParseAsync(request.Body,
            new JsonDocumentOptions { MaxDepth = 8 }, request.HttpContext.RequestAborted);
        var body = document.RootElement;
        if (body.ValueKind != JsonValueKind.Object ||
            body.EnumerateObject().Count() != 3 ||
            !body.TryGetProperty("nonce", out var nonceField) ||
            !body.TryGetProperty("ciphertext", out var ciphertextField) ||
            !body.TryGetProperty("tag", out var tagField) ||
            nonceField.ValueKind != JsonValueKind.String ||
            ciphertextField.ValueKind != JsonValueKind.String ||
            tagField.ValueKind != JsonValueKind.String)
            return Results.BadRequest(new { status = "rejected" });

        var nonce = Convert.FromBase64String(nonceField.GetString()!);
        var ciphertext = Convert.FromBase64String(ciphertextField.GetString()!);
        var tag = Convert.FromBase64String(tagField.GetString()!);
        if (nonce.Length != 12 || tag.Length != 16 || ciphertext.Length is < 1 or > 65536)
            return Results.BadRequest(new { status = "rejected" });

        plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, "message-api:v1:POST:/message"u8);
        return Results.Ok(new { status = "accepted" });
    }
    catch (Exception ex) when (ex is JsonException or FormatException or
                               CryptographicException or BadHttpRequestException)
    {
        return Results.BadRequest(new { status = "rejected" });
    }
    finally
    {
        if (plaintext is not null)
            CryptographicOperations.ZeroMemory(plaintext);
    }
}

// Allows integration tests to host this entry point with a separate test key.
public partial class Program { }
