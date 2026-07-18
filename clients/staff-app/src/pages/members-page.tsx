import { useQuery } from '@tanstack/react-query'
import { Search, UserPlus, Users } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import { searchMembers } from '@/lib/api'
import type { MemberStatus } from '@/lib/members'

const STATUS_VARIANT: Record<MemberStatus, 'success' | 'danger' | 'neutral'> = {
  Active: 'success',
  Suspended: 'danger',
  Expired: 'neutral',
}

export function MembersPage() {
  const [search, setSearch] = useState('')
  const navigate = useNavigate()

  const members = useQuery({
    queryKey: ['members', search],
    queryFn: () => searchMembers(search),
  })

  return (
    <div className="flex flex-col gap-8">
      <header className="flex animate-fade flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Patrons</p>
          <h1 className="font-display mt-2 text-3xl font-semibold sm:text-4xl">Members</h1>
          <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
            Everyone registered with your library — search by name, email or card number.
          </p>
        </div>
        <Button size="lg" onClick={() => navigate('/patrons/register')}>
          <UserPlus className="size-4" />
          Register member
        </Button>
      </header>

      <Card className="animate-rise">
        <CardContent className="flex flex-col gap-5">
          <div className="relative max-w-md">
            <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
            <Input
              placeholder="Search members…"
              className="pl-10"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>

          {members.isPending ? (
            <div className="grid min-h-32 place-items-center text-muted">
              <Spinner className="size-5" />
            </div>
          ) : !members.data || members.data.length === 0 ? (
            <div className="flex items-center gap-3 rounded-xl border border-dashed border-border-strong bg-surface-2 px-4 py-8 text-sm text-muted">
              <Users className="size-4 shrink-0" />
              {search ? 'No members match that search.' : 'No members yet — register the first one.'}
            </div>
          ) : (
            <div className="overflow-x-auto rounded-xl border border-border">
              <table className="w-full min-w-130 text-sm">
                <thead>
                  <tr className="border-b border-border bg-surface-2 text-left text-xs uppercase tracking-wider text-muted">
                    <th className="px-4 py-2.5 font-medium">Member</th>
                    <th className="px-4 py-2.5 font-medium">Card no.</th>
                    <th className="px-4 py-2.5 font-medium">Type</th>
                    <th className="px-4 py-2.5 font-medium">Branch</th>
                    <th className="px-4 py-2.5 font-medium">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {members.data.map((m) => (
                    <tr
                      key={m.id}
                      className="cursor-pointer border-b border-border transition-colors last:border-0 hover:bg-surface-2"
                      onClick={() => navigate(`/patrons/${m.id}`)}
                    >
                      <td className="px-4 py-3">
                        <Link
                          to={`/patrons/${m.id}`}
                          className="font-medium text-ink"
                          onClick={(e) => e.stopPropagation()}
                        >
                          {m.fullName}
                        </Link>
                        <p className="text-xs text-muted">{m.email}</p>
                      </td>
                      <td className="px-4 py-3 font-mono text-[13px]">{m.membershipNumber}</td>
                      <td className="px-4 py-3">{m.type}</td>
                      <td className="px-4 py-3">
                        {m.homeBranchName ?? <span className="text-faint">Library-wide</span>}
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant={STATUS_VARIANT[m.status]}>{m.status}</Badge>
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
