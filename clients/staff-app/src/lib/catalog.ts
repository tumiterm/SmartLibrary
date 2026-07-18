/** Typed contracts mirroring SmartLibrary.Api v1. */

export type BookLookupOutcome = 'FoundInLibrary' | 'FoundExternally' | 'NotFound'

export type BookFormat = 'Print' | 'Ebook' | 'Audio' | 'Video' | 'Other'

export type MetadataSource = 'Manual' | 'GoogleBooks'

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
  metadataSource: string
}

export interface BookLookupResult {
  outcome: BookLookupOutcome
  existsInLibrary: boolean
  bookId: string | null
  book: BookLookupDetails | null
}

export interface AddBookRequest {
  isbn: string | null
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
  format: BookFormat
  metadataSource: MetadataSource
}

export const BOOK_FORMATS: BookFormat[] = ['Print', 'Ebook', 'Audio', 'Video', 'Other']
