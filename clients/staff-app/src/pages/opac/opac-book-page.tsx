import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, BookMarked, Building2, Hourglass, Landmark } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Spinner } from '@/components/ui/spinner'
import { opacBook } from '@/lib/api'
import { BOOK_FORMATS } from '@/lib/catalog'

export function OpacBookPage() {
  const { id } = useParams<{ id: string }>()
  const book = useQuery({
    queryKey: ['opac-book', id],
    queryFn: () => opacBook(id!),
    enabled: !!id,
  })

  if (book.isPending) {
    return (
      <div className="grid min-h-64 place-items-center text-muted">
        <Spinner className="size-6" />
      </div>
    )
  }

  if (book.isError || !book.data) {
    return (
      <div className="text-center text-sm text-muted">
        <p>We couldn't find that title.</p>
        <Link to="/opac" className="mt-2 inline-flex items-center gap-1.5 text-accent hover:underline">
          <ArrowLeft className="size-4" /> Back to search
        </Link>
      </div>
    )
  }

  const b = book.data
  const formatLabel = BOOK_FORMATS.find((f) => f.value === b.format)?.label ?? b.format

  return (
    <div className="flex flex-col gap-8">
      <div className="animate-fade">
        <Link
          to="/opac"
          className="inline-flex items-center gap-1.5 text-sm text-muted transition-colors hover:text-ink"
        >
          <ArrowLeft className="size-4" /> Back to search
        </Link>
      </div>

      <div className="grid animate-rise gap-8 sm:grid-cols-[11rem_1fr]">
        {b.coverImageUrl ? (
          <img
            src={b.coverImageUrl}
            alt={`Cover of ${b.title}`}
            className="h-60 w-44 self-start rounded-xl border border-border object-cover shadow-pop"
          />
        ) : (
          <div className="grid h-60 w-44 place-items-center self-start rounded-xl border border-border bg-surface-2 text-faint shadow-card">
            <BookMarked className="size-10" />
          </div>
        )}

        <div className="min-w-0">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">{formatLabel}</p>
          <h1 className="font-display mt-2 text-3xl font-semibold leading-tight sm:text-4xl">{b.title}</h1>
          {b.subtitle && <p className="mt-1 text-lg text-muted">{b.subtitle}</p>}
          {b.authors.length > 0 && <p className="mt-2 text-sm font-medium">{b.authors.join(', ')}</p>}

          <div className="mt-4 flex flex-wrap gap-2">
            {b.isbn13 && <Badge>ISBN {b.isbn13}</Badge>}
            {b.publisher && <Badge>{b.publisher}</Badge>}
            {b.publishedDate && <Badge>{b.publishedDate}</Badge>}
            {b.pageCount && <Badge>{b.pageCount} pages</Badge>}
            {b.isReferenceOnly && <Badge variant="danger">Reference only — in-library use</Badge>}
          </div>

          {b.description && (
            <p className="mt-4 max-w-2xl text-sm leading-relaxed text-muted">{b.description}</p>
          )}
        </div>
      </div>

      <Card className="animate-rise">
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-wrap items-center gap-3">
            <Landmark className="size-4 text-accent" />
            <p className="font-medium">
              {b.copiesAvailable > 0
                ? `Available now — ${b.copiesAvailable} of ${b.copiesTotal} ${b.copiesTotal === 1 ? 'copy' : 'copies'}`
                : b.copiesTotal > 0
                  ? 'All copies are out right now'
                  : 'Not yet in circulation'}
            </p>
            {b.waitlistCount > 0 && (
              <Badge variant="brass">
                <Hourglass className="size-3" />
                {b.waitlistCount} waiting
              </Badge>
            )}
          </div>

          {b.availability.length > 0 && (
            <ul className="flex flex-col gap-2">
              {b.availability.map((a) => (
                <li
                  key={a.branchName}
                  className="flex items-center justify-between gap-3 rounded-xl border border-border bg-surface-2 px-4 py-2.5 text-sm"
                >
                  <span className="flex items-center gap-2.5">
                    <Building2 className="size-4 text-faint" />
                    {a.branchName}
                  </span>
                  <Badge variant={a.available > 0 ? 'success' : 'neutral'}>
                    {a.available > 0 ? `${a.available} on the shelf` : 'None available'}
                  </Badge>
                </li>
              ))}
            </ul>
          )}

          <p className="text-xs text-faint">
            {b.isReferenceOnly
              ? 'This title is for in-library use — visit any branch to read it.'
              : b.copiesAvailable > 0
                ? 'Bring your membership card to the branch desk to borrow it.'
                : 'Ask at any branch desk to join the waitlist — we’ll set a copy aside when it returns.'}
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
