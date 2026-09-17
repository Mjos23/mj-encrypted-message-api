using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

if (args.Length == 1 && args[0] == "--new-key")
{
    Console.WriteLine(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    return;
}
if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] != "--tamper"))
    throw new ArgumentException("Supply one message and optionally --tamper.");

var key = Convert.FromBase64String(
    Environment.GetEnvironmentVariable("MESSAGE_API_KEY_BASE64") ?? "");
if (key.Length != 32)
    throw new InvalidOperationException("MESSAGE_API_KEY_BASE64 must encode a 32-byte key.");
var plaintext = Encoding.UTF8.GetBytes(args[0]);
if (plaintext.Length is < 1 or > 65536)
    throw new ArgumentException("Message must contain 1..65536 UTF-8 bytes.");

var nonce = RandomNumberGenerator.GetBytes(12); // New nonce for each encryption.
var ciphertext = new byte[plaintext.Length];
var tag = new byte[16];
try
{
    using var aes = new AesGcm(key, 16);
    aes.Encrypt(nonce, plaintext, ciphertext, tag, "message-api:v1:POST:/message"u8);
}
finally
{
    CryptographicOperations.ZeroMemory(key);
    CryptographicOperations.ZeroMemory(plaintext);
}
if (args.Length == 2)
    tag[0] ^= 1;

using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
using var response = await client.PostAsJsonAsync("http://127.0.0.1:8000/message", new
{
    nonce = Convert.ToBase64String(nonce),
    ciphertext = Convert.ToBase64String(ciphertext),
    tag = Convert.ToBase64String(tag)
});
Console.WriteLine(await response.Content.ReadAsStringAsync());
Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
