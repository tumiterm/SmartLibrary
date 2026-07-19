/** Circulation contracts mirroring SmartLibrary.Api v1. */

import type { Member } from './members'

export interface Loan {
  id: string
  memberId: string
  memberName: string
  membershipNumber: string
  bookCopyId: string
  barcode: string
  bookId: string | null
  bookTitle: string
  borrowedAtUtc: string
  dueAtUtc: string
  returnedAtUtc: string | null
  daysLate: number | null
  isOverdue: boolean
}

export type FineStatus = 'Outstanding' | 'Paid' | 'Waived'

export interface Fine {
  id: string
  memberId: string
  loanId: string | null
  bookTitle: string | null
  amount: number
  reason: string
  status: FineStatus
  assessedAtUtc: string
  settledAtUtc: string | null
  notes: string | null
}

export interface CheckoutFailure {
  barcode: string
  error: string
}

export interface CheckoutResult {
  loans: Loan[]
  failures: CheckoutFailure[]
}

export type ReturnOutcome = 'Normal' | 'Damaged'

export interface ReturnResult {
  loan: Loan
  wasLate: boolean
  daysLate: number
  fineAssessed: Fine | null
  holdReadyFor: string | null
  outcome: ReturnOutcome
  finesAssessed: Fine[]
  returnedAtDifferentBranch: boolean
  homeBranchName: string | null
}

export interface LostBookResult {
  loan: Loan
  replacementCharge: Fine | null
}

export type HoldStatus = 'Pending' | 'Ready' | 'Fulfilled' | 'Cancelled' | 'Expired'

export interface Hold {
  id: string
  memberId: string
  memberName: string
  membershipNumber: string
  bookId: string
  bookTitle: string
  status: HoldStatus
  placedAtUtc: string
  readyAtUtc: string | null
  queuePosition: number | null
}

export type TransferStatus =
  | 'Requested'
  | 'InTransit'
  | 'Received'
  | 'Rejected'
  | 'Cancelled'
  | 'LostInTransit'
  | 'DamagedInTransit'

export type TransferAction = 'Dispatch' | 'Reject' | 'Cancel' | 'LostInTransit' | 'DamagedInTransit'

export interface Transfer {
  id: string
  barcode: string
  bookId: string | null
  bookTitle: string
  fromBranchName: string | null
  toBranchName: string
  status: TransferStatus
  requestedAtUtc: string
  dispatchedAtUtc: string | null
  completedAtUtc: string | null
  requestedBy: string | null
  notes: string | null
}

export interface Stocktake {
  id: string
  branchId: string | null
  branchName: string | null
  status: 'Open' | 'Completed' | 'Cancelled'
  startedAtUtc: string
  completedAtUtc: string | null
  expectedCount: number
  scannedCount: number
  missingCount: number
  foundCount: number
  startedBy: string | null
}

export interface StocktakeCopy {
  copyId: string
  barcode: string
  bookId: string
  bookTitle: string
  status: string
  branchName: string | null
}

export interface ScanResult {
  stocktake: Stocktake
  barcode: string
  bookTitle: string
  wasFound: boolean
  alreadyScanned: boolean
}

export interface StocktakeReport {
  stocktake: Stocktake
  missing: StocktakeCopy[]
  found: StocktakeCopy[]
}

export interface ReaderScore {
  score: number
  tier: string
  reasons: string[]
}

export interface MemberProfile {
  member: Member
  loans: Loan[]
  fines: Fine[]
  holds: Hold[]
  outstandingFines: number
  readerScore: ReaderScore
}
