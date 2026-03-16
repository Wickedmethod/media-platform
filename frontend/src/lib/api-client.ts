import { ApiError } from '@/lib/api-error'
import { useAuthStore } from '@/stores/auth'

/** Custom fetch mutator for Orval-generated API clients.
 *  Orval URLs already include the full path from the OpenAPI spec (e.g. /api/v1/queue). */
export const apiClient = async <T>(config: {
  url: string
  method: string
  params?: Record<string, unknown>
  data?: unknown
  headers?: HeadersInit
  signal?: AbortSignal
}): Promise<T> => {
  const headers = new Headers(config.headers)

  const auth = useAuthStore()
  if (auth.token) {
    await auth.refreshToken()
    headers.set('Authorization', `Bearer ${auth.token}`)
  }

  if (config.data && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const url = new URL(config.url, window.location.origin)
  if (config.params) {
    for (const [key, value] of Object.entries(config.params)) {
      if (value != null) url.searchParams.set(key, String(value))
    }
  }

  const response = await fetch(url, {
    method: config.method,
    headers,
    body: config.data ? JSON.stringify(config.data) : undefined,
    signal: config.signal,
  })

  if (!response.ok) {
    let body: ApiError['body'] = {}
    try {
      body = await response.json()
    } catch {
      body = { error: response.statusText }
    }
    throw new ApiError(response.status, body)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export default apiClient
