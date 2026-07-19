/** Dashboard, settings and global-search contracts mirroring SmartLibrary.Api v1. */

export interface ActivityItem {
  kind: 'Borrowed' | 'Returned'
  bookTitle: string
  memberName: string
  bookId: string | null
  memberId: string
  atUtc: string
}

export interface LowStockItem {
  bookId: string
  title: string
  available: number
  total: number
}

export interface Dashboard {
  totalBooks: number
  totalCopies: number
  copiesAvailable: number
  copiesOnLoan: number
  overdueLoans: number
  activeMembers: number
  pendingTransfers: number
  readyHolds: number
  outstandingFines: number
  recentActivity: ActivityItem[]
  lowStock: LowStockItem[]
}

export interface LibrarySettings {
  loanDays: number
  dailyFineAmount: number
  maxActiveLoans: number
  fineBlockThreshold: number
  maxRenewals: number
  holdPickupDays: number
  lowStockThreshold: number
  maxOverdueItems: number
  isCustomized: boolean
}

export interface SearchBookHit {
  id: string
  title: string
  authors: string[]
  isbn13: string | null
  coverImageUrl: string | null
}

export interface SearchCopyHit {
  barcode: string
  bookId: string
  bookTitle: string
  status: string
  branchName: string | null
}

export interface SearchMemberHit {
  id: string
  fullName: string
  membershipNumber: string
  email: string
}

export interface GlobalSearchResult {
  books: SearchBookHit[]
  copies: SearchCopyHit[]
  members: SearchMemberHit[]
}
