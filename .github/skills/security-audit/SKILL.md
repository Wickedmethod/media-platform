---
name: security-audit
description: 'Application security audit covering secrets handling, injection prevention, auth/authz validation, CORS, rate limiting, and OWASP Top 10. Use when reviewing security-sensitive code, especially in VaultFacade, ArcaneVaultBridge, and Keycloak-integrated services.'
argument-hint: File, module, or security concern to audit
---

# Security Audit

## When to Use

- Reviewing code that handles secrets, tokens, or credentials.
- Auditing authentication/authorization flows (Keycloak OIDC).
- Checking for injection vulnerabilities (SQL, XSS, command).
- Validating input sanitization and path traversal prevention.
- Reviewing CORS configuration and rate limiting.
- Pre-deployment security review of API endpoints.

## Audit Procedure

1. **Identify scope** — Which files, endpoints, or flows to audit.
2. **Check secrets handling** — Scan for leaked secrets, logging, and storage.
3. **Validate auth flows** — Verify Keycloak integration and role enforcement.
4. **Test input boundaries** — Check all external inputs for validation.
5. **Review dependencies** — Check for known vulnerabilities.
6. **Document findings** — Categorize by severity and provide fixes.

## Secrets Handling (CRITICAL)

### Prohibited Patterns

```typescript
// ❌ NEVER: Log secret values
logger.info(`Secret value: ${secret.value}`);
logger.debug(`Token: ${token}`);
console.log(password);

// ❌ NEVER: Secrets in source code
const API_KEY = 'sk-abc123...';
const DB_PASSWORD = 'mypassword';

// ❌ NEVER: Secrets in error messages
throw new Error(`Failed to decrypt ${secretValue}`);
```

### Required Patterns

```typescript
// ✅ CORRECT: Log only metadata
logger.info(`Secret fetched: path=${secret.path}, version=${secret.version}`);
logger.debug(`Auth token refreshed for service: ${serviceName}`);

// ✅ CORRECT: Mask in responses
function maskSecret(value: string): string {
  if (value.length <= 4) return '****';
  return value.substring(0, 2) + '***' + value.substring(value.length - 2);
}

// ✅ CORRECT: Environment variables or vault
const apiKey = process.env.API_KEY;
const dbPassword = await vaultClient.getSecret('db/credentials');
```

## Input Validation

### Path Traversal Prevention

```typescript
// ✅ CORRECT: Validate secret paths
function validatePath(path: string): boolean {
  if (path.includes('..')) return false;
  if (path.startsWith('/')) return false;
  if (!/^[a-zA-Z0-9\-_/]+$/.test(path)) return false;
  return true;
}
```

### SQL Injection Prevention

```typescript
// ✅ CORRECT: Parameterized queries (TypeORM)
const result = await repo.findOne({ where: { name: userInput } });

// ❌ NEVER: String concatenation in queries
const result = await repo.query(`SELECT * FROM users WHERE name = '${userInput}'`);
```

### XSS Prevention

```vue
<!-- ✅ CORRECT: Vue auto-escapes by default -->
<span>{{ userContent }}</span>

<!-- ❌ NEVER: Unescaped HTML from user input -->
<div v-html="userContent"></div>
```

## Authentication & Authorization

### Keycloak OIDC Checklist

- [ ] All API endpoints require authentication (except health/public).
- [ ] Role-based access control enforced at handler level.
- [ ] Token validation includes issuer, audience, and expiry checks.
- [ ] Refresh token rotation is implemented.
- [ ] Service accounts use client credentials grant (not user passwords).
- [ ] CORS origins are explicitly whitelisted (never `*` in production).

### Role Mapping Verification

| Role | Permissions |
|------|-------------|
| `reader` | Read, list, view audit logs |
| `writer` | All reader + create, update |
| `admin` | All writer + delete, manage policies |

Verify every endpoint enforces the correct minimum role.

## OWASP Top 10 Checklist

| # | Category | Check |
|---|----------|-------|
| A01 | Broken Access Control | Verify role checks on all endpoints |
| A02 | Cryptographic Failures | No hardcoded secrets, proper TLS, strong hashing |
| A03 | Injection | Parameterized queries, input validation, no eval() |
| A04 | Insecure Design | Rate limiting, account lockout, least privilege |
| A05 | Security Misconfiguration | No default credentials, minimal error disclosure |
| A06 | Vulnerable Components | Check `npm audit` / `dotnet list package --vulnerable` |
| A07 | Auth Failures | Token validation, session management, MFA |
| A08 | Data Integrity | Verify signatures, checksums, update mechanisms |
| A09 | Logging Failures | Audit all auth events, never log secrets |
| A10 | SSRF | Validate all outbound URLs, whitelist allowed hosts |

## Dependency Audit Commands

```bash
# Node.js / TypeScript projects
pnpm audit
npm audit --audit-level=high

# .NET projects
dotnet list package --vulnerable
dotnet list package --outdated
```

## Report Format

### Finding Template

```
**Severity**: Critical / High / Medium / Low / Info
**Category**: OWASP A01-A10
**Location**: file.ts:42
**Description**: What the vulnerability is
**Impact**: What could go wrong
**Fix**: Specific code change required
```

## Guardrails

- ✅ Always: Validate all external inputs at system boundaries.
- ✅ Always: Use parameterized queries, never string concatenation.
- ✅ Always: Log operation metadata, never secret values.
- ✅ Always: Enforce authentication and authorization on every endpoint.
- ⚠️ Ask First: Changing CORS configuration or auth middleware.
- 🚫 Never: Hardcode secrets, tokens, or passwords in source code.
- 🚫 Never: Disable TLS verification or certificate validation.
- 🚫 Never: Use `eval()`, `Function()`, or dynamic code execution with user input.
