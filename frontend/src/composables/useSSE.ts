import { ref, onUnmounted } from 'vue'

export interface UseSSEOptions {
  url: string
  withCredentials?: boolean
  queryParams?: Record<string, string>
  onEvent?: (event: string, data: unknown) => void
  onError?: (error: Event) => void
  reconnectInterval?: number
  maxReconnectAttempts?: number
}

export function useSSE(options: UseSSEOptions) {
  const connected = ref(false)
  const reconnectAttempts = ref(0)
  const maxAttempts = options.maxReconnectAttempts ?? 10
  const interval = options.reconnectInterval ?? 3000

  let eventSource: EventSource | null = null
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null

  function buildUrl(): string {
    const url = new URL(options.url, window.location.origin)
    if (options.queryParams) {
      for (const [key, value] of Object.entries(options.queryParams)) {
        url.searchParams.set(key, value)
      }
    }
    return url.toString()
  }

  function connect() {
    if (eventSource) {
      eventSource.close()
    }

    eventSource = new EventSource(buildUrl(), {
      withCredentials: options.withCredentials ?? false,
    })

    eventSource.onopen = () => {
      connected.value = true
      reconnectAttempts.value = 0
    }

    eventSource.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data) as unknown
        options.onEvent?.('message', data)
      } catch {
        options.onEvent?.('message', event.data)
      }
    }

    eventSource.onerror = (error) => {
      connected.value = false
      options.onError?.(error)
      eventSource?.close()
      scheduleReconnect()
    }

    eventSource.addEventListener('event', (event: MessageEvent) => {
      try {
        const parsed = JSON.parse(event.data) as { type?: string; data?: unknown }
        if (parsed.type) {
          options.onEvent?.(parsed.type, parsed.data)
        }
      } catch {
        // ignore malformed events
      }
    })
  }

  function scheduleReconnect() {
    if (reconnectAttempts.value >= maxAttempts) return

    reconnectTimer = setTimeout(() => {
      reconnectAttempts.value++
      connect()
    }, interval)
  }

  function disconnect() {
    if (reconnectTimer) {
      clearTimeout(reconnectTimer)
      reconnectTimer = null
    }
    eventSource?.close()
    eventSource = null
    connected.value = false
  }

  onUnmounted(disconnect)

  return {
    connected,
    reconnectAttempts,
    connect,
    disconnect,
  }
}
