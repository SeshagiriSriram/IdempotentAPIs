# IdempotentAPIs
A starter guide on building Idempotent APIs using C#
### Version History 

#### Version #3.01 
- On a whmsy, added a single program.cs to show the basics.  

#### Version #3 
- Added support for request Body Hashing 
- testing is same as for Version #2 and #1 with scenario #4 added 

#### Version #2
- Added support for Services Extenstion. You can now call 
  `
   builder.Services.AddIdempotencyProtection();
  `
- Testing remains the same as for V1 

#### Version #1 
- Simple ImMemory Based Web filter
- Endpoints can be annotated with [Idempotent] 
- It does not implement a ServiceCollectionExtension and developers will need to manually register storage dependencies. 

#### Version 3 Testing 
Do Scenarios #1 to #3. 

Scenario #4 
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
- Body Hashing: For production strength, combine the Idempotency-Key header with a SHA256 cryptographic hash of the HTTP request payload. This ensures malicious clients do not pass an identical key paired with entirely altered request 