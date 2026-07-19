import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Save, Settings2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import { getSettings, updateSettings } from '@/lib/api'

interface Field {
  key: 'loanDays' | 'dailyFineAmount' | 'maxActiveLoans' | 'fineBlockThreshold' | 'maxRenewals' | 'holdPickupDays'
  label: string
  hint: string
  step?: string
}

const FIELDS: Field[] = [
  { key: 'loanDays', label: 'Borrowing days', hint: 'How long a loan lasts before it is due.' },
  { key: 'maxActiveLoans', label: 'Max books per patron', hint: 'Active loans allowed at once.' },
  { key: 'maxRenewals', label: 'Max renewals', hint: 'Times a loan can be extended.' },
  { key: 'dailyFineAmount', label: 'Fine per overdue day', hint: 'Charged automatically at return.', step: '0.50' },
  { key: 'fineBlockThreshold', label: 'Fine block threshold', hint: 'Borrowing stops at this amount owed.', step: '10' },
  { key: 'holdPickupDays', label: 'Reservation pickup days', hint: 'Days to collect before a hold expires.' },
]

export function SettingsPage() {
  const queryClient = useQueryClient()
  const settings = useQuery({ queryKey: ['settings'], queryFn: getSettings })
  const [values, setValues] = useState<Record<Field['key'], string>>({
    loanDays: '',
    dailyFineAmount: '',
    maxActiveLoans: '',
    fineBlockThreshold: '',
    maxRenewals: '',
    holdPickupDays: '',
  })

  useEffect(() => {
    if (settings.data) {
      setValues({
        loanDays: String(settings.data.loanDays),
        dailyFineAmount: String(settings.data.dailyFineAmount),
        maxActiveLoans: String(settings.data.maxActiveLoans),
        fineBlockThreshold: String(settings.data.fineBlockThreshold),
        maxRenewals: String(settings.data.maxRenewals),
        holdPickupDays: String(settings.data.holdPickupDays),
      })
    }
  }, [settings.data])

  const save = useMutation({
    mutationFn: () =>
      updateSettings({
        loanDays: Number(values.loanDays),
        dailyFineAmount: Number(values.dailyFineAmount),
        maxActiveLoans: Number(values.maxActiveLoans),
        fineBlockThreshold: Number(values.fineBlockThreshold),
        maxRenewals: Number(values.maxRenewals),
        holdPickupDays: Number(values.holdPickupDays),
      }),
    onSuccess: () => {
      toast.success('Library rules saved', { description: 'They apply to every checkout from now on.' })
      void queryClient.invalidateQueries({ queryKey: ['settings'] })
    },
    onError: (error: Error) => toast.error('Could not save settings', { description: error.message }),
  })

  return (
    <div className="flex flex-col gap-8">
      <header className="animate-fade">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Settings</p>
        <h1 className="font-display mt-2 text-3xl font-semibold sm:text-4xl">Library rules</h1>
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
          Your library's own circulation policy. Until customized, platform defaults apply.
        </p>
      </header>

      <Card className="animate-rise">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Settings2 className="size-4 text-accent" />
            Circulation policy
            {settings.data && (
              <Badge variant={settings.data.isCustomized ? 'brass' : 'neutral'}>
                {settings.data.isCustomized ? 'Customized' : 'Platform defaults'}
              </Badge>
            )}
          </CardTitle>
          <CardDescription>Changes take effect immediately for new checkouts, renewals and fines.</CardDescription>
        </CardHeader>
        <CardContent>
          {settings.isPending ? (
            <div className="grid min-h-32 place-items-center text-muted">
              <Spinner className="size-5" />
            </div>
          ) : (
            <form
              onSubmit={(e) => {
                e.preventDefault()
                save.mutate()
              }}
              className="flex flex-col gap-5"
            >
              <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                {FIELDS.map((field) => (
                  <div key={field.key}>
                    <Label htmlFor={field.key}>{field.label}</Label>
                    <Input
                      id={field.key}
                      type="number"
                      min={0}
                      step={field.step ?? '1'}
                      required
                      value={values[field.key]}
                      onChange={(e) => setValues((v) => ({ ...v, [field.key]: e.target.value }))}
                    />
                    <p className="mt-1.5 text-xs text-faint">{field.hint}</p>
                  </div>
                ))}
              </div>
              <div className="flex justify-end border-t border-border pt-5">
                <Button type="submit" size="lg" disabled={save.isPending}>
                  {save.isPending ? <Spinner /> : <Save className="size-4" />}
                  Save rules
                </Button>
              </div>
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
