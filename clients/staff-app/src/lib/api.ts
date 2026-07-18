import type {
  AddBookRequest,
  AddCopyRequest,
  BookDetails,
  BookFields,
  BookFormat,
  BookListItem,
  BookLookupResult,
  Branch,
  PagedResult,
} from './catalog'
import type { Fine, Hold, Loan, MemberProfile, ReturnResult, Transfer } from './circulation'
import type { Member, RegisterMemberRequest } from './members'

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

async function requestVoid(path: string, init?: RequestInit): Promise<void> {
  const response = await fetch(`${BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-Tenant': TENANT,
      ...init?.headers,
    },
  })

  if (!response.ok) {
    let problem = null
    try {
      problem = await response.json()
    } catch {
      // Non-JSON error body.
    }
    throw new ApiError(response.status, problem)
  }
}

export function lookupBookByIsbn(isbn: string): Promise<BookLookupResult> {
  return request<BookLookupResult>(`/books/isbn/${encodeURIComponent(isbn)}`)
}

export function addBook(body: AddBookRequest): Promise<{ id: string }> {
  return request<{ id: string }>('/books', { method: 'POST', body: JSON.stringify(body) })
}

export function getBook(id: string): Promise<BookDetails> {
  return request<BookDetails>(`/books/${id}`)
}

export function updateBook(id: string, body: BookFields): Promise<void> {
  return requestVoid(`/books/${id}`, { method: 'PUT', body: JSON.stringify(body) })
}

export function addCopy(bookId: string, body: AddCopyRequest): Promise<{ id: string }> {
  return request<{ id: string }>(`/books/${bookId}/copies`, {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

export function getBranches(): Promise<Branch[]> {
  return request<Branch[]>('/branches')
}

export function createBranch(name: string): Promise<{ id: string }> {
  return request<{ id: string }>('/branches', {
    method: 'POST',
    body: JSON.stringify({ name, code: null, address: null }),
  })
}

export function searchMembers(search: string): Promise<Member[]> {
  const qs = search ? `?search=${encodeURIComponent(search)}` : ''
  return request<Member[]>(`/members${qs}`)
}

export function getMember(id: string): Promise<MemberProfile> {
  return request<MemberProfile>(`/members/${id}`)
}

export function registerMember(body: RegisterMemberRequest): Promise<Member> {
  return request<Member>('/members', { method: 'POST', body: JSON.stringify(body) })
}

export function getActiveLoans(): Promise<Loan[]> {
  return request<Loan[]>('/loans/active')
}

export function checkoutBook(membershipNumber: string, barcode: string): Promise<Loan> {
  return request<Loan>('/loans', {
    method: 'POST',
    body: JSON.stringify({ membershipNumber, barcode }),
  })
}

export function returnBook(barcode: string): Promise<ReturnResult> {
  return request<ReturnResult>('/loans/return', {
    method: 'POST',
    body: JSON.stringify({ barcode }),
  })
}

export function settleFine(fineId: string, waive: boolean): Promise<Fine> {
  return request<Fine>(`/loans/fines/${fineId}/settle`, {
    method: 'POST',
    body: JSON.stringify({ waive }),
  })
}

export function searchBooks(params: {
  search?: string
  format?: BookFormat | ''
  page?: number
}): Promise<PagedResult<BookListItem>> {
  const qs = new URLSearchParams()
  if (params.search) qs.set('search', params.search)
  if (params.format) qs.set('format', params.format)
  if (params.page) qs.set('page', String(params.page))
  const suffix = qs.toString() ? `?${qs.toString()}` : ''
  return request<PagedResult<BookListItem>>(`/books${suffix}`)
}

export function renewLoan(barcode: string): Promise<Loan> {
  return request<Loan>('/loans/renew', { method: 'POST', body: JSON.stringify({ barcode }) })
}

export function getPendingTransfers(): Promise<Transfer[]> {
  return request<Transfer[]>('/transfers/pending')
}

export function createTransfer(barcode: string, toBranchId: string): Promise<Transfer> {
  return request<Transfer>('/transfers', {
    method: 'POST',
    body: JSON.stringify({ barcode, toBranchId }),
  })
}

export function receiveTransfer(barcode: string): Promise<Transfer> {
  return request<Transfer>('/transfers/receive', {
    method: 'POST',
    body: JSON.stringify({ barcode }),
  })
}

export function placeHold(bookId: string, membershipNumber: string): Promise<Hold> {
  return request<Hold>('/holds', {
    method: 'POST',
    body: JSON.stringify({ bookId, membershipNumber }),
  })
}

export function cancelHold(holdId: string): Promise<Hold> {
  return request<Hold>(`/holds/${holdId}/cancel`, { method: 'POST' })
}
