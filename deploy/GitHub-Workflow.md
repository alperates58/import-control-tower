# GitHub ve Deployment Akışı

1. Geliştirici feature branch açar.
2. Kod ve testler tamamlanır.
3. Pull request açılır.
4. CI lint, test ve container build yapar.
5. PR main'e merge edilir.
6. Coolify GitHub webhook ile otomatik deploy başlatır.
7. Health check geçerse release aktif olur.
8. Migration kontrollü şekilde startup veya ayrı migration job ile uygulanır.

Coolify deployment'ın tek kaynak doğrusu GitHub `main` branch olmalıdır; sunucu üzerinde elle kod değiştirilmez.
