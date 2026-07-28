# Genel Kabul Kriterleri

- Yetkisiz kullanıcı finans alanlarını API dahil hiçbir yolla göremez.
- Aynı Excel dosyası tekrar yüklendiğinde mükerrer veri oluşmaz.
- Bir sipariş kalemi birden fazla sevkiyata bölünebilir.
- Bir sevkiyat birden fazla siparişten kalem içerebilir.
- Planlanan ve gerçekleşen tarihler ayrı saklanır.
- Her kritik değişiklik audit kaydı üretir.
- Yerel `docker compose up --build` tek komutla çalışır.
- Coolify GitHub push sonrası otomatik deploy edebilir.
- Health check, migration ve kalıcı PostgreSQL volume bulunur.
- Mobil, tablet ve masaüstü görünüm kullanılabilir olur.
