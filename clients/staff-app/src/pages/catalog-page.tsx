import { useQuery } from '@tanstack/react-query'
import { BookMarked, BookPlus, Library, Search } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input, Select } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import { getBranches, searchBooks } from '@/lib/api'
import { BOOK_FORMATS, type BookFormat } from '@/lib/catalog'

export function CatalogPage() {
  const [search, setSearch] = useState('')
  const [format, setFormat] = useState<BookFormat | ''>('')
  const [branchId, setBranchId] = useState('')
  const [page, setPage] = useState(1)
  const navigate = useNavigate()

  const branches = useQuery({ queryKey: ['branches'], queryFn: getBranches })
  const books = useQuery({
    queryKey: ['books', search, format, branchId, page],
    queryFn: () => searchBooks({ search, format, branchId, page }),
  })

  const totalPages = books.data ? Math.max(1, Math.ceil(books.data.totalCount / books.data.pageSize)) : 1

  return (
    <div className="flex flex-col gap-8">
      <header className="flex animate-fade flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Catalog</p>
          <h1 className="font-display mt-2 text-3xl font-semibold sm:text-4xl">Your collection</h1>
          <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
            Search across titles, ISBNs and publishers.
          </p>
        </div>
        <Button size="lg" onClick={() => navigate('/catalog/add')}>
          <BookPlus className="size-4" />
          Add book
        </Button>
      </header>

      <Card className="animate-rise">
        <CardContent className="flex flex-col gap-5">
          <div className="flex flex-col gap-3 sm:flex-row">
            <div className="relative flex-1">
              <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
              <Input
                placeholder="Search the catalog…"
                className="pl-10"
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value)
                  setPage(1)
                }}
              />
            </div>
            <Select
              aria-label="Filter by format"
              className="sm:w-40"
              value={format}
              onChange={(e) => {
                setFormat(e.target.value as BookFormat | '')
                setPage(1)
              }}
            >
              <option value="">All formats</option>
              {BOOK_FORMATS.map((f) => (
                <option key={f.value} value={f.value}>
                  {f.label}
                </option>
              ))}
            </Select>
            <Select
              aria-label="Filter by branch"
              className="sm:w-44"
              value={branchId}
              onChange={(e) => {
                setBranchId(e.target.value)
                setPage(1)
              }}
            >
              <option value="">All branches</option>
              {branches.data?.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name}
                </option>
              ))}
            </Select>
          </div>

          {books.isPending ? (
            <div className="grid min-h-40 place-items-center text-muted">
              <Spinner className="size-5" />
            </div>
          ) : !books.data || books.data.items.length === 0 ? (
            <div className="flex items-center gap-3 rounded-xl border border-dashed border-border-strong bg-surface-2 px-4 py-8 text-sm text-muted">
              <Library className="size-4 shrink-0" />
              {search || format ? 'Nothing matches that search.' : 'The catalog is empty — add your first book.'}
            </div>
          ) : (
            <>
              <ul className="flex flex-col divide-y divide-border">
                {books.data.items.map((book) => (
                  <li key={book.id}>
                    <Link
                      to={`/catalog/books/${book.id}`}
                      className="flex items-center gap-4 rounded-lg px-2 py-3 transition-colors hover:bg-surface-2"
                    >
                      {book.coverImageUrl ? (
                        <img
                          src={book.coverImageUrl}
                          alt=""
                          className="h-16 w-11 shrink-0 rounded-md border border-border object-cover shadow-card"
                        />
                      ) : (
                        <div className="grid h-16 w-11 shrink-0 place-items-center rounded-md border border-border bg-surface-2 text-faint">
                          <BookMarked className="size-4" />
                        </div>
                      )}
                      <div className="min-w-0 flex-1">
                        <p className="truncate font-medium text-ink">{book.title}</p>
                        <p className="truncate text-sm text-muted">
                          {book.authors.join(', ') || '—'}
                          {book.isbn13 && <span className="text-faint"> · {book.isbn13}</span>}
                        </p>
                      </div>
                      <div className="flex shrink-0 items-center gap-2">
                        <Badge>{BOOK_FORMATS.find((f) => f.value === book.format)?.label ?? book.format}</Badge>
                        <Badge variant={book.copiesAvailable > 0 ? 'success' : book.copiesTotal > 0 ? 'danger' : 'neutral'}>
                          {book.copiesAvailable}/{book.copiesTotal} available
                        </Badge>
                      </div>
                    </Link>
                  </li>
                ))}
              </ul>

              {totalPages > 1 && (
                <div className="flex items-center justify-between border-t border-border pt-4 text-sm text-muted">
                  <span>
                    {books.data.totalCount} title{books.data.totalCount === 1 ? '' : 's'}
                  </span>
                  <div className="flex items-center gap-2">
                    <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>
                      Previous
                    </Button>
                    <span>
                      {page} / {totalPages}
                    </span>
                    <Button
                      variant="secondary"
                      size="sm"
                      disabled={page >= totalPages}
                      onClick={() => setPage(page + 1)}
                    >
                      Next
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
