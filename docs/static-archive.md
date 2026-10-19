# Completed tournament archive

The production frontend runs with `VITE_ARCHIVE_MODE=true`. Existing views use a local Axios adapter backed by immutable JSON files. No DigitalOcean resource or runtime API is needed. All mutations except local login/logout return 409.

## Export

Restore the private backup into a disposable PostgreSQL 18 database on localhost. Supply `ConnectionStrings__DefaultConnection` through the process environment, then run:

```text
dotnet run --configuration Release --project backend/WorldCupTyper.ArchiveExporter -- <external-empty-output-directory>
```

The exporter refuses remote database hosts and nonempty output directories. It uses existing application services without starting background workers. The output includes the same anonymous response DTOs already exposed by the public API and an independently encrypted response map for each active account. Player maps omit admin endpoints. Full password hashes, JWT secrets and notification credentials are not included. Accounts requiring a password change must be resolved before export rather than bypassing that requirement.

Do not store production exports (including ciphertext) in Git. The output is stored as the `archive-data.zip` release asset on `tournament-archive-2026-10-20`; the existing Pages workflow downloads it to the ignored `frontend/public/archive/` directory when building.

## Password compatibility and boundaries

The exporter reads ASP.NET Identity PBKDF2 parameters. It derives an archive key using SHA-256 of a domain prefix followed by the existing derived subkey, then encrypts with AES-256-GCM and a fresh random 96-bit IV. Only the KDF parameters and encrypted data are published; the password verifier/subkey is not published. Login aliases are SHA-256 digests of normalized existing names/emails. The browser derives the same key from the entered password and authenticates the ciphertext. Unlock keys stay in sessionStorage and are cleared on logout; query caches are also cleared on account changes.

This is encrypted offline access, not a live identity service. Anyone can download the ciphertext and attempt passwords offline. There is no server-side rate limiting, password reset, immediate revocation or update mechanism. Removing/changing an account requires a new export and deployment; previously downloaded copies remain decryptable. The private original database backup remains the recovery source.

## Verification

```text
dotnet test backend/WorldCupTyper.sln --configuration Release
npm --prefix frontend run build
```

From `frontend/`, run `npx playwright test -c playwright.archive.config.ts`. Tests use synthetic data, verify existing-password derivation, incorrect password/tampering failures, desktop/mobile read views, refresh/logout, rejected writes, role separation and no API traffic. Existing smoke tests run with the regular application mode; the Pages deployment alone switches to archive mode.
