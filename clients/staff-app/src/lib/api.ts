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
  PublicBookDetails,
} from './catalog'
import type {
  CheckoutResult,
  Fine,
  Hold,
  Loan,
  LostBookResult,
  MemberProfile,
  ReturnOutcome,
  ReturnResult,
  ScanResult,
  Stocktake,
  StocktakeReport,
  Transfer,
  TransferAction,
} from './circulation'
import type { CopyCondition, CopyStatus } from './catalog'
import type { Member, MemberStatus, RegisterMemberRequest } from './members'
import type {
  CirculationReport,
  Dashboard,
  FinesReport,
  GlobalSearchResult,
  InventoryReport,
  LibrarySettings,
} from './system'

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

export function checkoutBooks(
  membershipNumber: string,
  barcodes: string[],
  branchId?: string | null,
): Promise<CheckoutResult> {
  return request<CheckoutResult>('/loans', {
    method: 'POST',
    body: JSON.stringify({ membershipNumber, barcodes, branchId: branchId || null }),
  })
}

export function returnBook(params: {
  barcode: string
  outcome?: ReturnOutcome
  condition?: CopyCondition | null
  damageCharge?: number | null
  branchId?: string | null
}): Promise<ReturnResult> {
  return request<ReturnResult>('/loans/return', {
    method: 'POST',
    body: JSON.stringify({
      barcode: params.barcode,
      outcome: params.outcome ?? 'Normal',
      condition: params.condition ?? null,
      damageCharge: params.damageCharge ?? null,
      branchId: params.branchId || null,
    }),
  })
}

export function reportLost(barcode: string, replacementCharge?: number | null): Promise<LostBookResult> {
  return request<LostBookResult>('/loans/lost', {
    method: 'POST',
    body: JSON.stringify({ barcode, replacementCharge: replacementCharge ?? null }),
  })
}

export function transferAction(id: string, action: TransferAction, note?: string): Promise<Transfer> {
  return request<Transfer>(`/transfers/${id}/action`, {
    method: 'POST',
    body: JSON.stringify({ action, note: note ?? null }),
  })
}

export function getOpenStocktake(): Promise<Stocktake | null> {
  return request<Stocktake | null>('/stocktakes/open')
}

export function getStocktakes(): Promise<Stocktake[]> {
  return request<Stocktake[]>('/stocktakes')
}

export function startStocktake(branchId?: string | null): Promise<Stocktake> {
  return request<Stocktake>('/stocktakes', {
    method: 'POST',
    body: JSON.stringify({ branchId: branchId || null, notes: null }),
  })
}

export function scanStocktakeItem(stocktakeId: string, barcode: string): Promise<ScanResult> {
  return request<ScanResult>(`/stocktakes/${stocktakeId}/scans`, {
    method: 'POST',
    body: JSON.stringify({ barcode }),
  })
}

export function completeStocktake(stocktakeId: string): Promise<StocktakeReport> {
  return request<StocktakeReport>(`/stocktakes/${stocktakeId}/complete`, {
    method: 'POST',
  })
}

export function opacSearch(params: {
  search?: string
  format?: BookFormat | ''
  page?: number
}): Promise<PagedResult<BookListItem>> {
  const qs = new URLSearchParams()
  if (params.search) qs.set('search', params.search)
  if (params.format) qs.set('format', params.format)
  if (params.page) qs.set('page', String(params.page))
  const suffix = qs.toString() ? `?${qs.toString()}` : ''
  return request<PagedResult<BookListItem>>(`/opac/books${suffix}`)
}

export function opacBook(id: string): Promise<PublicBookDetails> {
  return request<PublicBookDetails>(`/opac/books/${id}`)
}

export function getCirculationReport(from: string, to: string): Promise<CirculationReport> {
  return request<CirculationReport>(`/reports/circulation?from=${from}&to=${to}`)
}

export function getInventoryReport(): Promise<InventoryReport> {
  return request<InventoryReport>('/reports/inventory')
}

export function getFinesReport(from: string, to: string): Promise<FinesReport> {
  return request<FinesReport>(`/reports/fines?from=${from}&to=${to}`)
}

/** Fetches a CSV export (tenant header required) and triggers a browser download. */
export async function downloadReportCsv(
  path: 'circulation' | 'inventory' | 'fines',
  params: { from?: string; to?: string },
): Promise<void> {
  const qs = new URLSearchParams({ format: 'csv' })
  if (params.from) qs.set('from', params.from)
  if (params.to) qs.set('to', params.to)

  const response = await fetch(`${BASE}/reports/${path}?${qs.toString()}`, {
    headers: { 'X-Tenant': TENANT },
  })
  if (!response.ok) {
    throw new ApiError(response.status, null)
  }

  const blob = await response.blob()
  const disposition = response.headers.get('Content-Disposition') ?? ''
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)/i.exec(disposition)
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = match?.[1] ?? `${path}.csv`
  anchor.click()
  URL.revokeObjectURL(url)
}

export function settleFine(fineId: string, waive: boolean, reason?: string): Promise<Fine> {
  return request<Fine>(`/loans/fines/${fineId}/settle`, {
    method: 'POST',
    body: JSON.stringify({ waive, reason: reason ?? null }),
  })
}

export function getDashboard(): Promise<Dashboard> {
  return request<Dashboard>('/dashboard')
}

export function globalSearch(q: string): Promise<GlobalSearchResult> {
  return request<GlobalSearchResult>(`/search?q=${encodeURIComponent(q)}`)
}

export function getSettings(): Promise<LibrarySettings> {
  return request<LibrarySettings>('/settings')
}

export function updateSettings(body: Omit<LibrarySettings, 'isCustomized'>): Promise<LibrarySettings> {
  return request<LibrarySettings>('/settings', { method: 'PUT', body: JSON.stringify(body) })
}

export function setCopyStatus(copyId: string, status: CopyStatus): Promise<void> {
  return requestVoid(`/books/copies/${copyId}/status`, {
    method: 'POST',
    body: JSON.stringify({ status }),
  })
}

export function updateMember(id: string, body: RegisterMemberRequest): Promise<Member> {
  return request<Member>(`/members/${id}`, { method: 'PUT', body: JSON.stringify(body) })
}

export function setMemberStatus(id: string, status: MemberStatus): Promise<Member> {
  return request<Member>(`/members/${id}/status`, {
    method: 'POST',
    body: JSON.stringify({ status }),
  })
}

export function searchBooks(params: {
  search?: string
  format?: BookFormat | ''
  branchId?: string
  page?: number
}): Promise<PagedResult<BookListItem>> {
  const qs = new URLSearchParams()
  if (params.search) qs.set('search', params.search)
  if (params.format) qs.set('format', params.format)
  if (params.branchId) qs.set('branchId', params.branchId)
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
