import { ArrowRight, BookPlus, Library, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Card, CardContent } from '@/components/ui/card'

export function DashboardPage() {
  return (
    <div className="flex flex-col gap-8">
      <header className="animate-fade">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">
          Demo Library
        </p>
        <h1 className="font-display mt-2 text-3xl font-semibold sm:text-4xl">Good day.</h1>
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
          Your library at a glance. Circulation, patrons and reporting arrive in upcoming
          slices — cataloging is live now.
        </p>
      </header>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Link to="/catalog/add" className="group animate-rise rounded-2xl focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring">
          <Card className="h-full transition-all duration-200 group-hover:-translate-y-0.5 group-hover:shadow-pop">
            <CardContent className="flex h-full flex-col gap-3">
              <div className="grid size-10 place-items-center rounded-lg bg-accent-soft text-accent">
                <BookPlus className="size-5" />
              </div>
              <p className="font-medium">Add a book</p>
              <p className="text-sm leading-relaxed text-muted">
                ISBN lookup with Google Books prefill, or manual entry.
              </p>
              <span className="mt-auto inline-flex items-center gap-1 text-sm font-medium text-accent">
                Start <ArrowRight className="size-3.5 transition-transform duration-200 group-hover:translate-x-0.5" />
              </span>
            </CardContent>
          </Card>
        </Link>

        <Card className="animate-rise opacity-60 [animation-delay:60ms]">
          <CardContent className="flex h-full flex-col gap-3">
            <div className="grid size-10 place-items-center rounded-lg bg-surface-2 text-muted">
              <Library className="size-5" />
            </div>
            <p className="font-medium">Browse catalog</p>
            <p className="text-sm leading-relaxed text-muted">
              Faceted search across your collection. Coming in the next slice.
            </p>
          </CardContent>
        </Card>

        <Card className="animate-rise opacity-60 [animation-delay:120ms]">
          <CardContent className="flex h-full flex-col gap-3">
            <div className="grid size-10 place-items-center rounded-lg bg-surface-2 text-muted">
              <Users className="size-5" />
            </div>
            <p className="font-medium">Patrons</p>
            <p className="text-sm leading-relaxed text-muted">
              Registration, loans and holds. Coming soon.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
