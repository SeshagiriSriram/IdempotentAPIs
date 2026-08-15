# IdempotentAPIs

A starter guide on building Idempotent APIs using C#

## Version History

## Version 4.2

- Added || support for locking and node discovery
- Partially reduced load time from 1 second to 200ms.
- You could now work with a majority of redis nodes up.

## Version 4.1

- Support for Redlock
- Focus: Demonstrates Single-Thread Sequential Fallback (The Teaching Version).
- The Lesson: When querying clustered or independent infrastructure sequentially, a dead node early in the chain introduces a mandatory connectTimeout blocking penalty on every single request thread, crippling API performance despite structural high-availability.

## Version #4.0

### 🚀 Idempotency Filter Implementation

Successfully designed and verified a thread-safe, configuration-driven idempotency protection framework for ASP.NET Core APIs.

### 🔧 What's New & Fixed

- **`IdempotentAttribute`**: Built a compilation-compliant marker attribute using standard types (`int`/`string`) rather than nullable primitives to align with C# metadata compilation constraints. Supports named parameter overrides like `[Idempotent(CacheDurationInMinutes = 30)]`.
- **`IdempotencyOptions`**: Designed strongly-typed configuration bindings to map directly from `appsettings.json` sections with built-in `ValidateOnStart()` startup safety checks.
- **`IdempotentFilter`**:
  - Upgraded attribute discovery to a reflection-based `MethodInfo` approach to bypass environment-dependent `ActionDescriptor` metadata gaps.
  - Implemented request stream buffering (`EnableBuffering()`) and cryptographic body hashing (`SHA256`) to validate request payload integrity and detect key mismatches.
  - Added structured fallback hierarchy to cleanly prioritize endpoint-level attribute settings over default application properties.
- **`IIdempotencyStore`**: Establishes explicit pipeline contracts for fetching, writing, locking, and releasing transaction states.
  - *Memory Store Tier*: Employs .NET's native `IMemoryCache` to avoid memory leaks via absolute item expirations.
  - *Redis Store Tier (Phase 1)*: Leverages atomic, single-node string flags (`When.NotExists`) and addresses multi-overload type conversion ambiguities when passing `RedisValue` datasets back to `JsonSerializer`.

## Version #3.02

- Fixed issue of Body not being read leading to check of hash values being all the same.  

## Version #3.01

- On a whmsy, added a single program.cs to show the basics.  

## Version #3

- Added support for request Body Hashing
- testing is same as for Version #2 and #1 with scenario #4 added.

## Version #2

- Added support for Services Extenstion. You can now call builder.Services.AddIdempotencyProtection();
  `
- Testing remains the same as for V1

## Version #1

- Simple ImMemory Based Web filter
- Endpoints can be annotated with [Idempotent]
- It does not implement a ServiceCollectionExtension and developers will need to manually register storage dependencies.

### Version 3 Testing

Do Scenarios #1 to #3.

Scenario #4:

- repeat Scenario #2 keeping Key value same but different data. You should see an error.

### Version 1/2 Testing

scenario #1:

`
curl -X POST "https://yourdomain.com/api/post" \
     -H "Content-Type: application/json" \
     -d '{"OrderId":"12345","Amount":99.99}'
`

Scenario #2:

`
curl -X POST "https://yourdomain.com/api/post" \
     -H "Content-Type: application/json" \
     -H "Idempotency-Key: <yourkey>" \
     -d '{"OrderId":"12345","Amount":99.99}'
`

Scenario #3:

re-Try Scenario #2 after 3 minutes and you should see same result as Scenario #2

NB: On windows, do not use single quotes.

### Key consideratons for V3

- Storage Lifespan: For production environments, swap InMemoryIdempotencyStore for a distributed cache like Redis so the data persists across multiple application container instances and server restarts.
- Failure Responses: The logic above explicitly skips caching 4xx and 5xx error states. This permits clients to fix validation mistakes or retry broken infrastructure calls using the original key.

### Key consideratons for V1 and V2

- Storage Lifespan: For production environments, swap InMemoryIdempotencyStore for a distributed cache like Redis so the data persists across multiple application container instances and server restarts.
- Failure Responses: The logic above explicitly skips caching 4xx and 5xx error states. This permits clients to fix validation mistakes or retry broken infrastructure calls using the original key.
- Body Hashing: For production strength, combine the Idempotency-Key header with a SHA256 cryptographic hash of the HTTP request payload. This ensures malicious clients do not pass an identical key paired with entirely altered request.
