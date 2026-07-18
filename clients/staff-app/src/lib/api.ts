import type { AddBookRequest, BookLookupResult } from './catalog'

const BASE = '/api/v1'

/** Dev tenant until JWT auth lands — then the tenant comes from the token. */
const TENANT = 'demo'

interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | null

  constructor(status: number, problem: ProblemDetails | null) {
    const firstFieldError = problem?.errors ? Object.values(problem.errors)[0]?.[0] : undefined
    super(firstFieldError ?? problem?.detail ?? problem?.title ?? `Request failed (${status})`)
    this.status = status
    this.problem = problem
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-Tenant': TENANT,
      ...init?.headers,
    },
  })

  if (!response.ok) {
    let problem: ProblemDetails | null = null
    try {
      problem = (await response.json()) as ProblemDetails
    } catch {
      // Non-JSON error body — fall through with null problem details.
    }
    throw new ApiError(response.status, problem)
  }

  return (await response.json()) as T
}

export function lookupBookByIsbn(isbn: string): Promise<BookLookupResult> {
  return request<BookLookupResult>(`/books/isbn/${encodeURIComponent(isbn)}`)
}

export function addBook(body: AddBookRequest): Promise<{ id: string }> {
  return request<{ id: string }>('/books', { method: 'POST', body: JSON.stringify(body) })
}
