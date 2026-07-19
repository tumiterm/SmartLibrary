import { useMutation } from '@tanstack/react-query'
import { ArrowRight, BookOpen, CheckCircle2, PenLine, ScanBarcode, Search, Sparkles } from 'lucide-react'
import { useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input, Select, Textarea } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import { addBook, ApiError, lookupBookByIsbn, updateBook } from '@/lib/api'
import {
  BOOK_FORMATS,
  type BookFields,
  type BookFormat,
  type BookLookupDetails,
  type BookLookupResult,
} from '@/lib/catalog'

/* ── Book form (edits the cached external snapshot, or blank manual entry) ── */

interface BookFormProps {
  initial: BookLookupDetails | null
  isbn: string
  /** 'update' = record already cached (external hit); 'create' = manual entry. */
  mode: 'update' | 'create'
  bookId: string | null
  onDone: (bookId: string) => void
}

function BookForm({ initial, isbn, mode, bookId, onDone }: BookFormProps) {
  const [title, setTitle] = useState(initial?.title ?? '')
  const [subtitle, setSubtitle] = useState(initial?.subtitle ?? '')
  const [authors, setAuthors] = useState(initial?.authors.join(', ') ?? '')
  const [publisher, setPublisher] = useState(initial?.publisher ?? '')
  const [publishedDate, setPublishedDate] = useState(initial?.publishedDate ?? '')
  const [pageCount, setPageCount] = useState(initial?.pageCount?.toString() ?? '')
  const [language, setLanguage] = useState(initial?.language ?? '')
  const [categories, setCategories] = useState(initial?.categories.join(', ') ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [classification, setClassification] = useState(initial?.classificationNumber ?? '')
  const [format, setFormat] = useState<BookFormat>('Print')
  const [referenceOnly, setReferenceOnly] = useState(false)

  const splitList = (value: string) =>
    value
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean)

  const buildFields = (): BookFields => ({
    title: title.trim(),
    subtitle: subtitle.trim() || null,
    authors: splitList(authors),
    publisher: publisher.trim() || null,
    publishedDate: publishedDate.trim() || null,
    description: description.trim() || null,
    pageCount: pageCount ? Number(pageCount) : null,
    language: language.trim() || null,
    categories: splitList(categories),
    coverImageUrl: initial?.coverImageUrl ?? null,
    classificationNumber: classification.trim() || null,
    format,
    isReferenceOnly: referenceOnly,
  })

  const mutation = useMutation({
    mutationFn: async () => {
      if (mode === 'update' && bookId) {
        await updateBook(bookId, buildFields())
        return bookId
      }
      const created = await addBook({ ...buildFields(), isbn: isbn || null, metadataSource: 'Manual' })
      return created.id
    },
    onSuccess: (id) => {
      toast.success(mode === 'update' ? 'Saved to your catalog' : 'Added to your catalog', {
        description: title,
      })
      onDone(id)
    },
    onError: (error: Error) => {
      toast.error(
        error instanceof ApiError && error.status === 409 ? 'Already in catalog' : 'Could not save book',
        { description: error.message },
      )
    },
  })

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    mutation.mutate()
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-5">
      <div className="flex gap-6">
        {initial?.coverImageUrl && (
          <img
            src={initial.coverImageUrl}
            alt=""
            className="hidden h-40 w-28 shrink-0 rounded-lg border border-border object-cover shadow-card sm:block"
          />
        )}
        <div className="grid min-w-0 flex-1 gap-4 sm:grid-cols-2">
          <div className="sm:col-span-2">
            <Label htmlFor="title">Title</Label>
            <Input id="title" required value={title} onChange={(e) => setTitle(e.target.value)} />
          </div>
          <div className="sm:col-span-2">
            <Label htmlFor="subtitle">Subtitle</Label>
            <Input id="subtitle" value={subtitle} onChange={(e) => setSubtitle(e.target.value)} />
          </div>
          <div>
            <Label htmlFor="authors">Authors</Label>
            <Input
              id="authors"
              placeholder="Comma-separated"
              value={authors}
              onChange={(e) => setAuthors(e.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="publisher">Publisher</Label>
            <Input id="publisher" value={publisher} onChange={(e) => setPublisher(e.target.value)} />
          </div>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-4">
        <div>
          <Label htmlFor="publishedDate">Published</Label>
          <Input
            id="publishedDate"
            placeholder="2008-08-01"
            value={publishedDate}
            onChange={(e) => setPublishedDate(e.target.value)}
          />
        </div>
        <div>
          <Label htmlFor="pageCount">Pages</Label>
          <Input
            id="pageCount"
            type="number"
            min={1}
            value={pageCount}
            onChange={(e) => setPageCount(e.target.value)}
          />
        </div>
        <div>
          <Label htmlFor="language">Language</Label>
          <Input id="language" placeholder="en" value={language} onChange={(e) => setLanguage(e.target.value)} />
        </div>
        <div>
          <Label htmlFor="format">Format</Label>
          <Select id="format" value={format} onChange={(e) => setFormat(e.target.value as BookFormat)}>
            {BOOK_FORMATS.map((f) => (
              <option key={f.value} value={f.value}>
                {f.label}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <Label htmlFor="classification">Classification no.</Label>
          <Input
            id="classification"
            placeholder="e.g. 005.13 BLO (Dewey)"
            value={classification}
            onChange={(e) => setClassification(e.target.value)}
          />
        </div>
        <div>
          <Label htmlFor="categories">Categories</Label>
          <Input
            id="categories"
            placeholder="Comma-separated"
            value={categories}
            onChange={(e) => setCategories(e.target.value)}
          />
        </div>
      </div>

      <div>
        <Label htmlFor="description">Description</Label>
        <Textarea id="description" value={description} onChange={(e) => setDescription(e.target.value)} />
      </div>

      <label className="flex cursor-pointer items-center gap-2.5 text-sm text-muted">
        <input
          type="checkbox"
          className="size-4 accent-(--accent)"
          checked={referenceOnly}
          onChange={(e) => setReferenceOnly(e.target.checked)}
        />
        Reference only — this title never leaves the library
      </label>

      <div className="flex items-center justify-end gap-3 border-t border-border pt-5">
        <Button type="submit" size="lg" disabled={mutation.isPending || !title.trim()}>
          {mutation.isPending ? <Spinner /> : <BookOpen className="size-4" />}
          {mode === 'update' ? 'Save & view details' : 'Add to catalog'}
        </Button>
      </div>
    </form>
  )
}

/* ── Lookup result panel ──────────────────────────────────────────────────── */

function ResultPanel({
  result,
  isbn,
  onDone,
}: {
  result: BookLookupResult
  isbn: string
  onDone: (bookId: string) => void
}) {
  if (result.outcome === 'FoundInLibrary' && result.book) {
    return (
      <Card className="animate-rise border-success/30">
        <CardContent className="flex items-start gap-4">
          <div className="grid size-10 shrink-0 place-items-center rounded-full bg-success-soft text-success">
            <CheckCircle2 className="size-5" />
          </div>
          <div className="min-w-0 flex-1">
            <p className="font-medium">Already in your catalog</p>
            <p className="mt-0.5 truncate text-sm text-muted">
              {result.book.title}
              {result.book.authors.length > 0 && ` — ${result.book.authors.join(', ')}`}
            </p>
            <div className="mt-2 flex flex-wrap items-center gap-2">
              <Badge>ISBN {result.book.isbn13}</Badge>
              <Badge variant={result.copiesAvailable > 0 ? 'success' : 'danger'}>
                {result.copiesAvailable} of {result.copiesTotal} available
              </Badge>
            </div>
          </div>
          {result.bookId && (
            <Button variant="secondary" onClick={() => onDone(result.bookId!)}>
              View details
              <ArrowRight className="size-4" />
            </Button>
          )}
        </CardContent>
      </Card>
    )
  }

  if (result.outcome === 'FoundExternally' && result.book) {
    return (
      <Card className="animate-rise">
        <CardContent className="flex flex-col gap-5">
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="brass">
              <Sparkles className="size-3" />
              Found on Google Books — snapshot saved
            </Badge>
            <span className="text-sm text-muted">
              Review, add your library fields, then save. It won't hit Google again.
            </span>
          </div>
          <BookForm initial={result.book} isbn={isbn} mode="update" bookId={result.bookId} onDone={onDone} />
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="animate-rise">
      <CardContent className="flex flex-col gap-5">
        <div className="flex flex-wrap items-center gap-2">
          <Badge>
            <PenLine className="size-3" />
            Manual entry
          </Badge>
          <span className="text-sm text-muted">
            Nothing found locally or on Google Books — enter the details yourself.
          </span>
        </div>
        <BookForm initial={null} isbn={isbn} mode="create" bookId={null} onDone={onDone} />
      </CardContent>
    </Card>
  )
}

/* ── Page ─────────────────────────────────────────────────────────────────── */

export function AddBookPage() {
  const [isbn, setIsbn] = useState('')
  const [submittedIsbn, setSubmittedIsbn] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)
  const navigate = useNavigate()

  const lookup = useMutation({
    mutationFn: (value: string) => lookupBookByIsbn(value),
    onError: (error: Error) => {
      toast.error('Lookup failed', { description: error.message })
    },
  })

  const runLookup = (event: React.FormEvent) => {
    event.preventDefault()
    const value = isbn.trim()
    if (!value) return
    setSubmittedIsbn(value)
    lookup.mutate(value)
  }

  const goToDetails = (bookId: string) => {
    navigate(`/catalog/books/${bookId}`)
  }

  return (
    <div className="flex flex-col gap-8">
      <header className="animate-fade">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Catalog</p>
        <h1 className="font-display mt-2 text-3xl font-semibold sm:text-4xl">Add a book</h1>
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
          Scan or type an ISBN. We check your catalog first, then Google Books — and fall back
          to manual entry when neither knows it.
        </p>
      </header>

      <Card className="animate-rise">
        <CardContent>
          <form onSubmit={runLookup} className="flex flex-col gap-3 sm:flex-row sm:items-end">
            <div className="flex-1">
              <Label htmlFor="isbn">ISBN</Label>
              <div className="relative">
                <ScanBarcode className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
                <Input
                  id="isbn"
                  ref={inputRef}
                  autoFocus
                  inputMode="numeric"
                  placeholder="978-0-13-235088-4"
                  className="h-11 pl-10 font-mono text-[15px] tracking-wide"
                  value={isbn}
                  onChange={(e) => setIsbn(e.target.value)}
                  onKeyDown={(e) => {
                    // Barcode scanners terminate with Enter — submit explicitly.
                    if (e.key === 'Enter') {
                      e.preventDefault()
                      e.currentTarget.form?.requestSubmit()
                    }
                  }}
                />
              </div>
            </div>
            <Button type="submit" size="lg" disabled={lookup.isPending || !isbn.trim()}>
              {lookup.isPending ? <Spinner /> : <Search className="size-4" />}
              Look up
            </Button>
          </form>
        </CardContent>
      </Card>

      {lookup.data && (
        <ResultPanel key={submittedIsbn} result={lookup.data} isbn={submittedIsbn} onDone={goToDetails} />
      )}
    </div>
  )
}
