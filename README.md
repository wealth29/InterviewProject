# Currency Conversion API

A robust ASP.NET Core Web API for real-time and historical currency conversion, featuring rate limiting, retry policies, and automatic data synchronization with external services.

---

## Features

- **Real-time conversion**: Convert amounts using the latest exchange rates.
- **Historical rates**: Retrieve exchange rates for a date range or specific date.
- **API key-based rate limiting**: Control usage with configurable quotas.
- **Resilient external service integration**: Retries with exponential backoff via Polly.
- **Background data synchronization**: Auto-fetches rates every 5 minutes.
- **Input validation**: Ensures valid currencies, dates, and amounts.
- **Versioned endpoints**: Routes include `/api/v1/` for future compatibility.

---

## Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- SQL Server or LocalDB
- API key from an external currency data provider (e.g., [Example Provider](https://api.example.com))
- Tools: [Postman](https://www.postman.com/) or `curl` for testing

---

