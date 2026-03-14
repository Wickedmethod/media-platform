/** Structured API error matching the backend's ApiError response shape */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly body: { error?: string; detail?: string; errors?: Record<string, string[]> },
  ) {
    super(body.error ?? body.detail ?? `HTTP ${status}`)
    this.name = 'ApiError'
  }

  get isValidation() { return this.status === 400 }
  get isUnauthorized() { return this.status === 401 }
  get isForbidden() { return this.status === 403 }
  get isNotFound() { return this.status === 404 }
  get isConflict() { return this.status === 409 }
  get isRateLimited() { return this.status === 429 }
  get isServerError() { return this.status >= 500 }
}
