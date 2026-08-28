# Caseware-Take-Home-Exercise

## Architecture Summary

This solution implements Part 2 Option A as a small ASP.NET Core Web API slice in `.NET 10`.

The API exposes a single protected document endpoint:

`GET /api/workspaces/{workspaceId}/documents/{documentId}`

The current implementation is organized into:

- `Controllers` for HTTP entry points
- `Authorization` for policies, requirements, and handlers
- `Services` for the document access abstraction
- `Models` and `Contracts` for internal and response shapes
- `Common` for shared primitives such as the result pattern
- `Observability` for tracing and metrics primitives

## Authorization Flow

The document endpoint follows this sequence:

1. ASP.NET Core `JwtBearer` validates the incoming bearer token.
2. The controller authorizes against `DocumentAccessContext`.
3. If coarse authorization succeeds, the document service loads the resource.
4. If the resource does not exist, the endpoint returns `404`.
5. If the resource exists, resource-level authorization checks the token against the document.
6. Authorized requests return `200`.

Current response behavior:

- `200` for valid and authorized requests
- `401` for missing, invalid, expired, or incorrectly signed tokens
- `403` for valid tokens missing `documents.read` or with the wrong `firm_id`
- `404` when an authorized caller requests a document that does not exist

## Assumptions

This implementation makes the following explicit assumptions for the exercise:

- access tokens expose OAuth scopes in a `scope` claim
- scopes are space-delimited
- access tokens include a `firm_id` claim
- the API validates `issuer`, `audience`, signature, and lifetime

The exercise did not prescribe exact custom claim names, so these values are documented assumptions for this slice.

## Limitations

This implementation intentionally does not include:

- a real external identity provider
- dynamic workspace role evaluation
- resource override evaluation
- firm policy resolution
- persistent storage
- a full permission engine

The document service is stubbed in memory to keep the implementation focused on the authorization slice.

## Production Extension Path

In a production version, the resource authorization step could delegate dynamic permission evaluation to a dedicated authorization service or cache aligned with the broader architecture from Part 1.

Likely extensions would include:

- workspace role resolution
- resource-level overrides
- firm-level policy enforcement
- permission revocation propagation
- production-grade token issuer integration

## Local Run

Restore dependencies:

```powershell
dotnet restore Caseware.Collaborate.slnx
```

Build the solution:

```powershell
dotnet build Caseware.Collaborate.slnx
```

Run the API:

```powershell
dotnet run --project src/Collaborate.Api
```

Run tests:

```powershell
dotnet test Caseware.Collaborate.slnx
```

OpenAPI is exposed in development at:

- `/openapi/v1.json`
- `/swagger`
