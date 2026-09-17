# Using this project in applications

## Project entry

**Bangel C# Encrypted Message API — C#, ASP.NET Core, .NET, xUnit**

Developed a C# API that validates AES-256-GCM encrypted messages, with strict
request validation, a command-line client, HTTP integration tests, and GitHub
Actions configuration.

Repository: https://github.com/Mjos23/upgraded-octo-fortnight. It is private;
external reviewers will need access before they can inspect it.
Describe CI as configured until its
actual GitHub runs pass. Do not present test counts as a security certification.

## A short interview walkthrough

1. Show `POST /message` and explain its three fields: nonce, ciphertext, and tag.
2. Send a valid encrypted message and show the accepted response.
3. Run `--tamper` and show the rejected response.
4. Explain why AES-GCM authentication matters: encryption alone does not
   establish message integrity.
5. Show a test that changes one bit and a size-limit test that uses real HTTP.
6. Explain the shared-key assumption, nonce uniqueness, and replay limitation.

## Questions to be ready to answer

- What does an accepted message prove, and what does it not prove?
- Why use a fresh nonce and a fixed 16-byte authentication tag?
- How does associated data bind the message to this protocol?
- Why is the key in configuration instead of source code?
- Why return one rejection shape instead of detailed decryption errors?
- Why test over Kestrel instead of mocking the validation function?
- How would you add replay protection without breaking multiple server instances?
- What would change before deploying the API publicly?

Run the demo yourself and practice explaining these choices in your own words.
Be candid about tools and assistance used if asked. The useful evidence is that
you can understand, modify, test, and discuss the code and its limitations.
