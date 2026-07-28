# Teknik Mimari

## Yapı

Modüler monolith. İlk aşamada mikroservis yok.

- `apps/web`: React + TypeScript + Vite
- `apps/api`: ASP.NET Core Web API
- `tests`: unit, integration, e2e
- `infra`: Docker, database ve deployment dosyaları

## Backend katmanları

- Domain
- Application
- Infrastructure
- Api

## Temel teknik kararlar

- PostgreSQL 18
- EF Core migrations
- OpenAPI
- ProblemDetails hata standardı
- FluentValidation
- Serilog structured logging
- Health checks
- Background jobs için başlangıçta hosted services; gerektikçe Hangfire/Quartz kararı
- UTC saklama, Europe/Istanbul gösterim
- Optimistic concurrency
