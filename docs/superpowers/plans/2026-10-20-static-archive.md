# Static archive implementation plan

**Goal:** Keep the completed tournament browsable on GitHub Pages without paid resources.

1. Add exporter encryption tests using actual ASP.NET Identity hashes; prove the initial implementation fails, then implement authenticated encryption and strict hash parsing.
2. Add a console exporter referencing Infrastructure. Build DTO response maps with existing services; never start hosted workers. Store public responses and per-account ciphertext in an external directory.
3. Add an archive Axios adapter and browser crypto module. Preserve existing routes, reject writes, retain session-only unlock keys, and clear query caches on account changes.
4. Add a small archive notice to existing layout/login. Preserve the current visual design. Add browser tests with synthetic encrypted account data, checking wrong/correct login, read views, reload/logout, role isolation and no API traffic.
5. Restore the existing backup locally, export all permitted reads and verify every encrypted account payload against its original subkey without exposing keys. Build a private deployment artifact; check counts and sensitive-field boundaries.
6. Review the diff, run relevant checks, publish the encrypted artifact and open a PR. Honor required checks and review gates before merging. Verify the final public deployment with light smoke checks.
