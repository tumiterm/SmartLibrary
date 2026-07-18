import {
  ArrowLeftRight,
  BarChart3,
  BookMarked,
  LayoutDashboard,
  Library,
  Menu,
  Moon,
  Settings,
  Sun,
  Users,
  X,
} from 'lucide-react'
import { useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useTheme } from '@/components/theme-provider'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
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
  { label: 'Patrons', to: '/patrons', icon: Users },
  { label: 'Reports', to: '/reports', icon: BarChart3, soon: true },
  { label: 'Settings', to: '/settings', icon: Settings, soon: true },
]

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
