# Feuille de route : IA, agent et MCP

Chaque étape fait l'objet d'une PR sur `main`, CI verte, revue `architecture-reviewer`.

## Étape 0 — L'API démarre — fait (ADR-011)

- `Program.cs` : `AddPricelyAsync(connectionString, configuration)`, `MapPricelyEndpoints()`, OpenAPI en développement.
- Module Users : réactiver l'enregistrement de `UsersDbContext`, corriger le SQL PostgreSQL (`::json`) de son inbox.
- Migrations initiales Catalog, Pricing, Users (skill `add-migration`), base locale via l'AppHost Aspire.
- Authentification (ADR-010) : Keycloak dans l'AppHost (realm `pricely` importé au démarrage, scopes `pricely.read|write|review`, client du front), validation JWT Keycloak dans l'API, rattachement `sub` → `User`, suppression de `JwtTokenService` / `GenerateAccessToken` / `Jwt:Secret`.
- Critère : l'AppHost démarre (PostgreSQL + Keycloak + API) ; `/api/catalog/categories` renvoie 401 sans jeton et répond avec un jeton Keycloak.
- Fait : AppHost avec Keycloak et realm importé, validation JWT, politiques par scope, `sub` → `User` à la première requête, migrations `Initial` des quatre modules appliquées par l'API en Development, `/mcp` en `pricely.read` avec sa métadonnée OAuth. À vérifier avec un démon de conteneurs : le lancement par l'AppHost lui-même (Aspire n'a pas pu tourner faute de Docker). Vérifié à la main sur PostgreSQL 16 + Keycloak 26.6 + l'API : migrations appliquées, 401 sans jeton / 200 avec jeton Keycloak, provisionnement de l'utilisateur, 9 tools MCP, requêtes Dapper, outbox → inbox.

## Étape 1 — Socle Common AI (ADR-007) — fait

- `Common.Application.AI` : `AiFeature`, `AiProviderKeys`, `LlmTarget`, `ILlmTargetResolver`, `ILlmGateway`, `LlmClient`, `LlmServiceBase`, `LlmRequest` / `LlmResponse` / `LlmTokenUsage`, `LlmCalledIntegrationEvent`, `AddAiFeature`.
- `Common.Infrastructure.Ai` : `LlmGateway`, `DefaultLlmTargetResolver`, `AddAiProvidersAsync` ; fournisseur Anthropic (options, `HttpClient` authentifié, secrets en user-secrets).
- Horodatage UTC. Tests unitaires de la passerelle avec un faux fournisseur.

## Étape 2 — Module AiEngine — fait (sans agents hébergés ni recherche web des tarifs)

- Skill `new-module`. Agrégats `Model` (+ `ModelPrice` datés), `FeatureSetting`, `Agent`, `Session`, `LlmCall`.
- Consommation de `LlmCalledIntegrationEvent` (inbox) → `LlmCall` avec coût figé ; `LlmTargetResolver` qui remplace le résolveur par défaut.
- Endpoints : modèles et tarifs, cibles des fonctionnalités, agents, statistiques (synthèse, par jour, par modèle, par fonctionnalité).
- La recherche des tarifs sur le web est reportée.

## Étape 3 — Tools et MCP HTTP (ADR-009) — fait ; tools listés selon le scope du jeton (ADR-016)

- Abstraction de tool dans `Common.Presentation` (lecture / écriture, approbation requise) ; découverte par assembly, instanciation par DI.
- Tools de consultation : `search_products`, `get_product`, `get_category_tree`, `compare_prices`, `get_price_history`, `list_merchants`, `list_price_anomalies`, `get_collection_plan`.
- Serveur MCP en transport HTTP sur `/mcp`, protégé par les jetons Keycloak (ADR-010) : métadonnée `/.well-known/oauth-protected-resource`, scopes par tool ; remplace l'amorce stdio du template.
- Critère : interroger Pricely depuis Claude Desktop / Claude Code.

## Étape 4 — Agent interne (ADR-008, ADR-013) — fait ; streaming réel du fournisseur Anthropic depuis l'étape 6

- Passerelle : `IChatClient` par fonctionnalité, `FunctionInvokingChatClient`, journalisation `LlmCalled` conservée.
- `AgentConversation` (messages, appels d'outils, résultats), actions en attente pour les tools soumis à approbation.
- Fonctionnalité `pricing.assistant` ; endpoint de conversation en streaming.
- Fait : module générique `Agentic` (ADR-014) et agent `assistant` dans `src/Agents` (fonctionnalité `agents.assistant`), journal des tools et décisions dans AiEngine, chat client Anthropic avec tools, actions en attente et validation, endpoint SSE, conversations persistées (schéma `agentic`), tool d'écriture `collect_product_offers`, endpoints de lecture des écrans (offres d'un produit, historique, anomalies, plan de collecte, recherche), scopes sur tous les endpoints.

## Étape 5 — Front Blazor (ADR-015) — fait

- Chat en streaming ; composants de réponse typés (comparatif de prix, offres à valider, plan de collecte, anomalies, historique) ; boutons d'action qui appellent directement les commandes ; validation des actions en attente.
- 5a — fait : `src/Web/Pricely.Web` (Interactive Server, OIDC côté serveur avec `pricely-web`, jetons gardés sur le serveur), lancé par l'AppHost ; Assistant (événements SSE, pastilles d'outils, composants typés, carte « Action proposée » avec validation / refus, conversations, panneau de contexte) ; fiche produit (offres, médiane, historique sur 1 mois / 3 mois / 1 an, prix signalés, collecte) ; recherche de produits.
- 5b — fait : offres à valider (référence vs page observée avec la comparaison de `OfferMatchingPolicy`, avis de l'agent, confirmer / rejeter en `pricely.review`), profils de collecte (arbre, enseignes héritées ou propres, instructions, prompt effectif bloc par bloc), IA et coûts (synthèse, coût par jour, cibles des fonctionnalités, tarifs des modèles), finitions mobile (badge et raccourci « Offres à valider », comparatif en liste), consommation de la conversation dans l'assistant.

## Étape 6 — Collecte par l'agent (ADR-017) — fait

- Tools `fetch_merchant_page` (domaine de l'enseigne uniquement, instantané conservé), `submit_collected_offer` (exige un instantané, prix vérifié dans la page), `list_offers_pending_review`, `confirm_offer`, `reject_offer` ; prompt MCP `collect_product_prices`.
- `IOfferCollector` réel (agent autonome via la passerelle et les mêmes tools) pour `CollectOffersJob`.
- Fait : recherche sur le site de l'enseigne (`SearchTemplate`), lecture des pages limitée au site de l'enseigne et aux adresses publiques, instantanés `page_snapshots`, prix vérifié dans l'instantané, `AgentOfferCollector` (fonctionnalité `pricing.offer-collection`), tools `fetch_merchant_page`, `submit_collected_offer`, `confirm_offer`, `reject_offer`, prompt `collect_product_prices`. Avec : service de migration (ADR-018), conditionnement (ADR-019), jetons du front en cache distribué, consommation par conversation, suppression des tests d'intégration hérités.
