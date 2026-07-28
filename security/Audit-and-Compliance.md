# Audit ve Veri Güvenliği

- Kim, neyi, ne zaman, hangi eski/yeni değerle değiştirdi?
- Login, yetki, import, belge, durum, tarih ve sorumlu değişiklikleri loglanır.
- Audit kayıtları normal kullanıcı tarafından silinemez.
- Dosya uzantısı/MIME/boyut kontrolü yapılır.
- Secret değerler yalnızca environment variables/Coolify secrets içinde tutulur.
- Veritabanı günlük yedekleme ve restore testi deployment fazında zorunludur.
