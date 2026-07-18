import { useMutation, useQuery } from '@tanstack/react-query'
import { ArrowLeft, IdCard } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input, Select } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import { ApiError, getBranches, registerMember } from '@/lib/api'
import { MEMBER_TYPES, type MemberType } from '@/lib/members'

export function RegisterMemberPage() {
  const navigate = useNavigate()
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [type, setType] = useState<MemberType>('Public')
  const [branchId, setBranchId] = useState('')

  const branches = useQuery({ queryKey: ['branches'], queryFn: getBranches })

  const mutation = useMutation({
    mutationFn: () =>
      registerMember({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        phone: phone.trim() || null,
        type,
        homeBranchId: branchId || null,
      }),
    onSuccess: (member) => {
      toast.success('Member registered', { description: member.membershipNumber })
      navigate(`/patrons/${member.id}`)
    },
    onError: (error: Error) => {
      toast.error(
        error instanceof ApiError && error.status === 409 ? 'Already registered' : 'Could not register member',
        { description: error.message },
      )
    },
  })

  return (
    <div className="flex flex-col gap-8">
      <div className="animate-fade">
        <Link
          to="/patrons"
          className="inline-flex items-center gap-1.5 text-sm text-muted transition-colors hover:text-ink"
        >
          <ArrowLeft className="size-4" /> Members
        </Link>
      </div>

      <header className="animate-fade">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Patrons</p>
        <h1 className="font-display mt-2 text-3xl font-semibold sm:text-4xl">Register a member</h1>
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-muted">
          Register on the whole library, or tie them to a home branch. A membership card is
          issued the moment you save.
        </p>
      </header>

      <Card className="animate-rise">
        <CardContent>
          <form
            onSubmit={(e) => {
              e.preventDefault()
              mutation.mutate()
            }}
            className="flex flex-col gap-5"
          >
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <Label htmlFor="firstName">First name</Label>
                <Input id="firstName" required value={firstName} onChange={(e) => setFirstName(e.target.value)} />
              </div>
              <div>
                <Label htmlFor="lastName">Last name</Label>
                <Input id="lastName" required value={lastName} onChange={(e) => setLastName(e.target.value)} />
              </div>
              <div>
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
              </div>
              <div>
                <Label htmlFor="phone">Phone</Label>
                <Input id="phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
              </div>
              <div>
                <Label htmlFor="type">Member type</Label>
                <Select id="type" value={type} onChange={(e) => setType(e.target.value as MemberType)}>
                  {MEMBER_TYPES.map((t) => (
                    <option key={t} value={t}>
                      {t}
                    </option>
                  ))}
                </Select>
              </div>
              <div>
                <Label htmlFor="branch">Home branch</Label>
                <Select id="branch" value={branchId} onChange={(e) => setBranchId(e.target.value)}>
                  <option value="">— Library-wide —</option>
                  {branches.data?.map((b) => (
                    <option key={b.id} value={b.id}>
                      {b.name}
                    </option>
                  ))}
                </Select>
              </div>
            </div>

            <div className="flex items-center justify-end border-t border-border pt-5">
              <Button
                type="submit"
                size="lg"
                disabled={mutation.isPending || !firstName.trim() || !lastName.trim() || !email.trim()}
              >
                {mutation.isPending ? <Spinner /> : <IdCard className="size-4" />}
                Register & issue card
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
