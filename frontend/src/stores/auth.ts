import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type Keycloak from 'keycloak-js'

export const useAuthStore = defineStore('auth', () => {
  const keycloak = ref<Keycloak | null>(null)
  const initialized = ref(false)

  const isAuthenticated = computed(() => keycloak.value?.authenticated ?? false)
  const token = computed(() => keycloak.value?.token)
  const userName = computed(() => keycloak.value?.tokenParsed?.preferred_username as string | undefined)

  const roles = computed<string[]>(() => {
    const realmRoles = keycloak.value?.tokenParsed?.realm_access?.roles ?? []
    return realmRoles
  })

  const hasRole = (role: string) => roles.value.includes(role)
  const isAdmin = computed(() => hasRole('media-admin'))
  const isOperator = computed(() => hasRole('media-operator') || isAdmin.value)

  function setKeycloak(kc: Keycloak) {
    keycloak.value = kc
    initialized.value = true
  }

  async function refreshToken(minValidity = 30): Promise<boolean> {
    if (!keycloak.value) return false
    try {
      return await keycloak.value.updateToken(minValidity)
    } catch {
      return false
    }
  }

  function logout() {
    keycloak.value?.logout()
  }

  return {
    keycloak,
    initialized,
    isAuthenticated,
    token,
    userName,
    roles,
    isAdmin,
    isOperator,
    hasRole,
    setKeycloak,
    refreshToken,
    logout,
  }
})
