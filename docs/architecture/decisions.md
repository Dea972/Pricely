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

Les appels LLM passent tous par un port de `Common.Application.AI` :
- `AiFeature` : fonctionnalité IA déclarée par un module (code `"<module>.<usage>"`, libellé, module, fournisseur par défaut, conversation requise ou non), enregistrée par `services.AddAiFeature(...)`.
- `ILlmGateway.ForFeatureAsync(feature)` : renvoie le client du fournisseur et du modèle (ou de l'agent) ciblés pour cette fonctionnalité. Un module n'injecte jamais un fournisseur directement.
- `LlmServiceBase` : base des fournisseurs (services à clé, enregistrés une seule fois par `AddAiProviders` dans `Common.Infrastructure.Ai`) ; publie `LlmCalledIntegrationEvent` après chaque appel (tokens, durée, modèle, session, succès). La publication est directe sur le bus, sans outbox : un événement peut être perdu si le processus s'arrête, ce qui est accepté pour des statistiques d'usage.
- `ILlmTargetResolver` : par défaut, le fournisseur de la fonctionnalité ; remplacé en DI par celui du module AiEngine.

Le module **AiEngine** pilote et observe, il n'appelle pas les LLM pour les autres : catalogue `Model` / `ModelPrice` (tarifs datés), `FeatureSetting` (cible par fonctionnalité + cible `default`), `LlmCall` (journal via l'inbox, coût figé au tarif du jour), statistiques. La session n'est pas un agrégat : c'est une clé de regroupement portée par chaque `LlmCall` (`SessionId`), ce qui garde « un agrégat par transaction » et évite une ligne de session par appel. Agents hébergés reportés.

Conséquence : aucun module ne dépend d'AiEngine ; pas de projet `Contracts`. Choix de mise en œuvre : horodatage en UTC (`IDateTimeProvider`), fournisseur Anthropic d'abord (Vertex AI / Foundry plus tard), recherche web des tarifs et agents hébergés reportés (une cible = un modèle), `LlmCall.Record` regroupe les données de l'appel dans `LlmCallDetails` (pas de factory à 15 paramètres).

## ADR-008 — Agent : boucle d'outils dans Pricely

**Statut** : acceptée (2026-10-25)

La passerelle d'ADR-007 ne fait que message → texte (ou JSON) ; un agent hébergé chez un fournisseur utilise ses propres outils, définis chez lui. Pour un agent qui utilise les tools de Pricely, on ajoute à la passerelle un `IChatClient` (`Microsoft.Extensions.AI`) par fonctionnalité, enveloppé par `FunctionInvokingChatClient`, avec journalisation `LlmCalled` conservée.

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

Alternative écartée : un serveur MCP séparé en stdio dont les tools appellent l'API en HTTP. Un MCP distant est nécessaire et l'agent interne ne doit pas s'appeler lui-même en HTTP. L'amorce `IMcpTool` / `McpServerExtensions` du template (stdio, `Activator.CreateInstance`) est remplacée.

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

- **Keycloak local** : `Aspire.Hosting.Keycloak` (seulement publié en préversion, `13.6.1-preview`, aligné sur Aspire 13.6.1), port fixe 8180 (8080 au départ, abandonné le 2026-10-26 : port très courant, déjà pris en IPv6 par le relais `localhost` de WSL2 sur le poste du propriétaire, qui répondait à la place de Keycloak) pour que l'émetteur des jetons (`http://localhost:8180/realms/pricely`) soit stable, realm importé depuis `aspire/Pricely.AppHost/Realms/pricely-realm.json` (conteneur non persistant : le fichier est ré-importé à chaque lancement). L'API reçoit `Authentication__Authority` de l'AppHost ; l'audience attendue est `pricely-api`, ajoutée par un mapper d'audience sur les trois scopes `pricely.*` (sinon Keycloak émet `aud: account`). Les scopes de base (`basic` qui porte `sub`, `profile`, `email`) sont redéfinis dans le realm : un realm importé avec des `clientScopes` n'a pas les scopes par défaut.
- **Clients** : `pricely-web` (public, PKCE S256), `pricely-mcp` (public, PKCE, pré-enregistré pour les clients MCP dont l'enregistrement dynamique échoue), `pricely-dev` (mot de passe, développement local seulement, pour obtenir un jeton en script). `pricely.read` est un scope par défaut, `pricely.write` et `pricely.review` sont optionnels.
- **Enregistrement dynamique (DCR)** : activé en anonyme avec la politique « Trusted Hosts » limitée aux hôtes `localhost`, `127.0.0.1`, `claude.ai`, `claude.com` pour les URI de redirection, et « Consent Required » conservée. Realm de développement : en production, durcir ces politiques ou pré-enregistrer les clients.
- **Identité et utilisateur** : `sub` reste l'identifiant Keycloak et devient `User.IdentityId` ; l'identifiant interne passe dans la claim `pricely_user_id`. Le `User` est créé (rôle `Member`) à la première requête authentifiée par `ProvisionUserCommand`, idempotente ; la course entre deux premières requêtes simultanées est absorbée par l'index unique sur `identity_id`. Le jeton doit porter l'e-mail (scope `email`), sinon `Users.MissingEmail` : l'identité authentifiée n'a alors aucune permission (journalisée, pas d'exception) et les politiques répondent 403. `RegisterUser`, `GenerateAccessToken`, `JwtTokenService`, `JwtOptions`, `Jwt:Secret` et `IIdentityProviderService` sont supprimés : l'inscription est l'affaire de Keycloak.
- **Autorisation** : une politique par scope, nommée comme le scope (`PricelyScopes` dans `Common.Application`) : `.RequireAuthorization(PricelyScopes.Read)`. Les politiques de permission métier (`CustomClaimsTransformation`) restent. Pour l'instant seul `/mcp` exige `pricely.read` ; les endpoints existants restent « authentifié ». Conséquence : comme `pricely.read` est un scope par défaut de tous les clients, un jeton en lecture seule peut appeler les endpoints de mutation qui n'exigent aucun scope. Acceptable en développement local ; à corriger en posant `PricelyScopes.Write` / `Review` sur les endpoints de mutation avant tout déploiement. Le filtrage des tools par scope, annoncé par l'ADR-009, reste à faire avec les tools d'écriture.
- **MCP** : le schéma d'authentification `AddMcp` de `ModelContextProtocol.AspNetCore` sert la métadonnée `/.well-known/oauth-protected-resource` (RFC 9728) et le challenge 401 ; la validation du jeton reste faite par JwtBearer. Il est schéma de challenge par défaut, donc tout 401 de l'API pointe vers cette métadonnée.
- **Migrations** : option A du skill `add-migration`. `dotnet ef` passe par l'hôte `Pricely.Api` (`Microsoft.EntityFrameworkCore.Design`), une migration `Initial` par module, `.editorconfig` `generated_code = true` dans chaque dossier `Migrations`. En Development, l'API applique les migrations des quatre modules au démarrage (`ApplyMigrationsAsync`) ; hors Development, elles doivent être jouées par le déploiement (job ou script `dotnet ef migrations script`), jamais par l'API.
- **Validation de l'injection de dépendances** : `ValidateScopes` et `ValidateOnBuild` activés dans tous les environnements ; `StartupCompositionTests` construit l'hôte complet sans base ni Keycloak.

## ADR-012 — PostgreSQL à la place de SQL Server

**Statut** : acceptée (2026-10-26), sur demande explicite du propriétaire du projet.

La base est PostgreSQL (Npgsql 10 pour EF Core et Dapper, `Aspire.Hosting.PostgreSQL` dans l'AppHost, base `pricely-db`). Le reste de l'architecture est inchangé : un `DbContext` et un schéma par module, snake_case, Dapper en lecture, outbox / inbox maison. Les migrations `Initial` (non déployées) ont été régénérées, pas converties.

Conséquences et choix :
- **Casse** : PostgreSQL compare le texte en respectant la casse, SQL Server non. Les colonnes d'unicité métier (e-mail, nom d'enseigne, nom de typologie, identifiant de modèle, code de fonctionnalité) passent en `citext` pour conserver le comportement attendu ; les recherches de produits utilisent `ILIKE`.
- **Messages outbox / inbox** : colonne `text`, pas `jsonb`. Le contenu est sérialisé par Newtonsoft avec `$type` en première propriété, que `jsonb` réordonnerait. Le lot est relu avec `FOR UPDATE SKIP LOCKED`. Cela évite qu'une même ligne soit traitée deux fois, mais ne rend pas l'API multi-instances : les traitements d'inbox de Pricing (upsert des read models, `Get` puis `Insert`) ne sont ni atomiques ni ordonnés entre lots concurrents. **Une seule instance de l'API** tant que ces upserts ne sont pas rendus atomiques (`ON CONFLICT`) et ordonnés.
- **Extension** : `citext` est créée au niveau de la base (`CREATE EXTENSION IF NOT EXISTS`, idempotent, répétée par chaque migration de module) ; le rôle de migration doit y être autorisé sur un PostgreSQL managé. `citext` n'a pas de longueur : `HasMaxLength` n'est plus appliqué en base pour ces colonnes, seuls le domaine et les validateurs la bornent.
- **Dates** : `timestamptz`, `Kind` UTC obligatoire à l'écriture. Dapper normalise tout paramètre `DateTime` en UTC (`UtcDateTimeTypeHandler`) et `PriceObservation.Record` normalise la date d'observation fournie par un collecteur (`AsUtc`) ; les agrégations par jour et le tarif du jour sont calculés en UTC (`AT TIME ZONE 'UTC'`) quel que soit le fuseau de session ; une colonne `date` est convertie en `timestamp` dans les requêtes Dapper dont la réponse est un `DateTime`.
- **Requêtes** : `TOP` → `LIMIT`, `CROSS/OUTER APPLY` → `JOIN LATERAL`, booléens natifs, paramètres nullables castés explicitement.
- Les épingles de sécurité `System.Security.Cryptography.Pkcs` (qui venaient de `Microsoft.Data.SqlClient`) sont retirées.
- Point de vigilance hérité, non modifié : l'index unique des typologies `(parent_id, name)` n'empêche pas deux racines de même nom (valeurs nulles distinctes, comme avec le filtre SQL Server d'origine).

## ADR-013 — Agents : dossier `src/Agents`, socle commun, approbation des écritures

**Statut** : acceptée (2026-10-26), décisions du propriétaire du projet (dossier `src/Agents`, socle + un projet par agent, persistance dans PostgreSQL). Met en œuvre l'ADR-008. **Emplacement et structure remplacés par l'ADR-014** (le socle devient le module `Agentic`) ; les règles d'approbation, de concurrence et de notes restent valables.

- **Emplacement** : les agents ne sont pas un module. Ils vivent dans `src/Agents`, au même niveau que `src/API` : `Pricely.Agents.Core` (socle) et un projet par agent (`Pricely.Agents.Assistant`, plus tard l'agent de collecte). Un agent est un `IAgentDefinition` (nom de route, `AiFeature`, instructions, noms des tools autorisés) enregistré par `AddAgent<T>()`.
- **Frontières** : les agents ne référencent aucun module. Ils utilisent les tools des modules (ADR-009), dont les assemblies sont passées par la composition (`Pricely.Sdk`), comme pour le serveur MCP. Vérifié par `AgentsBoundaryTests`.
- **Boucle** : `LlmClient.AsChatClient(session, user)` donne un `IChatClient` du fournisseur ciblé (Anthropic : tool use de l'API Messages), journalisé à chaque appel (`LlmCalledIntegrationEvent`, session = conversation). `FunctionInvokingChatClient` exécute les tools ; les méthodes `[McpServerTool]` deviennent des `AIFunction` dont les paramètres de service (`ISender`) sont résolus dans le scope de la requête. Le streaming fournisseur est émulé (réponse complète découpée) : le client reçoit les événements de l'agent au fil de l'eau, pas les jetons.
- **Écritures** : un tool qui n'est pas `ReadOnly` n'est jamais exécuté par le modèle. Son appel crée une `PendingAction` (outil, arguments, scope requis) et le modèle reçoit « en attente de validation ». Le propriétaire de la conversation valide (`POST agents/actions/{id}/approve`, avec le scope requis, `pricely.write` par défaut) : l'API exécute alors directement le tool avec les arguments enregistrés, sans repasser par le modèle, puis note la décision dans la conversation. Refus : rien n'est modifié.
- **MCP** : `/mcp` n'expose que les tools `ReadOnly`, faute d'approbation humaine côté client MCP et de filtrage par scope ; `collect_product_offers` n'est donc disponible que pour l'agent interne.
- **Persistance** : schéma `agents` (`AgentsDbContext`, migration propre) : `conversations`, `messages` (message du modèle sérialisé, rejouable), `pending_actions`. Clé étrangère seulement à l'intérieur du schéma ; `user_id` est un `Guid` simple. Code applicatif sans MediatR (services du socle) : les agents orchestrent, ils n'ont pas de règles métier propres. Les dossiers `Domain`, `Application`, `Infrastructure`, `Endpoints` du socle ne sont pas des couches au sens des modules : seul `Domain` est contraint (ni EF Core, ni ASP.NET, ni Microsoft.Extensions.AI, test `AgentsBoundaryTests`) ; les services de `Application` utilisent directement `AgentsDbContext`.
- **Concurrence** : `pending_actions` et `conversations` ont un jeton de concurrence optimiste (`xmin`). L'approbation est enregistrée (statut `Approved`) **avant** l'exécution du tool : une seconde approbation simultanée échoue (`Actions.AlreadyDecided`), le tool ne tourne qu'une fois. Deux tours simultanés d'une même conversation : le second n'est pas enregistré (événement `error`, `Conversations.Concurrent`). Une action proposée est enregistrée dès sa création, avant d'être annoncée au client.
- **Notes pour le modèle** : la décision sur une action est ajoutée à la conversation comme un tour utilisateur balisé « [Note de Pricely…] », jamais comme une instruction système, et sans recopier le texte d'une erreur (contenu potentiellement externe). Un tour arrêté (limite de 10 allers-retours, erreur) ne laisse pas d'appel de tool sans résultat.
- **Scope d'approbation** : `pricely.write` par défaut, ou celui de l'attribut `[ToolScope]` du tool (par exemple `pricely.review` pour confirmer une offre). L'approbation revérifie que le tool fait partie de ceux de l'agent.
- **Endpoints** : `POST agents/{agent}/messages` (server-sent events : `conversation`, `tool_call`, `tool_result`, `action_proposed`, `text`, `error`, `done`), `GET agents/conversations`, `GET agents/conversations/{id}`, `POST agents/actions/{id}/approve|reject`.
- **Scopes (comble l'écart de l'ADR-011)** : les lectures des modules exigent `pricely.read`, les mutations `pricely.write`, la confirmation et le rejet d'offres `pricely.review`.
- **Utilisateur de développement** : son identifiant est fixé dans le realm (`sub` stable à chaque réimport de Keycloak). Si un e-mail appartient déjà à une autre identité, l'utilisateur n'est pas rattaché automatiquement (`Users.EmailAlreadyLinked`, pas de permission ni d'accès aux agents) : c'est une décision d'administrateur.

## ADR-014 — Module générique Agentic ; journal des agents dans AiEngine

**Statut** : acceptée (2026-10-26), décisions du propriétaire du projet (nom `Agentic`, contrat des agents dans Common, événements outils + décisions). Remplace la partie « emplacement et structure » de l'ADR-013 : le socle `src/Agents/Pricely.Agents.Core` n'existe plus.

- **Le socle est un module** : `Pricely.Modules.Agentic` (Domain, Application, Infrastructure, Presentation ; schéma `agentic` ; outbox), conçu pour être repris tel quel dans un autre projet. Il ne connaît **aucun autre module ni aucun agent** (`ModuleBoundaryTests`, dans les deux sens).
- **Contrats dans Common** (`Common.Application.AI.Agents`) : `AgentDefinition` (nom, `AiFeature`, instructions, noms des tools) déclaré par `AddAgent`, comme une `AiFeature` ; `AgentTool` (nom, lecture seule, scope d'approbation, `AIFunction`) fourni par `AddAgentTools`. Pricely convertit les méthodes `[McpServerTool]` des modules (`McpServerExtensions.ToAgentTools`) dans le Sdk. L'utilisateur courant passe par `ICurrentUser` (Common.Application, implémenté dans Common.Infrastructure). Le scope d'approbation par défaut est une option du module (`Agentic:DefaultApprovalScope`, `pricely.write` dans Pricely), un tool peut déclarer le sien (`[ToolScope]`).
- **Agents de Pricely** : `src/Agents/Pricely.Agents.<Nom>` ne référencent que `Common.Application` (`AgentsBoundaryTests`). Un nouvel agent : un `AgentDefinition` et un `AddAgent`, rien dans le module.
- **Cas d'usage** : `StartTurnCommand` (vérifie, crée la conversation), `RunTurnStreamRequest` (`IStreamRequest` MediatR : le tour en événements), `ApproveActionCommand`, `RejectActionCommand`, requêtes Dapper `GetConversationsQuery` / `GetConversationQuery` (handlers dans Application, ADR-023). Les règles de l'ADR-013 (approbation enregistrée avant exécution sous `xmin`, notes pour le modèle en tour utilisateur, tours fermés) sont conservées.
- **Journal dans AiEngine, par les ports de Common** :
  - chaque appel du modèle reste un `LlmCalledIntegrationEvent` (passerelle `ILlmGateway`), qui porte désormais `AgentName` (session = conversation) ;
  - nouveau `AiToolInvokedIntegrationEvent` (Common) pour chaque tool exécuté pour un agent (pendant un tour ou après validation), publié directement sur le bus comme `LlmCalled` (statistique, perte acceptée) ;
  - nouveau `AiActionDecidedIntegrationEvent` (Common) pour chaque décision (`Executed`, `Failed`, `Rejected`), publié par l'outbox du module (événement de domaine `PendingActionDecidedDomainEvent`) : livraison garantie.
  - AiEngine les consomme par son inbox dans le journal `AgentActivity` (schéma `aiengine`, table `agent_activities`) et expose `aiengine/usage/by-agent` et `aiengine/usage/by-tool`. AiEngine ne dépend pas du module Agentic (les événements sont dans Common).
  - `AiActionDecidedIntegrationEvent.SourceModule` est publié pour les consommateurs futurs ; AiEngine ne le conserve pas encore. Le code de fonctionnalité est vide si l'agent n'est plus déclaré (Agentic ne devine pas de convention de nommage).
  - Une `AgentActivity` prend l'identifiant de l'événement d'intégration : une relivraison ne crée pas de doublon. La décision est une énumération (`AgentActionDecision`), datée de `DecidedOnUtc`.
- **Un seul agrégat par transaction** (revue d'architecture) : un tour n'écrit la conversation qu'à la fin (même si le client s'est déconnecté : `finally`), chaque action proposée est enregistrée seule dès sa proposition, et la validation ou le refus n'écrit que l'action. La note de décision n'est donc plus stockée dans la conversation : elle est reconstruite au rejeu de l'historique (`ConversationNotes.ForDecision`), à la date de la décision, mais jamais avant les messages du tour qui a proposé l'action (une action peut être décidée pendant ce tour, avant son enregistrement). Deux tours concurrents sur une même conversation : le second échoue proprement (`Conversations.ConcurrentTurn`, `xmin`).
- **Options du module** (`Agentic`) : `DefaultApprovalScope` (par défaut `write`, un scope que personne n'a), `AccessPolicy` (politique des endpoints du module, `pricely.read` dans Pricely ; tout utilisateur authentifié si absente), `MaximumToolRoundTrips`. Le catalogue des agents échoue au démarrage si un agent cite un tool inexistant.
- **Bases de développement existantes** : l'ancien schéma `agents` (socle de l'ADR-013) n'est plus utilisé et peut être supprimé (`DROP SCHEMA agents CASCADE`).

## ADR-015 — Front Blazor : Interactive Server, OIDC côté serveur, API en HTTP

**Statut** : acceptée (2026-10-26), décisions du propriétaire du projet (Blazor Web App en rendu Server, OIDC côté serveur, CSS maison, étape 5 en deux PR).

- **Projet** `src/Web/Pricely.Web` dans la solution, lancé par l'AppHost (`pricely-web`, référence à l'API, autorité Keycloak). Keycloak reste en HTTP sur 8180 dans l'AppHost (`WithoutHttpsCertificate`) : avec un certificat de développement approuvé, Aspire 13.6 passerait l'endpoint en `https` alors que Keycloak répond en HTTP sur ce port, et l'émetteur ne serait plus celui de l'ADR-010. Rendu **Interactive Server sans prérendu** : chaque page appelle l'API une fois, depuis le circuit. Toutes les pages exigent un utilisateur connecté (`RequireAuthorization` sur les composants) ; la page d'erreur reste statique.
- **Le front ne parle à l'API qu'en HTTP** : aucune référence à un module, à Common ni au Sdk (`WebBoundaryTests`). Il a ses propres DTO (`Api/Contracts.cs`), lus en JSON ; un champ ajouté par l'API est ignoré tant que le front n'en a pas besoin. Clients typés `PricelyApiClient` (sans nouvel essai sur les POST, délai de 5 min : valider une action ou lancer une collecte exécute le travail avant de répondre) et `AgentStreamClient` (SSE, `SseParser`, sans timeout de lecture ni nouvel essai), adresse par la découverte de services d'Aspire (`https+http://pricely-api`). Une validation qui dépasse le délai n'est pas présentée comme un échec : l'API a pu l'exécuter, la conversation rouverte le dira.
- **Authentification** (ADR-010) : flux code + PKCE avec le client public `pricely-web` (PAR quand Keycloak le propose), fait par le serveur Blazor. Le navigateur ne reçoit **aucun jeton**, même chiffré : seul un cookie de session (`pricely`, `HttpOnly`, `SameSite=Lax`, 8 h glissantes) porte une clé `pricely_session`. Les jetons sont gardés en mémoire du serveur (`ServerTokenStore`), rafraîchis une minute avant expiration avec le refresh token (un rafraîchissement à la fois par session), et la session est close si Keycloak refuse (400 / 401) ; Keycloak indisponible ne la ferme pas. L'échange du refresh token n'est jamais rejoué ni interrompu (rotation). Une seule instance du front : plusieurs demanderaient un cache partagé (et la protection des données partagée). Un redémarrage perd les jetons : l'utilisateur se reconnecte (« Ta session a expiré »). Déconnexion en POST avec jeton antiforgery vérifié par l'endpoint, puis fin de session Keycloak (`id_token_hint`). Retour après connexion limité aux chemins du site (règle d'`IsLocalUrl`, caractères de contrôle refusés). Cookie `Secure` hors Development ; derrière un reverse proxy TLS, configurer les en-têtes transférés au déploiement.
- **Assistant** : l'écran applique les événements SSE du tour (`ConversationView`) et reconstruit une conversation stockée depuis `agents/conversations/{id}` (la proposition d'action se retrouve par le résultat `pending_approval` de son tool). Chaque tool de lecture a son composant (`ToolResultView` : comparatif, produits, historique, prix signalés, plan de collecte, offres à valider, enseignes, typologies) ; un tool inconnu n'apparaît que par sa pastille. La carte « Action proposée » appelle directement `approve` / `reject` (scope de l'action), sans repasser par le modèle, et garde la trace de la décision.
- **Aucune règle métier dans le front** : médiane, écart, prix suspect, mode de rapprochement, ordre « moins cher d'abord » viennent de l'API. Le front se limite à l'affichage (libellés, mise en forme française, géométrie du graphique, écart max. parmi les écarts fournis) et n'écrit pas le seuil du garde-fou. Un prix signalé n'est jamais présenté comme « Moins cher » (le badge va à la première offre de l'API si son prix n'est pas suspect ; si l'API doit un jour désigner elle-même la meilleure offre, le front suivra). Les relevés signalés ne sont pas tracés dans l'historique (comptés à part). Les liens vers les pages des enseignes, données collectées, ne sont rendus que s'ils sont en `http(s)`.
- **Style** : CSS maison à partir des tokens de `docs/design` (`wwwroot/app.css`), IBM Plex depuis Google Fonts, aucune bibliothèque de composants. Accessibilité : focus visible, `aria-current`, cibles de 44 px, une couleur n'est jamais le seul signal.
- **Découpage** : 5a (socle, Assistant, fiche produit, recherche) ; 5b (offres à valider, profils de collecte, IA et coûts, finitions mobile).

## ADR-016 — Tools MCP selon le scope du jeton

**Statut** : acceptée (2026-10-26), décision du propriétaire du projet (« écriture selon le scope »). Remplace la règle « `/mcp` n'expose que les tools en lecture » des ADR-013 et ADR-014 ; complète l'ADR-009.

- Chaque tool exige un scope : `pricely.read` pour un tool `ReadOnly`, sinon celui de son `[ToolScope]`, sinon `pricely.write` (`McpServerExtensions.RequiredScope`, la règle d'approbation de l'agent interne). Aujourd'hui : lecture (dont `fetch_merchant_page`) → `pricely.read` ; `collect_product_offers`, `submit_collected_offer` → `pricely.write` ; `confirm_offer`, `reject_offer` → `pricely.review`.
- `tools/list` ne renvoie que les tools autorisés par les scopes du jeton (filtre de requêtes du SDK MCP). Chaque appel revérifie le scope (`ScopedMcpServerTool`) : un client peut appeler un tool par son nom sans l'avoir listé ; le refus est une erreur de tool (« Le scope « pricely.review » est requis pour ce tool. »), pas une exception.
- Pas de file d'approbation côté MCP : les annotations (`ReadOnly = false`, `Destructive` pour `reject_offer`) font demander confirmation à l'utilisateur par le client MCP, et le scope du jeton est le consentement. L'agent interne, lui, garde ses actions en attente (ADR-013).
- Les prompts MCP des modules sont publiés (`WithPromptsFromAssembly`) : `collect_product_prices` donne la procédure de collecte de l'ADR-017.

## ADR-017 — Collecte par l'agent sur le site de l'enseigne

**Statut** : acceptée (2026-10-26), décision du propriétaire du projet (« recherche du site enseigne »). Met en œuvre les ADR-008 et ADR-009 pour la collecte.

- **Pas d'API de recherche web externe.** Une enseigne peut déclarer sa page de recherche (`Merchant.SearchTemplate`, adresse de son site contenant `{query}`, `PUT pricing/merchants/{id}/search`) ; l'agent y cherche le produit puis ouvre la page produit. Sans gabarit, il ne peut lire qu'une adresse du site (fournie par le modèle).
- **Lecture d'une page** (`MerchantPageReader`, tool `fetch_merchant_page`) : l'adresse et chaque redirection (3 au plus, jamais suivies automatiquement) doivent appartenir au site de l'enseigne active (`Merchant.Owns` : http(s), même hôte ou sous-domaine, `www.` ignoré, pas d'identifiants dans l'adresse, port par défaut ou celui du site). Le lecteur de pages (`IMerchantPageFetcher` à l'origine, `IWebPageReader` de Common depuis l'ADR-020) ne se connecte qu'à des adresses publiques, vérifiées **à la connexion** (`PublicAddressGuard` : ni boucle locale, ni réseau privé, ni lien local, ni métadonnées cloud), sans proxy (derrière un proxy, le garde-fou verrait l'adresse du proxy), une seule tentative (la résilience standard des ServiceDefaults est retirée de ce client), 3 Mo au plus, 20 s. Plages refusées : boucle locale, réseaux privés, partagé (100.64/10), lien local, multicast, documentation, IPv6 local unique et préfixes qui portent une IPv4 (`::/96`, `64:ff9b::/96`, `2002::/16`). Le HTML est réduit à un texte : données structurées (JSON-LD, méta produit et prix, microdonnées) puis texte visible.
- **Instantané** : chaque page lue est gardée (`PageSnapshot`, table `pricing.page_snapshots`, 200 000 caractères au plus), seule dans sa sauvegarde, avant toute offre (même exception à « une sauvegarde par commande » que `CollectedOfferRecorder`) ; le modèle en reçoit les 20 000 premiers caractères et l'identifiant. Une page de résultats de recherche est marquée (`IsSearchPage`) : aucune offre ne peut la citer.
- **Soumission** (`submit_collected_offer`, ou `submit_offer` pour l'agent interne de collecte) : l'offre cite un instantané ; son prix doit figurer dans le texte de la page (écritures française et anglaise, séparateurs de milliers), sinon `PageSnapshots.PriceNotShown`. L'adresse et la date de l'offre sont celles de l'instantané, jamais celles du modèle. L'enseigne doit être dans le plan de collecte du produit. Le GTIN, la marque et la référence fabricant soumis ne sont gardés que s'ils figurent dans la page (`PageSnapshot.ShowsIdentifier`, même normalisation que `OfferMatchingPolicy`) : le collecteur connaît les identifiants du produit de référence, et un identifiant qu'il n'a pas lu suffirait à faire confirmer l'offre. Titre, similarité et justification du modèle sont recopiés dans `MatchEvidence` (`ObservedTitle`, `AiRationale`) pour l'écran de revue.
- **L'agent ne confirme jamais** : l'offre passe par `OfferMatchingPolicy` comme toute offre collectée ; le reste attend la revue humaine (`confirm_offer` / `reject_offer`, scope `pricely.review`).
- **`IOfferCollector` = `AgentOfferCollector`** (remplace `NotConfiguredOfferCollector`) : fonctionnalité `pricing.offer-collection`, agent `pricing-collector`, session `collection:{produit}:{guid}` dans le journal AiEngine, tools limités aux enseignes de la demande et aux instantanés du passage, une offre par enseigne (la dernière transmise), au plus 6 allers-retours par enseigne (40 en tout). Une panne du modèle avant toute offre est un échec (`Collection.AgentFailed`) ; après, les offres déjà vérifiées sont gardées. `CollectOffersJob` reste désactivé par défaut.
- **Limites connues** : un site qui ne rend son contenu qu'en JavaScript n'expose pas son prix dans le HTML (le JSON-LD le porte souvent) ; la vérification du prix prouve qu'il figure dans la page, pas qu'il s'agit du bon produit (rôle de la revue) ; un très petit prix (« 1 ») est plus facile à retrouver par hasard.

## ADR-018 — Migrations appliquées par un service dédié

**Statut** : acceptée (2026-10-26), décision du propriétaire du projet (« service de migration »). Remplace la règle de l'ADR-011 « l'API applique les migrations en Development ».

- `src/API/Pricely.MigrationService` (worker) compose les modules comme l'API (`AddPricelyAsync`), applique les migrations des cinq modules (`ApplyMigrationsAsync`) puis s'arrête. L'hôte n'est jamais démarré : aucun job (outbox, inbox, collecte) n'y tourne. La validation complète du conteneur reste celle de l'API (les services HTTP de la composition n'ont pas de routage dans un worker).
- L'AppHost lance `pricely-migrations` après PostgreSQL et ne démarre l'API qu'une fois le service terminé (`WaitForCompletion`). L'API n'applique plus aucune migration, quel que soit l'environnement.
- Hors AppHost (déploiement, poste sans Aspire) : lancer le service avec la même chaîne `ConnectionStrings__pricely-db`, avant l'API. Un échec de migration fait échouer le service (code de sortie non nul) et l'API n'est pas démarrée par l'AppHost.

## ADR-019 — Un produit de référence par conditionnement

**Statut** : acceptée (2026-10-26), décision du propriétaire du projet. Clôt la décision ouverte sur les conditionnements.

- Un sac de 25 kg et un sac de 35 kg, un lot de 10 et l'unité sont des produits de référence distincts (GTIN différents en général). Une offre rapproche une page d'un seul produit ; une enseigne qui vend plusieurs conditionnements a une offre par produit.
- La collecte suit la même règle : l'agent ne transmet pas un autre conditionnement (consigne de son prompt), et le garde-fou de prix (ADR-005, `PriceAnomalyPolicy`) signale ce qui lui échapperait.
- Pas de prix unitaire calculé ni de facteur de conversion entre conditionnements pour l'instant.

## ADR-020 — Paramétrage assisté par l'IA : enseignes et produits

**Statut** : acceptée (2026-10-26), décisions du propriétaire du projet (« site → fiche complète », « produit depuis une page produit »).

- **Principe** : l'IA propose, l'utilisateur valide. Une proposition n'enregistre rien ; l'enregistrement passe par les commandes existantes (`RegisterMerchant`, `RegisterProduct`) et leurs règles. Ce que le modèle avance est vérifié par du code, jamais repris tel quel.
- **Lecture des pages partagée** : `IWebPageReader` (Common.Application) et `HttpWebPageReader` (Common.Infrastructure) remplacent le lecteur propre à Pricing (ADR-017 inchangé : adresses publiques vérifiées à la connexion, sans proxy, une tentative, 3 Mo). Le lecteur ne suit jamais une redirection lui-même ; `ReadFollowingAsync` la suit si l'appelant l'accepte (le site de l'enseigne pour Pricing). Il extrait aussi le titre et les pages de recherche que le site déclare (`SearchAction` des données structurées, formulaires GET avec un champ de recherche).
- **Enseigne** (Pricing, fonctionnalité `pricing.merchant-setup`) : `POST pricing/merchants/suggestions` lit la page d'accueil (redirections suivies, l'adresse finale devient le site), donne au modèle le texte, les pages de recherche déclarées et les typologies, puis :
  - ne garde qu'une page de recherche sur le site (`Merchant.BelongsToSite`), sinon la première déclarée ;
  - la teste par une vraie recherche (mot proposé par le modèle) : la page répond-elle, le mot y figure-t-il ;
  - ne garde que des typologies connues.
  Sans modèle (échec, pas de clé), la proposition vient du site seul (titre, page déclarée). `POST pricing/merchants/{id}/profiles` ajoute ensuite l'enseigne (active) aux profils choisis : une typologie qui héritait ses enseignes reçoit sa propre liste (les héritées, puis la nouvelle, `CollectionProfile.AddPreferredMerchant`), un profil par sauvegarde. L'adresse finale du site (après redirections) est montrée à l'utilisateur, qui peut la corriger.
- **Produit** (Catalog, fonctionnalité `catalog.product-entry`) : `POST catalog/products/suggestions` lit une page produit (fabricant ou enseigne) ; le GTIN n'est gardé que s'il est valide et que la page l'écrit comme un seul nombre (d'un bloc ou par groupes séparés d'espaces ou de tirets, avec ou sans ses zéros de tête ; jamais des chiffres voisins mis bout à bout), la marque et la référence fabricant que si elles y figurent (ensemble, elles confirment les offres automatiquement), la typologie que si elle existe. Un produit au même GTIN est signalé (recherché sous ses longueurs 8, 12, 13 et 14). Un GTIN seul ne suffit pas : sans API de recherche (ADR-017), il faut l'adresse d'une page.
- **Front** : `/merchants` (« Ajouter une enseigne avec l'IA » : proposition modifiable, état du test de recherche, typologies à cocher) et `/catalog` (« Pré-remplir avec l'IA » dans le formulaire produit). Scope `pricely.write`.

## ADR-021 — Tarifs des modèles : LiteLLM, l'IA en secours, appliqués par l'utilisateur

**Statut** : acceptée (2026-10-26), décisions du propriétaire du projet (« LiteLLM, IA en secours », « proposés, tu valides »).

- `POST aiengine/models/price-proposals` cherche le tarif de chaque modèle du catalogue et le présente à côté du tarif en vigueur ; rien n'est enregistré.
  1. **LiteLLM** (`IPublishedPriceList`, `LiteLlmPriceList`) : la liste publique `model_prices_and_context_window.json` (adresse dans `AiEngine:Prices:LiteLlmPriceList`), entrée `identifiant` ou `fournisseur/identifiant`, fournisseur vérifié (`litellm_provider`), prix par jeton × 1 000 000. Lue par un client HTTP ordinaire (proxy de la machine admis : c'est une source fixe, pas une adresse donnée par un utilisateur), une tentative, 60 s.
  2. **IA en secours** (fonctionnalité `aiengine.price-reading`), pour les modèles que LiteLLM ne connaît pas : la page de tarifs du fournisseur (`AiEngine:Prices:PricingPages:<Fournisseur>`, Anthropic : `https://platform.claude.com/docs/en/about-claude/pricing`) est lue par `IWebPageReader`, réduite aux lignes de prix, et le modèle extrait les tarifs. Un tarif n'est gardé que s'il est positif et que la page l'affiche (« $3 », « $3.75 », quelque part dans la page, pas forcément sur la ligne du modèle) ; il est marqué « à vérifier ». Le texte envoyé au modèle se limite aux lignes qui portent un prix ou un mot de l'identifiant ou du libellé d'un modèle demandé.
  3. Sans tarif de cache publié, le tarif d'entrée est proposé (signalé). Sans source, le modèle reste à saisir à la main.
- **Application** : l'utilisateur applique une proposition (ou toutes) dans `/ai` ; `SetModelPriceCommand` reçoit sa source (`PriceSource.LiteLlm`, `AiReading`, `Manual` par défaut), le tarif vaut à partir du jour (UTC) et le coût des appels passés ne change pas (tarif figé, ADR-007). Pas d'import automatique planifié.
- `/ai` permet aussi d'ajouter un modèle au catalogue (`POST aiengine/models`).

## ADR-022 — Contrats de module : lire un autre module sans copier ses données

**Statut** : acceptée (2026-10-26), décision du propriétaire du projet (« le contrat dans le module, référencé par la couche Application d'un autre module, plutôt que dupliquer par événement d'intégration »).

- **Contrat** : un projet optionnel `Pricely.Modules.<M>.Contracts` dans le module propriétaire, qui expose sa **surface publique de lecture** aux autres modules. Interfaces et `sealed record` immuables uniquement (plus les erreurs en classe statique), un dossier par sujet (`Categories/`) ; aucun type du Domain, aucune entité EF, aucun `IQueryable`. Il ne référence que `Common.Domain` (pour `Result`).
- **Qui le référence** : la couche **Application** d'un autre module (`<Autre>.Application` → `<M>.Contracts`), jamais son Domain ni sa Presentation ; l'Infrastructure du propriétaire le référence pour l'implémenter. C'est la seule référence entre modules, avec `*.IntegrationEvents`.
- **Implémentation** : `internal sealed` dans `<M>.Infrastructure/Contracts/<Sujet>/`, enregistrée dans `<M>Module` (`AddScoped`). Lecture **EF Core** sur le `DbContext` du module (`AsNoTracking`, projection `Select`), sans règle métier : la requête projette. Pas une requête MediatR : une requête porte une intention métier, un contrat est une lecture technique entre modules. Appel interne de confiance, aucune autorisation, jamais exposé tel quel par un endpoint.
- **Forme des méthodes** : asynchrones, `Task<Result<T>>`, `CancellationToken` en dernier paramètre, volumes bornés (pagination par curseur plutôt qu'`IAsyncEnumerable`) : le contrat doit pouvoir être servi demain par HTTP si le module sort du process. Une panne du propriétaire est un `Result` en échec que l'appelant propage. Conséquence d'exécution : l'Application du consommateur exige le contrat, enregistré par le module propriétaire ; un hôte doit donc composer les deux modules (c'est le cas de `AddPricelyAsync`).
- **Quand l'utiliser** :

  | Besoin | Mécanisme |
  |---|---|
  | Lire ponctuellement la donnée d'un autre module | son **contrat** |
  | Réagir à un changement dans un autre module | **événement d'intégration** (outbox / inbox), qui peut ne porter que des identifiants |
  | Garder une copie locale | événement d'intégration + read model, **à justifier** (lecture très fréquente, historisation, disponibilité) |

  Jamais d'interrogation en boucle d'un contrat pour détecter un changement : la réaction passe par un événement.
- **Première application : les typologies.** `Catalog.Contracts` expose `IProductCategoryReader` (`GetAsync`, `GetLineageAsync` — la typologie puis ses ancêtres, la plus proche d'abord —, `GetAllAsync`) et `ProductCategorySummary` (`Id`, `Name`, `ParentId`, `Depth`). Pricing l'utilise pour résoudre le profil de collecte (`CollectionProfileLookup`), vérifier qu'une typologie existe (`DefineCollectionProfile`, `AddMerchantToProfiles`) et présenter les typologies à l'assistant de paramétrage (`SuggestMerchant`). Le read model `CatalogCategory`, son dépôt, ses deux commandes d'upsert / renommage, leurs handlers d'événements et ses consumers sont supprimés ; la migration `DropCatalogCategories` retire la table `pricing.catalog_categories`. Une typologie créée ou renommée dans Catalog est donc visible tout de suite, sans attendre l'outbox ni l'inbox. Les identifiants de typologie restent des `Guid` simples dans Pricing (`CollectionProfile.ProductCategoryId`), sans clé étrangère entre schémas : Catalog n'a pas d'identifiant naturel pour une typologie.
- **Produits** : `CatalogProduct` est passé par contrat à son tour (ADR-025). Les événements `ProductCategory*` et `Product*` restent publiés par Catalog (contrat d'intégration stable, messages d'outbox / inbox existants) mais n'ont plus de consommateur.
- **Garde** : `test/Pricely.ArchitectureTests/ContractsTests.cs` (références limitées à `Common.Domain`, contenu du projet, forme des méthodes, implémentations `internal`, Domain et Presentation des consommateurs sans accès au contrat) et `ModuleBoundaryTests` (Users et AiEngine n'ont aucun accès à `Catalog.Contracts`).

## ADR-023 — Les handlers de requête sont toujours dans Application

**Statut** : acceptée (2026-10-26), décision du propriétaire du projet (« les query handlers doivent toujours être implémentés côté Application »). Remplace la convention « handler Dapper dans Infrastructure » de `CLAUDE.md`. Le point « Dapper accessible à l'Application » est remplacé par l'ADR-024 (`ReadStore`).

- Commandes et requêtes ont leur handler dans `Application/<Agrégats>/<UseCase>/`, à côté du message et de sa réponse. Les 19 handlers de requête de Catalog, Pricing, Users, AiEngine et Agentic qui étaient en Infrastructure y ont été déplacés (même code, espace de noms de la requête) ; `UsageSql` (AiEngine) les a suivis.
- ~~Dapper et `IDbConnectionFactory` sont accessibles à l'Application~~ (remplacé par l'ADR-024 : l'Application n'accède plus aux données, elle appelle des `ReadStore`). L'Application ne dépend ni d'EF Core ni de Dapper.
- L'Infrastructure n'implémente que des ports : dépôts, lecteurs de contrat (ADR-022), collecteurs, jobs, outbox / inbox.
- Gardes : `test/Pricely.ArchitectureTests/QueryHandlerTests.cs` (aucun `IQueryHandler` dans une Infrastructure ; aucune dépendance d'Application à `Microsoft.EntityFrameworkCore`).
- Règles de l'équipe rassemblées dans `.claude/rules/backend-architecture.md`.

## ADR-024 — Les lectures passent par des `ReadStore` ; EF Core par défaut, Dapper cantonné à l'Infrastructure

**Statut** : acceptée (2026-10-26), décision du propriétaire du projet (« EF Core avec `AsNoTracking` pour les requêtes, pas de Dapper dans `Common.Application`, des interfaces implémentées côté infrastructure ; si trop conséquent, Dapper en implémentation mais côté Infrastructure »). Précise l'ADR-023 (le handler reste dans Application) et remplace son point « Dapper accessible à l'Application ».

- **Nom** : `I<Sujet>ReadStore` (le côté lecture d'un sujet, par opposition au dépôt qui porte l'écriture) ; `Reader` reste réservé aux contrats entre modules (ADR-022).
- **Forme** : interface dans `Application/<Agrégats>/` (`IProductReadStore`, `IOfferReadStore`, `IUsageReadStore`…), qui renvoie des `Response` ou des lignes (`...Row`) ; implémentation `internal sealed` dans `Infrastructure/<Agrégats>/<Sujet>ReadStore.cs`, enregistrée dans `<M>Module`. Le handler de requête (Application) appelle le `ReadStore`, puis fait ce qui reste : `NotFound`, règle du domaine, enrichissement par le contrat d'un autre module. Plus aucun accès aux données dans l'Application.
- **EF Core par défaut** (`AsNoTracking`, projection `Select`) : changer de moteur ne touche que l'Infrastructure, sans réécrire du SQL. Fait ainsi : Catalog (produits, typologies), Pricing (enseignes, résumé des profils).
- **Dapper conservé, dans l'Infrastructure seulement**, pour les lectures que LINQ rend mal : jointures latérales et médiane (offres de Pricing), agrégats `FILTER` (usage d'AiEngine, avec la dernière tarification en vigueur), requêtes sur les tables de rôles (Users) et lectures multi-tables d'Agentic. Le SQL, inchangé, est réparti dans les `ReadStore` (sauf la recherche de produits, passée en EF Core : le GTIN saisi est désormais normalisé par `Gtin.Create`, un EAN-13 trouve donc le produit stocké, et les profils de collecte sont triés par nom côté handler) ; il est PostgreSQL, donc à réécrire si le moteur change. À convertir en EF Core au fil des besoins.
- **`Dapper` quitte `Common.Application`** ; `IDbConnectionFactory` passe dans `Common.Infrastructure` (espace de noms `Pricely.Common.Infrastructure.Data`).
- **Gardes** : `QueryHandlerTests` (aucun handler de requête en Infrastructure ; l'Application ne dépend ni d'EF Core ni de Dapper ; un `ReadStore` est `internal` et vit en Infrastructure).

## ADR-025 — Les produits de Catalog se lisent par contrat, comme les typologies

**Statut** : acceptée (2026-10-26), décision du propriétaire du projet (« faire la même chose pour les produits »). Complète l'ADR-022, dont la mention « `CatalogProduct` reste un read model » est remplacée.

- `Catalog.Contracts` expose `IProductReader` (`GetAsync`, `GetByIdsAsync` — au plus 1 000 identifiants —, `GetIdsPageAsync` par curseur) et `ProductSummary` (`Id`, `Name`, `Brand`, `Gtin`, `ManufacturerReference`, `ProductCategoryId`). Implémentation `ProductReader` (EF Core, lecture seule, `internal`).
- Dans Pricing, le read model `CatalogProduct`, son dépôt, sa configuration, la commande d'upsert et les deux handlers d'événements disparaissent ; la migration `DropCatalogProducts` supprime `pricing.catalog_products`. Le planificateur de collecte lit le produit chez Catalog ; le job de collecte passe par `GetProductsToCollectQuery` (Application), qui parcourt le catalogue par curseur, car l'Infrastructure n'utilise pas le contrat d'un autre module.
- **Conséquence sur les jointures** : les requêtes de Pricing qui joignaient `catalog_products` ou `catalog_categories` (anomalies de prix, offres à valider, résumé des profils) ne le peuvent plus. Leur `ReadStore` renvoie des lignes avec l'identifiant du produit ou de la typologie, et le handler les complète par le contrat (lecture par lot, `ProductLookup`). Une offre ou une observation dont le produit n'existe plus chez Catalog est écartée, comme le faisait la jointure interne. Un produit ou une typologie créé dans Catalog est visible tout de suite dans Pricing, sans événement. Écart assumé : la limite (`LIMIT`) des anomalies est appliquée avant l'écart des produits inconnus, un appel peut donc renvoyer un peu moins de lignes que demandé si un produit a disparu de Catalog (cas rare) ; les offres à valider dont le produit a disparu ne figurent plus dans la file.
- **Plus de consommateur d'événement dans Pricing** : `PricingModule.ConfigureConsumers` est vide, la référence à `Catalog.IntegrationEvents` est retirée de Pricing. Catalog continue de publier ses événements (voir décisions ouvertes).

## ADR-026 — Collecte : page produit par GTIN et prix déclarés par la page

**Statut** : acceptée (2026-10-27), décision du propriétaire du projet (« vas-y pour le collecteur »), après une mesure de cinq enseignes. Complète l'ADR-017 sans en changer les règles de lecture.

- **Mesure (2026-10-27, depuis un centre de données)** : Chausson se lit entièrement (recherche `?s=`, JSON-LD avec prix et GTIN) ; E.Leclerc rend sa recherche en JavaScript mais ses pages produit, ouvertes par le GTIN (`/fp/{gtin13}`), portent prix, vendeur et GTIN en JSON-LD ; Auchan se lit en microdonnées, avec plusieurs prix par page et sans GTIN ; Leroy Merlin (captcha DataDome) et Point.P (403) refusent la lecture. Pricely ne contourne pas ces protections ; pour ces enseignes, flux partenaires ou autre source (décision ouverte).
- **Page produit par GTIN** : une enseigne peut déclarer `Merchant.ProductPageTemplate`, adresse de son site avec `{gtin13}` ou `{gtin14}` (`PUT pricing/merchants/{id}/product-page`, écran `/merchants`). Quand le produit de référence a un GTIN, `ProductPageShortcut` ouvre cette page **avant l'agent** (par `MerchantPageReader`, mêmes règles d'adresse et d'instantané) et, si ses données structurées déclarent un produit **au même GTIN** (règle de `OfferMatchingPolicy`, 14 chiffres) avec un prix, transmet l'offre au `SubmittedOfferVerifier` sans appeler le modèle (sans similarité, aucun modèle n'ayant comparé ; une offre sans devise déclarée est laissée à l'agent). L'offre retenue est la première `Offer` de la page (celle du vendeur que la page met en avant, nommé dans la justification) ; une borne d'`AggregateOffer` seulement s'il n'y a rien d'autre. Sinon, l'enseigne passe à l'agent, qui reçoit l'adresse de la page par GTIN. Les offres lues ainsi sont gardées même si l'agent échoue ensuite pour les autres enseignes.
- **Données structurées** : le lecteur de pages (`HtmlText.Products`) extrait du JSON-LD les `Product` (aussi sous `@graph` ou `mainEntity`) : nom, GTIN, marque, référence, offres (prix, devise, disponibilité, vendeur). Un prix absent ou nul n'est pas une offre ; un `AggregateOffer` à `offerCount: 0` (produit indisponible) non plus. Un bloc JSON-LD invalide est ignoré. Les microdonnées (Auchan) ne sont pas encore lues comme offres : elles restent dans le texte.
- **Prix déclarés** : l'instantané garde les prix que la page déclare (`PageSnapshot.DeclaredPrices`, colonne `declared_prices`, tous les produits du JSON-LD, 200 au plus : jamais moins que ce que le lecteur extrait, pour qu'un carrousel déclaré avant le produit ne pousse pas son prix dehors). Quand il y en a, **un prix soumis doit être l'un d'eux** (agent comme raccourci) : le texte d'une page montre aussi l'éco-contribution et la livraison. Les prix d'autres produits *déclarés* dans la page (carrousel en JSON-LD) restent acceptés : c'est la revue et le GTIN qui disent si c'est le bon produit. Une page qui ne déclare qu'une fourchette (`AggregateOffer`) n'accepte que ses bornes. Sans données structurées, la règle de l'ADR-017 (le prix figure dans le texte) s'applique toujours. Migration `AddProductPageAndDeclaredPrices` ; les instantanés existants n'ont aucun prix déclaré.
- **Ce qui ne change pas** : l'agent ne confirme jamais ; une offre lue par GTIN passe par `OfferMatchingPolicy` comme les autres (elle se confirme par le GTIN que la page déclare). Une place de marché (vendeur tiers) reste une offre de l'enseigne : c'est le prix que l'acheteur paie sur son site.

## ADR-027 — Recherche web de l'assistant derrière `IWebSearcher`, coût journalisé

**Statut** : acceptée (2026-10-27), décision du propriétaire du projet (« que l'assistant cherche sur le web et pas uniquement dans ma base », Anthropic d'abord, « la recherche web abstraite dans une interface » pour comparer le coût d'Anthropic et de Brave). Étape A d'un chantier en trois étapes ; l'écriture en lot (B) et son écran (C) suivent.

- **Port** : `IWebSearcher` (`Common.Application/Web`) : une requête donne des pages (titre, adresse, extrait, âge), **jamais un prix vérifié**. Implémentation `WebSearcher` (`Common.Infrastructure/Web/Search`) qui choisit le fournisseur par configuration (`WebSearch:Provider`, `Anthropic` par défaut ou `Brave`), services à clé `IWebSearchProvider`. Changer de fournisseur ne touche que la configuration.
- **Anthropic** : outil serveur `web_search_20260318` (appel direct, sans filtrage dynamique, `max_uses` = `WebSearch:MaxSearchesPerRequest`, localisation FR), modèle choisi par la fonctionnalité `common.web-search` (cible modifiable dans AiEngine) ; les extraits viennent des citations. Facturation : recherches (`usage.server_tool_use.web_search_requests`) + jetons, ces derniers journalisés comme tout appel LLM (`LlmCalledIntegrationEvent`). **Brave** : API Search (`X-Subscription-Token`, clé `WebSearch:Brave:ApiKey` en user-secrets), une recherche par requête, sans jetons.
- **Journal et coût** : chaque requête publie `WebSearchPerformedIntegrationEvent` (bus direct) ; AiEngine l'enregistre (`WebSearch`, table `aiengine.web_searches`, migration `AddWebSearches`) avec le tarif du fournisseur (`AiEngine:WebSearch:PricePerSearch`, 0,01 $ pour Anthropic ; Brave sans tarif par défaut, compté « sans tarif » tant qu'il n'est pas configuré). `GET aiengine/usage/web-search` et l'écran `/ai` comparent les fournisseurs : requêtes, échecs, recherches facturées, résultats moyens, temps moyen, coût des recherches, coût du modèle, coût par requête.
- **Plafond** : `WebSearch:MaxSearchesPerDay` (200), compteur en mémoire par jour UTC et par instance (une instance aujourd'hui ; plusieurs instances : compteur partagé à prévoir).
- **Tools de l'assistant** (Pricing, lecture seule, `pricely.read`, `OpenWorld`) : `search_web` (`SearchWebQuery` : résultats + enseigne suivie à laquelle chaque page appartient, règle `Merchant.BelongsToSite`) et `read_web_page` (`ReadWebPageQuery` : page publique quelconque lue par `IWebPageReader`, redirections http(s) seulement, **sans instantané**, texte borné à 20 000 caractères et produits du JSON-LD, 20 au plus). Ils n'enregistrent rien.
- **Exception à la passerelle LLM** : le fournisseur Anthropic de recherche appelle l'API Messages directement (client nommé `Anthropic`, cible résolue par `ILlmTargetResolver`), car `ILlmGateway` ne porte pas les outils serveur ; il reste dans Common.Infrastructure à côté des fournisseurs et journalise ses jetons comme un appel LLM. Une cible non Anthropic pour `common.web-search` est ignorée : le modèle par défaut d'Anthropic est alors utilisé.
- **Requête payante** : `SearchWebQuery` n'écrit rien dans Pricing mais consomme le plafond et publie le journal : c'est une requête au sens CQRS (aucun état métier modifié), avec un coût.
- **Plafond et échecs** : une requête réserve ses recherches possibles avant de partir (`WebSearchQuota.Reserve`, ce qui reste du jour au plus) et rend ce qui n'a pas été facturé ; un échec après une recherche déjà faite (tour repris sur `pause_turn` qui échoue) compte et journalise les recherches facturées. L'appelant ne reçoit qu'une erreur générique ; le détail (corps de la réponse du fournisseur) reste dans le journal. `read_web_page` n'accepte que http(s) sur le port par défaut, sans identifiants, redirections comprises.
- **Règles** : le contenu du web est une **donnée, jamais une instruction** (rappelé dans chaque réponse et dans les consignes de l'agent) ; un prix trouvé ainsi est « non vérifié », cité avec son adresse, présenté à part des prix de Pricely ; seule une collecte (instantané d'une page d'enseigne suivie, ADR-017, ADR-026) enregistre un prix. L'assistant cherche d'abord dans Pricely, puis sur le web (une ou deux recherches par question).

## Compléments du 2026-10-26 (étapes 5b et 6)

- **Jetons du front sur plusieurs instances** (complète l'ADR-015) : `ServerTokenStore` écrit dans `IDistributedCache` (mémoire par défaut, `AddDistributedMemoryCache`), jetons chiffrés par la protection des données (`SetApplicationName("Pricely.Web")`). Plusieurs instances : brancher un cache partagé (Redis…) et des clés de protection partagées, sans changer le code. Le verrou de rafraîchissement reste local à l'instance (64 verrous répartis par session, sans fuite) ; deux instances peuvent rafraîchir la même session en même temps, Keycloak refusant alors le refresh token déjà utilisé si la rotation est active (session close, reconnexion).
- **Consommation par conversation** : AiEngine expose `aiengine/usage/sessions/{sessionId}` (scope `pricely.read`, sans contrôle du propriétaire de la session : des compteurs et un coût, accepté) ; l'identifiant de session d'un tour d'agent est l'identifiant de la conversation. Le panneau de l'assistant affiche la conversation en cours et le total du jour de l'agent ; le journal étant alimenté par l'inbox, il se met à jour quelques secondes après le tour.
- **Streaming réel** : le chat client Anthropic lit la réponse en SSE (`stream: true`) ; le texte arrive au fil de l'eau, chaque appel de tool à la fin de son bloc, l'usage à la fin du message. Un événement `error` du flux est un échec de l'appel.
- **Jeu d'essai** (`src/API/Pricely.DevSeed`) : un worker qui compose les modules comme l'API et passe par leurs commandes (typologies, produits, enseignes, profils), jamais par du SQL (seule exception : il attend les read models de Pricing par leurs dépôts, faute de requête). Il refuse de démarrer hors `Development`. L'hôte est démarré le temps du jeu d'essai : l'outbox de Catalog et l'inbox de Pricing alimentent les read models comme en production. Les relevés viennent d'un `IOfferCollector` propre au jeu d'essai (pages scriptées, 13 semaines), enregistré avant les modules ; la collecte, le rapprochement et le garde-fou de prix sont ceux du domaine. Enseignes fictives sur `.example` (RFC 2606) : l'agent de collecte ne peut pas les lire. Rejouable : ce qui existe (même nom, même GTIN, produit déjà doté d'offres) est laissé tel quel. Ressource `pricely-seed` de l'AppHost à démarrage manuel (`WithExplicitStart`), après les migrations. Il référence les commandes des modules : c'est un hôte, comme l'API, jamais référencé par un module.
- **Écrans d'alimentation** : `/catalog` (typologies, produits) et `/merchants` (ajout, page de recherche, désactivation et réactivation, `POST pricing/merchants/{id}/activate`). Les règles restent celles de l'API (trois niveaux, clé du GTIN, noms uniques), dont le front affiche les erreurs.
- **Tests d'intégration hérités** (`Pricely.Modules.Users.IntegrationTests`, `Pricely.IntegrationTests`) supprimés : ils visaient un module `Ticketing` du template et ne compilaient plus.

## Décisions ouvertes

- Enseignes qui refusent la lecture automatique (Leroy Merlin, Point.P, ADR-026) : flux partenaires (catalogues d'affiliation, avec GTIN) ou prix « non vérifiés » tirés d'une recherche web, gardés à part des prix vérifiés. Microdonnées (Auchan) à lire comme offres structurées.
- Test d'intégration des contrats (ADR-022) : l'implémentation `ProductCategoryReader` a été vérifiée contre un PostgreSQL 16 jetable (lignée, tri `citext`, cycle borné), mais le dépôt n'a pas de projet de tests d'intégration (les anciens ont été supprimés, voir plus haut). À créer (Testcontainers ou PostgreSQL de l'AppHost) avant d'ajouter d'autres contrats.
- Événements `ProductCategoryCreated`, `ProductCategoryRenamed`, `ProductRegistered`, `ProductUpdated` : plus aucun consommateur depuis les ADR-022 et ADR-025 (l'inbox de Pricing n'a plus de consommateur non plus). Les retirer de Catalog (domain event handlers, outbox) et de Pricing (inbox, job) ou les garder pour un futur abonné.
- Conservation des instantanés de pages (`pricing.page_snapshots`) et limite de débit des lectures de pages : `fetch_merchant_page` (un jeton `pricely.read` peut faire lire des pages d'enseigne), `pricing/merchants/suggestions` et `catalog/products/suggestions` (un jeton `pricely.write` fait lire une adresse publique quelconque et appeler un modèle, ADR-020) : rien ne purge ni ne limite aujourd'hui ; à fixer avant d'activer la collecte planifiée ou d'ouvrir `/mcp` hors du poste de développement.
- Garde-fou de prix : un relevé signalé reste un prix de référence pour les relevés suivants des autres offres (`GetLatestComparablePricesAsync`) ; un prix de lot peut ainsi faire signaler le prix normal d'une autre enseigne relevé juste après. Exclure les relevés signalés de la référence est à décider (règle métier).
- Sites rendus en JavaScript : un navigateur sans tête (Playwright) derrière le même port `IWebPageReader`, si le HTML et le JSON-LD ne suffisent pas.
- Recherche web (ADR-027), risque d'exfiltration par injection de consignes : `search_web` et `read_web_page` sont en lecture seule, donc exécutés sans validation ; une page lue peut pousser le modèle à mettre des données de la conversation dans l'adresse d'un `read_web_page` vers un site tiers. Les consignes de l'agent et l'avertissement de chaque réponse le réduisent sans l'écarter. Parade proposée : n'accepter dans `read_web_page` qu'une adresse renvoyée par une recherche récente ou appartenant à une enseigne suivie. À décider.
- Recherche web (ADR-027), rattachement à la conversation : les recherches et les jetons de `common.web-search` sont journalisés sous leur propre session, pas sous celle de la conversation de l'assistant (Pricing ne connaît pas la conversation) ; le coût d'une conversation les omet. Parade proposée : un contexte d'exécution d'agent (session, agent) exposé par Common.Application et renseigné par Agentic pendant l'appel d'un tool. À décider.
- Recherche web (ADR-027) : limite par utilisateur (le plafond est global), compteur partagé entre instances, tarif de Brave à configurer après choix de l'offre ; `read_web_page` permet à un jeton `pricely.read` de faire lire une page publique quelconque (même réserve que les suggestions).
- Proxy sortant obligatoire en production : la lecture des pages se connecte en direct (ADR-017) ; un proxy demanderait de vérifier l'adresse côté proxy.
