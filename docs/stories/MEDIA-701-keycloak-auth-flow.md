# MEDIA-701: Keycloak Authentication Flow

## Story

**Epic:** MEDIA-FE-ADMIN — Admin & User Frontend  
**Priority:** Critical  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-700

---

## Summary

Integrate Keycloak login into the frontend. All users must authenticate before using the app. The Keycloak realm `homelab` already exists. A new client `media-platform-frontend` is created for this SPA.

---

## Auth Flow

```
User opens app
  → keycloak-js checks for valid token
  → If no token → redirect to Keycloak login page
  → User logs in → redirect back with JWT
  → Frontend stores tokens in memory (NOT localStorage)
  → API calls include Authorization: Bearer <token>
  → Token refresh happens silently via keycloak-js
```

---

## Roles (from Keycloak)

| Role | Maps to | Capabilities |
|------|---------|-------------|
| `media-admin` | Admin | Full control: skip, stop, kill switch, policies, audit log |
| `media-user` | User | Add to queue, view queue, see now-playing, vote |

If no `media-admin` role → user gets User capabilities.

---

## Implementation

### Pinia Auth Store (`stores/auth.ts`)

```typescript
interface AuthState {
  keycloak: Keycloak | null
  authenticated: boolean
  user: {
    id: string
    name: string
    email: string
    roles: ('media-admin' | 'media-user')[]
  } | null
  isAdmin: boolean
}
```

### Keycloak Client Config

```json
{
  "clientId": "media-platform-frontend",
  "realm": "homelab",
  "url": "http://keycloak:8080"
}
```

### Route Guards

- `/admin/*` routes → requires `media-admin` role
- All other routes → requires authentication
- Unauthenticated users → redirect to Keycloak

### API Integration

- All API calls include `Authorization: Bearer <token>` header
- On 401 → attempt token refresh → if fails → redirect to login
- Generated Orval client configured with auth interceptor

---

## Tasks

- [ ] Create Keycloak client `media-platform-frontend` (public client, SPA)
- [ ] Install and configure `keycloak-js`
- [ ] Create `stores/auth.ts` Pinia store
- [ ] Init Keycloak before Vue app mount (`main.ts`)
- [ ] Add `Authorization` header to all API calls (Orval/axios interceptor)
- [ ] Create route guard middleware
- [ ] Add user info display in nav (name, avatar, role badge)
- [ ] Add logout button
- [ ] Handle token refresh edge cases
- [ ] Test with Keycloak running locally

---

## Acceptance Criteria

- [ ] Unauthenticated users are redirected to Keycloak login
- [ ] After login, user info is available in auth store
- [ ] API calls include Bearer token
- [ ] Admin routes are hidden/blocked for non-admin users
- [ ] Logout clears session and redirects to Keycloak logout
- [ ] Token refresh works silently before expiration

---

## Notes

- Tokens stored in memory only (not localStorage) for security
- The API's JWT validation is already implemented (MEDIA-604)
- Keycloak `media-admin` and `media-user` realm roles must be created
