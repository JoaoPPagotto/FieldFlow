# FieldFlow

[![ci](https://github.com/JoaoPPagotto/FieldFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/JoaoPPagotto/FieldFlow/actions/workflows/ci.yml)
![.NET 9](https://img.shields.io/badge/.NET-9-512BD4)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

> Plataforma de gestão de ordens de serviço em campo (*field service management*):
> clientes, ativos, ordens de serviço com máquina de estados, despacho de técnicos,
> SLA em horário comercial e dashboard de produtividade.

🚧 **Em construção** — acompanhe o [roadmap](#roadmap).

## Stack

| Camada | Tecnologias |
|---|---|
| Backend | .NET 9, ASP.NET Core (Controllers + OData), EF Core 9 / PostgreSQL 16, MediatR (CQRS), FluentValidation, AutoMapper, Serilog |
| Testes | xUnit, FluentAssertions, NSubstitute, Testcontainers, NetArchTest |
| Frontend | React 19, TypeScript, Vite, TanStack Query, Tailwind + shadcn/ui *(Fase 8)* |
| Infra | Docker Compose, GitHub Actions |

## Como rodar

```bash
cp .env.example .env
docker compose up --build
# API:     http://localhost:8080/swagger
# Health:  http://localhost:8080/health/ready
```

Sem Docker (precisa de um Postgres local):

```bash
cd backend
dotnet run --project src/FieldFlow.Api
```

## Estrutura

```
backend/
├── src/
│   ├── FieldFlow.Domain/          # entidades, value objects, Result, regras — sem dependências
│   ├── FieldFlow.Application/     # CQRS (commands/queries), validators, behaviors, DTOs
│   ├── FieldFlow.Infrastructure/  # EF Core, repositórios, Unit of Work, seed
│   └── FieldFlow.Api/             # controllers OData, middleware, Swagger, health checks
└── tests/
    ├── FieldFlow.Domain.UnitTests/
    ├── FieldFlow.Application.UnitTests/
    └── FieldFlow.Api.IntegrationTests/   # Testcontainers + testes de arquitetura
```

Detalhes em [docs/architecture.md](docs/architecture.md).

## Roadmap

- [x] Fase 0 — Fundação (solução, Docker, CI, health checks, testes de arquitetura)
- [ ] Fase 1 — Domínio e persistência
- [ ] Fase 2 — Autenticação (JWT próprio)
- [ ] Fase 3 — Cadastros
- [ ] Fase 4 — Ordens de serviço
- [ ] Fase 5 — Agenda
- [ ] Fase 6 — Execução em campo
- [ ] Fase 7 — Dashboard
- [ ] Fase 8 — Frontend
- [ ] Fase 9 — Endurecimento
- [ ] Fase 10 — Publicação

## Licença

[MIT](LICENSE)
