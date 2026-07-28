# PROMPT 01 — FOUNDATION

Aşağıdaki promptu Gemini 3.6 Flash coding agent'a proje klasörünün kökünde ver.

---

Sen kıdemli bir full-stack yazılım mimarı ve uygulama geliştiricisisin. Bu repoda sıfırdan **Import Control Tower** adlı kurumsal ithalat operasyon yönetim sisteminin yalnızca **Faz 00 — Temel Altyapı** bölümünü kuracaksın.

## İş hedefi

Sistem ileride yurt dışı satın alma siparişlerini Excel/Mikro ERP'den alacak; satın alma üretim sürecini, ithalat operasyonu sevkiyat/konteyner/gemi/gümrük sürecini yönetecek. Bu promptta iş modüllerini uygulama. Sadece güvenilir, genişlemeye hazır ve hem yerelde Docker hem Coolify üzerinde çalışacak teknik temel oluştur.

## Zorunlu teknoloji

- Monorepo
- Frontend: React 19.2, TypeScript, Vite
- Backend: ASP.NET Core 10 Web API
- Veritabanı: PostgreSQL 18
- EF Core 10 migrations
- Docker Compose
- Production frontend serving: Nginx
- OpenAPI
- Serilog structured logging
- Health checks
- UTC persistence, Europe/Istanbul presentation hazırlığı

## Hedef repo yapısı

```text
apps/
  api/
    src/ImportControlTower.Api
    src/ImportControlTower.Application
    src/ImportControlTower.Domain
    src/ImportControlTower.Infrastructure
  web/
tests/
  api-unit/
  api-integration/
  web-e2e/
infra/
  docker/
  scripts/
docs/
compose.yaml
compose.override.yaml
.env.example
README.md
```

## Yapılacaklar

1. Önce mevcut repo içeriğini incele. Var olan dosyaları sebepsiz silme.
2. Yukarıdaki monorepo iskeletini oluştur.
3. Backend solution ve proje referanslarını clean/modular monolith yapısına göre kur.
4. API'de:
   - `GET /api/v1/system/info`
   - `GET /health/live`
   - `GET /health/ready`
   endpointlerini oluştur.
5. PostgreSQL bağlantısını kur. İlk migration içinde yalnızca teknik bir `system_migrations` veya eşdeğer temel tablo kullan; iş domain tablolarını henüz oluşturma.
6. Migration uygulamasını güvenli ve tekrar çalışabilir biçimde tasarla. Birden fazla instance yarışına karşı not bırak.
7. Frontend'de premium kurumsal bir uygulama shell'i oluştur:
   - sol navigasyon placeholder
   - üst bar
   - “Import Control Tower” başlığı
   - API bağlantı durumunu gösteren sistem durumu kartı
   - responsive yapı
   İş ekranlarını henüz yapma.
8. TypeScript strict mode, ESLint ve formatlama ayarlarını ekle.
9. Backend nullable, analyzers ve warnings-as-errors yaklaşımını makul şekilde yapılandır.
10. Multi-stage production Dockerfile'lar oluştur.
11. `compose.yaml` üretim/Coolify uyumlu tek kaynak olsun:
    - web
    - api
    - db
    - kalıcı PostgreSQL volume
    - service health checks
    - internal network
    - environment variables
12. `compose.override.yaml` yerel geliştirme kolaylıklarını içersin.
13. `.env.example` oluştur; gerçek secret yazma.
14. Root README'ye şu komutları eksiksiz yaz:
    - local başlatma
    - build
    - test
    - migration oluşturma/uygulama
    - log izleme
    - kapatma ve volume koruma
15. GitHub Actions workflow ekle:
    - backend restore/build/test
    - frontend install/lint/build
    - Docker Compose config validation veya image build smoke test
16. Coolify dağıtımı için `docs/COOLIFY_DEPLOYMENT.md` oluştur:
    - yeni Project oluşturma
    - GitHub App ile private repository bağlama
    - main branch
    - Docker Compose build pack
    - compose path
    - environment/secrets
    - domain ve health check
    - auto deploy webhook
    - persistent storage
17. `.gitignore`, `.dockerignore`, editorconfig ve temel güvenlik ayarlarını ekle.
18. Basit unit/integration test altyapısı kur ve health/system endpointleri için en az bir çalışan test yaz.

## Kesin sınırlar

- Login/auth uygulama; Faz 01'e bırak.
- Excel import uygulama; Faz 02'ye bırak.
- Sipariş, sevkiyat, konteyner ve finans tabloları oluşturma.
- Kubernetes, mikroservis, message broker veya gereksiz karmaşıklık ekleme.
- Secret commit etme.
- Placeholder kodu production özelliği gibi sunma.
- Docker dışındaki yerel bağımlılıkları mümkün olduğunca azalt.

## Kalite kapısı

Tamamlandı demeden önce gerçekten çalıştır:

```bash
docker compose config
docker compose build
docker compose up -d
docker compose ps
curl -f http://localhost:8080/health/ready
curl -f http://localhost:3000
# uygun backend ve frontend test komutları
docker compose down
```

Port çakışması veya ortam farkı varsa çöz ve README'ye yaz. Windows Docker Desktop uyumluluğunu koru.

## Çıktı biçimi

İş bittikten sonra:

1. Değişen/oluşan dosyaları özetle.
2. Çalıştırdığın komutları ve gerçek sonuçlarını yaz.
3. Açık kalan konuları `OPEN_QUESTIONS.md` içine kaydet.
4. `IMPLEMENTATION_REPORT.md` oluştur.
5. Sonraki faza geçme.

Şimdi önce kısa bir uygulama planı çıkar, ardından beklemeden Faz 00'ı uygula.

---
