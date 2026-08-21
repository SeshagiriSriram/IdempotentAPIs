# IdempotentAPIs

A starter guide on building Idempotent APIs using C#

## 🧾 System Architecture Evolution Ledger

### Version 5.7
- finished cleanup of folder structures
- updated  this file README.md 
- FEAT: Added Basic Transaction ID Support. roadmap for 5.8 - make it available for any microservice irrespective of transport

### Version 5.6
- finished cleanup of folder structures
- updated  this file README.md 

### Version 5.5 
- started cleanup of folder structures

### 🚀 Version 5.4 (Current Baseline)

- **High-Availability Broker Clustering**: Added asynchronous failover routing supporting automated connection cycling across multiple independent RabbitMQ cluster endpoints.
- **Configurable Strategy Engines**: Implemented pluggable reconnection backoff logic, allowing runtime switching between standard `Fixed` duration intervals and an `Exponential` backoff curve to insulate thread sockets during long node outages.
- **Low-Latency Publishing Assurances**: Enhanced the outbox message loop with a strict `MaxRetryThreshold` payload isolation model to insulate processing queues against poison-message stall deadlocks.
- **Polymorphic Topology Formats**: Introduced convention-driven `Source` and `Target` abstraction schemas supporting seamless runtime declaration of individual point-to-point `Queues` or pub/sub broadcast `Exchanges`.
- **Decoupled Worker Configurations**: Refactored the core asynchronous processor block to completely eliminate all internal magic strings, resolving settings parameters safely via dependency injection options.

### 📬 Version 5.3

- **Transactional Outbox Core**: Designed database transactional outbox tracking matrices inside the operational `demo` schema boundary lines to eradicate multi-system "Dual Write" split-brain synchronization anomalies.
- **Asynchronous Protocol Modernization**: Implemented a non-blocking `BackgroundService` written explicitly to align with the advanced, non-blocking asynchronous streaming engines inside **RabbitMQ.Client v7.2**.
- **Point-to-Point Prototyping**: Developed an initial single-queue pipeline to anchor messaging operations securely.

### 📁 Version 5.2

- **Bounded Context Domain Alignment**: Realized clean separation of technical framework traits from domain scopes by shifting database entities, records, and contexts straight into the microservice workspace while leaving the Redlock engine purely domain-agnostic.
- **Deterministic Scripting Adjustments**: Overhauled structural bootstrapping execution rules to swap schema validation logic out for raw relational commands.

### 💾 Version 5.1

- **Decoupled Deployment Automation**: Extracted database initialization tasks completely away from the application runtime pipeline into an independent standalone CLI seeder deployment utility.
- **Unified Domain Data Signatures**: Realigned lookup primary key parameters (`AccountType`) from volatile integers onto deterministic, cluster-safe `Guid` identifiers.

#### Usage

- run `docker compose up -d`
- run the Standalone Seeder.
- Start your API.
- Test. Sample curl is below:

`
curl -X POST "<your end point>"  
-H "content-type: application/json"
-H "X-Idempotency-Key: order-key-v4"
-d "{\"AccountId\":\"22222222-2222-2222-2222-222222222222\", \"VendorId\":\"33333333-3333-3333-3333-33333333333b\", \"ItemId\":\"44444444-4444-4444-4444-444444444444\", \"Qty\":2}"
`

Interpret this as: For AccountId, place an order Qty of ItemId from VendorId.  

### 🏁 Version 5.0

- **Relational Storage Matrix**: Introduced data persistence capabilities utilizing persistent Docker named volumes backed by an atomic Entity Framework Core database layer.
- **Transactional Commerce Processing**: Built the core synchronous checkout endpoint (`OrdersController`) executing account validation and balance operations safely within an atomic relational envelope.

### Version 4.2

- Added || support for locking and node discovery
- Partially reduced load time from 1 second to 200ms.
- You could now work with a majority of redis nodes up.

### Version 4.1

- Support for Redlock
- Focus: Demonstrates Single-Thread Sequential Fallback (The Teaching Version).
- The Lesson: When querying clustered or independent infrastructure sequentially, a dead node early in the chain introduces a mandatory connectTimeout blocking penalty on every single request thread, crippling API performance despite structural high-availability.

### Version 4.0

#### 🚀 Idempotency Filter Implementation

Successfully designed and verified a thread-safe, configuration-driven idempotency protection framework for ASP.NET Core APIs.

#### 🔧 What's New & Fixed

- **`IdempotentAttribute`**: Built a compilation-compliant marker attribute using standard types (`int`/`string`) rather than nullable primitives to align with C# metadata compilation constraints. Supports named parameter overrides like `[Idempotent(CacheDurationInMinutes = 30)]`.
- **`IdempotencyOptions`**: Designed strongly-typed configuration bindings to map directly from `appsettings.json` sections with built-in `ValidateOnStart()` startup safety checks.
- **`IdempotentFilter`**:
  - Upgraded attribute discovery to a reflection-based `MethodInfo` approach to bypass environment-dependent `ActionDescriptor` metadata gaps.
  - Implemented request stream buffering (`EnableBuffering()`) and cryptographic body hashing (`SHA256`) to validate request payload integrity and detect key mismatches.
  - Added structured fallback hierarchy to cleanly prioritize endpoint-level attribute settings over default application properties.
- **`IIdempotencyStore`**: Establishes explicit pipeline contracts for fetching, writing, locking, and releasing transaction states.
  - *Memory Store Tier*: Employs .NET's native `IMemoryCache` to avoid memory leaks via absolute item expirations.
  - *Redis Store Tier (Phase 1)*: Leverages atomic, single-node string flags (`When.NotExists`) and addresses multi-overload type conversion ambiguities when passing `RedisValue` datasets back to `JsonSerializer`.

### Version 3.02

- Fixed issue of Body not being read leading to check of hash values being all the same.  

### Version 3.01

- On a whmsy, added a single program.cs to show the basics.  

### Version 3

- Added support for request Body Hashing
- testing is same as for Version 2 and Version 1 with scenario #4 added.

### Version 2

- Added support for Services Extenstion. You can now call builder.Services.AddIdempotencyProtection();
  `
- Testing remains the same as for V1

### Version 1

- Simple ImMemory Based Web filter
- Endpoints can be annotated with [Idempotent]
- It does not implement a ServiceCollectionExtension and developers will need to manually register storage dependencies.

#### Version 3 Testing

Do Scenarios #1 to #3.

Scenario #4:

- repeat Scenario #2 keeping Key value same but different data. You should see an error.

#### Version 1/2 Testing

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

#### Key consideratons for V3

- Storage Lifespan: For production environments, swap InMemoryIdempotencyStore for a distributed cache like Redis so the data persists across multiple application container instances and server restarts.
- Failure Responses: The logic above explicitly skips caching 4xx and 5xx error states. This permits clients to fix validation mistakes or retry broken infrastructure calls using the original key.

#### Key consideratons for V1 and V2

- Storage Lifespan: For production environments, swap InMemoryIdempotencyStore for a distributed cache like Redis so the data persists across multiple application container instances and server restarts.
- Failure Responses: The logic above explicitly skips caching 4xx and 5xx error states. This permits clients to fix validation mistakes or retry broken infrastructure calls using the original key.
- Body Hashing: For production strength, combine the Idempotency-Key header with a SHA256 cryptographic hash of the HTTP request payload. This ensures malicious clients do not pass an identical key paired with entirely altered request.
