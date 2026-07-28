# Excel Import Spesifikasyonu

## İlk şablon kolonları

| Kolon | Zorunlu | Tip | Açıklama |
|---|---|---|---|
| Sipariş No | Evet | Metin | ERP sipariş numarası |
| Firma Adı | Evet | Metin | Tedarikçi |
| Sipariş Tarihi | Evet | Tarih | Sipariş açılış tarihi |
| Stok Kodu | Evet | Metin | Benzersiz stok kodu |
| Stok İsmi | Evet | Metin | Stok açıklaması |
| Sipariş Miktar | Evet | Ondalık | Sipariş miktarı |
| Sipariş Kalan Miktar | Evet | Ondalık | Açık miktar |
| SAS Tarihi | Hayır | Tarih | ERP kaynaklı tarih |
| Birim | Hayır | Metin | Adet, kg vb. |
| Firma Kodu | Hayır | Metin | ERP firma kodu |
| Para Birimi | Hayır | Metin | Finans yetkisi ile görünür |
| Birim Fiyat | Hayır | Ondalık | Finans yetkisi ile görünür |

## Doğrulamalar

- Sipariş No + Stok Kodu + kaynak satır anahtarı idempotency temeli olur.
- Miktarlar negatif olamaz.
- Kalan miktar sipariş miktarını aşamaz.
- Tarihler tr-TR ve ISO biçimlerinde parse edilebilir.
- Hatalı satırlar Excel satır numarasıyla raporlanır.
- Kullanıcı import öncesi önizleme ve özet görür.
