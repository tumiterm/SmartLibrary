import { GlobalWorkerOptions, getDocument, type PDFDocumentProxy } from 'pdfjs-dist'
import workerUrl from 'pdfjs-dist/build/pdf.worker.min.mjs?url'
import { ArrowLeft, ChevronLeft, ChevronRight, ZoomIn, ZoomOut } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { fetchBookAssetData } from '@/lib/api'

GlobalWorkerOptions.workerSrc = workerUrl

/**
 * View-only reader. The PDF streams into memory and renders to canvas —
 * no toolbar, no download/print, no text selection, watermark on every page.
 * Deterrence, not absolute DRM: screenshots can't be prevented anywhere.
 */
export function ReaderPage() {
  const { id } = useParams<{ id: string }>()
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const docRef = useRef<PDFDocumentProxy | null>(null)
  const [pageCount, setPageCount] = useState(0)
  const [page, setPage] = useState(1)
  const [scale, setScale] = useState(1.3)
  const [state, setState] = useState<'loading' | 'ready' | 'error'>('loading')

  useEffect(() => {
    let cancelled = false
    let task: ReturnType<typeof getDocument> | null = null
    async function load() {
      try {
        const data = await fetchBookAssetData(id!)
        task = getDocument({ data })
        const doc = await task.promise
        if (cancelled) return
        docRef.current = doc
        setPageCount(doc.numPages)
        setState('ready')
      } catch {
        if (!cancelled) setState('error')
      }
    }
    void load()
    return () => {
      cancelled = true
      void task?.destroy()
      docRef.current = null
    }
  }, [id])

  useEffect(() => {
    async function render() {
      const doc = docRef.current
      const canvas = canvasRef.current
      if (!doc || !canvas || state !== 'ready') return
      const pdfPage = await doc.getPage(page)
      const viewport = pdfPage.getViewport({ scale })
      const context = canvas.getContext('2d')
      if (!context) return
      canvas.width = viewport.width
      canvas.height = viewport.height
      await pdfPage.render({ canvas, canvasContext: context, viewport }).promise
    }
    void render()
  }, [page, scale, state, pageCount])

  const watermark = `Demo Library · librarian@demo · ${new Date().toLocaleDateString()}`

  return (
    <div
      className="flex min-h-dvh select-none flex-col bg-[#111009] text-[#ece7dc]"
      onContextMenu={(e) => e.preventDefault()}
    >
      <header className="sticky top-0 z-40 flex h-13 items-center gap-2 border-b border-[#2b2822] bg-[#161512]/90 px-4 backdrop-blur-md">
        <Link
          to={`/catalog/books/${id}`}
          className="inline-flex items-center gap-1.5 rounded-lg px-2 py-1.5 text-sm text-[#a89f8c] transition-colors hover:text-[#ece7dc]"
        >
          <ArrowLeft className="size-4" /> Back
        </Link>
        <div className="ml-auto flex items-center gap-1.5">
          <Button
            variant="ghost"
            size="icon"
            className="text-[#a89f8c] hover:bg-[#201d19] hover:text-[#ece7dc]"
            aria-label="Zoom out"
            onClick={() => setScale((s) => Math.max(0.6, s - 0.2))}
          >
            <ZoomOut className="size-4" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="text-[#a89f8c] hover:bg-[#201d19] hover:text-[#ece7dc]"
            aria-label="Zoom in"
            onClick={() => setScale((s) => Math.min(2.6, s + 0.2))}
          >
            <ZoomIn className="size-4" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="text-[#a89f8c] hover:bg-[#201d19] hover:text-[#ece7dc]"
            aria-label="Previous page"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            <ChevronLeft className="size-4" />
          </Button>
          <span className="min-w-16 text-center font-mono text-xs text-[#a89f8c]">
            {state === 'ready' ? `${page} / ${pageCount}` : '—'}
          </span>
          <Button
            variant="ghost"
            size="icon"
            className="text-[#a89f8c] hover:bg-[#201d19] hover:text-[#ece7dc]"
            aria-label="Next page"
            disabled={page >= pageCount}
            onClick={() => setPage((p) => Math.min(pageCount, p + 1))}
          >
            <ChevronRight className="size-4" />
          </Button>
        </div>
      </header>

      <main className="relative flex flex-1 items-start justify-center overflow-auto p-6">
        {state === 'loading' && (
          <div className="grid min-h-64 place-items-center text-[#a89f8c]">
            <Spinner className="size-6" />
          </div>
        )}
        {state === 'error' && (
          <p className="mt-16 text-sm text-[#a89f8c]">
            Couldn't open the digital copy. It may not exist, or the API is unreachable.
          </p>
        )}
        {state === 'ready' && (
          <div className="relative shadow-pop">
            <canvas ref={canvasRef} className="rounded-sm bg-white" />
            {/* Dynamic watermark: who is reading, where, when. */}
            <div
              aria-hidden
              className="pointer-events-none absolute inset-0 flex flex-col items-center justify-around overflow-hidden"
            >
              {[0, 1, 2].map((i) => (
                <p
                  key={i}
                  className="-rotate-30 whitespace-nowrap text-lg font-semibold tracking-widest text-black/10"
                >
                  {watermark}
                </p>
              ))}
            </div>
          </div>
        )}
      </main>
    </div>
  )
}
