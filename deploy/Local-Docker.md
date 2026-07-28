# Yerel Docker Çalışma Modeli

Tek komut:

```bash
docker compose up --build
```

Servisler:

- `web`: Vite dev veya production Nginx profili
- `api`: ASP.NET Core
- `db`: PostgreSQL
- İsteğe bağlı `mailpit`: geliştirme e-postaları

Kalıcı volume, health check ve `.env.example` zorunludur. Windows Docker Desktop ile uyumlu olmalıdır.
