# Import Control Tower (İthalat Kontrol Kulesi)

Kurumsal ithalat ve tedarik zinciri operasyonlarının uçtan uca izlenebilirliğini, evrak takibini, finansal gizliliği ve rol bazlı erişim kontrolünü sağlayan monolitik web uygulaması.

---

## Teknoloji Yığını

- **Backend**: ASP.NET Core 10 Web API (Clean Modular Monolith mimarisi)
- **Identity & Security**: ASP.NET Core Identity, JWT Bearer, Peppered SHA-256 Refresh Token Rotation, `__Host-` Cookie, CSRF Middleware, Rate Limiting, Advisory Locking
- **Excel & Document Engine**: OpenXML v3.2.0 Security Scanner, ExcelDataReader v3.7.0 Forward-Only Streaming Engine
- **ORM & Database**: EF Core 10, Npgsql, PostgreSQL 18 (`postgres:18-alpine`), Shadow Property `xmin` Row Version Concurrency Mapping
- **Frontend**: React 19, TypeScript, Vite, Nginx Reverse Proxy
- **Orkestrasyon**: Docker Compose, Coolify PAAS

---

## Hızlı Başlangıç (Geliştirme Ortamı)

### 1. Ortam Değişkenlerini Hazırlama
```bash
cp .env.example .env
```

### 2. Docker Compose Servislerini Başlatma
```bash
docker compose build
docker compose up -d
```

### 3. Servis Durumlarını Kontrol Etme
```bash
docker compose ps
```

Erişim Adresleri:
- **Web Arayüzü**: http://localhost:3000
- **API Health Check**: http://localhost:3000/health/live
- **API System Info**: http://localhost:3000/api/v1/system/info

---

## Testleri Çalıştırma

Konteynerleştirilmiş SDK test çalıştırıcılarını kullanın:

```bash
# Backend Integration ve Unit Testleri (xUnit)
docker compose run --rm api-tests

# Frontend Tip ve Birim Testleri (TypeScript / React)
docker compose run --rm web-tests

# 20,000 Satırlık Yüksek Hacim Performans Benchmark Testi
docker compose run --rm api-tests dotnet test /src/tests/api-integration/ImportControlTower.Api.IntegrationTests.csproj --filter "FullyQualifiedName~BenchmarkTests"
```

---

## Fazlar ve Modüller

### Kimlik ve Yönetim (Faz 01)
Sistem varsayılan olarak **32 izinlik merkezi izin kataloğu** ve 7 varsayılan rol ile başlatılır:
- **SystemAdmin**: Tüm sistem izinleri
- **Management**: Üst yönetim izleme (Finans hariç)
- **Purchasing**: Satın alma operasyonları
- **ImportOperations**: İthalat ve evrak takibi
- **Planning**: Üretim ve ihtiyaç planlama
- **Finance**: Finansal modül yetkilisi
- **Viewer**: Salt okunur izleyici

Varsayılan Admin Hesabı (`.env` üzerinden değiştirilebilir):
- **E-Posta**: `admin@controltower.local`
- **Parola**: `AdminSecurePassword123!`

### Excel Sipariş İçe Aktarma (Faz 02)
- **OpenXML Güvenlik Tarama**: Formül hücre engelleme (`FORMULA_NOT_ALLOWED`), OLE/Gömülü nesne engelleme, harici bağlantı engelleme ve ZIP bomba tespiti.
- **ExcelDataReader Streaming**: Bellek verimliliği yüksek forward-only okuma motoru.
- **Otomatik Başlık Eşleme & Normalizasyon**: Türkçe/İngilizce alan takma adları, baştaki sıfırların korunması (`000123`), tarih format doğrulaması (`SAS Tarihi`).
- **Advisory Lock & Idempotent Confirmation**: Çakışan eşzamanlı yükleme ve onaylamalarda PostgreSQL connection-scoped advisory lock ve `import_confirmation_requests` idempotent yanıt takibi.
- **Sıfır Finansal Alan Garantisi**: Faz 02 veritabanı tabloları, DTO'lar, API endpoint'leri ve kullanıcı arayüzü finansal verilerden tamamen izole edilmiştir.

---

## Design System V2 (Frontend UI Bileşen Mimarisi)

Uygulama arayüzü Attio (veri yoğunluğu & çekmeceler), Linear (tipografi, navigasyon & hiyerarşi) ve Stripe (operasyonel açıklık & progressive disclosure) felsefelerinden esinlenerek tamamen özgün **Design System V2** standartlarına geçirilmiştir.

### Core Token Sistemi (`apps/web/src/index.css`)
- **Yüzey ve Arka Planlar**: Dark-first SaaS teması (`--bg-app: #090d16`, `--bg-surface: #0f172a`, `--bg-card: #141e33`, `--bg-input: #1a253c`).
- **Tipografi**: Inter (`--font-sans`) ve JetBrains Mono (`--font-mono`) tipografi hiyerarşisi.
- **Erişilebilirlik & Focus**: `:focus-visible` ile standart 2px accent mavi odaklama çerçevesi.
- **Responsive Layout**: 390px mobil görünümde tam ekran sidebar çekmecesi ve yatay taşmasız duyarlı tablo sarmalları (`.data-table-wrapper`).

### Yeniden Kullanılabilir UI Bileşen Kataloğu (`apps/web/src/components/ui/`)
- `<Button>` / `<IconButton>`: Varyantlar (`primary`, `secondary`, `danger`, `ghost`), `isLoading` durumları.
- `<Input>`, `<Select>`, `<Textarea>`, `<Checkbox>`, `<FormField>`: Standartlaştırılmış form bileşenleri ve label/error slotları.
- `<Badge>` / `<StatusBadge>`: Semantik durum rozetleri (`emerald`, `amber`, `rose`, `blue`, `cyan`, `purple`).
- `<Card>`, `<KPICard>`, `<Section>`, `<DetailField>`: Kartlar ve detay hücreleri.
- `<Modal>` / `<ConfirmDialog>`: Erişimli diyaloğlar, ESC tuşu kapama, focus trap.
- `<Drawer>`: Sağdan kayan detay ve versiyon çekmeceleri.
- `<DropdownMenu>`: `...` işlem menüleri.
- `<DataTable>` / `<Pagination>`: Hizada veri tabloları ve sayfalama barı.
- `<EmptyState>`, `<ErrorState>`, `<LoadingSkeleton>`: Geri bildirim ve yüklenme durumları.
- `<PageHeader>`, `<Tabs>`, `<ActivityTimeline>`: Sayfa başlıkları ve zaman çizelgeleri.

---

## Lisans

Tüm hakları saklıdır. Kurumsal özel mülk yazılımdır.

