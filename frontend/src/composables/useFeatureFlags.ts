import { ref } from 'vue'
import { FLAGS, type FlagKey } from '@/config/flags.config'

const overrides = ref<Record<string, boolean>>(loadOverrides())

function loadOverrides(): Record<string, boolean> {
  try {
    const stored = localStorage.getItem('feature-flags')
    return stored ? JSON.parse(stored) : {}
  } catch {
    return {}
  }
}

function saveOverrides() {
  localStorage.setItem('feature-flags', JSON.stringify(overrides.value))
}

export function useFeatureFlags() {
  function isEnabled(flag: FlagKey): boolean {
    const config = FLAGS[flag]

    // 1. Check localStorage override (only for overridable flags)
    if (config.overridable && config.key in overrides.value) {
      return overrides.value[config.key]!
    }

    // 2. Check URL query param (dev convenience)
    if (typeof window !== 'undefined') {
      const params = new URLSearchParams(window.location.search)
      const urlValue = params.get(config.key)
      if (urlValue !== null) {
        return urlValue === 'true' || urlValue === '1'
      }
    }

    // 3. Build-time default
    return config.defaultValue
  }

  function setOverride(flag: FlagKey, value: boolean) {
    const config = FLAGS[flag]
    if (!config.overridable) return

    overrides.value = { ...overrides.value, [config.key]: value }
    saveOverrides()
  }

  function clearOverride(flag: FlagKey) {
    const { [FLAGS[flag].key]: _, ...rest } = overrides.value
    overrides.value = rest
    saveOverrides()
  }

  function clearAllOverrides() {
    overrides.value = {}
    saveOverrides()
  }

  return {
    isEnabled,
    setOverride,
    clearOverride,
    clearAllOverrides,
    flags: FLAGS,
  }
}
