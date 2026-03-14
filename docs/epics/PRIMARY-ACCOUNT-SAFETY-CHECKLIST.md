# Primary Account Safety Checklist

Use this checklist before enabling your primary Google account in production.

## Account and OAuth

- [ ] OAuth is used for Google consent, never password entry in app.
- [ ] Scopes are least privilege and reviewed.
- [ ] Separate app credentials are used for this project only.

## Token and Secret Safety

- [ ] Refresh token is stored only in Vault.
- [ ] Access tokens are short-lived and not persisted.
- [ ] Tokens and secrets are redacted from logs.
- [ ] Secret rotation policy is defined and tested.

## Access Control

- [ ] Keycloak protects all YouTube write endpoints.
- [ ] Role-based access is configured with least privilege.
- [ ] Rate limits are enabled for sensitive actions.

## Monitoring and Incident Response

- [ ] Security alerts are configured and tested.
- [ ] Emergency revoke and kill switch are operational.
- [ ] Compromise runbook exists and a recovery drill has been completed.

## Network and Runtime

- [ ] API write actions are restricted to trusted network paths.
- [ ] TLS is enforced end-to-end where possible.
- [ ] Health checks and audit logs are active.
