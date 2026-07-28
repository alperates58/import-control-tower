# Import Control Tower

Şirketin Çin başta olmak üzere yurt dışı satın alma siparişlerini; tedarikçi üretiminden fabrika stok girişine kadar tek merkezden yöneten ithalat operasyon platformu.

---

## Faz 00 — Temel Altyapı (Foundation)

Bu proje monorepo mimarisinde geliştirilmektedir:
- **`apps/api`**: ASP.NET Core 10 Web API (Clean Modular Monolith)
- **`apps/web`**: React 19.2 + TypeScript + Vite + Nginx
- **Veritabanı**: PostgreSQL 18 (`postgres:18-alpine`)
- **Konteyner Yönetimi**: Docker Compose & Coolify

---

## Hızlı Başlangıç (Docker Compose)

Projeyi yerel ortamda tüm bağımlılıklarıyla birlikte çalıştırmak için tek komut yeterlidir:

### 1. Yerel Uygulamayı Başlatma
```bash
docker compose up -d --build
```
Başlatılan servisler:
- **Frontend App Shell (Nginx)**: [http://localhost:3000](http://localhost:3000)
- **API Swagger / Scalar UI**: [http://localhost:3000/api/v1/system/info](http://localhost:3000/api/v1/system/info) (Reverse proxy üzerinden) veya doğrudan API debug portu [http://localhost:8080/scalar/v1](http://localhost:8080/scalar/v1)
- **PostgreSQL 18**: `localhost:5432`

### 2. Docker Yapılandırma Doğrulama ve Derleme
```bash
# Docker compose dosya doğrulaması
docker compose config

# İmajların derlenmesi
docker compose build
```

### 3. Testleri Çalıştırma
```bash
# Backend Unit ve Integration testlerini API konteyneri içinde koşturma
docker compose exec api dotnet test apps/api/ImportControlTower.sln
```

### 4. Sağlık (Health Check) ve Sistem Endpoint Kontrolleri
```bash
# Web Shell
curl http://localhost:3000

# API Sistem Bilgisi (Nginx Reverse Proxy üzerinden)
curl http://localhost:3000/api/v1/system/info

# Liveness Check
curl http://localhost:3000/health/live

# Readiness Check (PostgreSQL Bağlantı Kontrolü)
curl http://localhost:3000/health/ready
```

### 5. Veritabanı Migration İşlemleri
Uygulama açılışta `system_migrations` tablosunu içeren veritabanı migration'larını otomatik olarak uygular. Yeni bir migration oluşturmak gerektiğinde:

```bash
# API konteyneri içinden migration ekleme:
docker compose exec api dotnet ef migrations add InitialCreate --project src/ImportControlTower.Infrastructure --startup-project src/ImportControlTower.Api
```

### 6. Canlı Log İzleme
```bash
# Tüm servislerin loglarını izleme
docker compose logs -f

# Yalnızca API logları
docker compose logs -f api

# Yalnızca PostgreSQL logları
docker compose logs -f db
```

### 7. Uygulamayı Durdurma ve Volume Koruma
```bash
# Servisleri durdur (Veritabanı verileri kalıcı volume içinde korunur)
docker compose down

# Sıfırlamak ve volume'leri silmek isterseniz:
docker compose down -v
```

---

## Klasör Yapısı

```text
import-control-tower/
├── apps/
│   ├── api/          # ASP.NET Core 10 Web API Monolith
│   └── web/          # React 19 + TypeScript Frontend Shell
├── tests/
│   ├── api-unit/     # Backend Unit Testleri
│   └── api-integration/ # Backend Integration Testleri
├── infra/            # Docker ve Script yapılandırmaları
├── docs/             # Dokümantasyon ve Coolify Dağıtım Rehberi
├── compose.yaml      # Üretim & Coolify Docker Compose
├── compose.override.yaml # Yerel geliştirme port haritalaması
├── .env.example      # Örnek ortam değişkenleri
├── OPEN_QUESTIONS.md # Faz 01 açık konuları
└── IMPLEMENTATION_REPORT.md # Faz 00 Doğrulama ve Uygulama Raporu
```
