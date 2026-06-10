import { LogOut, ShieldCheck } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { getErrorMessage } from '../../api/client'
import { InlineAlert } from '../../components/InlineAlert'
import { buttonClassName, inputClassName, secondaryButtonClassName } from '../../styles/ui'
import { useAuth } from './AuthContext'

export const ChangePasswordPage = () => {
  const navigate = useNavigate()
  const { changePassword, logout, user } = useAuth()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    if (newPassword !== confirmPassword) {
      setError('Powtórzone hasło musi być takie samo jak nowe hasło.')
      return
    }

    setIsSubmitting(true)

    try {
      await changePassword(currentPassword, newPassword)
      navigate('/', { replace: true })
    } catch (submitError) {
      setError(getErrorMessage(submitError))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="relative min-h-screen overflow-hidden bg-pitch-950">
      <div className="absolute inset-0 bg-stadium opacity-75" />
      <div className="relative mx-auto flex min-h-screen max-w-5xl items-center justify-center px-4 py-10 sm:px-6 lg:px-8">
        <div className="grid w-full gap-6 lg:grid-cols-[0.85fr_1fr] lg:items-stretch">
          <section className="glass-card flex flex-col justify-between rounded-[2rem] p-6 sm:p-8">
            <div>
              <div className="mb-8 inline-flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-400 text-slate-950">
                <ShieldCheck aria-hidden="true" size={26} />
              </div>
              <p className="font-display text-sm uppercase tracking-[0.28em] text-emerald-300/80">
                Pierwsze logowanie
              </p>
              <h1 className="mt-4 font-display text-4xl font-bold uppercase leading-none text-white sm:text-5xl">
                Ustaw swoje hasło
              </h1>
              <p className="mt-5 text-sm leading-6 text-slate-300 sm:text-base">
                Konto {user?.displayName ? `${user.displayName} ` : ''}działa teraz na haśle tymczasowym. Zmień je przed wejściem do typów, rankingu i panelu aplikacji.
              </p>
            </div>

            <button
              type="button"
              className={`${secondaryButtonClassName} mt-8 w-fit gap-2`}
              onClick={() => void logout()}
            >
              <LogOut aria-hidden="true" size={18} />
              Wyloguj
            </button>
          </section>

          <section className="glass-card rounded-[2rem] p-6 sm:p-8">
            <div className="mb-6">
              <p className="font-display text-3xl uppercase text-white">Zmiana hasła</p>
              <p className="mt-2 text-sm text-slate-400">Nowe hasło musi mieć co najmniej 8 znaków.</p>
            </div>

            {error ? <InlineAlert tone="error" message={error} className="mb-5" /> : null}

            <form className="space-y-4" onSubmit={(event) => void handleSubmit(event)}>
              <label className="block space-y-2">
                <span className="text-sm text-slate-300">Obecne hasło</span>
                <input
                  type="password"
                  className={inputClassName}
                  value={currentPassword}
                  onChange={(event) => setCurrentPassword(event.target.value)}
                  autoComplete="current-password"
                  required
                />
              </label>
              <label className="block space-y-2">
                <span className="text-sm text-slate-300">Nowe hasło</span>
                <input
                  type="password"
                  className={inputClassName}
                  value={newPassword}
                  onChange={(event) => setNewPassword(event.target.value)}
                  autoComplete="new-password"
                  minLength={8}
                  required
                />
              </label>
              <label className="block space-y-2">
                <span className="text-sm text-slate-300">Powtórz nowe hasło</span>
                <input
                  type="password"
                  className={inputClassName}
                  value={confirmPassword}
                  onChange={(event) => setConfirmPassword(event.target.value)}
                  autoComplete="new-password"
                  minLength={8}
                  required
                />
              </label>

              <button type="submit" disabled={isSubmitting} className={`${buttonClassName} w-full`}>
                {isSubmitting ? 'Zapisywanie...' : 'Zapisz nowe hasło'}
              </button>
            </form>
          </section>
        </div>
      </div>
    </div>
  )
}
