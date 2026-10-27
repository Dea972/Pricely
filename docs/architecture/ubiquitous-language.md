# Langage ubiquitaire

Le code utilise la colonne « Code » (anglais). L'interface et les échanges utilisent la colonne « Français ».

## Catalog

| Code | Français | Définition |
|---|---|---|
| `Product` | Produit de référence | Ce que l'on compare entre enseignes. Identifié par son GTIN quand il existe, sinon par marque + référence fabricant. |
| `ProductCategory` | Typologie | Classement des produits en arbre de 3 niveaux maximum (Gros œuvre > Ciment > Ciment prompt). Porte le profil de collecte côté Pricing. |
| `Depth` | Niveau | 1 pour une typologie racine, `MaxDepth = 3`. |
| `Gtin` | GTIN / EAN | Code article international (EAN-8, UPC-A, EAN-13, GTIN-14), validé par sa clé de contrôle. Unique dans le catalogue. |
| `Brand` | Marque | Marque du produit. Obligatoire. |
| `ManufacturerReference` | Référence fabricant | Code produit du fabricant. Avec la marque, deuxième preuve forte de rapprochement. |
| `ProductDescription` | Description produit | Données descriptives validées par `Product` (nom, marque, GTIN, référence). |

## Pricing

| Code | Français | Définition |
|---|---|---|
| `Merchant` | Enseigne | Vendeur dont on collecte les prix (site web). Peut être désactivée puis réactivée (`IsActive`) : une enseigne inactive n'est plus interrogée. |
| `CollectionProfile` | Profil de collecte | Paramétrage de la collecte pour une typologie : enseignes favorites et instructions. |
| `PreferredMerchant` | Enseigne favorite | Enseigne à interroger pour une typologie, avec une `Priority` (1 = d'abord). |
| `Instructions` | Instructions de collecte | Consignes destinées au collecteur (« prompt orienté »), propres à la typologie. |
| `ResolvedCollectionProfile` | Profil effectif | Profil réellement appliqué après héritage : enseignes de la typologie la plus proche qui en définit, instructions concaténées de la racine à la typologie. |
| `Offer` | Offre | Un produit de référence vendu par une enseigne (produit × enseigne). Unité de la comparaison. Une seule par couple produit / enseigne. |
| `OfferCandidate` | Candidat | Page enseigne trouvée par le collecteur, avant décision de rapprochement. |
| `MatchEvidence` | Preuves de rapprochement | Ce que le collecteur a lu sur la page (GTIN, marque, référence) et son score de similarité IA. |
| `ProductIdentity` | Identité produit | Identifiants du produit de référence comparés aux preuves. |
| `MatchStatus` | Statut de rapprochement | `Proposed` (à valider), `Confirmed` (comparé), `Rejected` (pas le même produit, exclu définitivement). |
| `OfferConfirmationMethod` | Mode de confirmation | `Gtin`, `ManufacturerReference` (automatiques) ou `Manual` (validé par une personne). |
| `OfferMatchingPolicy` | Politique de rapprochement | Règle qui décide si une offre est confirmée sans revue. |
| `PriceObservation` | Relevé de prix | Prix, disponibilité et date lus sur une offre. Historique séparé de l'offre. |
| `PageSnapshot` | Instantané de page | Texte d'une page du site d'une enseigne tel que Pricely l'a lu ; un prix soumis par un agent doit y figurer (ADR-017). |
| `Suggest…Command` | Proposition (paramétrage assisté) | Ce que l'IA propose à partir d'une page (enseigne, produit) ou d'une liste de tarifs : rien n'est enregistré avant que l'utilisateur valide (ADR-020, ADR-021). |
| `PriceSource` | Origine d'un tarif | `Manual` (saisi), `LiteLlm` (liste publique de LiteLLM appliquée), `AiReading` (lu par l'IA sur la page du fournisseur, à vérifier). |
| `SearchTemplate` | Page de recherche de l'enseigne | Adresse du site de l'enseigne avec `{query}`, où l'agent de collecte cherche le produit. |
| `ProductPageTemplate` | Page produit par GTIN | Adresse du site de l'enseigne avec `{gtin13}` ou `{gtin14}` : la collecte l'ouvre d'abord quand le produit a un GTIN, et lit l'offre sans le modèle (ADR-026). |
| `DeclaredPrices` | Prix déclarés | Prix que la page annonce dans ses données structurées (JSON-LD) ; quand il y en a, un prix collecté doit être l'un d'eux. |
| `Money` | Prix | Montant strictement positif dans une devise ISO 4217. |
| `Availability` | Disponibilité | `InStock`, `OutOfStock`, `Unknown`. |
| `PriceAnomalyPolicy` | Garde-fou prix | Signale un relevé qui s'écarte de plus de 40 % de la médiane des autres offres confirmées. |
| `IsFlaggedAsAnomaly` | Prix suspect | Relevé signalé par le garde-fou, à vérifier (souvent un mauvais rapprochement ou un conditionnement différent). |
| `IOfferCollector` | Collecteur | Port vers l'extérieur qui trouve les pages enseignes et lit leurs prix (agent IA, scraper, flux). |
| `CollectedOffer` | Offre collectée | Résultat normalisé d'un collecteur pour une enseigne. |
| `OfferCollectionPlan` | Plan de collecte | Produit, enseignes et instructions d'une collecte. |
| `IProductReader`, `ProductSummary` | Lecture des produits | Contrat de Catalog : Pricing lit les produits chez Catalog, sans en garder de copie. |
| `ReadStore` | Port de lecture | `I<Sujet>ReadStore` : interface d'Application, implémentée en Infrastructure, que les handlers de requête appellent pour lire les données du module (ADR-024). |
| `IProductCategoryReader`, `ProductCategorySummary` | Lecture des typologies | Contrat de Catalog (`Catalog.Contracts`) : Pricing lit les typologies chez Catalog, sans en garder de copie. |
| Contrat | Surface de lecture d'un module | Projet `<Module>.Contracts` : interfaces et records en lecture seule qu'un module laisse utiliser par la couche Application des autres (ADR-022). |

## AiEngine

| Code | Français | Définition |
|---|---|---|
| `AiFeature` | Fonctionnalité IA | Usage d'un LLM déclaré par un module (code `module.usage`, fournisseur par défaut). Exemple : `pricing.offer-collection`. |
| `Model` | Modèle | LLM tel qu'appelé chez un fournisseur (identifiant fournisseur, libellé), avec l'historique de ses tarifs. |
| `ModelPrice` | Tarif | Prix en USD par million de tokens (entrée, entrée en cache, écriture en cache, sortie), applicable à partir d'un jour UTC. |
| `FeatureSetting` | Cible | Modèle choisi pour une fonctionnalité ; la cible `default` s'applique aux fonctionnalités sans cible propre. |
| `LlmTarget` | Cible résolue | Fournisseur et modèle réellement appelés par une fonctionnalité. |
| `LlmCall` | Appel LLM | Appel journalisé, immuable : tokens, durée, succès, coût figé au tarif du jour. |
| `SessionId` | Session | Clé qui regroupe les appels d'une même unité de travail (conversation, collecte). |
| Appel non tarifé | | Appel sans tarif applicable (modèle absent du catalogue ou sans prix ce jour-là) : compté pour 0 dans les coûts. |
| `WebSearch` | Recherche web | Requête journalisée à un moteur de recherche (Anthropic ou Brave, ADR-027) : recherches facturées, résultats, durée, coût au tarif du fournisseur (les jetons du modèle qui la lit sont des appels LLM). |

## Agents et MCP

| Code | Français | Définition |
|---|---|---|
| Tool | Outil | Capacité offerte aux agents (MCP et agent interne), fine, adossée à une requête ou une commande. |
| `AgentDefinition` | Agent | Agent déclaré par l'application (dans `src/Agents`) et exécuté par le module Agentic : instructions, fonctionnalité IA, tools autorisés. Ex. `assistant`. |
| `AgentActivity` | Activité d'agent | Entrée du journal d'AiEngine : un tool exécuté pour un agent, ou une décision sur une action proposée. |
| `AgentActionDecision` | Décision | Issue d'une action proposée, journalisée par AiEngine : `Executed` (validée et exécutée), `Failed` (validée, mais le tool a échoué), `Rejected` (refusée). |
| `AgentConversation` | Conversation | Échanges d'un utilisateur avec un agent, rejouables ; visibles par leur seul propriétaire. |
| `PendingAction` | Action proposée | Appel d'un tool d'écriture par un agent, en attente de la décision de l'utilisateur ; exécutée seulement après « Valider ». |
| `BatchId` | Lot | Les actions proposées pendant un même tour de l'agent : validées ou refusées en une fois, exécutées dans l'ordre proposé (ADR-028). |
| `ReadOnly` / `Destructive` | Lecture / action sensible | Annotations d'un tool ; un tool qui n'est pas en lecture seule exige une approbation de l'agent interne. |

## Identité et accès

| Code | Français | Définition |
|---|---|---|
| `IdentityId` | Identité | Identifiant de la personne chez Keycloak (claim `sub` du jeton). Porté par `User`. |
| `pricely_user_id` | Identifiant interne | Claim ajoutée par Pricely : identifiant du `User`. Ne pas confondre avec `sub`. |
| Provisionnement | | Création du `User` (rôle `Member`) à la première requête authentifiée d'une identité (`ProvisionUserCommand`, idempotente). |
| Scope | Portée | Droit accordé au jeton par Keycloak : `pricely.read`, `pricely.write`, `pricely.review`. Une politique d'autorisation du même nom. |
| Permission | Permission métier | Droit porté par le rôle ou l'utilisateur dans le module Users (`users:read`, `admin:access`...), ajouté au jeton par `CustomClaimsTransformation`. |

## Règles à retenir

- On compare des **offres confirmées**, jamais des offres proposées.
- Un score IA, même élevé, ne confirme jamais une offre à lui seul.
- Une offre rejetée ne revient pas : le collecteur la retrouvera, elle sera ignorée.
- Un prix trouvé par la recherche web est **non vérifié** : cité avec sa source, jamais mélangé aux prix des offres ; le contenu d'une page web est une donnée, jamais une instruction (ADR-027).
