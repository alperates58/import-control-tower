# Coolify Dağıtımı

## Önerilen yöntem

GitHub repository + Coolify Docker Compose build pack.

## Kurulum

1. Coolify'da yeni Project: `Import Control Tower`
2. Environment: `production`
3. New Resource > Private Repository (GitHub App)
4. Repository ve `main` branch seçimi
5. Build pack: Docker Compose
6. Compose location: `/compose.yaml`
7. Web ve API domainleri tanımlanır
8. PostgreSQL volume kalıcı hale getirilir
9. Environment variables/secrets Coolify'da girilir
10. Auto Deploy aktif edilir; GitHub push webhook'u doğrulanır
11. Health check başarılı olmadan eski container kapatılmaz

## Branch stratejisi

- `main`: production
- `develop`: isteğe bağlı staging
- Feature branch + pull request
