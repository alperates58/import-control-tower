# Coolify PAAS Dağıtım Rehberi - Import Control Tower

Bu doküman, **Import Control Tower** uygulamasının Coolify PAAS ortamında Docker Compose mimarisi ile sıfır kesinti ve yüksek güvenlik standartlarında canlıya alınmasını anlatır.

---

## 1. Coolify Proje Yapılandırması

Coolify arayüzünde yeni bir **Docker Compose** kaynağı ekleyin ve GitHub repository bağlantısını kurun (`main` dalı).

### Zorunlu Ortam Değişkenleri (Environment Variables)

Coolify panelinde aşağıdaki ortam değişkenlerini üretim ortamı değerleriyle tanımlayın:

```env
POSTGRES_USER=ict_prod_user
POSTGRES_PASSWORD=STRONG_PRODUCTION_POSTGRES_PASSWORD_HERE
POSTGRES_DB=import_control_tower_prod

JWT_SECRET=STRONG_PRODUCTION_JWT_SECRET_AT_LEAST_32_BYTES_LONG
REFRESH_TOKEN_PEPPER=STRONG_PRODUCTION_PEPPER_KEY_HERE

SEED_ADMIN_EMAIL=admin@yourdomain.com
SEED_ADMIN_PASSWORD=STRONG_PRODUCTION_INITIAL_ADMIN_PASSWORD
SEED_ADMIN_FULL_NAME=Production System Administrator

ALLOWED_ORIGINS=https://import.yourdomain.com
FINANCIAL_MODULE_ENABLED=false
ASPNETCORE_ENVIRONMENT=Production
```

> [!CAUTION]
> Production ortamında `SEED_ADMIN_PASSWORD` varsayılan placeholder parolada bırakılırsa API güvenlik kapısı devreye girer ve uygulama başlatılmayı reddeder.

---

## 2. SSL, HTTPS ve Production Cookie Ayarları

Production ortamında Coolify ters vekili (Traefik / Nginx) HTTPS sonlandırmasını sağlar.

- Production cookie adı: `__Host-ict_refresh_token`
- Cookie Özellikleri: `Secure=true`, `HttpOnly=true`, `SameSite=Strict`, `Path=/`, `Domain=undefined`
- `ALLOWED_ORIGINS` değişkenine canlı domain adresi tam olarak girilmelidir (`https://import.yourdomain.com`).

---

## 3. Excel Yükleme Sınırları & Nginx Konfigürasyonu (Faz 02)

Phase 02 toplu Excel sipariş yükleme işlemleri için Nginx ve ASP.NET Core gövde boyut sınırları yapılandırılmıştır:

- **Nginx `client_max_body_size`**: `15M` (15 Megabayt)
- **ASP.NET Core `FormOptions.MultipartBodyLengthLimit`**: `15MB`
- **Rate Limiter (`upload-policy`)**: Kullanıcı bazlı dakikada 5 yükleme isteği (`429 Too Many Requests`).
- **Advisory Lock & Idempotency**: Aynı dosyanın ve onay isteğinin eşzamanlı işlenmesi PostgreSQL `pg_try_advisory_xact_lock` ve `import_confirmation_requests` ile engellenir.

---

## 4. Veritabanı Persistence & Startup Migration Lock

- PostgreSQL veritabanı verileri `/var/lib/postgresql` mount noktasında `ict_postgres_data` volume'ünde saklanır.
- Dağıtım sırasında birden fazla API örneği kalkarsa PostgreSQL Connection-Scoped Advisory Lock (`pg_try_advisory_lock(987654321)`) sayesinde veritabanı migration ve seed işlemleri paralel çakışma olmadan sırayla yürütülür.

---

## 5. Canlılık ve Hazırlık Kontrolleri (Health Checks)

Coolify sağlık kontrolleri için aşağıdaki endpointleri kullanır:
- **Liveness Probe**: `http://localhost:3000/health/live` (200 OK)
- **Readiness Probe**: `http://localhost:3000/health/ready` (Veritabanı bağlantısı doğrulanmış 200 OK)
