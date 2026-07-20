import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeftRight,
  BarChart3,
  BookMarked,
  ClipboardList,
  IdCard,
  LayoutDashboard,
  Library,
  Menu,
  Moon,
  ScanBarcode,
  Search,
  Settings,
  Sun,
  Users,
  X,
} from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useTheme } from '@/components/theme-provider'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { globalSearch } from '@/lib/api'
import { cn } from '@/lib/utils'

interface NavItem {
  label: string
  to: string
  icon: React.ComponentType<{ className?: string }>
  soon?: boolean
}

const NAV: NavItem[] = [
  { label: 'Dashboard', to: '/', icon: LayoutDashboard },
  { label: 'Catalog', to: '/catalog', icon: Library },
  { label: 'Circulation', to: '/circulation', icon: ArrowLeftRight },
  { label: 'Stocktake', to: '/stocktake', icon: ClipboardList },
  { label: 'Patrons', to: '/patrons', icon: Users },
  { label: 'Reports', to: '/reports', icon: BarChart3 },
  { label: 'Settings', to: '/settings', icon: Settings },
]

function GlobalSearch() {
  const [term, setTerm] = useState('')
  const [debounced, setDebounced] = useState('')
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()

  useEffect(() => {
    const t = setTimeout(() => setDebounced(term.trim()), 250)
    return () => clearTimeout(t)
  }, [term])

  useEffect(() => {
    const onClick = (e: MouseEvent) => {
      if (!containerRef.current?.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onClick)
    return () => document.removeEventListener('mousedown', onClick)
  }, [])

  const results = useQuery({
    queryKey: ['global-search', debounced],
    queryFn: () => globalSearch(debounced),
    enabled: debounced.length >= 2,
  })

  const go = (to: string) => {
    setOpen(false)
    setTerm('')
    navigate(to)
  }

  const hasResults =
    results.data &&
    (results.data.books.length > 0 || results.data.copies.length > 0 || results.data.members.length > 0)

  return (
    <div ref={containerRef} className="relative hidden w-72 md:block">
      <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-faint" />
      <Input
        placeholder="Search everything…"
        className="h-9 pl-9 text-[13px]"
        value={term}
        onChange={(e) => {
          setTerm(e.target.value)
          setOpen(true)
        }}
        onFocus={() => setOpen(true)}
      />
      {open && debounced.length >= 2 && (
        <div className="absolute left-0 right-0 top-11 z-50 max-h-96 overflow-y-auto rounded-xl border border-border bg-surface p-2 shadow-pop animate-fade">
          {!hasResults ? (
            <p className="px-3 py-4 text-sm text-muted">
              {results.isFetching ? 'Searching…' : 'Nothing found.'}
            </p>
          ) : (
            <>
              {results.data!.books.length > 0 && (
                <SearchGroup label="Books">
                  {results.data!.books.map((b) => (
                    <SearchHit key={b.id} onClick={() => go(`/catalog/books/${b.id}`)}>
                      <BookMarked className="size-3.5 shrink-0 text-faint" />
                      <span className="truncate font-medium">{b.title}</span>
                      <span className="ml-auto shrink-0 text-xs text-faint">{b.authors[0] ?? b.isbn13 ?? ''}</span>
                    </SearchHit>
                  ))}
                </SearchGroup>
              )}
              {results.data!.copies.length > 0 && (
                <SearchGroup label="Copies">
                  {results.data!.copies.map((c) => (
                    <SearchHit key={c.barcode} onClick={() => go(`/catalog/books/${c.bookId}`)}>
                      <ScanBarcode className="size-3.5 shrink-0 text-faint" />
                      <span className="font-mono text-[13px]">{c.barcode}</span>
                      <span className="truncate text-muted">{c.bookTitle}</span>
                      <span className="ml-auto shrink-0 text-xs text-faint">{c.status}</span>
                    </SearchHit>
                  ))}
                </SearchGroup>
              )}
              {results.data!.members.length > 0 && (
                <SearchGroup label="Members">
                  {results.data!.members.map((m) => (
                    <SearchHit key={m.id} onClick={() => go(`/patrons/${m.id}`)}>
                      <IdCard className="size-3.5 shrink-0 text-faint" />
                      <span className="truncate font-medium">{m.fullName}</span>
                      <span className="ml-auto shrink-0 font-mono text-xs text-faint">{m.membershipNumber}</span>
                    </SearchHit>
                  ))}
                </SearchGroup>
              )}
            </>
          )}
        </div>
      )}
    </div>
  )
}

function SearchGroup({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="mb-1 last:mb-0">
      <p className="px-3 pb-1 pt-2 text-[10px] font-semibold uppercase tracking-[0.16em] text-faint">{label}</p>
      {children}
    </div>
  )
}

function SearchHit({ onClick, children }: { onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      className="flex w-full cursor-pointer items-center gap-2 rounded-lg px-3 py-2 text-left text-sm transition-colors hover:bg-surface-2"
      onClick={onClick}
    >
      {children}
    </button>
  )
}

function Brand() {
  return (
    <div className="flex items-center gap-2.5 px-2">
      <div className="grid size-8 place-items-center rounded-lg bg-primary text-primary-foreground shadow-card">
        <BookMarked className="size-4" />
      </div>
      <span className="font-display text-[17px] font-semibold tracking-tight">
        SmartLibrary
      </span>
    </div>
  )
}

function SidebarNav({ onNavigate }: { onNavigate?: () => void }) {
  return (
    <nav className="mt-8 flex flex-col gap-1" aria-label="Main">
      {NAV.map((item) =>
        item.soon ? (
          <span
            key={item.label}
            aria-disabled
            className="flex cursor-not-allowed items-center gap-3 rounded-lg px-3 py-2 text-sm text-faint"
          >
            <item.icon className="size-4" />
            {item.label}
            <span className="ml-auto rounded-full border border-border px-1.5 py-px text-[10px] uppercase tracking-wider text-faint">
              soon
            </span>
          </span>
        ) : (
          <NavLink
            key={item.label}
            to={item.to}
            end={item.to === '/'}
            onClick={onNavigate}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors duration-150',
                isActive
                  ? 'border border-border bg-surface font-medium text-ink shadow-card'
                  : 'border border-transparent text-muted hover:bg-surface-2 hover:text-ink',
              )
            }
          >
            <item.icon className="size-4" />
            {item.label}
          </NavLink>
        ),
      )}
    </nav>
  )
}

export function AppShell() {
  const { theme, toggle } = useTheme()
  const [mobileOpen, setMobileOpen] = useState(false)
  const location = useLocation()
  const section = NAV.find(
    (n) => n.to !== '/' && location.pathname.startsWith(n.to.split('/').slice(0, 2).join('/')),
  )

  return (
    <div className="flex min-h-dvh">
      {/* Desktop sidebar */}
      <aside className="sticky top-0 hidden h-dvh w-60 shrink-0 flex-col border-r border-border p-4 lg:flex">
        <Brand />
        <SidebarNav />
        <div className="mt-auto rounded-xl border border-border bg-surface-2 p-3 text-xs leading-relaxed text-muted">
          <span className="font-medium text-ink">Demo Library</span>
          <br />
          Multi-tenant dev sandbox
        </div>
      </aside>

      {/* Mobile drawer */}
      {mobileOpen && (
        <div className="fixed inset-0 z-50 lg:hidden" role="dialog" aria-modal>
          <button
            aria-label="Close menu"
            className="absolute inset-0 bg-ink/40 backdrop-blur-sm animate-fade"
            onClick={() => setMobileOpen(false)}
          />
          <div className="absolute inset-y-0 left-0 flex w-72 flex-col border-r border-border bg-background p-4 shadow-pop animate-rise">
            <div className="flex items-center justify-between">
              <Brand />
              <Button variant="ghost" size="icon" aria-label="Close" onClick={() => setMobileOpen(false)}>
                <X className="size-4" />
              </Button>
            </div>
            <SidebarNav onNavigate={() => setMobileOpen(false)} />
          </div>
        </div>
      )}

      {/* Main column */}
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-40 flex h-14 items-center gap-3 border-b border-border bg-background/80 px-4 backdrop-blur-md sm:px-6">
          <Button
            variant="ghost"
            size="icon"
            className="lg:hidden"
            aria-label="Open menu"
            onClick={() => setMobileOpen(true)}
          >
            <Menu className="size-4" />
          </Button>
          <span className="text-sm text-muted">{section?.label ?? 'Dashboard'}</span>
          <div className="ml-auto flex items-center gap-2.5">
            <GlobalSearch />
            <Badge variant="brass">Demo Library</Badge>
            <Button
              variant="ghost"
              size="icon"
              aria-label={theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
              onClick={toggle}
            >
              {theme === 'dark' ? <Sun className="size-4" /> : <Moon className="size-4" />}
            </Button>
            <div
              aria-label="Signed-in user"
              className="grid size-8 place-items-center rounded-full border border-border bg-surface text-xs font-semibold text-muted"
            >
              IO
            </div>
          </div>
        </header>

        <main className="page-wash flex-1 px-4 py-8 sm:px-6 lg:px-10">
          <div className="mx-auto w-full max-w-4xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
