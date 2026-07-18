import { BookMarked } from 'lucide-react'
import type { Member } from '@/lib/members'
import { cn } from '@/lib/utils'

/**
 * Physical-card render of a membership. Deliberately always dark ("ink card")
 * in both themes — like the object it represents.
 */
export function MembershipCard({ member, className }: { member: Member; className?: string }) {
  const validThru = member.expiresAtUtc
    ? new Date(member.expiresAtUtc).toLocaleDateString(undefined, { month: '2-digit', year: 'numeric' })
    : '—'

  return (
    <div
      className={cn(
        'relative aspect-[1.586] w-full max-w-md select-none overflow-hidden rounded-3xl p-6 text-[#ece7dc] shadow-pop sm:p-7',
        className,
      )}
      style={{
        background:
          'radial-gradient(120% 160% at 85% -20%, rgba(217,166,46,0.28), transparent 45%),' +
          'radial-gradient(90% 120% at -10% 110%, rgba(217,166,46,0.12), transparent 50%),' +
          'linear-gradient(135deg, #191713 0%, #100f0c 55%, #171410 100%)',
        border: '1px solid rgba(217,166,46,0.25)',
      }}
    >
      {/* Top row: library + chip */}
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-2">
          <div className="grid size-7 place-items-center rounded-md bg-[#ece7dc] text-[#14120f]">
            <BookMarked className="size-3.5" />
          </div>
          <span className="font-display text-[15px] font-semibold tracking-tight">
            Demo Library
          </span>
        </div>
        <span className="rounded-full border border-[#d9a62e]/40 bg-[#d9a62e]/10 px-2.5 py-0.5 text-[10px] font-semibold uppercase tracking-[0.2em] text-[#d9a62e]">
          {member.type}
        </span>
      </div>

      {/* Chip emblem */}
      <div
        className="mt-5 h-8 w-11 rounded-md sm:mt-7"
        style={{
          background: 'linear-gradient(135deg, #e9c767, #b8860b 60%, #8a6a1f)',
          boxShadow: 'inset 0 0 0 1px rgba(0,0,0,0.35), inset 0 6px 10px rgba(255,255,255,0.25)',
        }}
      />

      {/* Number + name */}
      <p className="mt-4 font-mono text-xl tracking-[0.14em] sm:mt-5 sm:text-2xl">
        {member.membershipNumber}
      </p>
      <p className="mt-1.5 text-xs font-medium uppercase tracking-[0.18em] text-[#a89f8c]">
        {member.fullName}
      </p>

      {/* Bottom row */}
      <div className="absolute inset-x-6 bottom-5 flex items-end justify-between sm:inset-x-7 sm:bottom-6">
        <div className="flex gap-6 text-[10px] uppercase tracking-[0.16em] text-[#a89f8c]">
          <div>
            <p>Valid thru</p>
            <p className="mt-0.5 font-mono text-xs text-[#ece7dc]">{validThru}</p>
          </div>
          <div>
            <p>Branch</p>
            <p className="mt-0.5 text-xs normal-case tracking-normal text-[#ece7dc]">
              {member.homeBranchName ?? 'Library-wide'}
            </p>
          </div>
        </div>

        {/* Decorative barcode derived from the membership number */}
        <div aria-hidden className="flex h-7 items-stretch gap-px opacity-70">
          {member.membershipNumber
            .replace(/\D/g, '')
            .split('')
            .flatMap((digit, i) => {
              const d = Number(digit)
              return [
                <span key={`${i}-a`} className="bg-[#ece7dc]" style={{ width: 1 + (d % 3) }} />,
                <span key={`${i}-b`} style={{ width: 1 + ((d + i) % 2) }} />,
              ]
            })}
        </div>
      </div>
    </div>
  )
}
