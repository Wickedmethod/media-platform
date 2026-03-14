import { ref, readonly } from 'vue'

export interface Toast {
  id: string
  type: 'success' | 'error' | 'warning' | 'info'
  title: string
  message?: string
  duration?: number
  action?: { label: string; onClick: () => void }
}

const toasts = ref<Toast[]>([])

const MAX_TOASTS = 5

export function useToast() {
  function show(toast: Omit<Toast, 'id'>) {
    const id = crypto.randomUUID()
    toasts.value.push({ ...toast, id })

    // Cap visible toasts
    if (toasts.value.length > MAX_TOASTS) {
      toasts.value = toasts.value.slice(-MAX_TOASTS)
    }

    if (toast.duration !== 0) {
      setTimeout(() => dismiss(id), toast.duration ?? 5000)
    }
  }

  function dismiss(id: string) {
    toasts.value = toasts.value.filter((t) => t.id !== id)
  }

  function dismissAll() {
    toasts.value = []
  }

  const success = (title: string, message?: string) =>
    show({ type: 'success', title, message })

  const error = (title: string, message?: string) =>
    show({ type: 'error', title, message, duration: 8000 })

  const warning = (title: string, message?: string) =>
    show({ type: 'warning', title, message })

  const info = (title: string, message?: string) =>
    show({ type: 'info', title, message })

  return { toasts: readonly(toasts), show, dismiss, dismissAll, success, error, warning, info }
}
