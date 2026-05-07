export const FormField = ({
  label,
  hint,
  children,
}: {
  label: string
  hint?: string
  children: React.ReactNode
}) => {
  return (
    <label className="flex flex-col gap-2 text-sm text-slate-300">
      <span className="font-medium text-slate-200">{label}</span>
      {children}
      {hint ? <span className="text-xs text-slate-500">{hint}</span> : null}
    </label>
  )
}
