# NPPE Exam Prep

A paid online exam-preparation platform for the **NPPE** (National Professional
Practice Examination). Admins author practice exams; students purchase access,
take timed multiple-choice exams, receive instant scored feedback with
explanations, and review their attempt history.

## Architecture

Clean Architecture on **.NET 9 / ASP.NET Core Razor Pages**:

| Project | Responsibility |
| --- | --- |
| `NPPE.Domain` | Entities, enums, constants. No outward dependencies. |
| `NPPE.Application` | Use-cases via **MediatR** (Commands/Queries), DTOs, repository interfaces. |
| `NPPE.Infrastructure` | EF Core `DbContext`, repository implementations, migrations (SQL Server). |
| `NPPE.Web` | Razor Pages UI, ASP.NET Identity auth, Stripe webhook endpoint, DI wiring. |

Key patterns: CQRS + MediatR, repository pattern (generic + specialized),
DTO mapping at the Application boundary, soft-delete for exams (`IsActive`),
and role/policy-based authorization.

## Domain model

```
AppUser (IdentityUser)  ──1:M──> ExamAttempt ──1:M──> AttemptedAnswer
   │  IsPremium, Stripe* fields             Score, TakenAt
   └──1:M──> Payment

Exam ──1:M──> Question ──1:M──> AnswerOption (A–D, one IsCorrect)
 IsActive       Explanation for correct / incorrect
```

## Roles & access

- **Admin** — manage exams, questions, and answer options.
- **Student** — take exams (requires premium), view results and history, manage billing.
- **Premium** access is gated on the live `AppUser.IsPremium` flag, which is set
  and revoked by Stripe webhooks. It is intentionally *not* an auth policy so that
  a purchase takes effect immediately without re-login.

## Payments (Stripe)

Two plans, both in CAD (see `NPPE.Domain/Constants/PricingPlans.cs`):

- **One-time** — CA$29.00, permanent access.
- **Monthly subscription** — CA$9.99/month.

Flow: a `Pending` `Payment` row is created up front, the user is redirected to
Stripe Checkout, and the signed webhook at `POST /Payments/Webhook`
(`checkout.session.completed`, `customer.subscription.updated|deleted`,
`invoice.payment_failed`) reconciles payment/subscription state and toggles
`IsPremium`.

## Prerequisites

- .NET 9 SDK
- SQL Server (LocalDB or full) reachable via the `DefaultConnection` string
- A Stripe account (test keys) for payment flows
- (Optional) Google OAuth credentials for external login

## Configuration

Set these via **user-secrets** or environment variables (do **not** commit real
secrets — `appsettings.json` ships with placeholders only):

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Stripe:PublishableKey` / `Stripe:SecretKey` / `Stripe:WebhookSecret` | Stripe integration |
| `Authentication:Google:ClientId` / `:ClientSecret` | Google OAuth (optional) |
| `SeedAdmin:Email` / `SeedAdmin:Password` | Bootstraps an admin in non-Development environments (see below) |

## Running locally

```bash
dotnet build
dotnet run --project NPPE.Web
```

The app applies EF migrations automatically on startup. Endpoints:
`https://localhost:7001` and `http://localhost:5153`.

### Seeded accounts

- **Development only:** demo users are seeded automatically —
  `admin@nppe.ca / Admin@123!` and `student@nppe.ca / Student@123!`.
- **Other environments:** no default users are created. An initial admin is
  seeded **only** if `SeedAdmin:Email` and `SeedAdmin:Password` are supplied via
  configuration. Roles (`Admin`, `Student`) are always ensured.

## Stripe webhooks in development

Forward events to the local webhook endpoint with the Stripe CLI:

```bash
stripe listen --forward-to https://localhost:7001/Payments/Webhook
```

Use the signing secret it prints as `Stripe:WebhookSecret`.

## Localization

English (`en`, default) and French (`fr`), switchable via a `?culture=` query
parameter. Resources live under `NPPE.Web/Resources`.
