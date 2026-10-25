# Décisions d'architecture

Journal des décisions (format ADR court). Ajouter une entrée pour toute décision qui n'est pas couverte par `CLAUDE.md`. Ne pas réécrire une décision acceptée : en ajouter une nouvelle qui la remplace.

## ADR-001 — La collecte n'est pas un module

**Statut** : acceptée (2026-10-24)

Le scraping / la collecte de prix n'a ni langage propre ni invariants : il va chercher une donnée externe et la traduit dans le langage de Pricing. C'est une couche anti-corruption : port `IOfferCollector` dans `Pricing.Application`, implémentations dans `Pricing.Infrastructure`, cas d'usage `CollectProductOffers`.

Conséquence : pas de module `Collection`, pas d'événement `OffersCollected` ni de read model intermédiaire. Si l'exécution doit sortir du process de l'API (Playwright, proxys), créer un host `Pricely.Worker` qui embarque `Pricing.Infrastructure`, sans nouveau module.

## ADR-002 — Une offre est produit × enseigne ; le rapprochement est dans Pricing

**Statut** : acceptée (2026-10-24)

Ce qui permet de comparer, c'est un produit de référence vendu par une enseigne à un prix. Créer une `Offer`, c'est faire le rapprochement : il vit donc dans Pricing, Catalog reste le référentiel produit. Unicité `(ProductId, MerchantId)` en v1 (un conditionnement différent = un autre produit de référence).

## ADR-003 — Typologie hiérarchique et héritage du profil de collecte

**Statut** : acceptée (2026-10-24)

Typologie limitée à 3 niveaux. Le profil de collecte se résout en remontant l'arbre :
- enseignes favorites : **remplacement**, la typologie la plus proche qui en définit l'emporte (prévisible) ;
- instructions : **concaténation** racine → feuille (le parent cadre, l'enfant précise, pas de duplication).

Le profil appartient à Pricing (choisir les enseignes à interroger est une décision de collecte, pas une propriété du produit). Résolution : `CollectionProfileResolver`.

## ADR-004 — Confirmation automatique uniquement sur preuve forte

**Statut** : acceptée (2026-10-24)

Un seul mauvais rapprochement (lot de 10 comparé à l'unité) décrédibilise la comparaison. Confirmation automatique si GTIN identique (comparé sur 14 chiffres) ou marque + référence fabricant identiques (normalisées). Sinon `Proposed` et file de validation humaine ; la validation n'a lieu qu'une fois par offre.

Garde-fou : relevé à plus de 40 % de la médiane des autres offres confirmées → `IsFlaggedAsAnomaly`, même sur une offre confirmée.

À revoir : un seuil de confiance IA pour la confirmation automatique, **uniquement** quand l'historique des validations permettra de mesurer le taux réel (ex. « au-dessus de 0,95, 98 % confirmées »).

## ADR-005 — Historique de prix hors de l'agrégat Offer

**Statut** : acceptée (2026-10-24)

`PriceObservation` est un agrégat séparé lié par `OfferId` : l'historique croît sans limite et ne porte pas d'invariant commun avec l'offre. Le dernier prix se lit par requête (`CROSS APPLY TOP 1`), sans dénormalisation en v1.

## ADR-006 — Vulnérabilités NuGet corrigées, jamais masquées

**Statut** : acceptée (2026-10-24)

L'audit NuGet (transitif compris) reste bloquant. Correction par montée de version du package direct ; à défaut, référence directe à la version corrigée dans le projet le plus proche de la source, commentée avec l'avis GHSA, à retirer quand le parent est à jour. `CentralPackageTransitivePinningEnabled` est écarté : il impose toutes les versions centrales au graphe transitif et provoque des rétrogradations en cascade.

## ADR-007 — Appels LLM centralisés dans Common, piloté par le module AiEngine

**Statut** : acceptée (2026-10-25)

Repris de ChaussonStore. Les appels LLM passent tous par un port de `Common.Application.AI` :
- `AiFeature` : fonctionnalité IA déclarée par un module (code `"<module>.<usage>"`, libellé, module, fournisseur par défaut, conversation requise ou non), enregistrée par `services.AddAiFeature(...)`.
- `ILlmGateway.ForFeatureAsync(feature)` : renvoie le client du fournisseur et du modèle (ou de l'agent) ciblés pour cette fonctionnalité. Un module n'injecte jamais un fournisseur directement.
- `LlmServiceBase` : base des fournisseurs (services à clé, enregistrés une seule fois par `AddAiProviders` dans `Common.Infrastructure.Ai`) ; publie `LlmCalledIntegrationEvent` après chaque appel (tokens, durée, modèle, session, succès). La publication est directe sur le bus, sans outbox : un événement peut être perdu si le processus s'arrête, ce qui est accepté pour des statistiques d'usage.
- `ILlmTargetResolver` : par défaut, le fournisseur de la fonctionnalité ; remplacé en DI par celui du module AiEngine.

Le module **AiEngine** pilote et observe, il n'appelle pas les LLM pour les autres : catalogue `Model` / `ModelPrice` (tarifs datés), `FeatureSetting` (cible par fonctionnalité + cible `default`), `LlmCall` (journal via l'inbox, coût figé au tarif du jour), statistiques. La session n'est pas un agrégat (contrairement à ChaussonStore) : c'est une clé de regroupement portée par chaque `LlmCall` (`SessionId`), ce qui garde « un agrégat par transaction » et évite une ligne de session par appel. Agents hébergés reportés.

Conséquence : aucun module ne dépend d'AiEngine ; pas de projet `Contracts`. Écarts volontaires avec ChaussonStore : horodatage en UTC (`IDateTimeProvider`), fournisseur Anthropic d'abord (Vertex AI / Foundry plus tard), recherche web des tarifs et agents hébergés reportés (une cible = un modèle), `LlmCall.Record` regroupe les données de l'appel dans `LlmCallDetails` (pas de factory à 15 paramètres).

## ADR-008 — Agent : boucle d'outils dans Pricely

**Statut** : acceptée (2026-10-25)

La passerelle de ChaussonStore ne fait que message → texte (ou JSON) ; ses « agents » sont hébergés chez le fournisseur avec leurs propres outils. Pour un agent qui utilise les tools de Pricely, on ajoute à la passerelle un `IChatClient` (`Microsoft.Extensions.AI`) par fonctionnalité, enveloppé par `FunctionInvokingChatClient`, avec journalisation `LlmCalled` conservée.

La boucle tourne dans Pricely (et non dans un agent hébergé) pour :
- intercepter les tools d'écriture soumis à approbation (action en attente, validée par l'utilisateur) ;
- récupérer les résultats typés des tools pour les composants du front Blazor ;
- persister la conversation (`AgentConversation`) et la rejouer.

L'agent hébergé (Anthropic Managed Agents branché sur le MCP de Pricely) reste possible plus tard comme cible `Agent` d'AiEngine.

## ADR-009 — Tools définis une fois, exposés à l'agent interne et en MCP HTTP

**Statut** : acceptée (2026-10-25)

Chaque module déclare ses tools dans `Presentation` (comme ses endpoints) : méthodes décrites (`[Description]`, en français) qui envoient une commande ou une requête via `ISender`, avec deux métadonnées : lecture / écriture, approbation requise ou non. Une même définition (`AIFunction`) sert :
- à l'agent interne (front Blazor) ;
- au serveur MCP en **transport HTTP** (`/mcp` dans l'API, authentifié), utilisable depuis Claude.ai, Claude Desktop / Code ou tout client MCP.

Écart volontaire avec ChaussonStore (MCP stdio, tools clients Refit de l'API) : un MCP distant est nécessaire et l'agent interne ne doit pas s'appeler lui-même en HTTP. L'amorce `IMcpTool` / `McpServerExtensions` du template (stdio, `Activator.CreateInstance`) est remplacée.

**Mise en œuvre (2026-10-25)** : pas d'abstraction maison. Les tools sont des classes `public static` `[McpServerToolType]` du SDK MCP (`ModelContextProtocol.AspNetCore`), méthodes `[McpServerTool]` + `[Description]`, services (`ISender`, `CancellationToken`) injectés. Les métadonnées sont les annotations MCP : `ReadOnly`, `Destructive`, `Idempotent`, `OpenWorld`. Pour l'agent interne (étape 4), les mêmes méthodes seront converties en `AIFunction` (le SDK MCP s'appuie sur `Microsoft.Extensions.AI`) ; l'agent exigera une approbation pour tout tool qui n'est pas `ReadOnly`. Le filtrage par scope (ADR-010) s'appuiera sur ces mêmes annotations ou sur un attribut dédié, à décider avec l'étape 0.

Garde-fous : un tool ne contient pas de logique métier ; l'agent ne confirme jamais une offre lui-même (`OfferMatchingPolicy` ou décision humaine) ; un prix soumis par l'agent doit figurer dans une page réellement récupérée par Pricely (instantané de page).

## ADR-010 — Authentification : OAuth 2.1 avec Keycloak, Pricely serveur de ressources

**Statut** : acceptée (2026-10-25)

Un seul système d'identité pour l'API, le serveur MCP et le front Blazor.

- **Keycloak** est le serveur d'autorisation (realm `pricely`) : il gère l'enregistrement dynamique des clients (DCR) sur lequel s'appuie la spécification d'autorisation MCP, s'intègre à Aspire (`Aspire.Hosting.Keycloak`) et s'auto-héberge. Alternative managée équivalente : Auth0 (même code côté Pricely). Entra ID écarté (pas de DCR).
- **Pricely est un serveur de ressources** : validation des JWT de Keycloak (`AddJwtBearer`, émetteur et audience), publication de `/.well-known/oauth-protected-resource` (RFC 9728) et challenge 401 conforme pour le MCP (`ModelContextProtocol.AspNetCore`). Pricely n'émet plus de jetons.
- **Scopes** : `pricely.read` (consultation), `pricely.write` (administration, collecte), `pricely.review` (confirmation / rejet d'offres). Chaque tool et endpoint déclare le scope requis ; un client ne voit que les tools autorisés par son jeton.
- **Module Users** : garde profils, rôles et permissions métier ; le `sub` du jeton est rattaché à un `User` et `CustomClaimsTransformation` ajoute ses permissions. `JwtTokenService`, `GenerateAccessToken` et l'option `Jwt:Secret` sont supprimés.
- Les tools d'écriture restent soumis à approbation humaine, quel que soit le scope (ADR-008).
- Clé d'API statique écartée : utile seulement pour Claude Code / Desktop, inutilisable pour les connecteurs Claude.ai et le front, double mécanisme à maintenir.

À vérifier lors du branchement : le mode exact d'OAuth des connecteurs distants de Claude.ai (DCR ou client pré-enregistré) dans leur documentation à jour ; Keycloak couvre les deux.

## ADR-011 — Démarrage de l'API : Keycloak dans l'AppHost, provisionnement des utilisateurs, migrations

**Statut** : acceptée (2026-10-25), sur la base du plan validé pour l'étape 0. Met en œuvre l'ADR-010 ; ne la remplace pas. **Écart assumé avec l'ADR-010** : seul `/mcp` exige un scope, les endpoints existants restent « authentifié » (voir plus bas) ; à trancher avant d'exposer l'API hors du poste de développement.

- **Keycloak local** : `Aspire.Hosting.Keycloak` (seulement publié en préversion, `13.6.1-preview`, aligné sur Aspire 13.6.1), port fixe 8080 pour que l'émetteur des jetons (`http://localhost:8080/realms/pricely`) soit stable, realm importé depuis `aspire/Pricely.AppHost/Realms/pricely-realm.json` (conteneur non persistant : le fichier est ré-importé à chaque lancement). L'API reçoit `Authentication__Authority` de l'AppHost ; l'audience attendue est `pricely-api`, ajoutée par un mapper d'audience sur les trois scopes `pricely.*` (sinon Keycloak émet `aud: account`). Les scopes de base (`basic` qui porte `sub`, `profile`, `email`) sont redéfinis dans le realm : un realm importé avec des `clientScopes` n'a pas les scopes par défaut.
- **Clients** : `pricely-web` (public, PKCE S256), `pricely-mcp` (public, PKCE, pré-enregistré pour les clients MCP dont l'enregistrement dynamique échoue), `pricely-dev` (mot de passe, développement local seulement, pour obtenir un jeton en script). `pricely.read` est un scope par défaut, `pricely.write` et `pricely.review` sont optionnels.
- **Enregistrement dynamique (DCR)** : activé en anonyme avec la politique « Trusted Hosts » limitée aux hôtes `localhost`, `127.0.0.1`, `claude.ai`, `claude.com` pour les URI de redirection, et « Consent Required » conservée. Realm de développement : en production, durcir ces politiques ou pré-enregistrer les clients.
- **Identité et utilisateur** : `sub` reste l'identifiant Keycloak et devient `User.IdentityId` ; l'identifiant interne passe dans la claim `pricely_user_id`. Le `User` est créé (rôle `Member`) à la première requête authentifiée par `ProvisionUserCommand`, idempotente ; la course entre deux premières requêtes simultanées est absorbée par l'index unique sur `identity_id`. Le jeton doit porter l'e-mail (scope `email`), sinon `Users.MissingEmail` : l'identité authentifiée n'a alors aucune permission (journalisée, pas d'exception) et les politiques répondent 403. `RegisterUser`, `GenerateAccessToken`, `JwtTokenService`, `JwtOptions`, `Jwt:Secret` et `IIdentityProviderService` sont supprimés : l'inscription est l'affaire de Keycloak.
- **Autorisation** : une politique par scope, nommée comme le scope (`PricelyScopes` dans `Common.Application`) : `.RequireAuthorization(PricelyScopes.Read)`. Les politiques de permission métier (`CustomClaimsTransformation`) restent. Pour l'instant seul `/mcp` exige `pricely.read` ; les endpoints existants restent « authentifié ». Conséquence : comme `pricely.read` est un scope par défaut de tous les clients, un jeton en lecture seule peut appeler les endpoints de mutation qui n'exigent aucun scope. Acceptable en développement local ; à corriger en posant `PricelyScopes.Write` / `Review` sur les endpoints de mutation avant tout déploiement. Le filtrage des tools par scope, annoncé par l'ADR-009, reste à faire avec les tools d'écriture.
- **MCP** : le schéma d'authentification `AddMcp` de `ModelContextProtocol.AspNetCore` sert la métadonnée `/.well-known/oauth-protected-resource` (RFC 9728) et le challenge 401 ; la validation du jeton reste faite par JwtBearer. Il est schéma de challenge par défaut, donc tout 401 de l'API pointe vers cette métadonnée.
- **Migrations** : option A du skill `add-migration`. `dotnet ef` passe par l'hôte `Pricely.Api` (`Microsoft.EntityFrameworkCore.Design`), une migration `Initial` par module, `.editorconfig` `generated_code = true` dans chaque dossier `Migrations`. En Development, l'API applique les migrations des quatre modules au démarrage (`ApplyMigrationsAsync`) ; hors Development, elles doivent être jouées par le déploiement (job ou script `dotnet ef migrations script`), jamais par l'API.
- **Validation de l'injection de dépendances** : `ValidateScopes` et `ValidateOnBuild` activés dans tous les environnements ; `StartupCompositionTests` construit l'hôte complet sans base ni Keycloak.

## Décisions ouvertes

- Application des migrations en production (job de déploiement, script SQL idempotent) : ADR-011 l'interdit à l'API, le mécanisme reste à choisir.
- Filtrage des tools MCP par scope (annotations ou attribut dédié), avec les tools d'écriture.
- Front Blazor : Blazor Server ou Blazor Web App en rendu interactif automatique ; projet de la solution (`src/Web/Pricely.Web`) ou application séparée.
- API de recherche web pour l'agent de collecte (Brave, Tavily, Bing…).
- Plusieurs conditionnements d'un même produit chez une enseigne : aujourd'hui un produit de référence par conditionnement.
