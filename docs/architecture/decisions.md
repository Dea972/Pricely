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

## Décisions ouvertes

- Démarrage de l'API : câbler `AddPricelyAsync` dans `Program.cs` et réactiver le `DbContext` Users, ou isoler Users tant qu'il n'est pas nettoyé.
- Migrations : via l'hôte ou via des `IDesignTimeDbContextFactory` par module (skill `add-migration`).
- Collecteur : agent IA (`Dea972/Agent` ou `Microsoft.Extensions.AI`), choix du fournisseur et des enseignes à scraper en direct.
- Plusieurs conditionnements d'un même produit chez une enseigne : aujourd'hui un produit de référence par conditionnement.
