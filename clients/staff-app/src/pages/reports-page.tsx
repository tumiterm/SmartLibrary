import { useQuery } from '@tanstack/react-query'
import { BarChart3, Download, HandCoins, Library } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import { downloadReportCsv, getCirculationReport, getFinesReport, getInventoryReport } from '@/lib/api'

function isoDate(d: Date) {
  return d.toISOString().slice(0, 10)
}

function Stat({ label, value, tone }: { label: string; value: string | number; tone?: 'danger' | 'brass' }) {
  return (
    <div className="rounded-xl border border-border bg-surface-2 px-4 py-3">
      <p
        className={`font-display text-2xl font-semibold ${tone === 'danger' ? 'text-danger' : tone === 'brass' ? 'text-accent' : ''}`}
      >
        {value}
      </p>
      <p className="mt-0.5 text-[11px] uppercase tracking-wider text-muted">{label}</p>
    </div>
  )
}

function DownloadButton({
  path,
  params,
}: {
  path: 'circulation' | 'inventory' | 'fines'
  params: { from?: string; to?: string }
}) {
  const [busy, setBusy] = useState(false)
  return (
    <Button
      variant="secondary"
      size="sm"
      disabled={busy}
      onClick={async () => {
        setBusy(true)
        try {
          await downloadReportCsv(path, params)
        } catch (error) {
          toast.error('Export failed', { description: (error as Error).message })
        } finally {
          setBusy(false)
        }
      }}
    >
      {busy ? <Spinner /> : <Download className="size-4" />}
      CSV
    </Button>
  )
}

export function ReportsPage() {
  const [from, setFrom] = useState(() => isoDate(new Date(Date.now() - 30 * 24 * 3600 * 1000)))
  const [to, setTo] = useState(() => isoDate(new Date(Date.now() + 24 * 3600 * 1000)))

  const circulation = useQuery({
    queryKey: ['report-circulation', from, to],
    queryFn: () => getCirculationReport(from, to),
    enabled: !!from && !!to,
  })
  const inventory = useQuery({ queryKey: ['report-inventory'], queryFn: getInventoryReport })
  const fines = useQuery({
    queryKey: ['report-fines', from, to],
    queryFn: () => getFinesReport(from, to),
    enabled: !!from && !!to,
  })

  return (
    <div className="flex flex-col gap-8">
      <header className="flex animate-fade flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Reports</p>
          <h1 className="font-display mt-2 text-3xl font-semibold sm:text-4xl">The numbers</h1>
          <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
            Circulation, inventory and fines — on screen and as CSV exports.
          </p>
        </div>
        <div className="flex items-end gap-3">
          <div>
            <Label htmlFor="rep-from">From</Label>
            <Input id="rep-from" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div>
            <Label htmlFor="rep-to">To</Label>
            <Input id="rep-to" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>
        </div>
      </header>

      {/* ── Circulation ── */}
      <Card className="animate-rise">
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <CardTitle className="flex items-center gap-2">
                <BarChart3 className="size-4 text-accent" />
                Circulation
              </CardTitle>
              <CardDescription>Loans in the selected period.</CardDescription>
            </div>
            <DownloadButton path="circulation" params={{ from, to }} />
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-5">
          {circulation.isPending ? (
            <div className="grid min-h-24 place-items-center text-muted">
              <Spinner className="size-5" />
            </div>
          ) : circulation.data ? (
            <>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-5">
                <Stat label="Checkouts" value={circulation.data.checkouts} />
                <Stat label="Returns" value={circulation.data.returns} />
                <Stat label="Late returns" value={circulation.data.lateReturns} tone={circulation.data.lateReturns > 0 ? 'danger' : undefined} />
                <Stat label="Out right now" value={circulation.data.activeLoansNow} tone="brass" />
                <Stat label="Overdue now" value={circulation.data.overdueNow} tone={circulation.data.overdueNow > 0 ? 'danger' : undefined} />
              </div>
              {(circulation.data.topTitles.length > 0 || circulation.data.topMembers.length > 0) && (
                <div className="grid gap-5 sm:grid-cols-2">
                  <div>
                    <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-muted">Most borrowed</p>
                    <ul className="flex flex-col gap-1.5 text-sm">
                      {circulation.data.topTitles.map((t) => (
                        <li key={t.id} className="flex items-center justify-between gap-2">
                          <span className="truncate">{t.label}</span>
                          <Badge variant="brass">{t.count}</Badge>
                        </li>
                      ))}
                    </ul>
                  </div>
                  <div>
                    <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-muted">Most active members</p>
                    <ul className="flex flex-col gap-1.5 text-sm">
                      {circulation.data.topMembers.map((m) => (
                        <li key={m.id} className="flex items-center justify-between gap-2">
                          <span className="truncate">{m.label}</span>
                          <Badge>{m.count}</Badge>
                        </li>
                      ))}
                    </ul>
                  </div>
                </div>
              )}
            </>
          ) : (
            <p className="text-sm text-muted">Pick a valid period.</p>
          )}
        </CardContent>
      </Card>

      {/* ── Inventory ── */}
      <Card className="animate-rise">
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Library className="size-4 text-accent" />
                Inventory
              </CardTitle>
              <CardDescription>The collection as it stands right now.</CardDescription>
            </div>
            <DownloadButton path="inventory" params={{}} />
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-5">
          {inventory.isPending ? (
            <div className="grid min-h-24 place-items-center text-muted">
              <Spinner className="size-5" />
            </div>
          ) : inventory.data ? (
            <>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                <Stat label="Titles" value={inventory.data.titles} />
                <Stat label="Copies" value={inventory.data.copies} />
                {inventory.data.byStatus
                  .filter((s) => s.label === 'Available' || s.label === 'OnLoan')
                  .map((s) => (
                    <Stat key={s.label} label={s.label === 'OnLoan' ? 'On loan' : s.label} value={s.count} />
                  ))}
              </div>
              <div className="grid gap-5 lg:grid-cols-2">
                <div className="overflow-x-auto rounded-xl border border-border">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-border bg-surface-2 text-left text-xs uppercase tracking-wider text-muted">
                        <th className="px-4 py-2.5 font-medium">Branch</th>
                        <th className="px-4 py-2.5 font-medium">Copies</th>
                        <th className="px-4 py-2.5 font-medium">Available</th>
                        <th className="px-4 py-2.5 font-medium">On loan</th>
                        <th className="px-4 py-2.5 font-medium">Other</th>
                      </tr>
                    </thead>
                    <tbody>
                      {inventory.data.byBranch.map((r) => (
                        <tr key={r.branchName} className="border-b border-border last:border-0">
                          <td className="px-4 py-2.5 font-medium">{r.branchName}</td>
                          <td className="px-4 py-2.5">{r.copies}</td>
                          <td className="px-4 py-2.5">{r.available}</td>
                          <td className="px-4 py-2.5">{r.onLoan}</td>
                          <td className="px-4 py-2.5">{r.other}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <div className="flex flex-col gap-4">
                  <div>
                    <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-muted">Copies by status</p>
                    <div className="flex flex-wrap gap-2">
                      {inventory.data.byStatus.map((s) => (
                        <Badge key={s.label} variant={s.label === 'Available' ? 'success' : s.label === 'OnLoan' ? 'brass' : 'neutral'}>
                          {s.label}: {s.count}
                        </Badge>
                      ))}
                    </div>
                  </div>
                  <div>
                    <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-muted">Titles by format</p>
                    <div className="flex flex-wrap gap-2">
                      {inventory.data.byFormat.map((s) => (
                        <Badge key={s.label}>
                          {s.label}: {s.count}
                        </Badge>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            </>
          ) : null}
        </CardContent>
      </Card>

      {/* ── Fines ── */}
      <Card className="animate-rise">
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <CardTitle className="flex items-center gap-2">
                <HandCoins className="size-4 text-accent" />
                Fines
              </CardTitle>
              <CardDescription>Assessed in the selected period; outstanding is a live snapshot.</CardDescription>
            </div>
            <DownloadButton path="fines" params={{ from, to }} />
          </div>
        </CardHeader>
        <CardContent>
          {fines.isPending ? (
            <div className="grid min-h-24 place-items-center text-muted">
              <Spinner className="size-5" />
            </div>
          ) : fines.data ? (
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <Stat label="Assessed" value={fines.data.assessedTotal.toFixed(2)} />
              <Stat label="Collected" value={fines.data.paidTotal.toFixed(2)} tone="brass" />
              <Stat label="Waived" value={fines.data.waivedTotal.toFixed(2)} />
              <Stat
                label="Outstanding now"
                value={fines.data.outstandingNow.toFixed(2)}
                tone={fines.data.outstandingNow > 0 ? 'danger' : undefined}
              />
            </div>
          ) : (
            <p className="text-sm text-muted">Pick a valid period.</p>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
