# Security and deployment setup

This repository intentionally contains no production credentials. The API fails at startup when required configuration is missing or unsafe.

## Required secrets

Configure these through the hosting platform's secret store or environment variables:

```text
ConnectionStrings__DefaultConnection=<rotated SQL Server connection string>
JwtSettings__SecretKey=<random secret of at least 32 bytes>
JwtSettings__Issuer=<expected issuer>
JwtSettings__Audience=<expected audience>
AllowedHosts=<production API host name>
Security__AllowedCorsOrigins__0=<approved HTTPS origin, when a browser client is used>
```

For local development, use .NET User Secrets rather than editing tracked JSON files:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<local connection string>" --project ExhibitionManagementSystem/ExhibitionManagementSystem.csproj
dotnet user-secrets set "JwtSettings:SecretKey" "<local random secret, at least 32 bytes>" --project ExhibitionManagementSystem/ExhibitionManagementSystem.csproj
```

Never reuse values that were previously committed. Rotate the database credential and JWT signing key, then invalidate existing sessions before deployment.

## Database migration procedure

1. Take and verify a restorable database backup.
2. Generate the SQL script and review it; do not run `database update` directly against production.
3. Resolve any duplicate active booth or merged-booth reservations reported by the preflight checks.
4. Apply the reviewed script in staging and execute the reservation, invoice, payment, and tenant-isolation tests.
5. Apply it in production during a controlled maintenance window and verify `/health/ready`.

Example script generation:

```powershell
$env:ConnectionStrings__DefaultConnection = "<target connection string>"
dotnet ef migrations script 0 `
  --project ExhibitionManagementSystem.DataAccess/ExhibitionManagementSystem.DataAccess.csproj `
  --startup-project ExhibitionManagementSystem/ExhibitionManagementSystem.csproj `
  --context ApplicationDbContext `
  --output artifacts/migrations.sql
```

The migration `AddSponsorshipAndBadgeSchema` closes an existing model/migration drift. Confirm those tables were not created manually before applying it. The concurrency migration backfills reservation tenant ownership, adds row-version guards, and creates filtered unique indexes that prevent duplicate active reservations. `AddAtomicInvoiceSequence` creates the per-tenant, per-year counter used to issue invoice numbers atomically. `AddTenantSessionsIdempotencyAndCreditNotes` backfills tenant ownership for booths, tickets, and payments; migrates existing hashed refresh tokens into device sessions; and adds idempotency and immutable credit-note storage. Its preflight checks intentionally stop if an existing row cannot be assigned to a tenant.

## Idempotent reservation and payment requests

Clients must include a stable `Idempotency-Key` header (maximum 100 characters) when calling `POST /api/reservations` or `POST /api/financial/payments`. Reuse the same key only when retrying the same JSON request. A different body with the same key returns `409 Conflict`; a completed retry replays the original successful response. Records expire after 24 hours.

## Operational checks

- `GET /health/live`: process liveness only.
- `GET /health/ready`: includes database connectivity.
- Every API response includes `X-Correlation-ID`; use it to locate internal logs without returning exception details to clients.
- Run the CI secret scan against full Git history. Removing a secret from the current files does not remove it from older commits.

## Release blockers outside the repository

- Rotate and revoke the exposed database and JWT credentials.
- Review database and hosting logs for suspicious access or unexpected privileged accounts.
- Clean leaked values from Git history if the repository was ever shared, then rotate again.
- Configure real HTTPS certificates and explicit production CORS origins.
- Configure the Desktop `ApiBaseUrl` with the production HTTPS endpoint.
- Test backup restoration and migration rollback/roll-forward in staging.
