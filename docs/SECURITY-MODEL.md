# Security model

## What acceptance proves

The authentication tag validates for the supplied nonce and ciphertext, the
configured 256-bit shared key, and the fixed associated data
`message-api:v1:POST:/message`. The protocol has one response decision and no
downstream action.

Anyone possessing the key can generate accepted envelopes. An observer who
captures a valid envelope can also replay that envelope. Acceptance alone
therefore does not prove a fresh request or an individual sender's identity.

## Controls implemented

| Boundary | Control |
| --- | --- |
| Startup configuration | Require a Base64-encoded 32-byte key; no default key |
| HTTP input | JSON content type, body-size limit, and bounded JSON depth |
| Envelope | Exactly three named string fields with validated decoded sizes |
| Ciphertext | Standard AES-GCM with a required 16-byte tag |
| Protocol context | Fixed associated data for version, method, and route |
| Responses | Accepted/rejected only; plaintext is not returned |
| Logging | Application code does not log keys, plaintext, or request bodies |
| Managed buffers | Clear decrypted bytes after each request and key bytes at shutdown |

Clearing byte arrays does not erase immutable strings, runtime copies, crash
dumps, or memory owned by the operating system. The console client's plaintext
is also an operating-system command-line argument; use harmless demo text.

The sender creates random 96-bit nonces. Nonces must not be reused for different
encryptions with the same key. Random generation is suitable for this small
demo; it is not a complete high-volume nonce-management policy.

## Extensions deliberately outside this version

- Replay resistance with an authenticated timestamp/message identifier and
  bounded, persistent replay state appropriate to the deployment.
- Individual credentials or identity tokens, authorization, and revocation.
- Key rotation and a dedicated secret-management service.
- Rate limiting and operational abuse controls.
- Application-specific plaintext validation and business actions.
- Public HTTPS hosting, monitoring, and operational recovery.

Add and test each extension against its own requirements. Do not use this
prototype to authorize transactions or once-only actions based solely on the
current accepted result.

## Test material

The fixed key `00 01 ... 1f` is public fixture data. Its matching envelope was
generated with Python's `cryptography.hazmat.primitives.ciphers.aead.AESGCM`:
nonce bytes `00 01 ... 0b`, plaintext UTF-8 `Hello Mike`, and the protocol's
associated data. It is included to catch interoperability regressions.
No generated deployment keys belong in the repository.
