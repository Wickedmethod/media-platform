# MEDIA-709: Error Handling & Toast Notification System

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** High  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-700 (project setup)  
**Absorbs:** MEDIA-761 (Global Error Boundary & Fallback UI)

---

## Summary

Create a global error handling system with toast notifications for the Vue SPA. Every API error, validation failure, and connection issue surfaces as a clear, actionable toast — no silent failures.

---

## Architecture

```
API Response (error)
    │
    ▼
Custom Fetch Mutator (api-client.ts)
    │ throws ApiError
    ▼
TanStack Query onError
    │
    ▼
useToast composable
    │
    ▼
ToastContainer (renders toasts)
```

---

## Toast Types

Toasts are triggered from two sources:

### 1. API Error Toasts (automatic)
All API errors surface as toasts via the global error handler (see below).

### 2. SSE Event Toasts (real-time)
When another user adds a song, all connected clients see a toast:
```
┌──────────────────────────────┐
│ 🎵 @jonas added a song       │
│ Bohemian Rhapsody — Queen    │
└──────────────────────────────┘
```
This is triggered by the `item-added` SSE event in `usePlayerStore` (see MEDIA-704).

---

## Toast Composable

```typescript
// src/composables/useToast.ts
interface Toast {
  id: string
  type: 'success' | 'error' | 'warning' | 'info'
  title: string
  message?: string
  duration?: number    // ms, default 5000. 0 = persistent
  action?: { label: string; onClick: () => void }
}

const toasts = ref<Toast[]>([])

export function useToast() {
  function show(toast: Omit<Toast, 'id'>) {
    const id = crypto.randomUUID()
    toasts.value.push({ ...toast, id })

    if (toast.duration !== 0) {
      setTimeout(() => dismiss(id), toast.duration ?? 5000)
    }
  }

  function dismiss(id: string) {
    toasts.value = toasts.value.filter(t => t.id !== id)
  }

  // Convenience methods
  const success = (title: string, message?: string) =>
    show({ type: 'success', title, message })

  const error = (title: string, message?: string) =>
    show({ type: 'error', title, message, duration: 8000 })

  const warning = (title: string, message?: string) =>
    show({ type: 'warning', title, message })

  return { toasts: readonly(toasts), show, dismiss, success, error, warning }
}
```

---

## API Error Classification

```typescript
// src/lib/api-error.ts
export class ApiError extends Error {
  constructor(
    public status: number,
    public body: { detail?: string; errors?: Record<string, string[]> },
  ) {
    super(body.detail ?? `HTTP ${status}`)
  }

  get isValidation() { return this.status === 400 }
  get isUnauthorized() { return this.status === 401 }
  get isForbidden() { return this.status === 403 }
  get isNotFound() { return this.status === 404 }
  get isConflict() { return this.status === 409 }
  get isRateLimited() { return this.status === 429 }
  get isServerError() { return this.status >= 500 }
}
```

### Error → Toast Mapping

| HTTP Status | Toast Type | Message | Action |
|-------------|-----------|---------|--------|
| 400 | `warning` | Validation details from `errors` field | — |
| 401 | `error` | "Session expired" | "Log in" → redirect |
| 403 | `error` | "Not authorized for this action" | — |
| 404 | `warning` | "Item not found" | — |
| 409 | `warning` | "Conflict — item already exists" | — |
| 429 | `warning` | "Too many requests. Retry in {n}s" | Auto-retry button |
| 500+ | `error` | "Server error. Try again later." | "Retry" button |
| Network | `error` | "No connection to server" | "Retry" button |

---

## Global Error Handler

```typescript
// src/plugins/error-handler.ts
export function setupGlobalErrorHandler(app: App) {
  const { error: showError } = useToast()

  // Vue error boundary
  app.config.errorHandler = (err, instance, info) => {
    console.error('Vue error:', err, info)
    showError('Something went wrong', err instanceof Error ? err.message : 'Unknown error')
  }

  // Unhandled promise rejections
  window.addEventListener('unhandledrejection', (event) => {
    if (event.reason instanceof ApiError) {
      handleApiError(event.reason)
      event.preventDefault() // Don't pollute console
    }
  })
}

function handleApiError(err: ApiError) {
  const { error: showError, warning: showWarning } = useToast()
  const authStore = useAuthStore()

  if (err.isUnauthorized) {
    showError('Session expired', 'Please log in again')
    authStore.logout()
    return
  }

  if (err.isRateLimited) {
    const retryAfter = parseInt(err.body.detail?.match(/\d+/)?.[0] ?? '5')
    showWarning('Too many requests', `Try again in ${retryAfter} seconds`)
    return
  }

  if (err.isForbidden) {
    showError('Not authorized', 'You don\'t have permission for this action')
    return
  }

  if (err.isServerError) {
    showError('Server error', 'Something went wrong. Try again later.')
    return
  }

  if (err.isValidation && err.body.errors) {
    const messages = Object.values(err.body.errors).flat().join(', ')
    showWarning('Validation error', messages)
    return
  }

  showError('Error', err.message)
}
```

---

## Toast UI Component

Using shadcn-vue Toast (or Sonner):

```vue
<!-- src/shared/components/ToastContainer.vue -->
<template>
  <Teleport to="body">
    <div class="fixed top-4 right-4 z-[100] flex flex-col gap-2 max-w-sm">
      <TransitionGroup name="toast">
        <div
          v-for="toast in toasts"
          :key="toast.id"
          :class="[
            'rounded-lg border px-4 py-3 shadow-lg backdrop-blur-sm',
            typeClasses[toast.type],
          ]"
        >
          <div class="flex items-start gap-3">
            <component :is="typeIcons[toast.type]" class="h-5 w-5 shrink-0 mt-0.5" />
            <div class="flex-1 min-w-0">
              <p class="font-medium text-sm">{{ toast.title }}</p>
              <p v-if="toast.message" class="text-xs mt-1 opacity-80">{{ toast.message }}</p>
            </div>
            <button @click="dismiss(toast.id)" class="shrink-0 opacity-50 hover:opacity-100">
              <X class="h-4 w-4" />
            </button>
          </div>
          <button
            v-if="toast.action"
            @click="toast.action.onClick"
            class="mt-2 text-xs font-medium underline"
          >
            {{ toast.action.label }}
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>
```

### Style by Type

| Type | Background | Border | Icon |
|------|-----------|--------|------|
| `success` | `bg-emerald-500/10` | `border-emerald-500/20` | CheckCircle |
| `error` | `bg-red-500/10` | `border-red-500/20` | AlertCircle |
| `warning` | `bg-amber-500/10` | `border-amber-500/20` | AlertTriangle |
| `info` | `bg-blue-500/10` | `border-blue-500/20` | Info |

---

## TanStack Query Integration

```typescript
// src/plugins/query-client.ts
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => {
        // Don't retry auth errors
        if (error instanceof ApiError && (error.isUnauthorized || error.isForbidden)) {
          return false
        }
        return failureCount < 2
      },
      staleTime: 10_000, // 10s
    },
    mutations: {
      onError: (error) => {
        // Global mutation error handler
        if (error instanceof ApiError) {
          handleApiError(error)
        }
      },
    },
  },
})
```

---

## Global Error Boundary & Fallback UI (absorbed from MEDIA-761)

Vue's `onErrorCaptured` lifecycle hook enables component-level error boundaries. Wrap route views in an `ErrorBoundary` component that catches render errors and displays a recovery UI instead of a blank screen.

```vue
<!-- src/shared/components/ErrorBoundary.vue -->
<script setup lang="ts">
import { ref, onErrorCaptured } from 'vue'
import FallbackError from './FallbackError.vue'

const error = ref<Error | null>(null)

onErrorCaptured((err: Error) => {
  error.value = err
  return false // prevent propagation
})

function retry() {
  error.value = null
}
</script>

<template>
  <FallbackError v-if="error" :error="error" @retry="retry" />
  <slot v-else />
</template>
```

```vue
<!-- src/shared/components/FallbackError.vue -->
<script setup lang="ts">
defineProps<{ error: Error }>()
defineEmits<{ retry: [] }>()
</script>

<template>
  <div class="flex flex-col items-center justify-center min-h-[200px] gap-4 p-8 text-center">
    <AlertCircle class="h-12 w-12 text-destructive" />
    <h2 class="text-lg font-semibold">Something went wrong</h2>
    <p class="text-sm text-muted-foreground max-w-md">{{ error.message }}</p>
    <Button variant="outline" @click="$emit('retry')">Try Again</Button>
  </div>
</template>
```

Usage in `App.vue`:

```vue
<ErrorBoundary>
  <RouterView />
</ErrorBoundary>
```

---

## Tasks

- [ ] Create `ApiError` class in `src/lib/api-error.ts`
- [ ] Create `useToast` composable with show/dismiss/convenience methods
- [ ] Create `ToastContainer.vue` with animated toasts
- [ ] Set up global error handler in `src/plugins/error-handler.ts`
- [ ] Configure TanStack Query default error handling
- [ ] Handle 401 → redirect to Keycloak login
- [ ] Handle 429 → show retry countdown
- [ ] Add error boundary for component-level errors (`onErrorCaptured`)
- [ ] Create `ErrorBoundary.vue` wrapper component with fallback UI
- [ ] Create `FallbackError.vue` with "Something went wrong" + retry button
- [ ] Wire `app.config.errorHandler` to log + toast unhandled Vue errors
- [ ] Write unit tests for error classification
- [ ] Write unit tests for toast composable

---

## Acceptance Criteria

- [ ] API errors surface as typed toasts (not silent failures)
- [ ] 401 triggers automatic logout + "Session expired" toast
- [ ] 429 shows retry countdown with "Try again in Xs"
- [ ] 403 shows "Not authorized" toast
- [ ] Validation errors show specific field messages
- [ ] Network errors show "No connection" with retry button
- [ ] Toasts auto-dismiss after 5s (errors after 8s)
- [ ] Toasts can be manually dismissed
- [ ] Toasts stack vertically, max 3 visible at once
- [ ] Unhandled promise rejections caught and displayed
- [ ] `ErrorBoundary.vue` catches component render errors and shows fallback UI
- [ ] Fallback UI shows error message + "Try Again" button that re-renders the slot
- [ ] Route-level `ErrorBoundary` wraps `<RouterView>` in App.vue
- [ ] `app.config.errorHandler` logs errors and shows toast for unhandled Vue errors
