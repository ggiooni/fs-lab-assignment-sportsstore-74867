# SportsStore - Full Stack Development Assignment

**Student:** Nicolas Alberto Boggioni Troncoso
**Student Number:** 74867
**Module:** Full Stack Development

## Overview

Modernised SportsStore application upgraded from .NET 6 to .NET 9, with structured logging via Serilog, Stripe payment integration, and a GitHub Actions CI pipeline.

## Upgrade Steps (.NET 6 → .NET 9)

1. Updated `TargetFramework` to `net9.0` in both `.csproj` files
2. Updated NuGet dependencies to their .NET 9 compatible versions:
   - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` → 9.0.0
   - `Microsoft.EntityFrameworkCore.Design` → 9.0.0
   - `Microsoft.EntityFrameworkCore.SqlServer` → 9.0.0
   - Test packages updated (xunit, Moq, coverlet, etc.)
3. Resolved breaking changes and verified all existing functionality works

## Serilog Logging Setup

Structured logging is configured using `Serilog.AspNetCore` with the following setup:

- **Configuration:** Defined in `appsettings.json` under the `Serilog` section
- **Sinks:**
  - **Console** — outputs structured logs to the terminal
  - **Rolling File** — writes to `Logs/sportsstore-YYYYMMDD.log` with daily rolling and 30-day retention
- **Enrichers:** `FromLogContext`, `WithMachineName`, `WithEnvironmentName`
- **Logging points:**
  - Application startup and shutdown
  - HTTP request logging via `UseSerilogRequestLogging()`
  - Checkout flow (cart status, order creation, validation errors)
  - Payment processing (Stripe session creation, success, failure, cancellation)
  - Authentication (login success, login failure, logout)

## Stripe Configuration

Stripe payment integration uses the official `Stripe.net` SDK with Stripe Checkout Sessions.

### Setup (required to run locally)

1. Create a Stripe account and get your test API keys from [dashboard.stripe.com/test/apikeys](https://dashboard.stripe.com/test/apikeys)

2. Store keys using .NET User Secrets (keys are never committed to source control):
   ```bash
   cd SportsStore
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY_HERE"
   dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY_HERE"
   ```

### Payment Flow

1. Customer fills in shipping details on the checkout page
2. Order is saved to the database
3. Customer is redirected to a Stripe-hosted checkout page
4. On **success**: payment is verified, `PaymentIntentId` is stored with the order, cart is cleared
5. On **cancel**: customer is shown a cancellation page with option to retry
6. On **failure**: customer is shown an error page with option to retry

### Architecture

- `IPaymentService` — interface for payment operations (clean separation)
- `StripePaymentService` — Stripe implementation using Checkout Sessions
- Payment logic is fully separated from the `OrderController`

## How to Run Locally

### Prerequisites

- .NET 9 SDK
- SQL Server (LocalDB on Windows, or Docker)

### Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/ggiooni/fs-lab-assignment-sportsstore-74867.git
   cd fs-lab-assignment-sportsstore-74867
   ```

2. Configure Stripe user secrets (see Stripe Configuration section above)

3. Build and run:
   ```bash
   dotnet build
   cd SportsStore
   dotnet run
   ```

4. Open `https://localhost:5001` in your browser

5. To test Stripe payments, use the test card number `4242 4242 4242 4242` with any future expiry date and any CVC.

### Running Tests

```bash
dotnet test
```

## CI Pipeline

GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push to `main` and on pull requests:

1. Restores dependencies
2. Builds the solution (with warnings as errors)
3. Runs all tests with TRX reporting
4. Uploads test results as artifacts
