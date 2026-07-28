# Coolify Dağıtım Rehberi (Faz 00)

Bu doküman, **Import Control Tower** projesinin Coolify PAAS platformu üzerinde Docker Compose build pack kullanılarak canlıya alınması adımlarını açıklar.

## 1. Coolify Proje Yapılandırması

1. Coolify paneline giriş yapın ve **New Project** oluşturun:
   - **Proje Adı:** `Import Control Tower`
   - **Environment:** `production`
2. **Add Resource** butonuna tıklayın ve **Private Repository (GitHub App)** seçeneğini işaretleyin.
3. Repozitörünüzü seçin ve Hedef Branch olarak `main` belirtin.

## 2. Build Pack Yapılandırması

1. **Build Pack:** `Docker Compose` seçin.
2. **Docker Compose Location:** `/compose.yaml` (Ana dizin).
3. **Base Directory:** `/`

## 3. Ortam Değişkenleri (Environment Variables & Secrets)

Coolify arayüzünden **Environment Variables** bölümüne aşağıdaki değerleri ekleyin:

```env
POSTGRES_USER=ict_prod_user
POSTGRES_PASSWORD=BURAYA_GÜVENLİ_RASTGELE_BİR_ŞİFRE_YAZIN
POSTGRES_DB=import_control_tower_prod
ASPNETCORE_ENVIRONMENT=Production
WEB_PORT=80
```

## 4. Kalıcı Veritabanı Volume (Persistent Storage)

`compose.yaml` içerisindeki `postgres_data` volume tanımı Coolify tarafından otomatik olarak kalıcı hale getirilir.
- Volume Mount Noktası: `/var/lib/postgresql`
- PGDATA: `/var/lib/postgresql/18/docker`

Coolify panelinden `db` servisinin kalıcı volume bağlantısını doğrulayın.

## 5. Domain ve SSL Tanımları

1. **Frontend (Web):**
   - Domain: `https://ict.sirketiniz.com` (ya da tanımlı domain)
   - Port: `80` (Coolify Nginx konteynerinin 80 portunu dış dünyaya bağlar)
2. **Same-Origin API Yapılandırması:**
   - Frontend Nginx reverse proxy yapılandırması gereği tüm `/api/` ve `/health/` istekleri iç ağdaki (`ict-network`) `api:8080` servisine yönlendirilir.

## 6. Health Checks & Zero-Downtime Deployment

`compose.yaml` içerisinde tanımlı olan health check kuralları Coolify tarafından otomatik olarak kullanılır:
- `db`: `pg_isready`
- `api`: `/health/live` (HTTP 200 OK)
- `web`: HTTP 200 OK

Coolify, yeni konteynerler `Healthy` durumuna geçmeden eski konteynerleri kapatmaz (Zero-downtime deployment).

## 7. Otomatik Otomatik Dağıtım (Auto-Deploy Webhook)

1. Coolify üzerinde **Auto Deploy** seçeneğini aktif edin.
2. GitHub Repository ayarlarına giderek Coolify Webhook URL'sini `push` olayları için tanımlayın. `main` branch'ine yapılan her push işlemi otomatik olarak Coolify üzerinde derlenecek ve canlıya alınacaktır.
