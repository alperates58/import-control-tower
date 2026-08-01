# Coolify PAAS Dağıtım Rehberi - Import Control Tower

Bu doküman, **Import Control Tower** uygulamasının Coolify PAAS ortamında Docker Compose mimarisi (`compose.coolify.yaml`) ile sıfır kesinti ve yüksek güvenlik standartlarında canlıya alınmasını anlatır.

---

## 1. Coolify Proje ve Compose Yapılandırması

Coolify panelinde yeni bir **Docker Compose** kaynağı ekleyin:
- **Repository**: `https://github.com/alperates58/import-control-tower` (`main` dalı)
- **Compose Dosya Yolu**: `/compose.coolify.yaml`
- **Domain Bağlantısı**: `https://import.alperates.com.tr` (Yalnızca `web` servisine bağlanır).
- **Host Portları**: Güvenlik gereği `db`, `minio` ve `api` servisleri için dışarıya (host) port **açılmaz**.
- **Traefik Etiketi**: `web` servisine `traefik.docker.network=coolify` etiketi eklenmiştir.
- **İç Ağ API Alias'ı**: Çakışmaları önlemek için `api` servisi iç ağda `import-control-tower-api-internal` adresiyle çözümlenir (`proxy_pass http://import-control-tower-api-internal:8080/api/`).

---

## 2. Sunucu Dizinleri ve İzin Yapılandırması (UID/GID)

Dağıtım öncesinde sunucu üzerinde kalıcı verilerin saklanacağı dizinleri oluşturun ve sahiplik izinlerini ayarlayın:

```bash
sudo mkdir -p /data/import-control-tower/db /data/import-control-tower/minio /data/import-control-tower/backups
sudo chown -R 70:70 /data/import-control-tower/db
sudo chown -R 1000:1000 /data/import-control-tower/minio
sudo chown -R root:root /data/import-control-tower/backups
sudo chmod 0700 /data/import-control-tower/db
sudo chmod 0750 /data/import-control-tower/minio /data/import-control-tower/backups
```

---

## 3. Zorunlu Ortam Değişkenleri (Environment Variables)

Coolify paneli -> **Environment Variables** bölümünde aşağıdaki değişkenleri üretim değerleriyle tanımlayın:

### Zorunlu Değişkenler (Varsayılanı Olmayan Değerler)
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `POSTGRES_DB`
- `JWT_SECRET` (En az 32 karakter uzunluğunda rastgele güçlü dize)
- `REFRESH_TOKEN_PEPPER` (Rastgele güçlü gizli dize)
- `SEED_ADMIN_EMAIL`
- `SEED_ADMIN_PASSWORD` (İlk giriş için güçlü yönetici parolası)
- `STORAGE_ACCESS_KEY` (MinIO admin kullanıcısı)
- `STORAGE_SECRET_KEY` (MinIO admin parolası)

### Opsiyonel / Varsayılan Bırakılabilen Değişkenler
- `ALLOWED_ORIGINS`: `https://import.alperates.com.tr`
- `SEED_ADMIN_FULL_NAME`: `Production System Administrator`
- `FINANCIAL_MODULE_ENABLED`: `false`
- `STORAGE_REGION`: `us-east-1`
- `STORAGE_BUCKET`: `import-control-tower-documents`
- `STORAGE_SIGNED_URL_LIFETIME_MINUTES`: `15`
- `STORAGE_MAX_UPLOAD_MB`: `25`

> [!CAUTION]
> Production ortamında `SEED_ADMIN_PASSWORD` varsayılan placeholder veya zayıf bir değer bırakılırsa API güvenlik kapısı devreye girer ve uygulama başlatılmayı reddeder.

---

## 4. DNS, SSL ve Sağlık Kontrolleri (Health Checks)

1. **Cloudflare / DNS Ayarı**:
   - `import.alperates.com.tr` A kaydını Coolify sunucu IP adresine yönlendirin.
   - SSL sertifikasının doğrulabilmesi için ilk kurulumda Cloudflare turuncu bulut (Proxy) durumunu geçici olarak **DNS-Only (Gri Bulut)** yapın.
2. **Healthcheck Yapısı**:
   - **`db`**: `pg_isready` (5s interval, 10 retries)
   - **`api`**: `curl -f http://localhost:8080/health/live` (10s interval, 30s start_period)
   - **`web`**: `wget --no-verbose --tries=1 --spider http://127.0.0.1:80/` (10s interval, 5 retries)

---

## 5. SSL, Cookie ve Nginx Konfigürasyonu

- **Production Cookie**: `__Host-ict_refresh_token` (`Secure=true`, `HttpOnly=true`, `SameSite=Strict`, `Path=/`)
- **Nginx `client_max_body_size`**: `30M` (Faz 04 25MB evrak yükleme desteği için)
- **Startup Migration Lock**: Dağıtım sırasında PostgreSQL Connection-Scoped Advisory Lock (`pg_try_advisory_lock(987654321)`) sayesinde veritabanı migration ve seed işlemleri paralel çakışma olmadan sırayla yürütülür.
