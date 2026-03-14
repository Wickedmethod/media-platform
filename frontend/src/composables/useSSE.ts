import { ref, onUnmounted, computed } from 'vue'

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'failed'

export interface UseSSEOptions {
  url: string
  withCredentials?: boolean
  queryParams?: Record<string, string>
  onEvent?: (event: string, data: unknown) => void
  onError?: (error: Event) => void
  maxReconnectAttempts?: number
  heartbeatTimeoutMs?: number
}

/** SSE event names emitted by the backend /events endpoint */
const SSE_EVENT_TYPES = [
  'state-changed',
  'track-changed',
  'position-updated',
  'queue-updated',
  'item-added',
  'kill-switch-toggled',
  'playback-error',
  'policy-changed',
  'heartbeat',
  'player-offline',
  'player-online',
  'player-disconnected',
  'update-available',
] as const

export type SSEEventType = (typeof SSE_EVENT_TYPES)[number]

const BACKOFF_BASE_MS = 1000
const BACKOFF_MAX_MS = 30_000
const POLL_INTERVAL_MS = 5_000
const SSE_FAIL_THRESHOLD = 3

export function useSSE(options: UseSSEOptions) {
  const status = ref<ConnectionStatus>('disconnected')
  const reconnectAttempts = ref(0)
  const maxAttempts = options.maxReconnectAttempts ?? 10
  const heartbeatTimeout = options.heartbeatTimeoutMs ?? 60_000

  const connected = computed(() => status.value === 'connected')
  const isReconnecting = computed(() => status.value === 'reconnecting')
  const isFailed = computed(() => status.value === 'failed')

  let eventSource: EventSource | null = null
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null
  let heartbeatTimer: ReturnType<typeof setTimeout> | null = null
  let pollTimer: ReturnType<typeof setInterval> | null = null
  let sseFailCount = 0

  function buildUrl(): string {
    const url = new URL(options.url, window.location.origin)
    if (options.queryParams) {
      for (const [key, value] of Object.entries(options.queryParams)) {
        url.searchParams.set(key, value)
      }
    }
    return url.toString()
  }

  function resetHeartbeat() {
    if (heartbeatTimer) clearTimeout(heartbeatTimer)
    heartbeatTimer = setTimeout(() => {
      // No event received within timeout — assume dead connection
      eventSource?.close()
      status.value = 'reconnecting'
      scheduleReconnect()
    }, heartbeatTimeout)
  }

  function dispatchEvent(eventType: string, data: unknown) {
    resetHeartbeat()
    if (eventType !== 'heartbeat') {
      options.onEvent?.(eventType, data)
    }
  }

  function connect() {
    if (eventSource) {
      eventSource.close()
    }
    stopPolling()

    status.value = reconnectAttempts.value > 0 ? 'reconnecting' : 'connecting'

    eventSource = new EventSource(buildUrl(), {
      withCredentials: options.withCredentials ?? false,
    })

    eventSource.onopen = () => {
      status.value = 'connected'
      reconnectAttempts.value = 0
      sseFailCount = 0
      resetHeartbeat()
    }

    // Listen for named SSE event types from the backend
    for (const eventType of SSE_EVENT_TYPES) {
      eventSource.addEventListener(eventType, (event: MessageEvent) => {
        try {
          const data = JSON.parse(event.data) as unknown
          dispatchEvent(eventType, data)
        } catch {
          // ignore malformed events
        }
      })
    }

    // Fallback: unnamed messages
    eventSource.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data) as unknown
        dispatchEvent('message', data)
      } catch {
        // ignore
      }
    }

    eventSource.onerror = (error) => {
      status.value = 'reconnecting'
      options.onError?.(error)
      eventSource?.close()
      eventSource = null
      if (heartbeatTimer) clearTimeout(heartbeatTimer)
      sseFailCount++

      if (sseFailCount >= SSE_FAIL_THRESHOLD) {
        startPolling()
      } else {
        scheduleReconnect()
      }
    }
  }

  function scheduleReconnect() {
    if (reconnectAttempts.value >= maxAttempts) {
      status.value = 'failed'
      return
    }

    // Exponential backoff: 1s, 2s, 4s, 8s, ... max 30s
    const delay = Math.min(BACKOFF_BASE_MS * Math.pow(2, reconnectAttempts.value), BACKOFF_MAX_MS)
    reconnectTimer = setTimeout(() => {
      reconnectAttempts.value++
      connect()
    }, delay)
  }

  function startPolling() {
    if (pollTimer) return
    status.value = 'connected' // polling is a degraded but functional state

    pollTimer = setInterval(async () => {
      try {
        const response = await fetch(buildUrl().replace('/events', '/now-playing'))
        if (response.ok) {
          const data = await response.json() as unknown
          options.onEvent?.('poll-state', data)
        }
      } catch {
        // polling failed — will retry next interval
      }
    }, POLL_INTERVAL_MS)

    // Periodically attempt SSE reconnect while polling
    reconnectAttempts.value = 0
    sseFailCount = 0
    reconnectTimer = setTimeout(() => {
      stopPolling()
      connect()
    }, BACKOFF_MAX_MS)
  }

  function stopPolling() {
    if (pollTimer) {
      clearInterval(pollTimer)
      pollTimer = null
    }
  }

  function disconnect() {
    if (reconnectTimer) {
      clearTimeout(reconnectTimer)
      reconnectTimer = null
    }
    if (heartbeatTimer) {
      clearTimeout(heartbeatTimer)
      heartbeatTimer = null
    }
    stopPolling()
    eventSource?.close()
    eventSource = null
    status.value = 'disconnected'
  }

  onUnmounted(disconnect)

  return {
    status,
    connected,
    isReconnecting,
    isFailed,
    reconnectAttempts,
    connect,
    disconnect,
  }
}
