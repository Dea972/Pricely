# Pricely

Price comparison and price monitoring platform, built as a .NET modular monolith (Clean Architecture, DDD, CQRS).

## Structure

```
aspire/                 .NET Aspire AppHost and ServiceDefaults
src/
  API/Pricely.Api       HTTP host
  Adapters/Pricely.Sdk  Shared composition helpers
  Common/               Shared Domain / Application / Infrastructure / Presentation building blocks
  Modules/<Module>/     One Clean Architecture slice per module (Domain, Application, Infrastructure, Presentation, IntegrationEvents)
test/                   Solution-level architecture and integration tests
```

## Modules

| Module | Role |
|---|---|
| Users | Accounts, roles, permissions, authentication |

## Commands

```bash
dotnet build
dotnet test
```
