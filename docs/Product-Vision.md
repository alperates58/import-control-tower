# Ürün Vizyonu

Import Control Tower, bir “gemi nerede?” uygulaması değildir. İthalat siparişinin satın alma teyidinden fabrika stok girişine kadar tüm iş akışını görünür, ölçülebilir ve denetlenebilir hale getirir.

## Başarı tanımı

Bir kullanıcı herhangi bir siparişi açtığında şu soruların yanıtını 30 saniye içinde görebilmelidir:

- Sipariş hangi aşamada?
- Son işlemi kim, ne zaman yaptı?
- Üretim başladı mı ve ne zaman bitecek?
- Yüklemeye hazır olma tarihi nedir?
- Forwarder, booking, konteyner, gemi, ETD ve ETA bilgileri nedir?
- Güncel ETA değişti mi?
- Eksik belge veya geciken görev var mı?
- Ürün hangi konteyner/sevkiyat içinde?
- Tahmini fabrika teslim tarihi nedir?
- Bu gecikme üretimi etkiler mi? (ileri faz)

## Ürün ilkeleri

1. Excel sadece giriş kanalıdır; ana çalışma ortamı değildir.
2. Tek durum yerine ticari, lojistik ve gümrük süreçleri ayrı izlenir.
3. Planlanan ve gerçekleşen tarihler ayrı tutulur.
4. Her değişiklik audit kaydı üretir.
5. Rol ve alan bazlı yetki zorunludur.
6. Fiyat bilgileri varsayılan olarak gizlidir.
7. Otomatik takip bulunmasa bile manuel süreç kusursuz çalışmalıdır.
8. Sistem konteyner, sevkiyat ve sipariş ilişkilerini doğru modellemelidir.
