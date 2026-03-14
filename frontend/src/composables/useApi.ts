import { useAuthStore } from "@/stores/auth";
import { ApiError } from "@/lib/api-error";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "/api";

export interface ApiOptions extends RequestInit {
  skipAuth?: boolean;
}

export async function apiFetch<T>(
  path: string,
  options: ApiOptions = {},
): Promise<T> {
  const { skipAuth, ...fetchOptions } = options;
  const headers = new Headers(fetchOptions.headers);

  if (!skipAuth) {
    const auth = useAuthStore();
    if (auth.token) {
      await auth.refreshToken();
      headers.set("Authorization", `Bearer ${auth.token}`);
    }
  }

  if (!headers.has("Content-Type") && fetchOptions.body) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...fetchOptions,
    headers,
  });

  if (!response.ok) {
    let body: ApiError["body"] = {};
    try {
      body = await response.json();
    } catch {
      body = { error: response.statusText };
    }
    throw new ApiError(response.status, body);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}
