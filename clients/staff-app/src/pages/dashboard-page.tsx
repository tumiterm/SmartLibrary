import { useQuery } from '@tanstack/react-query'
import {
  AlarmClock,
  ArrowDownToLine,
  ArrowRight,
  ArrowUpFromLine,
  BookPlus,
  PackageOpen,
  Users,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Spinner } from '@/components/ui/spinner'
import { getDashboard } from '@/lib/api'
import { cn } from '@/lib/utils'

function Stat({
  label,
  value,
  to,
  accent,
}: {
  label: string
  value: string | number
  to?: string
  accent?: 'danger' | 'brass'
}) {
  const body = (
    <Card
      className={cn(
        'h-full transition-all duration-200',
        to && 'group-hover:-translate-y-0.5 group-hover:shadow-pop',
      )}
    >
      <CardContent className="p-5">
        <p
          className={cn(
            'font-display text-3xl font-semibold',
            accent === 'danger' && 'text-danger',
            accent === 'brass' && 'text-accent',
          )}
        >
          {value}
        </p>
        <p className="mt-1 text-xs uppercase tracking-wider text-muted">{label}</p>
      </CardContent>
    </Card>
  )

  return to ? (
    <Link to={to} className="group rounded-2xl focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring">
      {body}
    </Link>
  ) : (
    body
  )
}

function timeAgo(value: string) {
  const seconds = Math.max(1, Math.floor((Date.now() - new Date(value).getTime()) / 1000))
  if (seconds < 60) return `${seconds}s ago`
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  return `${Math.floor(hours / 24)}d ago`
}

export function DashboardPage() {
  const dashboard = useQuery({ queryKey: ['dashboard'], queryFn: getDashboard })

  return (
    <div className="flex flex-col gap-8">
      <header className="animate-fade">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">
          Demo Library
        </p>
        <h1 className="font-display mt-2 text-3xl font-semibold sm:text-4xl">Good day.</h1>
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
          Your library at a glance.
        </p>
      </header>

      {dashboard.isPending ? (
        <div className="grid min-h-40 place-items-center text-muted">
          <Spinner className="size-6" />
        </div>
      ) : dashboard.data ? (
        <>
          <div className="grid animate-rise grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            <Stat label="Titles" value={dashboard.data.totalBooks} to="/catalog" />
            <Stat label="Copies" value={dashboard.data.totalCopies} to="/catalog" />
            <Stat label="Available" value={dashboard.data.copiesAvailable} />
            <Stat label="On loan" value={dashboard.data.copiesOnLoan} to="/circulation" accent="brass" />
            <Stat
              label="Overdue"
              value={dashboard.data.overdueLoans}
              to="/circulation"
              accent={dashboard.data.overdueLoans > 0 ? 'danger' : undefined}
            />
            <Stat label="Active patrons" value={dashboard.data.activeMembers} to="/patrons" />
            <Stat
              label="Fines outstanding"
              value={dashboard.data.outstandingFines.toFixed(2)}
              accent={dashboard.data.outstandingFines > 0 ? 'danger' : undefined}
            />
            <Stat
              label="In transit / holds ready"
              value={`${dashboard.data.pendingTransfers} / ${dashboard.data.readyHolds}`}
              to="/circulation"
            />
          </div>

          <div className="grid animate-rise gap-6 lg:grid-cols-[1fr_20rem]">
            <Card>
              <CardHeader>
                <CardTitle>Recent activity</CardTitle>
              </CardHeader>
              <CardContent>
                {dashboard.data.recentActivity.length === 0 ? (
                  <p className="text-sm text-muted">No circulation yet today.</p>
                ) : (
                  <ul className="flex flex-col divide-y divide-border">
                    {dashboard.data.recentActivity.map((item, i) => (
                      <li key={`${item.memberId}-${item.atUtc}-${i}`} className="flex items-center gap-3 py-2.5 first:pt-0 last:pb-0">
                        <span
                          className={cn(
                            'grid size-7 shrink-0 place-items-center rounded-full',
                            item.kind === 'Borrowed'
                              ? 'bg-accent-soft text-accent'
                              : 'bg-success-soft text-success',
                          )}
                        >
                          {item.kind === 'Borrowed' ? (
                            <ArrowUpFromLine className="size-3.5" />
                          ) : (
                            <ArrowDownToLine className="size-3.5" />
                          )}
                        </span>
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm">
                            <Link to={`/patrons/${item.memberId}`} className="font-medium hover:text-accent">
                              {item.memberName}
                            </Link>{' '}
                            <span className="text-muted">{item.kind === 'Borrowed' ? 'borrowed' : 'returned'}</span>{' '}
                            {item.bookId ? (
                              <Link to={`/catalog/books/${item.bookId}`} className="font-medium hover:text-accent">
                                {item.bookTitle}
                              </Link>
                            ) : (
                              item.bookTitle
                            )}
                          </p>
                        </div>
                        <span className="shrink-0 text-xs text-faint">{timeAgo(item.atUtc)}</span>
                      </li>
                    ))}
                  </ul>
                )}
              </CardContent>
            </Card>

            <div className="flex flex-col gap-4">
              {dashboard.data.lowStock.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2 text-sm">
                      <PackageOpen className="size-4 text-danger" />
                      Running low
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ul className="flex flex-col gap-2 text-sm">
                      {dashboard.data.lowStock.map((item) => (
                        <li key={item.bookId} className="flex items-center justify-between gap-2">
                          <Link
                            to={`/catalog/books/${item.bookId}`}
                            className="min-w-0 truncate font-medium text-ink hover:text-accent"
                          >
                            {item.title}
                          </Link>
                          <span
                            className={cn(
                              'shrink-0 text-xs',
                              item.available === 0 ? 'font-semibold text-danger' : 'text-muted',
                            )}
                          >
                            {item.available} of {item.total} left
                          </span>
                        </li>
                      ))}
                    </ul>
                  </CardContent>
                </Card>
              )}
              {[
                { to: '/catalog/add', icon: BookPlus, label: 'Add a book', sub: 'ISBN lookup or manual entry' },
                { to: '/circulation', icon: AlarmClock, label: 'The desk', sub: 'Check out, return, renew, transfer' },
                { to: '/patrons/register', icon: Users, label: 'Register member', sub: 'Issue a membership card' },
              ].map((action) => (
                <Link
                  key={action.to}
                  to={action.to}
                  className="group rounded-2xl focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
                >
                  <Card className="transition-all duration-200 group-hover:-translate-y-0.5 group-hover:shadow-pop">
                    <CardContent className="flex items-center gap-3 p-4">
                      <div className="grid size-9 shrink-0 place-items-center rounded-lg bg-accent-soft text-accent">
                        <action.icon className="size-4" />
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium">{action.label}</p>
                        <p className="truncate text-xs text-muted">{action.sub}</p>
                      </div>
                      <ArrowRight className="size-4 shrink-0 text-faint transition-transform duration-200 group-hover:translate-x-0.5" />
                    </CardContent>
                  </Card>
                </Link>
              ))}
            </div>
          </div>
        </>
      ) : (
        <p className="text-sm text-muted">Could not load the dashboard.</p>
      )}
    </div>
  )
}
