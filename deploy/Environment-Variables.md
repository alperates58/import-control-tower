# Environment Variables

```env
APP_ENV=Development
POSTGRES_DB=import_control_tower
POSTGRES_USER=ict_app
POSTGRES_PASSWORD=change-me
CONNECTION_STRING=Host=db;Port=5432;Database=import_control_tower;Username=ict_app;Password=change-me
JWT_ISSUER=import-control-tower
JWT_AUDIENCE=import-control-tower-web
JWT_SECRET=change-with-strong-secret
APP_BASE_URL=http://localhost:3000
API_BASE_URL=http://localhost:8080
CORS_ORIGINS=http://localhost:3000
TIME_ZONE=Europe/Istanbul
FINANCIAL_MODULE_ENABLED=false
STORAGE_PATH=/app/storage
```

Gerçek secret değerler repoya commit edilmez.
