# Arquitetura

## Camadas e dependências

```
Api ──► Application ──► Domain
 └──────► Infrastructure ──► Application, Domain
```

- **Domain** não referencia nada (nem EF Core, nem MediatR, nem `Microsoft.*`).
- **Application** define as abstrações (`IRepository<T>`, `IUnitOfWork`, `IDateTimeProvider`, `ICurrentUser`); **Infrastructure** implementa.
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
