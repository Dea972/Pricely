---
name: add-offer-collector
description: Implement or extend an IOfferCollector in the Pricing module (AI agent, dedicated merchant scraper, partner feed) as an anti-corruption layer returning normalized CollectedOffer. Use when working on price collection, scraping, crawling or the AI collection agent.
---

# Collecteur d'offres

La collecte n'est **pas** un module (ADR-001) : c'est le port `IOfferCollector` de `Pricing.Application/Abstractions/Collection`, implémenté dans `Pricing.Infrastructure/Collection`.

```csharp
public interface IOfferCollector
{
    Task<Result<IReadOnlyCollection<CollectedOffer>>> CollectAsync(
        OfferCollectionRequest request,
        CancellationToken cancellationToken = default);
}
```

## Ce que reçoit le collecteur

`OfferCollectionRequest` construit par `OfferCollectionPlanner` :
- `Product` : `ProductId`, `Name`, `Brand`, `Gtin?`, `ManufacturerReference?` (read model Catalog).
- `Merchants` : enseignes **actives** à interroger, par `Priority` (1 d'abord), avec `Website`. Résultat de l'héritage de profil (typologie la plus proche qui en définit).
- `Instructions` : consignes de la typologie, de la plus générale à la plus précise (concaténées par `CollectionProfileResolver.InstructionsSeparator`). C'est le « prompt orienté » : l'intégrer tel quel au prompt d'un agent IA.

## Ce que doit rendre le collecteur

Des `CollectedOffer` normalisés, jamais de HTML brut :
- `MerchantId` : uniquement une enseigne de la requête (les autres sont ignorées et comptées dans `IgnoredCount`).
- `ProductPage` (`Uri` absolue), `MerchantProductReference?`, `PriceAmount` (TTC, unité de vente affichée), `Currency` (ISO 4217), `Availability`, `ObservedOnUtc`.
- `Evidence` (`MatchEvidence`) : `ObservedGtin`, `ObservedBrand`, `ObservedManufacturerReference` **tels que lus sur la page** et `AiSimilarity` entre 0 et 1. Ne jamais recopier les identifiants du produit de référence dans l'évidence : c'est ce qui déclenche la confirmation automatique (`OfferMatchingPolicy`) et fausserait la comparaison.
- Échec global (configuration, quota, panne) : `Result.Failure` avec une `Error` explicite. Page introuvable chez une enseigne : simplement ne pas renvoyer d'offre pour elle.

## Implémentations

- **Agent IA** (cible) : adaptateur vers le runtime `Dea972/Agent` ou `Microsoft.Extensions.AI`. Prompt = description du produit + enseignes + `Instructions`, sortie structurée (JSON → `CollectedOffer`), validation stricte avant de rendre le résultat. Clés en user-secrets / variables d'environnement, options `Pricing:Collection:Agent:*`.
- **Scraper dédié** pour une enseigne stratégique ou qui bloque l'agent : `<Merchant>OfferCollector` interne, `HttpClient` typé avec `Microsoft.Extensions.Http.Resilience`, respect du `robots.txt` et des CGU, rate limiting.
- **Plusieurs implémentations** : un `CompositeOfferCollector` qui choisit par enseigne (configuration ou propriété de `Merchant` à ajouter, à valider avec l'utilisateur) et agrège les résultats.

Enregistrement dans `PricingModule.AddPricingModule` : remplacer `services.TryAddScoped<IOfferCollector, NotConfiguredOfferCollector>();` (le `TryAdd` permet aussi de l'override depuis l'hôte). Activer ensuite le job : `Pricing:Collection:Enabled = true`, `IntervalInMinutes`.

## Exécution hors process

Playwright, proxys et rate limiting n'ont pas leur place dans le process de l'API. Le jour où c'est nécessaire : un host `Pricely.Worker` qui embarque `Pricing.Infrastructure` et exécute `CollectOffersJob`. Même module, autre process : pas de nouveau module.

## Tests

Tester le mapping et la validation de l'adaptateur avec des réponses enregistrées (fixtures), sans appel réseau. Vérifier qu'un `CollectedOffer` avec un GTIN observé identique aboutit bien à une offre `Confirmed`, et qu'un score IA élevé seul reste `Proposed`.
