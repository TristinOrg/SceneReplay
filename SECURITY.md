# Security Policy

## Supported versions

Security fixes are currently applied to the latest tagged release.

## Reporting a vulnerability

Do not open a public issue for a vulnerability that could expose credentials, execute untrusted code, exhaust resources, or compromise generated project data. Report it privately through GitHub Security Advisories for `TristinOrg/SceneReplay`.

Include the affected version, reproduction conditions, expected impact, and the smallest safe proof of concept. Do not include production credentials or private project data.

## Untrusted model documents

Treat externally generated SceneReplay JSON as untrusted input. Validate every document, configure platform-appropriate `ShapeValidationLimits`, keep AI-provider credentials outside SceneReplay documents, and do not add custom generators that execute commands or load arbitrary paths from shape parameters.
