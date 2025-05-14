# MyCurrencyApi

An ASP.NET Core 8 Web API for real‑time and historical currency conversion. It integrates with a simulated external exchange‑rate service, supports API‑key based rate limiting, and persists rates in a SQL Server database. Background services periodically fetch real‑time and historical rates, and the API exposes versioned endpoints for conversion and history retrieval.

---

## 📋 Features

* **Real‑time conversion** using latest stored rates or external fallback
* **Historical rates** lookup over date ranges
* **Historical conversion** for specific dates
* **Background services**:

  * Real‑time fetch every 5 minutes
  * One‑time historical backfill (last year) for major pairs
* **Resilient HTTP client** with Polly retry and circuit‑breaker policies
* **API‑key based throttling** (in‑memory) via `X-Api-Key` header
* **EF Core persistence** with upsert logic and unique constraints
* **Swagger/OpenAPI** documentation and testing
* **URL‑segment versioning** (`/api/v1/...`)

---

## 🚀 Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* SQL Server or LocalDB instance
*  Git

---

## 🔧 Installation & Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/yourusername/MyCurrencyApi.git
   cd MyCurrencyApi
   ```

2. **Restore dependencies**

   ```bash
   dotnet restore
   ```

3. **Configuration**

   * Open `appsettings.json`

     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "<Your SQL Server connection string>"
       },
       "ExternalService": {
         "BaseUrl": "https://api.example.com",
         "ApiKey": "<your-external-service-key>"
       },
       "RateLimiting": {
         "ApiKeys": {
           "abc123": 1000,
           "def456": 500
         }
       }
     }
     ```
   * Replace placeholders with your actual values.

4. **Apply EF Core migrations**

   ```bash
   dotnet ef database update
   ```

   This creates the `ExchangeRates` table with unique indices.

---

## ▶️ Running the Application

```bash
dotnet run
```

The API listens on `https://localhost:5001` and `http://localhost:5000` by default.

---

## 🛠️ Background Services

* **RealTimeFetchService**: Runs every 5 minutes to upsert the latest real‑time rates for USD→GBP, USD→EUR, GBP→EUR, etc.
* **HistoricalFetchService**: On startup, backfills daily rates for the past year for the same pairs.

These are registered in `Program.cs` via `AddHostedService<...>()`.

---

## 🗂️ API Endpoints

All endpoints require an `X-Api-Key` header with a valid key from your configuration.

### Real‑time Conversion

```
GET /api/v1/convert?base={BASE}&target={TARGET}&amount={AMOUNT}
```

* **Response**: `200 OK` with converted amount as `decimal`.

### Historical Rates

```
GET /api/v1/rates/history?base={BASE}&target={TARGET}&from={YYYY-MM-DD}&to={YYYY-MM-DD}
```

* **Response**: `200 OK` with JSON `{ "2025-04-01": 0.79, ... }`.

### Historical Conversion

```
GET /api/v1/convert/historical?base={BASE}&target={TARGET}&date={YYYY-MM-DD}&amount={AMOUNT}
```

* **Response**: `200 OK` with converted amount.

---

## 🔑 Authentication & Rate Limiting

1. **API Keys** are defined in `appsettings.json` under `RateLimiting.ApiKeys`.
2. Include `X-Api-Key` in request headers.
3. Quotas are decremented per request. Exceeding quota returns `429 Too Many Requests`.
4. Missing or invalid keys return `401 Unauthorized`.

---

## 📖 Swagger / OpenAPI

Swagger UI is enabled in Development mode. Run the app and browse to:

```
https://localhost:5001/swagger/index.html
```

Click **Authorize** (🔐), enter your API key (e.g. `abc123`), and test endpoints directly.

---

## ⚙️ Architecture Overview

* **HttpClientFactory + Polly**: Resilient HTTP calls with retries & circuit breaker.
* **EF Core**: Single `ExchangeRate` entity stores both real‑time (Timestamp) and historical (Date) rates with unique indices.
* **BackgroundServices**: Hosted services for scheduled fetch and backfill.
* **Custom Middleware**: `ApiKeyMiddleware` for header validation and quota checks.
* **AspNetCoreRateLimit**: In‑memory policy stores for IP/Key throttling.
* **Versioned Controllers** in `Controllers/v1` for future-proofing.

---

## 🤝 Contributing

1. Fork the repo
2. Create a feature branch: `git checkout -b feature/YourFeature`
3. Commit changes: \`git commit -m "Add YourFeature"
4. Push to your branch: `git push origin feature/YourFeature`
5. Open a Pull Request

Please follow existing code style and include tests where applicable.

---

