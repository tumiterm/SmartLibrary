import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlarmClock,
  ArrowDownToLine,
  ArrowLeftRight,
  ArrowUpFromLine,
  IdCard,
  PackageCheck,
  RotateCw,
  ScanBarcode,
} from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input, Select } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import {
  checkoutBook,
  createTransfer,
  getActiveLoans,
  getBranches,
  getPendingTransfers,
  receiveTransfer,
  renewLoan,
  returnBook,
} from '@/lib/api'

function formatDue(value: string) {
  return new Date(value).toLocaleDateString(undefined, { day: 'numeric', month: 'short' })
}

function CheckoutPanel({ onDone }: { onDone: () => void }) {
  const [card, setCard] = useState('')
  const [barcode, setBarcode] = useState('')

  const mutation = useMutation({
    mutationFn: () => checkoutBook(card.trim(), barcode.trim()),
    onSuccess: (loan) => {
      toast.success(`Checked out to ${loan.memberName}`, {
        description: `${loan.bookTitle} — due ${formatDue(loan.dueAtUtc)}`,
      })
      setCard('')
      setBarcode('')
      onDone()
    },
    onError: (error: Error) => toast.error('Checkout refused', { description: error.message }),
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <ArrowUpFromLine className="size-4 text-accent" />
          Check out
        </CardTitle>
        <CardDescription>Scan the member's card, then the book.</CardDescription>
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
            <Label htmlFor="co-barcode">Copy barcode</Label>
            <div className="relative">
              <ScanBarcode className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
              <Input
                id="co-barcode"
                placeholder="BC-0000"
                className="pl-10 font-mono"
                value={barcode}
                onChange={(e) => setBarcode(e.target.value)}
              />
            </div>
          </div>
          <Button type="submit" disabled={mutation.isPending || !card.trim() || !barcode.trim()}>
            {mutation.isPending ? <Spinner /> : <ArrowUpFromLine className="size-4" />}
            Check out
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}

function ReturnPanel({ onDone }: { onDone: () => void }) {
  const [barcode, setBarcode] = useState('')

  const mutation = useMutation({
    mutationFn: () => returnBook(barcode.trim()),
    onSuccess: (result) => {
      if (result.wasLate) {
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
      setBarcode('')
      onDone()
    },
    onError: (error: Error) => toast.error('Return failed', { description: error.message }),
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <ArrowDownToLine className="size-4 text-accent" />
          Return
        </CardTitle>
        <CardDescription>One scan — we find the loan and assess any fine.</CardDescription>
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
          <Button type="submit" variant="secondary" disabled={mutation.isPending || !barcode.trim()}>
            {mutation.isPending ? <Spinner /> : <ArrowDownToLine className="size-4" />}
            Return
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}

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

function TransferPanel({ onDone }: { onDone: () => void }) {
  const [barcode, setBarcode] = useState('')
  const [toBranchId, setToBranchId] = useState('')
  const branches = useQuery({ queryKey: ['branches'], queryFn: getBranches })

  const mutation = useMutation({
    mutationFn: () => createTransfer(barcode.trim(), toBranchId),
    onSuccess: (transfer) => {
      toast.success('Transfer started', {
        description: `${transfer.bookTitle} → ${transfer.toBranchName} (in transit)`,
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
        <CardDescription>Send an available copy to another branch.</CardDescription>
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
            Send
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}

export function CirculationPage() {
  const queryClient = useQueryClient()
  const loans = useQuery({ queryKey: ['active-loans'], queryFn: getActiveLoans })
  const transfers = useQuery({ queryKey: ['pending-transfers'], queryFn: getPendingTransfers })

  const receive = useMutation({
    mutationFn: (barcode: string) => receiveTransfer(barcode),
    onSuccess: (transfer) => {
      toast.success('Transfer received', {
        description: `${transfer.bookTitle} is now at ${transfer.toBranchName}.`,
      })
      void queryClient.invalidateQueries({ queryKey: ['pending-transfers'] })
    },
    onError: (error: Error) => toast.error('Could not receive transfer', { description: error.message }),
  })

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['active-loans'] })
    void queryClient.invalidateQueries({ queryKey: ['pending-transfers'] })
  }

  return (
    <div className="flex flex-col gap-8">
      <header className="animate-fade">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Circulation</p>
        <h1 className="font-display mt-2 text-3xl font-semibold sm:text-4xl">The desk</h1>
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
          Check books out and in. Everything is scan-first — card, then barcode.
        </p>
      </header>

      <div className="grid animate-rise gap-6 sm:grid-cols-2 xl:grid-cols-4">
        <CheckoutPanel onDone={refresh} />
        <ReturnPanel onDone={refresh} />
        <RenewPanel onDone={refresh} />
        <TransferPanel onDone={refresh} />
      </div>

      {transfers.data && transfers.data.length > 0 && (
        <Card className="animate-rise">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <PackageCheck className="size-4 text-accent" />
              In transit
              <Badge variant="brass">{transfers.data.length}</Badge>
            </CardTitle>
            <CardDescription>Scan these in at the receiving branch.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto rounded-xl border border-border">
              <table className="w-full min-w-130 text-sm">
                <thead>
                  <tr className="border-b border-border bg-surface-2 text-left text-xs uppercase tracking-wider text-muted">
                    <th className="px-4 py-2.5 font-medium">Book</th>
                    <th className="px-4 py-2.5 font-medium">Barcode</th>
                    <th className="px-4 py-2.5 font-medium">Route</th>
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
                      <td className="px-4 py-2.5 text-right">
                        <Button
                          size="sm"
                          variant="secondary"
                          disabled={receive.isPending}
                          onClick={() => receive.mutate(t.barcode)}
                        >
                          <PackageCheck className="size-4" />
                          Receive
                        </Button>
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
          <CardTitle>On loan now</CardTitle>
          <CardDescription>
            {loans.data ? `${loans.data.length} active loan${loans.data.length === 1 ? '' : 's'}, soonest due first.` : 'Loading…'}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loans.isPending ? (
            <div className="grid min-h-24 place-items-center text-muted">
              <Spinner className="size-5" />
            </div>
          ) : !loans.data || loans.data.length === 0 ? (
            <div className="flex items-center gap-3 rounded-xl border border-dashed border-border-strong bg-surface-2 px-4 py-6 text-sm text-muted">
              <AlarmClock className="size-4 shrink-0" />
              Nothing is out right now.
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
                  {loans.data.map((loan) => (
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
