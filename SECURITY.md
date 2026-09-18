# Security

## Supported version

Security fixes are provided for the latest published 1 Bullet release.

## Reporting a vulnerability

Please do not publish Riot credentials, lockfile contents, authorization headers, access tokens, or other private account data in a public issue.

For ordinary bugs that do not expose sensitive information, use the repository issue tracker.

For a security issue, contact the repository owner privately through an available GitHub contact method before posting technical details publicly.

## Local-data model

1 Bullet is designed so that Riot local-client credentials remain on the user's machine.

- The application backend binds to loopback only.
- Browser CORS access is restricted to loopback origins.
- Lockfile passwords and authorization headers must never be written to release assets or project telemetry.
- Custom background images remain local.
- Update checks use this repository's GitHub Releases endpoint.

## Release security checks

Before publishing a release:

- verify `VERSION` and `runtime.json`
- scan the source artifact for private paths/secrets
- run Python import/compile checks
- build the WPF client in Release mode
- verify installer/portable artifacts
- publish SHA-256 checksums

See `docs/RELEASE_CHECKLIST.md`.
