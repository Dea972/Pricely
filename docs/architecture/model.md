# Modèle

## Catalog (schéma `catalog`)

```mermaid
classDiagram
    class ProductCategory {
        Guid Id
        string Name
        Guid? ParentId
        int Depth  "1..3"
        +CreateRoot(name)
        +CreateChild(name, parent)
        +Rename(name)
    }
    class Product {
        Guid Id
        string Name
        string Brand
        Gtin? Gtin
        string? ManufacturerReference
        Guid ProductCategoryId
        +Register(description, categoryId)
        +Update(description, categoryId)
    }
    class Gtin {
        <<value object>>
        string Value
    }
    ProductCategory "0..1" <-- "*" ProductCategory : ParentId
    Product --> ProductCategory : ProductCategoryId
    Product *-- Gtin
```

Événements publiés : `ProductCategoryCreated`, `ProductCategoryRenamed`, `ProductRegistered`, `ProductUpdated`.

## Pricing (schéma `pricing`)

```mermaid
classDiagram
    class Merchant {
        Guid Id
        string Name
        Uri Website
        bool IsActive
        string? SearchTemplate
        +ConfigureSearch(template)
        +Owns(address)
    }
    class CollectionProfile {
        Guid Id
        Guid ProductCategoryId  "unique, Catalog"
        string? Instructions
        +Define(categoryId, merchantIds, instructions)
        +Update(merchantIds, instructions)
    }
    class PreferredMerchant {
        Guid MerchantId
        int Priority
    }
    class Offer {
        Guid Id
        Guid ProductId  "Catalog"
        Guid MerchantId
        Uri ProductPage
        string? MerchantProductReference
        MatchEvidence Evidence
        MatchStatus Status
        OfferConfirmationMethod? ConfirmationMethod
        +Propose(candidate, product, now)
        +Confirm(now)
        +Reject(now)
    }
    class PriceObservation {
        Guid Id
        Guid OfferId
        Money Price
        Availability Availability
        DateTime ObservedOnUtc
        bool IsFlaggedAsAnomaly
        +Record(offer, price, availability, observedOn, referencePrices)
    }
    class PageSnapshot {
        Guid Id
        Guid MerchantId
        Uri Address
        string Content
        DateTime FetchedOnUtc
        +Capture(merchant, address, content, now)
        +ShowsPrice(amount)
    }
    class CatalogProduct {
        <<read model>>
    }
    class ProductCategory {
        <<Catalog.Contracts>>
    }
    CollectionProfile *-- "*" PreferredMerchant
    PreferredMerchant --> Merchant
    Offer --> Merchant
    PriceObservation --> Offer
    PageSnapshot --> Merchant
    Offer ..> CatalogProduct : ProductId
    CollectionProfile ..> ProductCategory : ProductCategoryId
```

Flèches pointillées : identifiants d'un autre module, sans clé étrangère. `CatalogProduct` est une copie locale (événements de Catalog) ; `ProductCategory` n'est pas copiée : Pricing la lit chez Catalog par son contrat (ADR-022).

## Flux de collecte

```mermaid
sequenceDiagram
    participant Job as CollectOffersJob / endpoint
    participant H as CollectProductOffersCommandHandler
    participant P as OfferCollectionPlanner
    participant C as IOfferCollector
    participant R as CollectedOfferRecorder
    Job->>H: CollectProductOffersCommand(productId)
    H->>P: PlanAsync
    P-->>H: produit + enseignes actives (profil résolu) + instructions
    H->>C: CollectAsync(request)
    C-->>H: CollectedOffer[]
    loop par offre collectée d'une enseigne demandée
        H->>R: RecordAsync
        R->>R: Offer existante ? sinon Offer.Propose (OfferMatchingPolicy)
        R->>R: PriceObservation.Record (PriceAnomalyPolicy)
        R->>R: SaveChanges
    end
    H-->>Job: CollectProductOffersResponse (compteurs)
```
