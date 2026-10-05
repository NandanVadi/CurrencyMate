# CurrencyMate

CurrencyMate is a premium ASP.NET Core MVC web application for currency conversion, travel budget planning, and historical exchange-rate trends.

## Core Features
- **Currency Conversion:** Live currency conversion using real exchange rates.
- **Trend Analysis:** 30-day historical movement charts for currency pairs.
- **Travel Budget:** Plan and calculate daily travel budgets in foreign currencies.
- **History & Favorites:** Save favorite pairs and search past conversions.
- **Secure Accounts:** Cookie-based authentication and secure user sessions.
- **Dark/Light Theme:** A refined, premium user interface with local theme persistence.

## Technology Stack
- .NET 10 / C#
- ASP.NET Core MVC & Razor Views
- Entity Framework Core with SQLite
- Chart.js for data visualization
- Vanilla CSS and JavaScript

## Prerequisites
- .NET 10 SDK

## Setup and Run
1. **Clone the repository:**
   ```bash
   git clone https://github.com/NandanVadi/CurrencyMate.git
   cd CurrencyMate
   ```
2. **Restore packages:**
   ```bash
   dotnet restore
   ```
3. **Database Setup:**
   The SQLite database is automatically created on the first run.
4. **Run the application:**
   ```bash
   dotnet run
   ```
5. **Access the application:**
   Open `http://localhost:5000` (or the provided port) in your browser.

## Testing & Verification Checklist
- [x] Application restores and builds successfully.
- [x] Registration, Login, and Logout function securely.
- [x] Protected routes redirect to Login when unauthenticated.
- [x] Back-forward cache (bfcache) prevents restoring authenticated pages after logout.
- [x] Conversions calculate correctly and history is saved.
- [x] Trend charts display 30-day historical data.
- [x] Travel Budget accurately splits total budget by days.
- [x] Theme toggle successfully switches between dark and light modes.

## Collaboration Workflow
- NandanVadi: `feature/nandan-conversion-trends`
- DarshParekh205: `feature/darsh-accounts-travel-history`
Each contributor works on their respective feature branch, commits their work, and opens a Pull Request to `main`.

## Contributors
- NandanVadi (nandanvadi@gmail.com)
- DarshParekh205 (parekhdarsh002@gmail.com)
