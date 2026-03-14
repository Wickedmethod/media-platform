import { defineStore } from "pinia";
import { ref, computed } from "vue";
import type Keycloak from "keycloak-js";

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  roles: string[];
}

export const useAuthStore = defineStore("auth", () => {
  const keycloak = ref<Keycloak | null>(null);
  const initialized = ref(false);
  const devUser = ref<AuthUser | null>(null);

  const isAuthenticated = computed(
    () => devUser.value !== null || (keycloak.value?.authenticated ?? false),
  );

  const token = computed(() => {
    if (devUser.value) return "dev-token";
    return keycloak.value?.token;
  });

  const user = computed<AuthUser | null>(() => {
    if (devUser.value) return devUser.value;
    if (!keycloak.value?.tokenParsed) return null;
    const t = keycloak.value.tokenParsed;
    return {
      id: t.sub ?? "",
      name: (t.preferred_username as string) ?? "",
      email: (t.email as string) ?? "",
      roles: t.realm_access?.roles ?? [],
    };
  });

  const userName = computed(() => user.value?.name);

  const roles = computed<string[]>(() => user.value?.roles ?? []);

  const hasRole = (role: string) => roles.value.includes(role);
  const isAdmin = computed(() => hasRole("media-admin"));
  const isOperator = computed(() => hasRole("media-operator") || isAdmin.value);

  function setKeycloak(kc: Keycloak) {
    keycloak.value = kc;
    initialized.value = true;
  }

  function setDevUser(u: AuthUser) {
    devUser.value = u;
    initialized.value = true;
  }

  async function refreshToken(minValidity = 30): Promise<boolean> {
    if (devUser.value) return true;
    if (!keycloak.value) return false;
    try {
      return await keycloak.value.updateToken(minValidity);
    } catch {
      return false;
    }
  }

  function logout() {
    if (devUser.value) {
      devUser.value = null;
      initialized.value = false;
      return;
    }
    keycloak.value?.logout();
  }

  return {
    keycloak,
    initialized,
    isAuthenticated,
    token,
    user,
    userName,
    roles,
    isAdmin,
    isOperator,
    hasRole,
    setKeycloak,
    setDevUser,
    refreshToken,
    logout,
  };
});
