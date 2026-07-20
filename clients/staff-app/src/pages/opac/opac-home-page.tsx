import { useQuery } from '@tanstack/react-query'
import { BookMarked, Search } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import { opacSearch } from '@/lib/api'
import { BOOK_FORMATS, type BookFormat } from '@/lib/catalog'

export function OpacHomePage() {
  const [term, setTerm] = useState('')
  const [search, setSearch] = useState('')
  const [format, setFormat] = useState<BookFormat | ''>('')
  const [page, setPage] = useState(1)

  // Debounced as-you-type search.
  useEffect(() => {
    const t = setTimeout(() => {
      setSearch(term.trim())
      setPage(1)
    }, 300)
    return () => clearTimeout(t)
  }, [term])

  const books = useQuery({
    queryKey: ['opac-books', search, format, page],
    queryFn: () => opacSearch({ search, format, page }),
  })

  const totalPages = books.data ? Math.max(1, Math.ceil(books.data.totalCount / books.data.pageSize)) : 1

  return (
    <div className="flex flex-col gap-8">
      <section className="animate-fade py-6 text-center sm:py-10">
        <h1 className="font-display text-4xl font-semibold sm:text-5xl">
          Find your next book.
        </h1>
        <p className="mx-auto mt-3 max-w-md text-sm leading-relaxed text-muted">
          Search the whole collection — every branch, every format.
        </p>
        <div className="relative mx-auto mt-6 max-w-xl">
          <Search className="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-faint" />
          <Input
            autoFocus
            placeholder="Title, author, or ISBN…"
            className="h-13 rounded-full pl-12 pr-4 text-base shadow-card"
            value={term}
            onChange={(e) => setTerm(e.target.value)}
          />
        </div>
        <div className="mt-4 flex flex-wrap justify-center gap-1.5">
          <button
            type="button"
            className={`cursor-pointer rounded-full border px-3 py-1 text-xs font-medium transition-colors ${format === '' ? 'border-accent bg-accent-soft text-accent' : 'border-border text-muted hover:text-ink'}`}
            onClick={() => {
              setFormat('')
              setPage(1)
            }}
          >
            All
          </button>
          {BOOK_FORMATS.map((f) => (
            <button
              key={f.value}
              type="button"
              className={`cursor-pointer rounded-full border px-3 py-1 text-xs font-medium transition-colors ${format === f.value ? 'border-accent bg-accent-soft text-accent' : 'border-border text-muted hover:text-ink'}`}
              onClick={() => {
                setFormat(f.value)
                setPage(1)
              }}
            >
              {f.label}
            </button>
          ))}
        </div>
      </section>

      {books.isPending ? (
        <div className="grid min-h-40 place-items-center text-muted">
          <Spinner className="size-6" />
        </div>
      ) : !books.data || books.data.items.length === 0 ? (
        <p className="animate-fade text-center text-sm text-muted">
          Nothing matches — try another title or author.
        </p>
      ) : (
        <>
          <div className="grid animate-rise grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {books.data.items.map((book) => (
              <Link
                key={book.id}
                to={`/opac/books/${book.id}`}
                className="group flex flex-col gap-3 rounded-2xl border border-border bg-surface p-4 shadow-card transition-all duration-200 hover:-translate-y-0.5 hover:shadow-pop focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
              >
                {book.coverImageUrl ? (
                  <img
                    src={book.coverImageUrl}
                    alt=""
                    className="mx-auto h-40 w-28 rounded-lg border border-border object-cover shadow-card"
                  />
                ) : (
                  <div className="mx-auto grid h-40 w-28 place-items-center rounded-lg border border-border bg-surface-2 text-faint">
                    <BookMarked className="size-8" />
                  </div>
                )}
                <div className="min-w-0">
                  <p className="line-clamp-2 text-sm font-medium leading-snug text-ink group-hover:text-accent">
                    {book.title}
                  </p>
                  <p className="mt-0.5 truncate text-xs text-muted">{book.authors.join(', ') || '—'}</p>
                </div>
                <div className="mt-auto">
                  <Badge variant={book.copiesAvailable > 0 ? 'success' : 'danger'}>
                    {book.copiesAvailable > 0 ? 'Available' : 'On loan'}
                  </Badge>
                </div>
              </Link>
            ))}
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-center gap-3 text-sm text-muted">
              <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>
                Previous
              </Button>
              <span>
                {page} / {totalPages}
              </span>
              <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>
                Next
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
