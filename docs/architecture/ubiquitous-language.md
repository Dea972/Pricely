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
| `Merchant` | Enseigne | Vendeur dont on collecte les prix (site web). Peut être désactivée (`IsActive`). |
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
| `Money` | Prix | Montant strictement positif dans une devise ISO 4217. |
| `Availability` | Disponibilité | `InStock`, `OutOfStock`, `Unknown`. |
| `PriceAnomalyPolicy` | Garde-fou prix | Signale un relevé qui s'écarte de plus de 40 % de la médiane des autres offres confirmées. |
| `IsFlaggedAsAnomaly` | Prix suspect | Relevé signalé par le garde-fou, à vérifier (souvent un mauvais rapprochement ou un conditionnement différent). |
| `IOfferCollector` | Collecteur | Port vers l'extérieur qui trouve les pages enseignes et lit leurs prix (agent IA, scraper, flux). |
| `CollectedOffer` | Offre collectée | Résultat normalisé d'un collecteur pour une enseigne. |
| `OfferCollectionPlan` | Plan de collecte | Produit, enseignes et instructions d'une collecte. |
| `CatalogProduct`, `CatalogCategory` | Copies locales du catalogue | Read models de Pricing alimentés par les événements de Catalog. |

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

## Agents et MCP

| Code | Français | Définition |
|---|---|---|
| Tool | Outil | Capacité offerte aux agents (MCP et agent interne), fine, adossée à une requête ou une commande. |
| `AgentDefinition` | Agent | Agent déclaré par l'application (dans `src/Agents`) et exécuté par le module Agentic : instructions, fonctionnalité IA, tools autorisés. Ex. `assistant`. |
| `AgentActivity` | Activité d'agent | Entrée du journal d'AiEngine : un tool exécuté pour un agent, ou une décision sur une action proposée. |
| `AgentActionDecision` | Décision | Issue d'une action proposée, journalisée par AiEngine : `Executed` (validée et exécutée), `Failed` (validée, mais le tool a échoué), `Rejected` (refusée). |
| `AgentConversation` | Conversation | Échanges d'un utilisateur avec un agent, rejouables ; visibles par leur seul propriétaire. |
| `PendingAction` | Action proposée | Appel d'un tool d'écriture par un agent, en attente de la décision de l'utilisateur ; exécutée seulement après « Valider ». |
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
