# Secure Config API

[![CI](https://github.com/pierre3150/secure-config-api/actions/workflows/ci.yml/badge.svg)](https://github.com/pierre3150/secure-config-api/actions/workflows/ci.yml)

API REST en **.NET 8** pour la gestion centralisée de configurations applicatives, avec **chiffrement AES-256** des valeurs sensibles au repos (connection strings, clés API, secrets).

Ce projet est une version publique et simplifiée d'un système de gestion de configuration développé en environnement professionnel (WPF/MVVM + EF Core), réécrite ici en API REST pour démonstration.

## Stack technique

- **.NET 8** / ASP.NET Core Web API
- **Entity Framework Core 8** — SQLite en dev, PostgreSQL en production (Render)
- **AES-256-CBC** — chiffrement des valeurs, IV aléatoire par entrée
- **xUnit** — tests unitaires + tests d'intégration (`WebApplicationFactory`)
- **Docker** multi-stage build
- **GitHub Actions** — CI (build, tests, coverage, image Docker) sur chaque PR
- **Render** — déploiement continu + PostgreSQL managé

## Architecture

```
src/SecureConfigApi/
  Controllers/    -> Endpoints REST
  Services/       -> Logique métier + chiffrement
  Data/           -> DbContext EF Core
  Models/         -> Entités et DTOs
tests/SecureConfigApi.Tests/
  -> Tests unitaires (chiffrement, service) + tests d'intégration (endpoints)
```

## Endpoints

| Méthode | Route                 | Description                               |
|---------|------------------------|--------------------------------------------|
| GET     | `/api/configs`         | Liste les clés (métadonnées uniquement)    |
| GET     | `/api/configs/{key}`   | Récupère la valeur déchiffrée              |
| POST    | `/api/configs`         | Crée ou met à jour une entrée (chiffrée)   |
| DELETE  | `/api/configs/{key}`   | Supprime une entrée                        |
| GET     | `/health`               | Health check (utilisé par Render)          |

## Lancer en local

```bash
dotnet restore
dotnet run --project src/SecureConfigApi
```

Swagger disponible sur `/swagger` en environnement Development.

## Tests

```bash
dotnet test
```

## Workflow Git

- `main` — branche protégée, déploiement production, merge uniquement via PR avec CI verte
- `feature/*` — une branche par fonctionnalité, PR obligatoire vers `main`

## Déploiement

Déployé sur [Render](https://render.com) via `render.yaml` (Docker + PostgreSQL managé), déclenché automatiquement sur chaque merge vers `main`.
