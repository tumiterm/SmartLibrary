import { BookMarked, Moon, Sun } from 'lucide-react'
import { Link, Outlet } from 'react-router-dom'
import { useTheme } from '@/components/theme-provider'
import { Button } from '@/components/ui/button'

/** Minimal patron-facing chrome — no staff navigation, search is the hero. */
export function OpacShell() {
  const { theme, toggle } = useTheme()

  return (
    <div className="flex min-h-dvh flex-col">
      <header className="sticky top-0 z-40 border-b border-border bg-background/80 backdrop-blur-md">
        <div className="mx-auto flex h-14 w-full max-w-5xl items-center gap-3 px-4 sm:px-6">
          <Link to="/opac" className="flex items-center gap-2.5">
            <div className="grid size-8 place-items-center rounded-lg bg-primary text-primary-foreground shadow-card">
              <BookMarked className="size-4" />
            </div>
            <span className="font-display text-[17px] font-semibold tracking-tight">
              Demo Library
            </span>
          </Link>
          <span className="mt-0.5 hidden text-[11px] uppercase tracking-[0.18em] text-faint sm:block">
            Catalogue
          </span>
          <div className="ml-auto">
            <Button
              variant="ghost"
              size="icon"
              aria-label={theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
              onClick={toggle}
            >
              {theme === 'dark' ? <Sun className="size-4" /> : <Moon className="size-4" />}
            </Button>
          </div>
        </div>
      </header>

      <main className="page-wash flex-1 px-4 py-8 sm:px-6">
        <div className="mx-auto w-full max-w-5xl">
          <Outlet />
        </div>
      </main>

      <footer className="border-t border-border px-4 py-5 text-center text-xs text-faint">
        Ask at any branch desk to borrow, reserve, or renew · Powered by SmartLibrary
      </footer>
    </div>
  )
}
