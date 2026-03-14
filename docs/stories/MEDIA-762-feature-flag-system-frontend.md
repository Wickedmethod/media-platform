# MEDIA-762: Feature Flag System for Frontend

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Low  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-700 (Project Setup)

---

## Summary

Create a lightweight feature flag system for the Vue frontend. Flags enable safe rollouts of incomplete features, A/B experimentation, and per-environment overrides — without deploying new code. Start with a local-first approach (build-time + localStorage overrides), upgradeable to API-driven flags later.

---

## Architecture

```
Flag Resolution Order (first match wins):
    │
    ├─ 1. localStorage override  (dev/testing)
    ├─ 2. URL query param        (?ff_newSearch=true)
    ├─ 3. API response            (future: /api/v1/flags)
    └─ 4. Build-time defaults     (flags.config.ts)
```

---

## Flag Definition

```typescript
// src/config/flags.config.ts
export interface FeatureFlag {
  key: string
  description: string
  defaultValue: boolean
  /** When true, flag is available for localStorage override */
  overridable: boolean
}

export const FLAGS = {
  newSearch: {
    key: 'ff_newSearch',
    description: 'New Invidious search UI with filters',
    defaultValue: false,
    overridable: true,
  },
  dragReorder: {
    key: 'ff_dragReorder',
    description: 'Drag & drop queue reordering',
    defaultValue: false,
    overridable: true,
  },
  adminDashboard: {
    key: 'ff_adminDashboard',
    description: 'Admin dashboard with analytics',
    defaultValue: true,
    overridable: true,
  },
  multiDevice: {
    key: 'ff_multiDevice',
    description: 'Multi-device personal playback sessions (v2)',
    defaultValue: false,
    overridable: false,
  },
} as const satisfies Record<string, FeatureFlag>

export type FlagKey = keyof typeof FLAGS
```

---

## Composable

```typescript
// src/composables/useFeatureFlags.ts
import { computed, ref, watch } from 'vue'
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

    // 1. Check localStorage override
    if (config.overridable && config.key in overrides.value) {
      return overrides.value[config.key]
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

    overrides.value[config.key] = value
    saveOverrides()
  }

  function clearOverride(flag: FlagKey) {
    delete overrides.value[FLAGS[flag].key]
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
```

---

## Conditional Rendering

### Template Directive Pattern

```vue
<template>
  <NewSearchUI v-if="isEnabled('newSearch')" />
  <LegacySearchUI v-else />
</template>

<script setup lang="ts">
const { isEnabled } = useFeatureFlags()
</script>
```

### Route Guard Pattern

```typescript
// src/router/index.ts
{
  path: '/admin',
  component: AdminDashboard,
  beforeEnter: () => {
    const { isEnabled } = useFeatureFlags()
    if (!isEnabled('adminDashboard')) {
      return { name: 'home' }
    }
  },
}
```

---

## Dev Tools Panel (Admin Only)

A hidden panel accessible via `/admin/flags` (or keyboard shortcut `Ctrl+Shift+F`) showing all flags with toggle switches:

```vue
<!-- src/views/admin/FlagPanel.vue -->
<template>
  <div class="space-y-4 p-6">
    <h2 class="text-lg font-semibold">Feature Flags</h2>
    <div v-for="(config, key) in flags" :key="key" class="flex items-center justify-between">
      <div>
        <p class="font-medium">{{ key }}</p>
        <p class="text-sm text-muted-foreground">{{ config.description }}</p>
      </div>
      <Switch
        :checked="isEnabled(key as FlagKey)"
        :disabled="!config.overridable"
        @update:checked="(val: boolean) => setOverride(key as FlagKey, val)"
      />
    </div>
    <Button variant="outline" size="sm" @click="clearAllOverrides">Reset All</Button>
  </div>
</template>
```

---

## Future: API-Driven Flags

When the platform needs server-controlled rollouts:

```typescript
// GET /api/v1/flags → { "ff_newSearch": true, "ff_multiDevice": false }
// Resolution becomes: localStorage > URL > API > build-time default
```

This is deferred to a future story — the local-first approach covers all current needs.

---

## Tasks

- [ ] Create `flags.config.ts` with typed flag definitions
- [ ] Implement `useFeatureFlags` composable with localStorage + URL params
- [ ] Add `FlagPanel.vue` admin view with toggle switches
- [ ] Add route guard helper for flag-gated routes
- [ ] Use flags in at least one existing feature (e.g., gate drag-reorder)
- [ ] Write unit tests for flag resolution order
- [ ] Write unit tests for localStorage persistence
- [ ] Document flag naming convention (`ff_` prefix) in README

---

## Acceptance Criteria

- [ ] Flags defined as typed constants with descriptions
- [ ] `useFeatureFlags().isEnabled('flagName')` returns correct boolean
- [ ] localStorage overrides take precedence over defaults
- [ ] URL query param `?ff_flagName=true` activates a flag
- [ ] Non-overridable flags ignore localStorage/URL overrides
- [ ] Admin flag panel shows all flags with toggle switches
- [ ] Clearing overrides restores build-time defaults
- [ ] Flag resolution is synchronous (no async loading for local flags)
