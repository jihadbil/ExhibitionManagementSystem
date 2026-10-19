# Backend hardening

This change concerns the API, services, persistence, models, and shared DTOs. The desktop layer is outside its scope. It has not been deployed and no production database has been contacted.

## Runtime configuration

Set secrets through the deployment environment or a secret store:

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Explicit SQL Server connection, with encryption and certificate validation appropriate to that server |
| `JwtSettings__SecretKey` | A new, randomly generated signing secret of at least 32 bytes |
| `Bootstrap__AdminEmail` | The explicitly chosen initial platform administrator |
| `Bootstrap__AdminPassword` | Required when creating that administrator for the first time; remove after provisioning |
| `Cors__AllowedOrigins__0`, etc. | Exact browser origins, if browser clients need cross-origin access |
| `PasswordReset__Enabled` | Set `true` only when SMTP and the reset page are configured |
| `PasswordReset__SmtpHost`, `PasswordReset__SmtpPort` | SMTP submission server supporting STARTTLS; default port 587 |
| `PasswordReset__SenderAddress` | Valid sender email address |
| `PasswordReset__SmtpUser`, `PasswordReset__SmtpPassword` | SMTP credentials, when required; configure both together |
| `PasswordReset__ResetUrl` | Absolute HTTPS URL of the application's password reset page |
| `DataProtection__KeysPath` | Absolute persistent key directory, required when reset delivery is enabled |

Startup fails when the signing key, schema migration, or required first-administrator configuration is invalid. A database connection string is never printed. Existing JWTs without the new security stamp claim must be replaced by signing in again. Credentials and signing keys previously stored in the repository must be rotated wherever they were actually used; deleting them from current files does not remove them from Git history. Review existing privileged accounts before reopening access.

Roles are global Identity roles (`SuperAdmin`, `Admin`, `Staff`, `Exhibitor`). Resource authorization remains scoped to the caller's tenant. Tenant administrators cannot grant platform roles or modify platform-administrator accounts. Registration is an authenticated tenant-administration operation, and visitor registration also requires authentication for this closed deployment. Public invitations/self-registration would require a separately designed tenant-resolution flow.

Tenant DELETE deactivates the tenant and retains financial/history records. Deactivation prevents login, refresh, and protected requests. Account disabling, role changes, and password changes invalidate sessions. Platform-wide exchange-rate changes require `SuperAdmin`.

## Database upgrade

`20261019092447_HardenBackendIntegrity` joins existing collection navigations to their actual foreign keys, creates missing badge/sponsorship tables, adds row versions, and prevents duplicate active booth/merge bookings per exhibition.

`20261019101604_CompleteBackendHardening` adds payment replay keys, an invoice number sequence, a unique active advertising-space assignment index, and the encrypted password-reset outbox. Existing payments retain null replay keys. The sequence starts above the largest existing numeric `INV-` suffix. Duplicate active advertising assignments stop this migration before DDL; reconcile them explicitly. Its downgrade refuses to discard payment replay keys or outbox history.

The migration stops before altering tables if a legacy shadow relationship disagrees with its explicit foreign key or active bookings conflict. It deliberately does not pick a winner or delete financial records. Reconcile those conflicts in a reviewed data-repair operation and retry. Its downgrade refuses to drop populated badge/sponsorship tables.

Review the SQL and test a backup/representative copy before a real upgrade. The tests use an isolated SQL Server; their successful execution does not inspect or certify production data. EF tooling honors environment configuration and has no implicit fallback database. Run tooling with an explicit test or deployment connection and the intended environment.

## Financial behavior

- Payment amounts must be positive, have at most two fractional digits, and match the invoice currency. Draft/cancelled/paid invoices cannot accept a payment.
- New payment requests require `idempotencyKey` (1–100 ASCII letters, digits, `.`, `_`, `:`, or `-`). Generate it once per intended collection and preserve it across retries. It is case-sensitive and scoped to the invoice. Repeating the same payload returns the original payment, including after full settlement; reusing the key with different amount, method, currency, reference or notes returns 409. Existing client integrations must supply this field.
- Invoice numbers use a SQL sequence shared across application instances. They are unique identifiers, with intentional gaps after rollback; they are not a gap-free fiscal numbering policy. A concurrency conflict returns 409. Reload before retrying, retaining the payment key when retrying collection.
- Invoice line totals round individually to two decimal places, away from zero at a midpoint, before invoice totals are summed. Tax follows the same rounding rule.
- Paid, partially paid, and cancelled invoices cannot have their items or reservation services changed. An unpaid invoice's item change and totals are saved atomically with its audit log.
- Reservation updates cannot move a booking to a different booth/merge or overwrite computed amounts. Cancel an unpaid booking and create a correctly priced replacement to reallocate it. A cancelled booking cannot be reapproved; settled payments must be resolved before cancellation.
- Prices are converted from the rule currency into the requested reservation currency. Unqualified service default prices use the tenant base currency. Decimal service quantities are preserved.
- The dashboard reports completed cash receipts and outstanding balances per invoice. Exhibition financial reports use issued, non-cancelled invoice totals. Totals in multiple currencies use the rates applicable on the report generation date; missing rates fail explicitly. Historical exchange-rate restatement and accounting/refund policy remain separate product decisions.
- Business records and audit logs share the transaction. An explicit actor and tenant are required; an absent actor is never attributed to the first database user.

## Sponsorship behavior

Active packages enforce their capacity inside a serializable transaction. Non-cancelled, non-deleted contracts consume capacity, including completed contracts. Cancelling or deleting a contract releases its active advertising assignments atomically; the assignments remain in soft-deleted history. Completed contracts also release advertising spaces. Completed or cancelled contracts cannot be reactivated.

An advertising space has at most one active assignment, enforced in SQL as well as the service. Creating a contract with an invalid/unavailable space rolls back the entire contract. An assigned space cannot be marked available, moved, or deleted through a metadata update. Packages and sponsors with retained contracts cannot be deleted. Capacity cannot be lowered below existing allocations.

## Password reset delivery

The forgot-password endpoints (`/api/Auth/forgot-password` and the existing `/api/Auth/reset-password` alias) return a generic success for known and unknown accounts when delivery is enabled. When delivery is disabled, both return 503. They never return the reset token. The configured HTTPS page receives URL-encoded `email` and `token`; it submits these with `newPassword` and `confirmPassword` to `/api/Auth/reset-password/confirm` (204 on success).

Messages are encrypted using ASP.NET Data Protection before entering the SQL outbox. A background worker claims messages using a row version and a five-minute lease, sends over STARTTLS with a 30-second timeout, and retries temporary failures up to five attempts. Outbox messages expire after 30 minutes and are discarded if the account/tenant is disabled or its security stamp changes. Terminal messages clear the encrypted payload. A repeated request within one minute reuses a pending message.

Delivery is at least once: a crash after SMTP accepts a message but before SQL records completion can resend the email. The reset token itself becomes invalid when consumed. Delivery retries survive application restarts only when all application instances share the same persistent Data Protection key ring and application name. Restrict that directory to the application identity, treat it as a secret, and provide storage encryption appropriate to the deployment. SMTP credentials, the reset page, and key storage must be configured in the target environment; the regression suite uses a fake transport and makes no external email delivery claim.

## Input and query bounds

The API validates ordering of exhibition/schedule/pricing dates, defined enum values, key numeric precision/ranges, and positive resource IDs in create contracts. Validation errors return 400. API pagination requires positive values, limits page size to 200 and small list limits to 100, and rejects offsets exceeding SQL's supported integer range. Visitor search requires a nonblank term of at most 200 characters and returns at most 100 ordered results, with ticket counts projected in SQL. User page roles are loaded in one query for the page; rating summaries aggregate in SQL. Invoice reads use split queries to avoid multiplying payment and item rows. Request cancellation is passed through application queries and audited transactions; rollback remains possible after cancellation.

This does not replace domain-specific validation or production workload measurements. Broader validation coverage, remaining unpaged domain lists, load tests, and architectural separation remain follow-up work.

## Verification

With .NET 10, Python 3, and a local Docker daemon:

```sh
python3 tests/run-backend-tests.py --verbosity normal
```

The runner creates a dedicated SQL Server container, binds its random port to loopback, generates temporary credentials, and removes its own container on exit. The test fixture rejects database names outside `BackendRegression_` and non-local servers. It uses the actual migration chain and HTTP pipeline, not `EnsureCreated`. The CI workflow runs this same command without building the desktop project.

The regression suite covers role escalation, tenant isolation, disabled sessions, relationships, payment replay/overpayment, paid invoices, fractional rounding/services, orphan removal, ticket dates, atomic audit writes, currency conversion, reports, concurrent collection/capacity/space assignment, migration reconciliation, bounded search, request cancellation, user query counts, and encrypted/retried password-reset delivery through a fake transport. It uses actual SQL Server migrations, including upgrades with existing financial and conflicting sponsorship records.

Validation on 2026-10-19: all 47 regression tests passed, none skipped. The Release API build passed with no errors and 170 existing nullable-analysis warnings. The NuGet audit of direct and transitive backend packages reported no known vulnerabilities at that time. No desktop files were changed, and no production migration or external email delivery was performed.
