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
| Catalog | Reference products (GTIN, brand, manufacturer reference) and product typologies (3-level tree) |
| Pricing | Merchants, collection profiles per typology, offers (product × merchant), price observations, collection |

### Pricing in a nutshell

- **Offer** = product × merchant. Creating an offer *is* the matching. GTIN or brand + manufacturer reference confirm it automatically; anything else waits for a human review.
- **Collection profile** per typology: preferred merchants (nearest typology wins) and collection instructions (concatenated from the root typology down).
- **Price observation**: history kept outside the offer. A price that deviates more than 40 % from the median of the other confirmed offers is flagged.
- **Collection** goes through the `IOfferCollector` port (AI agent, scrapers, partner feeds). The default implementation fails explicitly until a real collector is plugged in. The periodic job is disabled by default (`Pricing:Collection:Enabled`).
- Catalog data is never read directly: Pricing keeps local read models fed by Catalog integration events (inbox).

## Commands

```bash
dotnet build
dotnet test
```
