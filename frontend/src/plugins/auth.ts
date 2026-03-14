import Keycloak from "keycloak-js";
import { config } from "@/config";
import { useAuthStore } from "@/stores/auth";

const DEV_BYPASS = import.meta.env.VITE_AUTH_BYPASS === "true";

export async function initAuth(): Promise<void> {
  const auth = useAuthStore();

  if (DEV_BYPASS) {
    auth.setDevUser({
      id: "dev-user-001",
      name: "Dev Admin",
      email: "dev@localhost",
      roles: ["media-admin", "media-user"],
    });
    return;
  }

  const keycloak = new Keycloak({
    url: config.keycloakUrl,
    realm: config.keycloakRealm,
    clientId: config.keycloakClientId,
  });

  try {
    const authenticated = await keycloak.init({
      onLoad: "login-required",
      checkLoginIframe: false,
      pkceMethod: "S256",
    });

    auth.setKeycloak(keycloak);

    if (!authenticated) {
      keycloak.login();
      return;
    }

    // Silent token refresh
    setInterval(async () => {
      try {
        await keycloak.updateToken(60);
      } catch {
        keycloak.login();
      }
    }, 30_000);
  } catch (err) {
    console.error("[auth] Keycloak init failed:", err);
    throw err;
  }
}
