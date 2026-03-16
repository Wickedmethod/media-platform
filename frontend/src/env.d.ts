/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string;
  readonly VITE_API_EVENTS_URL: string;
  readonly VITE_KEYCLOAK_URL: string;
  readonly VITE_KEYCLOAK_REALM: string;
  readonly VITE_KEYCLOAK_CLIENT_ID: string;
  readonly VITE_WORKER_KEY: string;
  readonly VITE_AUTH_BYPASS: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
