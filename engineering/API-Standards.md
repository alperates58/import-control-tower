# API Standartları

- Base path: `/api/v1`
- JSON camelCase
- Pagination: cursor veya page/pageSize; büyük listelerde server-side
- Filtering/sorting whitelist ile
- Hatalar RFC ProblemDetails
- Correlation ID her istekte
- Idempotency-Key Excel import ve kritik create işlemlerinde
- OpenAPI her build'de üretilir
- Finans alanları DTO seviyesinde permission-aware projection ile çıkarılır
- Audit sadece UI değil servis katmanında oluşturulur
