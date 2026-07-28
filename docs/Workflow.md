# Uçtan Uca İş Akışı

## 1. Sipariş içe aktarma
Excel yüklenir, kolonlar doğrulanır, önizleme gösterilir, hatalı satırlar reddedilir, başarılı satırlar idempotent şekilde yazılır.

## 2. Satın alma hazırlığı
Sipariş teyidi alınır; üretim başlangıç/bitiş ve yüklemeye hazır olma tarihleri girilir.

## 3. Nakliye hazırlığı
Hazır olma tarihinden önce ithalat operasyona görev açılır. Forwarder teklifi ve booking oluşturulur.

## 4. Yükleme
Sevkiyat ve konteynerler açılır. Sipariş kalemleri miktar bazında konteynerlere atanır. Gerçek yükleme tarihi girilir.

## 5. Deniz yolu
ETD/ATD, ETA, aktarma ve varış olayları izlenir. MVP'de manuel, ileri fazda API ile otomatik.

## 6. Gümrük
Belge kontrolü, beyanname, kontrol ve çekim adımları takip edilir.

## 7. İç nakliye ve kabul
Liman çıkışı, fabrika varışı, depo kabulü, kalite ve ERP stok girişi kaydedilir.

## 8. Kapanış
Tüm zorunlu alanlar ve görevler tamamlanınca ithalat dosyası kapatılır.
