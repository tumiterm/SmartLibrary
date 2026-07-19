/** Typed contracts mirroring SmartLibrary.Api v1. */

export type BookLookupOutcome = 'FoundInLibrary' | 'FoundExternally' | 'NotFound'

export type BookFormat =
  | 'Print'
  | 'Ebook'
  | 'Audio'
  | 'Video'
  | 'Other'
  | 'Pdf'
  | 'EMagazine'
  | 'ENewspaper'

export type MetadataSource = 'Manual' | 'GoogleBooks'

export type CopyStatus =
  | 'Available'
  | 'OnLoan'
  | 'OnHold'
  | 'InTransit'
  | 'Lost'
  | 'Damaged'
  | 'Withdrawn'
  | 'Missing'
  | 'Disposed'

export type CopyCondition = 'New' | 'Good' | 'Fair' | 'Poor'

export interface BookLookupDetails {
  isbn13: string | null
  isbn10: string | null
  title: string
  subtitle: string | null
  authors: string[]
  publisher: string | null
  publishedDate: string | null
  description: string | null
  pageCount: number | null
  language: string | null
  categories: string[]
  coverImageUrl: string | null
  classificationNumber: string | null
  metadataSource: string
}

export interface BookLookupResult {
  outcome: BookLookupOutcome
  existsInLibrary: boolean
  bookId: string | null
  copiesTotal: number
  copiesAvailable: number
  book: BookLookupDetails | null
}

export interface BookCopy {
  id: string
  barcode: string
  shelfNumber: string | null
  callNumber: string | null
  branchId: string | null
  branchName: string | null
  status: CopyStatus
  condition: CopyCondition
  price: number | null
  acquiredAtUtc: string
  notes: string | null
}

export interface LoanSummary {
  id: string
  patronName: string
  borrowedAtUtc: string
  dueAtUtc: string | null
  returnedAtUtc: string | null
}

export interface HoldQueueItem {
  id: string
  memberName: string
  membershipNumber: string
  status: string
  placedAtUtc: string
  position: number
}

export interface BookListItem {
  id: string
  isbn13: string | null
  title: string
  subtitle: string | null
  authors: string[]
  coverImageUrl: string | null
  format: BookFormat
  classificationNumber: string | null
  copiesTotal: number
  copiesAvailable: number
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface BookDetails {
  id: string
  isbn13: string | null
  isbn10: string | null
  title: string
  subtitle: string | null
  authors: string[]
  publisher: string | null
  publishedDate: string | null
  description: string | null
  pageCount: number | null
  language: string | null
  categories: string[]
  coverImageUrl: string | null
  classificationNumber: string | null
  format: BookFormat
  metadataSource: string
  isReferenceOnly: boolean
  createdAtUtc: string
  copiesTotal: number
  copiesAvailable: number
  isLowStock: boolean
  copies: BookCopy[]
  borrowHistory: LoanSummary[]
  holds: HoldQueueItem[]
}

export interface Branch {
  id: string
  name: string
  code: string | null
  address: string | null
}

export interface BookFields {
  title: string
  subtitle: string | null
  authors: string[]
  publisher: string | null
  publishedDate: string | null
  description: string | null
  pageCount: number | null
  language: string | null
  categories: string[]
  coverImageUrl: string | null
  classificationNumber: string | null
  format: BookFormat
  isReferenceOnly: boolean
}

export interface AddBookRequest extends BookFields {
  isbn: string | null
  metadataSource: MetadataSource
}

export interface AddCopyRequest {
  barcode: string
  shelfNumber: string | null
  callNumber: string | null
  branchId: string | null
  condition: CopyCondition
  price: number | null
  notes: string | null
}

export const BOOK_FORMATS: { value: BookFormat; label: string }[] = [
  { value: 'Print', label: 'Print' },
  { value: 'Ebook', label: 'E-book' },
  { value: 'Pdf', label: 'PDF' },
  { value: 'EMagazine', label: 'E-magazine' },
  { value: 'ENewspaper', label: 'E-newspaper' },
  { value: 'Audio', label: 'Audio' },
  { value: 'Video', label: 'Video' },
  { value: 'Other', label: 'Other' },
]

export const COPY_CONDITIONS: CopyCondition[] = ['New', 'Good', 'Fair', 'Poor']

/** Formats with a physical presence — these get a branch and shelf. */
export function isPhysical(format: BookFormat): boolean {
  return format === 'Print' || format === 'Other'
}
