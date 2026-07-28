# Açık Konular ve İlerleme Notları (Faz 00)

## Faz 01 Öncesi Kararlaştırılacak Konular

1. **Authentication ve Kullanıcı Kimlik Doğrulama Stratejisi:**
   - Faz 01'de ASP.NET Core Identity + JWT / Refresh Token mimarisi kurulacaktır.
   - Şirket içi kullanıcı rollerinin (`admin`, `purchasing`, `import_specialist`, `finance`, `management`) yetkilendirme şeması kesinleştirilecektir.

2. **Finansal Gizlilik Yetki Alanı (`financial.view` & `financial.edit`):**
   - Finansal veri gizliliği politikasına göre (`security/Financial-Privacy.md`), `financial.view` yetkisi olmayan kullanıcılar DTO seviyesinde serileştirilen alanları göremeyecektir. Faz 01'de bu izin mekanizmasının Action Filter / Claims-based yetkilendirme olarak API seviyesinde uygulanması kararlaştırılacaktır.

3. **Veritabanı Migration Çalıştırma Stratejisi:**
   - Faz 00'da API başlangıcında `Database.MigrateAsync()` kullanılmıştır. Çoklu instance (horizontal scaling) ortamlarında migration'ların startup yerine dedicated init-container veya CI/CD pipeline adımında çalıştırılması değerlendirilebilir.
