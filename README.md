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
| Users | Profiles, roles and business permissions (identity is held by Keycloak) |
| Catalog | Reference products (GTIN, brand, manufacturer reference) and product typologies (3-level tree) |
| Pricing | Merchants, collection profiles per typology, offers (product × merchant), price observations, collection |
| AiEngine | LLM model catalog and prices, target model per AI feature, LLM call journal and costs, usage statistics |

Agents live in `src/Agents` (not a module): `Pricely.Agents.Core` (tool loop, conversations, approvals) and one project per agent (`Pricely.Agents.Assistant`).

### AI and MCP

- Every LLM call goes through `ILlmGateway` (Common) for a declared `AiFeature`; providers are registered once (Anthropic: `dotnet user-secrets set "Ai:Anthropic:ApiKey" "<key>" --project src/API/Pricely.Api`).
- MCP server over streamable HTTP on `/mcp` (authenticated) with read-only tools (write tools are only given to the internal agent, behind approval): `get_category_tree`, `search_products`, `get_product`, `list_merchants`, `compare_prices`, `get_price_history`, `list_price_anomalies`, `get_collection_plan`, `list_offers_pending_review`.

### Pricing in a nutshell

- **Offer** = product × merchant. Creating an offer *is* the matching. GTIN or brand + manufacturer reference confirm it automatically; anything else waits for a human review.
- **Collection profile** per typology: preferred merchants (nearest typology wins) and collection instructions (concatenated from the root typology down).
- **Price observation**: history kept outside the offer. A price that deviates more than 40 % from the median of the other confirmed offers is flagged.
- **Collection** goes through the `IOfferCollector` port (AI agent, scrapers, partner feeds). The default implementation fails explicitly until a real collector is plugged in. The periodic job is disabled by default (`Pricing:Collection:Enabled`).
- Catalog data is never read directly: Pricing keeps local read models fed by Catalog integration events (inbox).

## Run locally

Requires the .NET 10 SDK and a container runtime (Docker or Podman).

```bash
dotnet user-secrets set "Parameters:anthropic-api-key" "<key>" --project aspire/Pricely.AppHost   # once, optional: without it only the LLM calls fail
dotnet run --project aspire/Pricely.AppHost
```

The AppHost starts PostgreSQL (`pricely-db`), Keycloak (http://localhost:8080, realm `pricely` imported from `aspire/Pricely.AppHost/Realms/pricely-realm.json`) and the API (http://localhost:5136). In Development the API applies the EF Core migrations of every module at startup. The Keycloak admin credentials are the generated Aspire parameters (dashboard).

## Authentication

Pricely validates the JWT access tokens of Keycloak (ADR-010); it never issues tokens. Scopes: `pricely.read`, `pricely.write`, `pricely.review`. The first authenticated request of an identity creates its `User` (role `Member`).

| Realm client | Use |
|---|---|
| `pricely-web` | Public client with PKCE for the future front |
| `pricely-mcp` | Pre-registered public client for MCP clients (dynamic registration is also enabled on the realm) |
| `pricely-dev` | Local development only: password grant to get a token from a script |

The development user is `dev` / `dev` (`dev@pricely.local`).

### Get a token

```bash
TOKEN=$(curl -s http://localhost:8080/realms/pricely/protocol/openid-connect/token \
  -d grant_type=password -d client_id=pricely-dev -d username=dev -d password=dev \
  -d "scope=openid pricely.read" | jq -r .access_token)

curl -i http://localhost:5136/api/catalog/categories                            # 401
curl -i http://localhost:5136/api/catalog/categories -H "Authorization: Bearer $TOKEN"   # 200
```

`src/API/Pricely.Api/Pricely.Api.http` contains the same requests for the IDE. Tokens last 15 minutes.

Scopes: reads need `pricely.read`, writes `pricely.write`, offer confirmation and rejection `pricely.review`. Ask for them in the token request, e.g. `-d "scope=openid pricely.read pricely.write pricely.review"`.

### Talk to the assistant

```bash
curl -N http://localhost:5136/api/agents/assistant/messages -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" -d '{"message":"Compare les prix du ciment"}'
```

The answer is a stream of server-sent events (`conversation`, `tool_call`, `tool_result`, `action_proposed`, `text`, `done`). A write tool (for example `collect_product_offers`) is never run by the model: approve it with `POST /api/agents/actions/{id}/approve` (token with `pricely.write`) or reject it. Conversations: `GET /api/agents/conversations`. Needs the Anthropic key.

### Use the MCP server from Claude Code

`/mcp` is a streamable HTTP MCP server protected by the `pricely.read` scope. It publishes its OAuth metadata on `/.well-known/oauth-protected-resource` and Keycloak registers clients dynamically.

```bash
claude mcp add --transport http pricely http://localhost:5136/mcp
```

Then run `/mcp` in Claude Code, select `pricely` and authenticate: the browser opens the Keycloak login (`dev` / `dev`). If the client cannot register dynamically, use the pre-registered client:

```bash
claude mcp add --transport http --client-id pricely-mcp --callback-port 8765 pricely http://localhost:5136/mcp
```

Without the OAuth flow (scripts, tests), pass a token: `claude mcp add --transport http pricely http://localhost:5136/mcp --header "Authorization: Bearer $TOKEN"`.

## Commands

```bash
dotnet build
dotnet test
dotnet ef migrations add <Name> -p src/Modules/<M>/Pricely.Modules.<M>.Infrastructure -s src/API/Pricely.Api -c <M>DbContext -o Database/Migrations
```

`dotnet ef` builds the API host: set `ConnectionStrings__pricely-db` (any PostgreSQL connection string, e.g. `Host=localhost;Database=pricely-db;Username=postgres;Password=x`; nothing connects when adding a migration).

## Working with Claude Code

- `CLAUDE.md`: architecture rules, conventions, analyzer pitfalls, Pricing business rules, known gaps.
- `.claude/skills/`: `new-module`, `new-aggregate`, `new-use-case`, `new-integration-event`, `add-migration`, `add-offer-collector`, `add-agent-tool`.
- `.claude/agents/architecture-reviewer.md`: review sub-agent to run on every diff.
- `docs/architecture/`: ubiquitous language, decisions (ADR), model (Mermaid).
