/** Member contracts mirroring SmartLibrary.Api v1. */

export type MemberType = 'Public' | 'Student' | 'Staff'

export type MemberStatus = 'Active' | 'Suspended' | 'Expired'

export interface Member {
  id: string
  membershipNumber: string
  firstName: string
  lastName: string
  fullName: string
  email: string
  phone: string | null
  type: MemberType
  status: MemberStatus
  homeBranchId: string | null
  homeBranchName: string | null
  joinedAtUtc: string
  expiresAtUtc: string | null
  createdAtUtc: string
  createdBy: string | null
}

export interface RegisterMemberRequest {
  firstName: string
  lastName: string
  email: string
  phone: string | null
  type: MemberType
  homeBranchId: string | null
}

export const MEMBER_TYPES: MemberType[] = ['Public', 'Student', 'Staff']
