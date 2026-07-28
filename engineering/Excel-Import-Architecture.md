# Excel Import Mimarisi

1. Dosya upload edilir ve geçici alana alınır.
2. Sheet/kolon eşleme yapılır.
3. Satırlar normalize edilir.
4. Validation sonucu önizleme olarak saklanır.
5. Kullanıcı onayı sonrası transaction ile import yapılır.
6. ImportBatch ve satır sonuçları kaydedilir.
7. Tekrar yüklemede fingerprint ve satır anahtarlarıyla mükerrerlik önlenir.
8. Hatalar indirilebilir CSV/XLSX raporu olarak sunulur.

MVP için ClosedXML kullanılabilir.
