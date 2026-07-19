import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCheck, ClipboardList, PackageSearch, Play, ScanBarcode, Sparkles } from 'lucide-react'
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
  completeStocktake,
  getBranches,
  getOpenStocktake,
  getStocktakes,
  scanStocktakeItem,
  startStocktake,
} from '@/lib/api'
import type { StocktakeReport } from '@/lib/circulation'

function formatDate(value: string) {
  return new Date(value).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })
}

export function StocktakePage() {
  const queryClient = useQueryClient()
  const [branchId, setBranchId] = useState('')
  const [barcode, setBarcode] = useState('')
  const [report, setReport] = useState<StocktakeReport | null>(null)

  const branches = useQuery({ queryKey: ['branches'], queryFn: getBranches })
  const open = useQuery({ queryKey: ['stocktake-open'], queryFn: getOpenStocktake })
  const history = useQuery({ queryKey: ['stocktakes'], queryFn: getStocktakes })

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['stocktake-open'] })
    void queryClient.invalidateQueries({ queryKey: ['stocktakes'] })
  }

  const start = useMutation({
    mutationFn: () => startStocktake(branchId || null),
    onSuccess: (s) => {
      toast.success('Stocktake started', {
        description: `${s.expectedCount} copies expected on the shelf — start scanning.`,
      })
      setReport(null)
      refresh()
    },
    onError: (error: Error) => toast.error('Could not start', { description: error.message }),
  })

  const scan = useMutation({
    mutationFn: (code: string) => scanStocktakeItem(open.data!.id, code),
    onSuccess: (result) => {
      if (result.alreadyScanned) {
        toast.info('Already counted', { description: `${result.barcode} — ${result.bookTitle}` })
      } else if (result.wasFound) {
        toast.success('Found! Welcome back', {
          description: `${result.bookTitle} was written off — restored to Available.`,
          duration: 8000,
        })
      } else {
        toast.success(`Counted ${result.stocktake.scannedCount} of ${result.stocktake.expectedCount}`, {
          description: `${result.barcode} — ${result.bookTitle}`,
        })
      }
      setBarcode('')
      refresh()
    },
    onError: (error: Error) => toast.error('Scan failed', { description: error.message }),
  })

  const complete = useMutation({
    mutationFn: () => completeStocktake(open.data!.id),
    onSuccess: (result) => {
      setReport(result)
      toast.success('Stocktake complete', {
        description: `${result.stocktake.scannedCount} counted · ${result.missing.length} missing · ${result.found.length} found`,
      })
      refresh()
    },
    onError: (error: Error) => toast.error('Could not complete', { description: error.message }),
  })

  return (
    <div className="flex flex-col gap-8">
      <header className="animate-fade">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Inventory</p>
        <h1 className="font-display mt-2 text-3xl font-semibold sm:text-4xl">Stocktake</h1>
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
          Count the shelves. Unscanned copies go Missing; scanning a written-off copy brings it
          back. Nothing is ever deleted.
        </p>
      </header>

      {open.data ? (
        <Card className="animate-rise">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <ClipboardList className="size-4 text-accent" />
              Counting {open.data.branchName ?? 'the whole library'}
              <Badge variant="brass">
                {open.data.scannedCount} / {open.data.expectedCount}
              </Badge>
              {open.data.foundCount > 0 && <Badge variant="success">{open.data.foundCount} found</Badge>}
            </CardTitle>
            <CardDescription>
              Started {formatDate(open.data.startedAtUtc)} by {open.data.startedBy ?? 'staff'}.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <div className="h-1.5 overflow-hidden rounded-full bg-surface-2">
              <div
                className="h-full rounded-full bg-accent transition-all duration-300"
                style={{
                  width: `${open.data.expectedCount === 0 ? 100 : Math.min(100, (open.data.scannedCount / open.data.expectedCount) * 100)}%`,
                }}
              />
            </div>
            <form
              onSubmit={(e) => {
                e.preventDefault()
                if (barcode.trim()) scan.mutate(barcode.trim())
              }}
              className="flex flex-col gap-3 sm:flex-row sm:items-end"
            >
              <div className="flex-1 sm:max-w-sm">
                <Label htmlFor="st-barcode">Scan copy</Label>
                <div className="relative">
                  <ScanBarcode className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
                  <Input
                    id="st-barcode"
                    autoFocus
                    placeholder="BC-0000"
                    className="pl-10 font-mono"
                    value={barcode}
                    onChange={(e) => setBarcode(e.target.value)}
                  />
                </div>
              </div>
              <Button type="submit" disabled={scan.isPending || !barcode.trim()}>
                {scan.isPending ? <Spinner /> : <ScanBarcode className="size-4" />}
                Count
              </Button>
              <Button
                variant="secondary"
                disabled={complete.isPending}
                onClick={() => complete.mutate()}
              >
                {complete.isPending ? <Spinner /> : <CheckCheck className="size-4" />}
                Complete stocktake
              </Button>
            </form>
          </CardContent>
        </Card>
      ) : (
        <Card className="animate-rise">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Play className="size-4 text-accent" />
              Start a count
            </CardTitle>
            <CardDescription>One stocktake can be open at a time.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
              <div className="sm:w-64">
                <Label htmlFor="st-branch">Scope</Label>
                <Select id="st-branch" value={branchId} onChange={(e) => setBranchId(e.target.value)}>
                  <option value="">Whole library</option>
                  {branches.data?.map((b) => (
                    <option key={b.id} value={b.id}>
                      {b.name}
                    </option>
                  ))}
                </Select>
              </div>
              <Button disabled={start.isPending} onClick={() => start.mutate()}>
                {start.isPending ? <Spinner /> : <Play className="size-4" />}
                Start stocktake
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {report && (
        <Card className="animate-rise border-accent/30">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <PackageSearch className="size-4 text-accent" />
              Results
            </CardTitle>
            <CardDescription>
              {report.stocktake.scannedCount} counted of {report.stocktake.expectedCount} expected.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-5">
            {report.found.length > 0 && (
              <div>
                <p className="mb-2 flex items-center gap-1.5 text-sm font-medium text-success">
                  <Sparkles className="size-4" /> Found ({report.found.length})
                </p>
                <ul className="flex flex-col gap-1 text-sm text-muted">
                  {report.found.map((c) => (
                    <li key={c.copyId}>
                      <span className="font-mono text-[13px]">{c.barcode}</span> —{' '}
                      <Link to={`/catalog/books/${c.bookId}`} className="text-ink hover:text-accent">
                        {c.bookTitle}
                      </Link>{' '}
                      restored to Available
                    </li>
                  ))}
                </ul>
              </div>
            )}
            {report.missing.length === 0 ? (
              <p className="text-sm text-success">Nothing missing — a perfect shelf. 🎉</p>
            ) : (
              <div>
                <p className="mb-2 text-sm font-medium text-danger">Missing ({report.missing.length})</p>
                <div className="overflow-x-auto rounded-xl border border-border">
                  <table className="w-full min-w-130 text-sm">
                    <thead>
                      <tr className="border-b border-border bg-surface-2 text-left text-xs uppercase tracking-wider text-muted">
                        <th className="px-4 py-2.5 font-medium">Barcode</th>
                        <th className="px-4 py-2.5 font-medium">Book</th>
                        <th className="px-4 py-2.5 font-medium">Branch</th>
                      </tr>
                    </thead>
                    <tbody>
                      {report.missing.map((c) => (
                        <tr key={c.copyId} className="border-b border-border last:border-0">
                          <td className="px-4 py-2.5 font-mono text-[13px]">{c.barcode}</td>
                          <td className="px-4 py-2.5">
                            <Link to={`/catalog/books/${c.bookId}`} className="font-medium text-ink hover:text-accent">
                              {c.bookTitle}
                            </Link>
                          </td>
                          <td className="px-4 py-2.5 text-muted">{c.branchName ?? '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      <Card className="animate-rise">
        <CardHeader>
          <CardTitle>Past counts</CardTitle>
        </CardHeader>
        <CardContent>
          {!history.data || history.data.length === 0 ? (
            <p className="text-sm text-muted">No stocktakes yet.</p>
          ) : (
            <div className="overflow-x-auto rounded-xl border border-border">
              <table className="w-full min-w-130 text-sm">
                <thead>
                  <tr className="border-b border-border bg-surface-2 text-left text-xs uppercase tracking-wider text-muted">
                    <th className="px-4 py-2.5 font-medium">Scope</th>
                    <th className="px-4 py-2.5 font-medium">Started</th>
                    <th className="px-4 py-2.5 font-medium">Counted</th>
                    <th className="px-4 py-2.5 font-medium">Missing</th>
                    <th className="px-4 py-2.5 font-medium">Found</th>
                    <th className="px-4 py-2.5 font-medium">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {history.data.map((s) => (
                    <tr key={s.id} className="border-b border-border last:border-0">
                      <td className="px-4 py-2.5 font-medium">{s.branchName ?? 'Whole library'}</td>
                      <td className="px-4 py-2.5">{formatDate(s.startedAtUtc)}</td>
                      <td className="px-4 py-2.5">
                        {s.scannedCount} / {s.expectedCount}
                      </td>
                      <td className="px-4 py-2.5">
                        {s.missingCount > 0 ? <Badge variant="danger">{s.missingCount}</Badge> : '0'}
                      </td>
                      <td className="px-4 py-2.5">
                        {s.foundCount > 0 ? <Badge variant="success">{s.foundCount}</Badge> : '0'}
                      </td>
                      <td className="px-4 py-2.5">
                        <Badge variant={s.status === 'Open' ? 'brass' : 'neutral'}>{s.status}</Badge>
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
