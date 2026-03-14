import { useAuthStore } from '@/stores/auth'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api'

export interface ApiOptions extends RequestInit {
  skipAuth?: boolean
}

export async function apiFetch<T>(path: string, options: ApiOptions = {}): Promise<T> {
  const { skipAuth, ...fetchOptions } = options
  const headers = new Headers(fetchOptions.headers)

  if (!skipAuth) {
    const auth = useAuthStore()
    if (auth.token) {
      await auth.refreshToken()
      headers.set('Authorization', `Bearer ${auth.token}`)
    }
  }

  if (!headers.has('Content-Type') && fetchOptions.body) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...fetchOptions,
    headers,
  })

  if (!response.ok) {
    throw new ApiError(response.status, response.statusText, await response.text())
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
    public readonly body: string,
  ) {
    super(`API Error ${status}: ${statusText}`)
    this.name = 'ApiError'
  }
}
