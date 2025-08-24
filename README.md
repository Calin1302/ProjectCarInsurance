# ProjectCarInsurance

# Task A – Insurance Policies
- Adăugat `EndDate` obligatoriu în `InsurancePolicy`.
- Configurat validări EF Core.
- Seed inițial cu proprietari, mașini și polițe.

# Task B – Claims & History
- Creat model și DTO pentru `Claim`.
- Adăugat endpoint pentru înregistrarea unui claim (`POST /api/cars/{id}/claims`).
- Adăugat endpoint pentru istoricul mașinii (`GET /api/cars/{id}/history`).

# Task C – Harden Insurance Validity
- Validare `carId` (404 dacă nu există).
- Validare format dată (400 pentru input invalid).
- Teste automate cu MSTest pentru boundary cases (StartDate, EndDate, before, after).

# Task D – Background Service
- Implementat `PolicyExpiryNotifier` ca `IHostedService`.
- Rulează la interval configurabil (ex. 10 secunde pentru testare).
- Loghează polițele expirate și marchează ca procesate.
