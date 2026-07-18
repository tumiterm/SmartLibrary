import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  BookMarked,
  Building2,
  History,
  Hourglass,
  IdCard,
  Plus,
} from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input, Select } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import { addCopy, cancelHold, createBranch, getBook, getBranches, placeHold } from '@/lib/api'
import {
  BOOK_FORMATS,
  COPY_CONDITIONS,
  isPhysical,
  type BookDetails,
  type CopyCondition,
  type CopyStatus,
} from '@/lib/catalog'

const STATUS_VARIANT: Record<CopyStatus, 'success' | 'brass' | 'neutral' | 'danger'> = {
  Available: 'success',
  OnLoan: 'brass',
  OnHold: 'brass',
  InTransit: 'neutral',
  Lost: 'danger',
  Damaged: 'danger',
  Withdrawn: 'neutral',
}

function formatLabel(value: string) {
  return BOOK_FORMATS.find((f) => f.value === value)?.label ?? value
}

/* ── Add copy form ────────────────────────────────────────────────────────── */

function AddCopyForm({ book }: { book: BookDetails }) {
  const queryClient = useQueryClient()
  const physical = isPhysical(book.format)
  const [barcode, setBarcode] = useState('')
  const [shelfNumber, setShelfNumber] = useState('')
  const [branchId, setBranchId] = useState('')
  const [condition, setCondition] = useState<CopyCondition>('Good')
  const [price, setPrice] = useState('')
  const [newBranch, setNewBranch] = useState('')

  const branches = useQuery({ queryKey: ['branches'], queryFn: getBranches })

  const createBranchMutation = useMutation({
    mutationFn: () => createBranch(newBranch.trim()),
    onSuccess: async (created) => {
      toast.success('Branch created', { description: newBranch.trim() })
      setNewBranch('')
      await queryClient.invalidateQueries({ queryKey: ['branches'] })
      setBranchId(created.id)
    },
    onError: (error: Error) => toast.error('Could not create branch', { description: error.message }),
  })

  const addCopyMutation = useMutation({
    mutationFn: () =>
      addCopy(book.id, {
        barcode: barcode.trim(),
        shelfNumber: shelfNumber.trim() || null,
        callNumber: null,
        branchId: branchId || null,
        condition,
        price: price ? Number(price) : null,
        notes: null,
      }),
    onSuccess: async () => {
      toast.success('Copy added', { description: `Barcode ${barcode.trim()}` })
      setBarcode('')
      setShelfNumber('')
      setPrice('')
      await queryClient.invalidateQueries({ queryKey: ['book', book.id] })
    },
    onError: (error: Error) => toast.error('Could not add copy', { description: error.message }),
  })

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault()
        addCopyMutation.mutate()
      }}
      className="flex flex-col gap-4"
    >
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
        <div>
          <Label htmlFor="copy-barcode">Barcode</Label>
          <Input
            id="copy-barcode"
            required
            placeholder="Scan or type"
            className="font-mono"
            value={barcode}
            onChange={(e) => setBarcode(e.target.value)}
          />
        </div>
        {physical && (
          <>
            <div>
              <Label htmlFor="copy-branch">Branch</Label>
              <Select id="copy-branch" value={branchId} onChange={(e) => setBranchId(e.target.value)}>
                <option value="">— No branch —</option>
                {branches.data?.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
              </Select>
            </div>
            <div>
              <Label htmlFor="copy-shelf">Shelf no.</Label>
              <Input
                id="copy-shelf"
                placeholder="e.g. A-12"
                value={shelfNumber}
                onChange={(e) => setShelfNumber(e.target.value)}
              />
            </div>
          </>
        )}
        <div>
          <Label htmlFor="copy-condition">Condition</Label>
          <Select
            id="copy-condition"
            value={condition}
            onChange={(e) => setCondition(e.target.value as CopyCondition)}
          >
            {COPY_CONDITIONS.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <Label htmlFor="copy-price">Price</Label>
          <Input
            id="copy-price"
            type="number"
            min={0}
            step="0.01"
            placeholder="0.00"
            value={price}
            onChange={(e) => setPrice(e.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
        {physical ? (
          <div className="flex items-center gap-2">
            <Input
              aria-label="New branch name"
              placeholder="New branch name…"
              className="h-9 w-48 text-[13px]"
              value={newBranch}
              onChange={(e) => setNewBranch(e.target.value)}
            />
            <Button
              variant="ghost"
              size="sm"
              disabled={!newBranch.trim() || createBranchMutation.isPending}
              onClick={() => createBranchMutation.mutate()}
            >
              {createBranchMutation.isPending ? <Spinner /> : <Building2 className="size-4" />}
              Create branch
            </Button>
          </div>
        ) : (
          <span className="text-xs text-faint">
            Digital format — no branch or shelf required.
          </span>
        )}
        <Button type="submit" disabled={addCopyMutation.isPending || !barcode.trim()}>
          {addCopyMutation.isPending ? <Spinner /> : <Plus className="size-4" />}
          Add copy
        </Button>
      </div>
    </form>
  )
}

/* ── Waitlist ─────────────────────────────────────────────────────────────── */

function WaitlistCard({ book }: { book: BookDetails }) {
  const queryClient = useQueryClient()
  const [card, setCard] = useState('')

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['book', book.id] })

  const place = useMutation({
    mutationFn: () => placeHold(book.id, card.trim()),
    onSuccess: (hold) => {
      toast.success(`${hold.memberName} joined the waitlist`, {
        description: hold.queuePosition ? `Position ${hold.queuePosition} in the queue` : undefined,
      })
      setCard('')
      void refresh()
    },
    onError: (error: Error) => toast.error('Could not place hold', { description: error.message }),
  })

  const cancel = useMutation({
    mutationFn: (holdId: string) => cancelHold(holdId),
    onSuccess: () => {
      toast.success('Hold cancelled')
      void refresh()
    },
    onError: (error: Error) => toast.error('Could not cancel hold', { description: error.message }),
  })

  return (
    <Card className="animate-rise">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Hourglass className="size-4 text-accent" />
          Waitlist
          {book.holds.length > 0 && <Badge variant="brass">{book.holds.length} waiting</Badge>}
        </CardTitle>
        <CardDescription>
          Returned copies go to the queue before the shelf; renewals are blocked while anyone waits.
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {book.holds.length > 0 && (
          <ul className="flex flex-col divide-y divide-border">
            {book.holds.map((hold) => (
              <li key={hold.id} className="flex items-center gap-3 py-2.5 first:pt-0 last:pb-0">
                <span className="grid size-6 shrink-0 place-items-center rounded-full bg-surface-2 text-xs font-semibold text-muted">
                  {hold.position}
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">{hold.memberName}</p>
                  <p className="font-mono text-xs text-muted">{hold.membershipNumber}</p>
                </div>
                <Badge variant={hold.status === 'Ready' ? 'brass' : 'neutral'}>
                  {hold.status === 'Ready' ? 'Ready for pickup' : 'Waiting'}
                </Badge>
                <Button
                  size="sm"
                  variant="ghost"
                  disabled={cancel.isPending}
                  onClick={() => cancel.mutate(hold.id)}
                >
                  Cancel
                </Button>
              </li>
            ))}
          </ul>
        )}

        <form
          onSubmit={(e) => {
            e.preventDefault()
            place.mutate()
          }}
          className="flex flex-col gap-3 sm:flex-row sm:items-end"
        >
          <div className="flex-1 sm:max-w-xs">
            <Label htmlFor="hold-card">Membership no.</Label>
            <div className="relative">
              <IdCard className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
              <Input
                id="hold-card"
                placeholder="M-2026-000000"
                className="pl-10 font-mono"
                value={card}
                onChange={(e) => setCard(e.target.value)}
              />
            </div>
          </div>
          <Button type="submit" variant="secondary" disabled={place.isPending || !card.trim()}>
            {place.isPending ? <Spinner /> : <Hourglass className="size-4" />}
            Place hold
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}

/* ── Page ─────────────────────────────────────────────────────────────────── */

export function BookDetailsPage() {
  const { id } = useParams<{ id: string }>()
  const book = useQuery({
    queryKey: ['book', id],
    queryFn: () => getBook(id!),
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
      <Card>
        <CardContent className="flex flex-col items-start gap-3">
          <p className="font-medium">Book not found</p>
          <p className="text-sm text-muted">It may belong to a different library.</p>
          <Button variant="secondary" size="sm" onClick={() => window.history.back()}>
            <ArrowLeft className="size-4" /> Back
          </Button>
        </CardContent>
      </Card>
    )
  }

  const b = book.data

  return (
    <div className="flex flex-col gap-8">
      <div className="animate-fade">
        <Link
          to="/catalog/add"
          className="inline-flex items-center gap-1.5 text-sm text-muted transition-colors hover:text-ink"
        >
          <ArrowLeft className="size-4" /> Catalog
        </Link>
      </div>

      {/* ── Bibliographic header ── */}
      <header className="flex animate-rise flex-col gap-6 sm:flex-row">
        {b.coverImageUrl ? (
          <img
            src={b.coverImageUrl}
            alt={`Cover of ${b.title}`}
            className="h-56 w-40 shrink-0 self-start rounded-xl border border-border object-cover shadow-pop"
          />
        ) : (
          <div className="grid h-56 w-40 shrink-0 place-items-center self-start rounded-xl border border-border bg-surface-2 text-faint shadow-card">
            <BookMarked className="size-10" />
          </div>
        )}

        <div className="min-w-0 flex-1">
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">
            {formatLabel(b.format)}
            {b.classificationNumber && ` · ${b.classificationNumber}`}
          </p>
          <h1 className="font-display mt-2 text-3xl font-semibold leading-tight sm:text-4xl">
            {b.title}
          </h1>
          {b.subtitle && <p className="mt-1 text-lg text-muted">{b.subtitle}</p>}
          {b.authors.length > 0 && (
            <p className="mt-2 text-sm font-medium text-ink">{b.authors.join(', ')}</p>
          )}

          <div className="mt-4 flex flex-wrap gap-2">
            {b.isbn13 && <Badge>ISBN {b.isbn13}</Badge>}
            {b.publisher && <Badge>{b.publisher}</Badge>}
            {b.publishedDate && <Badge>{b.publishedDate}</Badge>}
            {b.pageCount && <Badge>{b.pageCount} pages</Badge>}
            {b.language && <Badge>{b.language.toUpperCase()}</Badge>}
            <Badge variant="brass">{b.metadataSource === 'GoogleBooks' ? 'Google Books' : 'Manual'}</Badge>
          </div>

          {b.description && (
            <p className="mt-4 line-clamp-4 max-w-2xl text-sm leading-relaxed text-muted">
              {b.description}
            </p>
          )}
        </div>

        <div className="shrink-0 self-start rounded-2xl border border-border bg-surface p-5 text-center shadow-card">
          <p className="font-display text-4xl font-semibold text-ink">
            {b.copiesAvailable}
            <span className="text-xl text-faint"> / {b.copiesTotal}</span>
          </p>
          <p className="mt-1 text-xs uppercase tracking-wider text-muted">copies available</p>
        </div>
      </header>

      {/* ── Copies ── */}
      <Card className="animate-rise">
        <CardHeader>
          <CardTitle>Copies</CardTitle>
          <CardDescription>
            {b.copiesTotal === 0
              ? 'No copies registered yet — add the first one below.'
              : `${b.copiesTotal} registered, ${b.copiesAvailable} available to borrow.`}
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-5">
          {b.copies.length > 0 && (
            <div className="overflow-x-auto rounded-xl border border-border">
              <table className="w-full min-w-130 text-sm">
                <thead>
                  <tr className="border-b border-border bg-surface-2 text-left text-xs uppercase tracking-wider text-muted">
                    <th className="px-4 py-2.5 font-medium">Barcode</th>
                    <th className="px-4 py-2.5 font-medium">Branch</th>
                    <th className="px-4 py-2.5 font-medium">Shelf</th>
                    <th className="px-4 py-2.5 font-medium">Condition</th>
                    <th className="px-4 py-2.5 font-medium">Price</th>
                    <th className="px-4 py-2.5 font-medium">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {b.copies.map((copy) => (
                    <tr key={copy.id} className="border-b border-border last:border-0">
                      <td className="px-4 py-2.5 font-mono text-[13px]">{copy.barcode}</td>
                      <td className="px-4 py-2.5">{copy.branchName ?? <span className="text-faint">—</span>}</td>
                      <td className="px-4 py-2.5">{copy.shelfNumber ?? <span className="text-faint">—</span>}</td>
                      <td className="px-4 py-2.5">{copy.condition}</td>
                      <td className="px-4 py-2.5">
                        {copy.price != null ? copy.price.toFixed(2) : <span className="text-faint">—</span>}
                      </td>
                      <td className="px-4 py-2.5">
                        <Badge variant={STATUS_VARIANT[copy.status]}>{copy.status}</Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <AddCopyForm book={b} />
        </CardContent>
      </Card>

      {/* ── Waitlist ── */}
      <WaitlistCard book={b} />

      {/* ── Borrow history ── */}
      <Card className="animate-rise">
        <CardHeader>
          <CardTitle>Borrow history</CardTitle>
        </CardHeader>
        <CardContent>
          {b.borrowHistory.length === 0 ? (
            <div className="flex items-center gap-3 rounded-xl border border-dashed border-border-strong bg-surface-2 px-4 py-6 text-sm text-muted">
              <History className="size-4 shrink-0" />
              This book hasn't circulated yet.
            </div>
          ) : (
            <div className="overflow-x-auto rounded-xl border border-border">
              <table className="w-full min-w-130 text-sm">
                <thead>
                  <tr className="border-b border-border bg-surface-2 text-left text-xs uppercase tracking-wider text-muted">
                    <th className="px-4 py-2.5 font-medium">Patron</th>
                    <th className="px-4 py-2.5 font-medium">Borrowed</th>
                    <th className="px-4 py-2.5 font-medium">Due</th>
                    <th className="px-4 py-2.5 font-medium">Returned</th>
                  </tr>
                </thead>
                <tbody>
                  {b.borrowHistory.map((loan) => (
                    <tr key={loan.id} className="border-b border-border last:border-0">
                      <td className="px-4 py-2.5 font-medium">{loan.patronName}</td>
                      <td className="px-4 py-2.5">
                        {new Date(loan.borrowedAtUtc).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })}
                      </td>
                      <td className="px-4 py-2.5">
                        {loan.dueAtUtc
                          ? new Date(loan.dueAtUtc).toLocaleDateString(undefined, { day: 'numeric', month: 'short' })
                          : '—'}
                      </td>
                      <td className="px-4 py-2.5">
                        {loan.returnedAtUtc ? (
                          new Date(loan.returnedAtUtc).toLocaleDateString(undefined, { day: 'numeric', month: 'short' })
                        ) : (
                          <span className="text-accent">still out</span>
                        )}
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
