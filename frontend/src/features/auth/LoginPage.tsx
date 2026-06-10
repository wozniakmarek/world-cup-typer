import { useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate } from 'react-router-dom'
import { getErrorMessage } from '../../api/client'
import { useAuth } from './AuthContext'
import { buttonClassName, inputClassName } from '../../styles/ui'

export const LoginPage = () => {
  const { isAuthenticated, login, requiresPasswordChange } = useAuth()
  const [loginValue, setLoginValue] = useState('admin@marekwozniak.me')
  const [password, setPassword] = useState('ChangeMe123!')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (isAuthenticated) {
    return <Navigate to={requiresPasswordChange ? '/change-password' : '/'} replace />
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      await login(loginValue, password)
    } catch (submitError) {
      setError(getErrorMessage(submitError))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="relative min-h-screen overflow-hidden bg-pitch-950">
      <div className="absolute inset-0 bg-stadium opacity-90" />
      <div className="relative mx-auto flex min-h-screen max-w-7xl flex-col justify-center gap-10 px-4 py-10 sm:px-6 lg:flex-row lg:items-center lg:gap-20 lg:px-8">
        <div className="max-w-xl space-y-5">
          <p className="font-display text-sm uppercase tracking-[0.32em] text-emerald-300/80">{'Typer Mistrzostw \u015Awiata'}</p>
          <h1 className="font-display text-5xl font-bold uppercase leading-none text-white sm:text-6xl">
            {'Wchodzisz na muraw\u0119. Typy, ranking i emocje w jednym miejscu.'}
          </h1>
          <p className="max-w-lg text-base text-slate-300 sm:text-lg">
            {'Prywatne MVP dla grupy znajomych: obstawianie mecz\u00F3w, rozliczenia 3/1/0, ranking i panel admina gotowe pod dalszy rozw\u00F3j PWA.'}
          </p>
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="glass-card rounded-3xl p-4">
              <p className="text-xs uppercase tracking-[0.24em] text-slate-400">3 pkt</p>
              <p className="mt-3 text-sm text-slate-200">{'Dok\u0142adny wynik po 90 min'}</p>
            </div>
            <div className="glass-card rounded-3xl p-4">
              <p className="text-xs uppercase tracking-[0.24em] text-slate-400">1 pkt</p>
              <p className="mt-3 text-sm text-slate-200">{'Trafiony zwyci\u0119zca lub remis'}</p>
            </div>
            <div className="glass-card rounded-3xl p-4">
              <p className="text-xs uppercase tracking-[0.24em] text-slate-400">MVP</p>
              <p className="mt-3 text-sm text-slate-200">Przygotowane pod deploy i PWA</p>
            </div>
          </div>
        </div>

        <div className="glass-card w-full max-w-md rounded-[2rem] p-6 sm:p-8">
          <div className="mb-6">
            <p className="font-display text-3xl uppercase text-white">Logowanie</p>
            <p className="mt-2 text-sm text-slate-400">{'Zaloguj si\u0119 mailem albo nazw\u0105 gracza.'}</p>
          </div>

          <form className="space-y-4" onSubmit={(event) => void handleSubmit(event)}>
            <label className="block space-y-2">
              <span className="text-sm text-slate-300">Login</span>
              <input
                className={inputClassName}
                value={loginValue}
                onChange={(event) => setLoginValue(event.target.value)}
                placeholder="admin@marekwozniak.me"
              />
            </label>
            <label className="block space-y-2">
              <span className="text-sm text-slate-300">{'Has\u0142o'}</span>
              <input
                type="password"
                className={inputClassName}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                placeholder="••••••••"
              />
            </label>
            {error ? <div className="rounded-2xl bg-rose-500/15 px-4 py-3 text-sm text-rose-200">{error}</div> : null}
            <button type="submit" disabled={isSubmitting} className={`${buttonClassName} w-full`}>
              {isSubmitting ? 'Logowanie...' : 'Wejd\u017A do aplikacji'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
