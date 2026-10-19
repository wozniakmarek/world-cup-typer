export const ArchiveNotice = () => import.meta.env.VITE_ARCHIVE_MODE === 'true' ? (
  <p role="note" className="mb-5 rounded-2xl border border-emerald-300/20 bg-emerald-300/5 px-4 py-3 text-sm leading-6 text-emerald-100">
    <strong>Archiwum zakończonego turnieju.</strong> Wyniki, typy, ranking i podsumowania zostały zachowane. Zaloguj się dotychczasowym hasłem. Zmiany są wyłączone.
  </p>
) : null
