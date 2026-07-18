import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  Award,
  BookCopy,
  HandCoins,
  Hourglass,
  Mail,
  Phone,
  ShieldCheck,
  Sparkles,
} from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { MembershipCard } from '@/components/membership-card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Spinner } from '@/components/ui/spinner'
import { cancelHold, getMember, settleFine } from '@/lib/api'
import type { ReaderScore } from '@/lib/circulation'
import type { MemberStatus } from '@/lib/members'

const STATUS_VARIANT: Record<MemberStatus, 'success' | 'danger' | 'neutral'> = {
  Active: 'success',
  Suspended: 'danger',
  Expired: 'neutral',
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString(undefined, { day: 'numeric', month: 'long', year: 'numeric' })
}

function formatShort(value: string) {
  return new Date(value).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })
}

/* ── Reader Score ─────────────────────────────────────────────────────────── */

function scoreColor(score: number) {
  if (score >= 75) return 'var(--success)'
  if (score >= 55) return 'var(--accent)'
  return 'var(--danger)'
}

function ReaderScoreCard({ score }: { score: ReaderScore }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Award className="size-4 text-accent" />
          Reader Score
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <div className="flex items-end gap-4">
          <p className="font-display text-5xl font-semibold leading-none" style={{ color: scoreColor(score.score) }}>
            {score.score}
          </p>
          <div className="pb-1">
            <Badge variant="brass">
              <Sparkles className="size-3" />
              {score.tier}
            </Badge>
          </div>
        </div>

        <div className="h-1.5 overflow-hidden rounded-full bg-surface-2">
          <div
            className="h-full rounded-full transition-all duration-500"
            style={{ width: `${score.score}%`, backgroundColor: scoreColor(score.score) }}
          />
        </div>

        <ul className="flex flex-col gap-1.5 text-[13px] leading-relaxed text-muted">
          {score.reasons.map((reason) => (
            <li key={reason} className="flex gap-2">
              <span className="text-faint">·</span>
              {reason}
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  )
}

/* ── Page ─────────────────────────────────────────────────────────────────── */

export function MemberDetailsPage() {
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()
  const profile = useQuery({
    queryKey: ['member', id],
    queryFn: () => getMember(id!),
    enabled: !!id,
  })

  const settle = useMutation({
    mutationFn: ({ fineId, waive }: { fineId: string; waive: boolean }) => settleFine(fineId, waive),
    onSuccess: (fine) => {
      toast.success(fine.status === 'Waived' ? 'Fine waived' : 'Fine paid', {
        description: fine.amount.toFixed(2),
      })
      void queryClient.invalidateQueries({ queryKey: ['member', id] })
    },
    onError: (error: Error) => toast.error('Could not settle fine', { description: error.message }),
  })

  const cancelHoldMutation = useMutation({
    mutationFn: (holdId: string) => cancelHold(holdId),
    onSuccess: () => {
      toast.success('Hold cancelled')
      void queryClient.invalidateQueries({ queryKey: ['member', id] })
    },
    onError: (error: Error) => toast.error('Could not cancel hold', { description: error.message }),
  })

  if (profile.isPending) {
    return (
      <div className="grid min-h-64 place-items-center text-muted">
        <Spinner className="size-6" />
      </div>
    )
  }

  if (profile.isError || !profile.data) {
    return (
      <Card>
        <CardContent className="flex flex-col items-start gap-3">
          <p className="font-medium">Member not found</p>
          <p className="text-sm text-muted">They may belong to a different library.</p>
          <Button variant="secondary" size="sm" onClick={() => window.history.back()}>
            <ArrowLeft className="size-4" /> Back
          </Button>
        </CardContent>
      </Card>
    )
  }

  const { member: m, loans, fines, holds, outstandingFines, readerScore } = profile.data
  const activeHolds = holds.filter((h) => h.status === 'Pending' || h.status === 'Ready')
  const activeLoans = loans.filter((l) => !l.returnedAtUtc)
  const pastLoans = loans.filter((l) => l.returnedAtUtc)

  return (
    <div className="flex flex-col gap-8">
      <div className="animate-fade">
        <Link to="/patrons" className="inline-flex items-center gap-1.5 text-sm text-muted transition-colors hover:text-ink">
          <ArrowLeft className="size-4" /> Members
        </Link>
      </div>

      <header className="animate-fade">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="font-display text-3xl font-semibold sm:text-4xl">{m.fullName}</h1>
          <Badge variant={STATUS_VARIANT[m.status]}>{m.status}</Badge>
          {outstandingFines > 0 && <Badge variant="danger">{outstandingFines.toFixed(2)} owed</Badge>}
        </div>
        <p className="mt-2 text-sm text-muted">
          {m.type} member · joined {formatDate(m.joinedAtUtc)}
        </p>
      </header>

      <div className="grid animate-rise gap-6 lg:grid-cols-[minmax(0,28rem)_1fr]">
        <div className="flex flex-col gap-4">
          <MembershipCard member={m} />
          <Card>
            <CardHeader>
              <CardTitle>Contact</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-2.5 text-sm">
              <p className="flex items-center gap-2.5">
                <Mail className="size-4 text-faint" />
                {m.email}
              </p>
              <p className="flex items-center gap-2.5">
                <Phone className="size-4 text-faint" />
                {m.phone ?? <span className="text-faint">No phone on file</span>}
              </p>
              <p className="flex items-center gap-2.5 text-muted">
                <ShieldCheck className="size-4 text-faint" />
                Registered by <span className="font-medium text-ink">{m.createdBy ?? 'system'}</span> on{' '}
                {formatDate(m.createdAtUtc)}
              </p>
            </CardContent>
          </Card>
        </div>

        <div className="flex flex-col gap-4">
          <ReaderScoreCard score={readerScore} />

          {activeHolds.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Hourglass className="size-4 text-accent" />
                  Holds
                </CardTitle>
              </CardHeader>
              <CardContent>
                <ul className="flex flex-col divide-y divide-border">
                  {activeHolds.map((hold) => (
                    <li key={hold.id} className="flex items-center gap-3 py-2.5 first:pt-0 last:pb-0">
                      <div className="min-w-0 flex-1">
                        <Link
                          to={`/catalog/books/${hold.bookId}`}
                          className="truncate text-sm font-medium text-ink hover:text-accent"
                        >
                          {hold.bookTitle}
                        </Link>
                        <p className="text-xs text-muted">placed {formatShort(hold.placedAtUtc)}</p>
                      </div>
                      <Badge variant={hold.status === 'Ready' ? 'brass' : 'neutral'}>
                        {hold.status === 'Ready' ? 'Ready for pickup' : 'Waiting'}
                      </Badge>
                      <Button
                        size="sm"
                        variant="ghost"
                        disabled={cancelHoldMutation.isPending}
                        onClick={() => cancelHoldMutation.mutate(hold.id)}
                      >
                        Cancel
                      </Button>
                    </li>
                  ))}
                </ul>
              </CardContent>
            </Card>
          )}

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <HandCoins className="size-4 text-accent" />
                Fines
                {outstandingFines > 0 && <Badge variant="danger">{outstandingFines.toFixed(2)} outstanding</Badge>}
              </CardTitle>
            </CardHeader>
            <CardContent>
              {fines.length === 0 ? (
                <p className="text-sm text-muted">No fines — spotless.</p>
              ) : (
                <ul className="flex flex-col divide-y divide-border">
                  {fines.map((fine) => (
                    <li key={fine.id} className="flex flex-wrap items-center gap-3 py-2.5 first:pt-0 last:pb-0">
                      <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium">
                          {fine.amount.toFixed(2)} · {fine.reason}
                        </p>
                        <p className="truncate text-xs text-muted">
                          {fine.bookTitle ?? 'General'} · {formatShort(fine.assessedAtUtc)}
                        </p>
                      </div>
                      {fine.status === 'Outstanding' ? (
                        <div className="flex gap-2">
                          <Button
                            size="sm"
                            disabled={settle.isPending}
                            onClick={() => settle.mutate({ fineId: fine.id, waive: false })}
                          >
                            Pay
                          </Button>
                          <Button
                            size="sm"
                            variant="ghost"
                            disabled={settle.isPending}
                            onClick={() => settle.mutate({ fineId: fine.id, waive: true })}
                          >
                            Waive
                          </Button>
                        </div>
                      ) : (
                        <Badge variant={fine.status === 'Paid' ? 'success' : 'neutral'}>{fine.status}</Badge>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      <Card className="animate-rise">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <BookCopy className="size-4 text-accent" />
            Loans
            {activeLoans.length > 0 && <Badge variant="brass">{activeLoans.length} active</Badge>}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {loans.length === 0 ? (
            <div className="flex items-center gap-3 rounded-xl border border-dashed border-border-strong bg-surface-2 px-4 py-6 text-sm text-muted">
              <BookCopy className="size-4 shrink-0" />
              No loans yet — send them home with a book.
            </div>
          ) : (
            <div className="overflow-x-auto rounded-xl border border-border">
              <table className="w-full min-w-130 text-sm">
                <thead>
                  <tr className="border-b border-border bg-surface-2 text-left text-xs uppercase tracking-wider text-muted">
                    <th className="px-4 py-2.5 font-medium">Book</th>
                    <th className="px-4 py-2.5 font-medium">Borrowed</th>
                    <th className="px-4 py-2.5 font-medium">Due</th>
                    <th className="px-4 py-2.5 font-medium">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {[...activeLoans, ...pastLoans].map((loan) => (
                    <tr key={loan.id} className="border-b border-border last:border-0">
                      <td className="px-4 py-2.5">
                        {loan.bookId ? (
                          <Link to={`/catalog/books/${loan.bookId}`} className="font-medium text-ink hover:text-accent">
                            {loan.bookTitle}
                          </Link>
                        ) : (
                          loan.bookTitle
                        )}
                      </td>
                      <td className="px-4 py-2.5">{formatShort(loan.borrowedAtUtc)}</td>
                      <td className="px-4 py-2.5">{formatShort(loan.dueAtUtc)}</td>
                      <td className="px-4 py-2.5">
                        {loan.returnedAtUtc ? (
                          (loan.daysLate ?? 0) > 0 ? (
                            <Badge variant="danger">{loan.daysLate}d late</Badge>
                          ) : (
                            <Badge variant="success">Returned</Badge>
                          )
                        ) : loan.isOverdue ? (
                          <Badge variant="danger">Overdue</Badge>
                        ) : (
                          <Badge variant="brass">On loan</Badge>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
