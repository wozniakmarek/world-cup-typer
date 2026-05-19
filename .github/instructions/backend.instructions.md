---
applyTo: "backend/**"
---

# Backend guardrails

- Walidacja wejścia musi być po stronie API (nie tylko w UI).
- Każda zmiana reguł biznesowych (3/1/0, kickoff, widoczność typów) wymaga testu.
- Nie hardcoduj sekretów, connection stringów ani kluczy JWT.
- Dbaj o zgodność DTO i kontraktów API z aktualnym zachowaniem klienta.
- Zmiany w auth/autoryzacji rób ostrożnie; domyślnie preferuj bezpieczniejsze ustawienia.
