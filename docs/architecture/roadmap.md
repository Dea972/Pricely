# Feuille de route : IA, agent et MCP

Chaque étape fait l'objet d'une PR sur `main`, CI verte, revue `architecture-reviewer`.

## Étape 0 — L'API démarre — fait (ADR-011)

- `Program.cs` : `AddPricelyAsync(connectionString, configuration)`, `MapPricelyEndpoints()`, OpenAPI en développement.
- Module Users : réactiver l'enregistrement de `UsersDbContext`, corriger le SQL PostgreSQL (`::json`) de son inbox.
- Migrations initiales Catalog, Pricing, Users (skill `add-migration`), base locale via l'AppHost Aspire.
- Authentification (ADR-010) : Keycloak dans l'AppHost (realm `pricely` importé au démarrage, scopes `pricely.read|write|review`, client du front), validation JWT Keycloak dans l'API, rattachement `sub` → `User`, suppression de `JwtTokenService` / `GenerateAccessToken` / `Jwt:Secret`.
- Critère : l'AppHost démarre (SQL Server + Keycloak + API) ; `/api/catalog/categories` renvoie 401 sans jeton et répond avec un jeton Keycloak.
- Fait : AppHost avec Keycloak et realm importé, validation JWT, politiques par scope, `sub` → `User` à la première requête, migrations `Initial` des quatre modules appliquées par l'API en Development, `/mcp` en `pricely.read` avec sa métadonnée OAuth. À vérifier avec un démon de conteneurs : l'AppHost complet (SQL Server, application des migrations, `/api/catalog/categories` en 200) n'a pas été exécuté faute de Docker ; le realm (import, jeton, enregistrement dynamique) et l'API sans base l'ont été (401, métadonnée, `tools/list` avec jeton, 403 sans `pricely.read`).

## Étape 1 — Socle Common AI (ADR-007) — fait

- `Common.Application.AI` : `AiFeature`, `AiProviderKeys`, `LlmTarget`, `ILlmTargetResolver`, `ILlmGateway`, `LlmClient`, `LlmServiceBase`, `LlmRequest` / `LlmResponse` / `LlmTokenUsage`, `LlmCalledIntegrationEvent`, `AddAiFeature`.
- `Common.Infrastructure.Ai` : `LlmGateway`, `DefaultLlmTargetResolver`, `AddAiProvidersAsync` ; fournisseur Anthropic (options, `HttpClient` authentifié, secrets en user-secrets).
- Horodatage UTC. Tests unitaires de la passerelle avec un faux fournisseur.

## Étape 2 — Module AiEngine — fait (sans agents hébergés ni recherche web des tarifs)

- Skill `new-module`. Agrégats `Model` (+ `ModelPrice` datés), `FeatureSetting`, `Agent`, `Session`, `LlmCall`.
- Consommation de `LlmCalledIntegrationEvent` (inbox) → `LlmCall` avec coût figé ; `LlmTargetResolver` qui remplace le résolveur par défaut.
- Endpoints : modèles et tarifs, cibles des fonctionnalités, agents, statistiques (synthèse, par jour, par modèle, par fonctionnalité).
- La recherche des tarifs sur le web est reportée.

## Étape 3 — Tools et MCP HTTP (ADR-009) — fait, sauf le filtrage des tools par scope (`/mcp` exige `pricely.read`)

- Abstraction de tool dans `Common.Presentation` (lecture / écriture, approbation requise) ; découverte par assembly, instanciation par DI.
- Tools de consultation : `search_products`, `get_product`, `get_category_tree`, `compare_prices`, `get_price_history`, `list_merchants`, `list_price_anomalies`, `get_collection_plan`.
- Serveur MCP en transport HTTP sur `/mcp`, protégé par les jetons Keycloak (ADR-010) : métadonnée `/.well-known/oauth-protected-resource`, scopes par tool ; remplace l'amorce stdio du template.
- Critère : interroger Pricely depuis Claude Desktop / Claude Code.

## Étape 4 — Agent interne (ADR-008)

- Passerelle : `IChatClient` par fonctionnalité, `FunctionInvokingChatClient`, journalisation `LlmCalled` conservée.
- `AgentConversation` (messages, appels d'outils, résultats), actions en attente pour les tools soumis à approbation.
- Fonctionnalité `pricing.assistant` ; endpoint de conversation en streaming.

## Étape 5 — Front Blazor

- Chat en streaming ; composants de réponse typés (comparatif de prix, offres à valider, plan de collecte, anomalies, historique) ; boutons d'action qui appellent directement les commandes ; validation des actions en attente.

## Étape 6 — Collecte par l'agent

- Tools `fetch_merchant_page` (domaine de l'enseigne uniquement, instantané conservé), `submit_collected_offer` (exige un instantané, prix vérifié dans la page), `list_offers_pending_review`, `confirm_offer`, `reject_offer` ; prompt MCP `collect_product_prices`.
- `IOfferCollector` réel (agent autonome via la passerelle et les mêmes tools) pour `CollectOffersJob`.
