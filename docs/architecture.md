# Arquitetura

## Camadas e dependências

```
Api ──► Application ──► Domain
 └──────► Infrastructure ──► Application, Domain
```

- **Domain** não referencia nada (nem EF Core, nem MediatR, nem `Microsoft.*`).
- **Domain** define os contratos de persistência (`IRepository<T>`, `IUnitOfWork`, `IWorkOrderRepository`...) e **Application** os de serviços (`IDateTimeProvider`, `ICurrentUser`, `IBusinessCalendarProvider`); **Infrastructure** implementa todos.
- As regras são verificadas por testes com **NetArchTest** (`tests/FieldFlow.Api.IntegrationTests/Architecture`).

## Padrões adotados

| Padrão | Onde | Observação |
|---|---|---|
| `BaseEntity` com auditoria (`created_*`, `updated_*`, `deleted_*`) | Domain/Common | Preenchida automaticamente por interceptor de `SaveChanges` |
| Soft delete global | `ApplicationDbContext` | Query filter aplicado por reflexão a toda entidade que herda `BaseEntity`; `DELETE` nunca é físico |
| CQRS com MediatR | Application | Um arquivo `{Entity}Command.cs` / `{Entity}Query.cs` por agregado |
| `ValidationBehavior` | Application/Behaviors | FluentValidation roda antes de todo handler |
| Repository + Unit of Work | Domain (interfaces) / Infrastructure | Repositório específico por agregado estende o genérico |
| `Result<T>` no domínio | Domain/Common | Regra de negócio **não** lança exceção |
| `AppException` na borda | Application/Errors | Handlers traduzem `Result` falho → `AppException` com código |
| Problem Details (RFC 7807) | Api/Middleware | Toda exceção vira `application/problem+json` com `code` e `errors` |
| Controllers + OData | Api | `GET` de coleção devolve `IQueryable<TEntity>` para o `$filter/$orderby/$top` compor no banco |
| Mensagens pt-BR / códigos SCREAMING_SNAKE_CASE | `ValidationMessages` / `ErrorCodes` | Nunca literal solto em validator |

## Banco de dados

- PostgreSQL, schema `fieldflow`, nomes em `snake_case` (`EFCore.NamingConventions`).
- Tabelas e PKs nomeadas explicitamente na configuração Fluent de cada entidade (`IEntityTypeConfiguration<T>`).
- O `ApplicationDbContext` é a **fonte única de verdade** do mapeamento — nada de `[Table]`/`[Column]` nas entidades.
- `Program` aplica as migrations no boot; a ausência de banco **não** impede a subida (o `/health/ready` reporta).

## Migrations

Enquanto não existir migration versionada, o boot cria o schema com `EnsureCreated` (aviso no log).
Para versionar o schema (recomendado antes da Fase 2):

```bash
./scripts/add-migration.sh Initial        # ou .\scripts\add-migration.ps1 Initial
docker compose down -v                    # descarta o banco criado via EnsureCreated
docker compose up --build                 # agora o boot aplica as migrations
```

## Diferenças em relação ao template de Web API de referência

A estrutura segue um template de Web API de referência (Clean Architecture + CQRS + Repository/UoW + OData),
com adaptações deliberadas para este projeto:

| Template | FieldFlow | Motivo |
|---|---|---|
| Projeto único com pastas `src/Core`, `src/Infrastructure`, `src/Presentation` | 4 projetos (`Domain`, `Application`, `Infrastructure`, `Api`) | Regras de dependência verificáveis por NetArchTest |
| Interfaces de repositório na Infrastructure | Interfaces no Domain | Application não pode depender de Infrastructure |
| Regras lançam `AppException` | Domínio devolve `Result`; `AppException` só na borda | Máquina de estados testável sem exceções (ADR 0002) |
| `ApiErrorResponse` próprio | Problem Details (RFC 7807) com `code`/`errors` | Padrão de mercado; o front mapeia `errors` nos campos |
| Keycloak + tabelas de permissão em SQL Server | JWT próprio (Fase 2) | Demo sobe com `docker compose up` (ADR 0003) |
| `SiteId` multi-tenant e header `module` | Removidos | Fora do escopo do produto (multi-tenant é "próximo passo") |
| `HasColumnName` explícito por coluna | `UseSnakeCaseNamingConvention()` + tabelas/PKs nomeadas | Mesmo resultado no banco, menos ruído |
| `DELETE` chama `MarkDeleted` no repositório | Interceptor converte todo `Remove` em soft delete | Impossível fazer hard delete por engano |
