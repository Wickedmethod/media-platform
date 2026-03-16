export const config = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  apiEventsUrl: import.meta.env.VITE_API_EVENTS_URL || '/api/events',
  keycloakUrl: import.meta.env.VITE_KEYCLOAK_URL || 'http://keycloak:8080',
  keycloakRealm: import.meta.env.VITE_KEYCLOAK_REALM || 'media-platform',
  keycloakClientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID || 'media-platform-web',
} as const
