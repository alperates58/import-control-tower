# Fiyat ve Finans Gizliliği

## Varsayılan politika

- Fiyat kolonları Excel'de bulunabilir ama zorunlu değildir.
- `financial.view` izni olmayan kullanıcıya fiyat alanları serialize edilmez.
- Finans alanları tablo kolon seçicisinde bile görünmez.
- Export işlemleri aynı yetki kurallarına uyar.
- Audit log finansal değerleri maskeli veya özel erişimli tutar.
- Feature flag ile finans modülü tenant/sistem genelinde kapatılabilir.

## Karar noktası

Patron fiyat bilgisini istemezse MVP kurulumu `FinancialModuleEnabled=false` ile yapılır.
