# Static tournament archive

The tournament is complete. Preserve existing login, player/admin read views, predictions, results, ranking charts and final recaps on GitHub Pages with no hosted API or database. All mutations are rejected with a clear archive message.

Export exact read responses through the existing application services against a local restore of the verified backup. Do not start hosted workers or contact external providers. Export only active accounts. Retain authorization by encrypting each account's response map separately, using its existing ASP.NET Identity PBKDF2 subkey and domain-separated SHA-256/AES-256-GCM. Only public endpoint responses and hashed login aliases are unencrypted. Do not publish password hashes or raw encryption keys.

The browser derives the encryption key from the entered password and public KDF parameters. AES-GCM authentication rejects incorrect passwords. Session keys last only for the browser tab session. An Axios adapter serves the existing DTOs without any API requests. Public endpoints remain public; player and admin response maps contain only the reads already permitted to that role. Clear cached queries when switching accounts.

Keep all production exports, including ciphertext, outside Git. Deployment obtains the generated archive from a release asset. Password reset, revocation and live updates require rebuilding the archive. This provides offline encrypted access, not server-side rate limiting or revocation. Test compatibility, wrong passwords, logout/reload, role separation, data immutability and zero API traffic before deployment.
