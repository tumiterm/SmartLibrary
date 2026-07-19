import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlarmClock,
  ArrowDownToLine,
  ArrowLeftRight,
  ArrowUpFromLine,
  Building2,
  IdCard,
  PackageCheck,
  PackageX,
  RotateCw,
  ScanBarcode,
  SearchX,
} from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input, Select } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import {
  checkoutBooks,
  createTransfer,
  getActiveLoans,
  getBranches,
  getPendingTransfers,
  receiveTransfer,
  renewLoan,
  reportLost,
  returnBook,
  transferAction,
} from '@/lib/api'
import { COPY_CONDITIONS, type CopyCondition } from '@/lib/catalog'
import type { ReturnOutcome, TransferStatus } from '@/lib/circulation'

const DESK_BRANCH_KEY = 'smartlibrary-desk-branch'

function formatDue(value: string) {
  return new Date(value).toLocaleDateString(undefined, { day: 'numeric', month: 'short' })
}

/* ── Checkout ─────────────────────────────────────────────────────────────── */

function CheckoutPanel({ deskBranchId, onDone }: { deskBranchId: string; onDone: () => void }) {
  const [card, setCard] = useState('')
  const [barcode, setBarcode] = useState('')
  const [scanned, setScanned] = useState<string[]>([])

  const addBarcode = () => {
    const value = barcode.trim()
    if (!value) return
    if (!scanned.some((b) => b.toLowerCase() === value.toLowerCase())) {
      setScanned((s) => [...s, value])
    }
    setBarcode('')
  }

  const mutation = useMutation({
    mutationFn: () => {
      const pending = barcode.trim() && !scanned.includes(barcode.trim()) ? [...scanned, barcode.trim()] : scanned
      return checkoutBooks(card.trim(), pending, deskBranchId || null)
    },
    onSuccess: (result) => {
      if (result.loans.length > 0) {
        const first = result.loans[0]
        toast.success(
          `${result.loans.length} book${result.loans.length === 1 ? '' : 's'} checked out to ${first.memberName}`,
          { description: `Due ${formatDue(first.dueAtUtc)}` },
        )
      }
      for (const failure of result.failures) {
        toast.error(`${failure.barcode} refused`, { description: failure.error })
      }
      setCard('')
      setBarcode('')
      setScanned([])
      onDone()
    },
    onError: (error: Error) => toast.error('Checkout refused', { description: error.message }),
  })

  const total = scanned.length + (barcode.trim() ? 1 : 0)

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <ArrowUpFromLine className="size-4 text-accent" />
          Check out
        </CardTitle>
        <CardDescription>Scan the card, then every book — one transaction.</CardDescription>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={(e) => {
            e.preventDefault()
            mutation.mutate()
          }}
          className="flex flex-col gap-4"
        >
          <div>
            <Label htmlFor="co-card">Membership no.</Label>
            <div className="relative">
              <IdCard className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
              <Input
                id="co-card"
                placeholder="M-2026-000000"
                className="pl-10 font-mono"
                value={card}
                onChange={(e) => setCard(e.target.value)}
              />
            </div>
          </div>
          <div>
            <Label htmlFor="co-barcode">Copy barcodes</Label>
            <div className="relative">
              <ScanBarcode className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
              <Input
                id="co-barcode"
                placeholder="Scan, Enter, repeat…"
                className="pl-10 font-mono"
                value={barcode}
                onChange={(e) => setBarcode(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault()
                    addBarcode()
                  }
                }}
              />
            </div>
            {scanned.length > 0 && (
              <div className="mt-2 flex flex-wrap gap-1.5">
                {scanned.map((b) => (
                  <button
                    key={b}
                    type="button"
                    title="Remove"
                    className="cursor-pointer rounded-full border border-border bg-surface-2 px-2 py-0.5 font-mono text-xs text-muted transition-colors hover:border-danger hover:text-danger"
                    onClick={() => setScanned((s) => s.filter((x) => x !== b))}
                  >
                    {b} ×
                  </button>
                ))}
              </div>
            )}
          </div>
          <Button type="submit" disabled={mutation.isPending || !card.trim() || total === 0}>
            {mutation.isPending ? <Spinner /> : <ArrowUpFromLine className="size-4" />}
            Check out {total > 1 ? `${total} books` : ''}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}

/* ── Return (condition-aware) ─────────────────────────────────────────────── */

function ReturnPanel({ deskBranchId, onDone }: { deskBranchId: string; onDone: () => void }) {
  const [barcode, setBarcode] = useState('')
  const [outcome, setOutcome] = useState<ReturnOutcome>('Normal')
  const [condition, setCondition] = useState<CopyCondition | ''>('')
  const [damageCharge, setDamageCharge] = useState('')

  const reset = () => {
    setBarcode('')
    setOutcome('Normal')
    setCondition('')
    setDamageCharge('')
  }

  const mutation = useMutation({
    mutationFn: () =>
      returnBook({
        barcode: barcode.trim(),
        outcome,
        condition: condition || null,
        damageCharge: outcome === 'Damaged' && damageCharge ? Number(damageCharge) : null,
        branchId: deskBranchId || null,
      }),
    onSuccess: (result) => {
      if (result.outcome === 'Damaged') {
        toast.warning(`Returned damaged — pulled from circulation`, {
          description: `${result.loan.bookTitle}${result.finesAssessed.length > 0 ? ` — charges: ${result.finesAssessed.map((f) => f.amount.toFixed(2)).join(' + ')}` : ''}`,
        })
      } else if (result.wasLate) {
        toast.warning(`Returned ${result.daysLate} day${result.daysLate === 1 ? '' : 's'} late`, {
          description: `${result.loan.bookTitle} — fine of ${result.fineAssessed?.amount.toFixed(2)} assessed to ${result.loan.memberName}`,
        })
      } else {
        toast.success('Returned on time', {
          description: `${result.loan.bookTitle} — thanks, ${result.loan.memberName}`,
        })
      }
      if (result.holdReadyFor) {
        toast.info('Do not shelve this book', {
          description: `Set it aside — it's reserved for ${result.holdReadyFor}.`,
          duration: 10000,
        })
      }
      if (result.returnedAtDifferentBranch && result.homeBranchName) {
        toast.info('Wrong branch', {
          description: `This copy lives at ${result.homeBranchName} — send it back via transfer.`,
          duration: 10000,
        })
      }
      reset()
      onDone()
    },
    onError: (error: Error) => toast.error('Return failed', { description: error.message }),
  })

  const lostMutation = useMutation({
    mutationFn: () => reportLost(barcode.trim()),
    onSuccess: (result) => {
      toast.warning(`${result.loan.bookTitle} written off as lost`, {
        description: result.replacementCharge
          ? `Replacement charge of ${result.replacementCharge.amount.toFixed(2)} assessed to ${result.loan.memberName}`
          : `No price on file — add a manual charge if needed.`,
        duration: 10000,
      })
      reset()
      onDone()
    },
    onError: (error: Error) => toast.error('Could not report lost', { description: error.message }),
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <ArrowDownToLine className="size-4 text-accent" />
          Return
        </CardTitle>
        <CardDescription>Scan, judge the condition, done.</CardDescription>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={(e) => {
            e.preventDefault()
            mutation.mutate()
          }}
          className="flex flex-col gap-4"
        >
          <div>
            <Label htmlFor="ret-barcode">Copy barcode</Label>
            <div className="relative">
              <ScanBarcode className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
              <Input
                id="ret-barcode"
                placeholder="BC-0000"
                className="pl-10 font-mono"
                value={barcode}
                onChange={(e) => setBarcode(e.target.value)}
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label htmlFor="ret-outcome">Came back</Label>
              <Select
                id="ret-outcome"
                value={outcome}
                onChange={(e) => setOutcome(e.target.value as ReturnOutcome)}
              >
                <option value="Normal">In good order</option>
                <option value="Damaged">Damaged</option>
              </Select>
            </div>
            <div>
              <Label htmlFor="ret-condition">Condition</Label>
              <Select
                id="ret-condition"
                value={condition}
                onChange={(e) => setCondition(e.target.value as CopyCondition | '')}
              >
                <option value="">Unchanged</option>
                {COPY_CONDITIONS.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </Select>
            </div>
          </div>
          {outcome === 'Damaged' && (
            <div>
              <Label htmlFor="ret-charge">Damage charge (optional)</Label>
              <Input
                id="ret-charge"
                type="number"
                min={0}
                step="0.50"
                placeholder="0.00"
                value={damageCharge}
                onChange={(e) => setDamageCharge(e.target.value)}
              />
            </div>
          )}
          <div className="flex gap-2">
            <Button
              type="submit"
              variant="secondary"
              className="flex-1"
              disabled={mutation.isPending || !barcode.trim()}
            >
              {mutation.isPending ? <Spinner /> : <ArrowDownToLine className="size-4" />}
              Return
            </Button>
            <Button
              variant="ghost"
              title="The member cannot return it — write the copy off and raise a replacement charge"
              disabled={lostMutation.isPending || !barcode.trim()}
              onClick={() => lostMutation.mutate()}
            >
              {lostMutation.isPending ? <Spinner /> : <SearchX className="size-4" />}
              Lost
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}

/* ── Renew ────────────────────────────────────────────────────────────────── */

function RenewPanel({ onDone }: { onDone: () => void }) {
  const [barcode, setBarcode] = useState('')

  const mutation = useMutation({
    mutationFn: () => renewLoan(barcode.trim()),
    onSuccess: (loan) => {
      toast.success('Renewed', {
        description: `${loan.bookTitle} — now due ${formatDue(loan.dueAtUtc)} for ${loan.memberName}`,
      })
      setBarcode('')
      onDone()
    },
    onError: (error: Error) => toast.error('Renewal refused', { description: error.message }),
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <RotateCw className="size-4 text-accent" />
          Renew
        </CardTitle>
        <CardDescription>Blocked if overdue, at the limit, or if someone's waiting.</CardDescription>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={(e) => {
            e.preventDefault()
            mutation.mutate()
          }}
          className="flex flex-col gap-4"
        >
          <div>
            <Label htmlFor="renew-barcode">Copy barcode</Label>
            <div className="relative">
              <ScanBarcode className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
              <Input
                id="renew-barcode"
                placeholder="BC-0000"
                className="pl-10 font-mono"
                value={barcode}
                onChange={(e) => setBarcode(e.target.value)}
              />
            </div>
          </div>
          <Button type="submit" variant="secondary" disabled={mutation.isPending || !barcode.trim()}>
            {mutation.isPending ? <Spinner /> : <RotateCw className="size-4" />}
            Renew
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}

/* ── Transfer request ─────────────────────────────────────────────────────── */

function TransferPanel({ onDone }: { onDone: () => void }) {
  const [barcode, setBarcode] = useState('')
  const [toBranchId, setToBranchId] = useState('')
  const branches = useQuery({ queryKey: ['branches'], queryFn: getBranches })

  const mutation = useMutation({
    mutationFn: () => createTransfer(barcode.trim(), toBranchId),
    onSuccess: (transfer) => {
      toast.success('Transfer requested', {
        description: `${transfer.bookTitle} → ${transfer.toBranchName}. Dispatch it when it leaves.`,
      })
      setBarcode('')
      setToBranchId('')
      onDone()
    },
    onError: (error: Error) => toast.error('Transfer refused', { description: error.message }),
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <ArrowLeftRight className="size-4 text-accent" />
          Transfer
        </CardTitle>
        <CardDescription>Request → dispatch → receive, with full history.</CardDescription>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={(e) => {
            e.preventDefault()
            mutation.mutate()
          }}
          className="flex flex-col gap-4"
        >
          <div>
            <Label htmlFor="tr-barcode">Copy barcode</Label>
            <div className="relative">
              <ScanBarcode className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
              <Input
                id="tr-barcode"
                placeholder="BC-0000"
                className="pl-10 font-mono"
                value={barcode}
                onChange={(e) => setBarcode(e.target.value)}
              />
            </div>
          </div>
          <div>
            <Label htmlFor="tr-branch">Destination branch</Label>
            <Select id="tr-branch" value={toBranchId} onChange={(e) => setToBranchId(e.target.value)}>
              <option value="">Choose a branch…</option>
              {branches.data?.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name}
                </option>
              ))}
            </Select>
          </div>
          <Button type="submit" variant="secondary" disabled={mutation.isPending || !barcode.trim() || !toBranchId}>
            {mutation.isPending ? <Spinner /> : <ArrowLeftRight className="size-4" />}
            Request
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}

/* ── Page ─────────────────────────────────────────────────────────────────── */

const TRANSFER_BADGE: Record<TransferStatus, 'brass' | 'neutral' | 'success' | 'danger'> = {
  Requested: 'neutral',
  InTransit: 'brass',
  Received: 'success',
  Rejected: 'danger',
  Cancelled: 'neutral',
  LostInTransit: 'danger',
  DamagedInTransit: 'danger',
}

export function CirculationPage() {
  const queryClient = useQueryClient()
  const [overdueOnly, setOverdueOnly] = useState(false)
  const [deskBranchId, setDeskBranchId] = useState(() => localStorage.getItem(DESK_BRANCH_KEY) ?? '')
  const branches = useQuery({ queryKey: ['branches'], queryFn: getBranches })
  const loans = useQuery({ queryKey: ['active-loans'], queryFn: getActiveLoans })
  const transfers = useQuery({ queryKey: ['pending-transfers'], queryFn: getPendingTransfers })

  useEffect(() => {
    localStorage.setItem(DESK_BRANCH_KEY, deskBranchId)
  }, [deskBranchId])

  const visibleLoans = (loans.data ?? []).filter((l) => !overdueOnly || l.isOverdue)
  const overdueCount = (loans.data ?? []).filter((l) => l.isOverdue).length

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['active-loans'] })
    void queryClient.invalidateQueries({ queryKey: ['pending-transfers'] })
  }

  const receive = useMutation({
    mutationFn: (barcode: string) => receiveTransfer(barcode),
    onSuccess: (transfer) => {
      toast.success('Transfer received', {
        description: `${transfer.bookTitle} is now at ${transfer.toBranchName}.`,
      })
      refresh()
    },
    onError: (error: Error) => toast.error('Could not receive transfer', { description: error.message }),
  })

  const act = useMutation({
    mutationFn: ({ id, action }: { id: string; action: Parameters<typeof transferAction>[1] }) =>
      transferAction(id, action),
    onSuccess: (transfer) => {
      toast.success(`Transfer ${transfer.status}`, { description: transfer.bookTitle })
      refresh()
    },
    onError: (error: Error) => toast.error('Transfer action failed', { description: error.message }),
  })

  return (
    <div className="flex flex-col gap-8">
      <header className="flex animate-fade flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Circulation</p>
          <h1 className="font-display mt-2 text-3xl font-semibold sm:text-4xl">The desk</h1>
          <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
            Check books out and in. Everything is scan-first — card, then barcode.
          </p>
        </div>
        <div className="w-56">
          <Label htmlFor="desk-branch">
            <span className="inline-flex items-center gap-1.5">
              <Building2 className="size-3.5" /> This desk's branch
            </span>
          </Label>
          <Select id="desk-branch" value={deskBranchId} onChange={(e) => setDeskBranchId(e.target.value)}>
            <option value="">Any branch (HQ mode)</option>
            {branches.data?.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name}
              </option>
            ))}
          </Select>
        </div>
      </header>

      <div className="grid animate-rise gap-6 sm:grid-cols-2 xl:grid-cols-4">
        <CheckoutPanel deskBranchId={deskBranchId} onDone={refresh} />
        <ReturnPanel deskBranchId={deskBranchId} onDone={refresh} />
        <RenewPanel onDone={refresh} />
        <TransferPanel onDone={refresh} />
      </div>

      {transfers.data && transfers.data.length > 0 && (
        <Card className="animate-rise">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <PackageCheck className="size-4 text-accent" />
              Open transfers
              <Badge variant="brass">{transfers.data.length}</Badge>
            </CardTitle>
            <CardDescription>Dispatch at the source; scan in at the destination.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto rounded-xl border border-border">
              <table className="w-full min-w-130 text-sm">
                <thead>
                  <tr className="border-b border-border bg-surface-2 text-left text-xs uppercase tracking-wider text-muted">
                    <th className="px-4 py-2.5 font-medium">Book</th>
                    <th className="px-4 py-2.5 font-medium">Barcode</th>
                    <th className="px-4 py-2.5 font-medium">Route</th>
                    <th className="px-4 py-2.5 font-medium">Status</th>
                    <th className="px-4 py-2.5 font-medium"></th>
                  </tr>
                </thead>
                <tbody>
                  {transfers.data.map((t) => (
                    <tr key={t.id} className="border-b border-border last:border-0">
                      <td className="px-4 py-2.5 font-medium">{t.bookTitle}</td>
                      <td className="px-4 py-2.5 font-mono text-[13px]">{t.barcode}</td>
                      <td className="px-4 py-2.5 text-muted">
                        {t.fromBranchName ?? 'Unassigned'} → <span className="text-ink">{t.toBranchName}</span>
                      </td>
                      <td className="px-4 py-2.5">
                        <Badge variant={TRANSFER_BADGE[t.status]}>{t.status}</Badge>
                      </td>
                      <td className="px-4 py-2.5">
                        <div className="flex justify-end gap-1.5">
                          {t.status === 'Requested' && (
                            <>
                              <Button
                                size="sm"
                                variant="secondary"
                                disabled={act.isPending}
                                onClick={() => act.mutate({ id: t.id, action: 'Dispatch' })}
                              >
                                Dispatch
                              </Button>
                              <Button
                                size="sm"
                                variant="ghost"
                                disabled={act.isPending}
                                onClick={() => act.mutate({ id: t.id, action: 'Cancel' })}
                              >
                                Cancel
                              </Button>
                            </>
                          )}
                          {t.status === 'InTransit' && (
                            <>
                              <Button
                                size="sm"
                                variant="secondary"
                                disabled={receive.isPending}
                                onClick={() => receive.mutate(t.barcode)}
                              >
                                <PackageCheck className="size-4" />
                                Receive
                              </Button>
                              <Button
                                size="sm"
                                variant="ghost"
                                title="Never arrived — write off as lost"
                                disabled={act.isPending}
                                onClick={() => act.mutate({ id: t.id, action: 'LostInTransit' })}
                              >
                                <PackageX className="size-4" />
                              </Button>
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      )}

      <Card className="animate-rise">
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <CardTitle>On loan now</CardTitle>
              <CardDescription>
                {loans.data ? `${loans.data.length} active loan${loans.data.length === 1 ? '' : 's'}, soonest due first.` : 'Loading…'}
              </CardDescription>
            </div>
            {overdueCount > 0 && (
              <Button
                variant={overdueOnly ? 'danger' : 'secondary'}
                size="sm"
                onClick={() => setOverdueOnly((v) => !v)}
              >
                <AlarmClock className="size-4" />
                {overdueOnly ? 'Showing overdue' : `Overdue (${overdueCount})`}
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {loans.isPending ? (
            <div className="grid min-h-24 place-items-center text-muted">
              <Spinner className="size-5" />
            </div>
          ) : visibleLoans.length === 0 ? (
            <div className="flex items-center gap-3 rounded-xl border border-dashed border-border-strong bg-surface-2 px-4 py-6 text-sm text-muted">
              <AlarmClock className="size-4 shrink-0" />
              {overdueOnly ? 'Nothing is overdue. Well-behaved patrons.' : 'Nothing is out right now.'}
            </div>
          ) : (
            <div className="overflow-x-auto rounded-xl border border-border">
              <table className="w-full min-w-130 text-sm">
                <thead>
                  <tr className="border-b border-border bg-surface-2 text-left text-xs uppercase tracking-wider text-muted">
                    <th className="px-4 py-2.5 font-medium">Book</th>
                    <th className="px-4 py-2.5 font-medium">Barcode</th>
                    <th className="px-4 py-2.5 font-medium">Member</th>
                    <th className="px-4 py-2.5 font-medium">Due</th>
                  </tr>
                </thead>
                <tbody>
                  {visibleLoans.map((loan) => (
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
                      <td className="px-4 py-2.5 font-mono text-[13px]">{loan.barcode}</td>
                      <td className="px-4 py-2.5">
                        <Link to={`/patrons/${loan.memberId}`} className="hover:text-accent">
                          {loan.memberName}
                        </Link>
                      </td>
                      <td className="px-4 py-2.5">
                        <Badge variant={loan.isOverdue ? 'danger' : 'neutral'}>
                          {loan.isOverdue ? 'Overdue · ' : ''}
                          {formatDue(loan.dueAtUtc)}
                        </Badge>
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
