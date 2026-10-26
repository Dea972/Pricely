---
name: new-integration-event
description: Connect two Pricely modules with an integration event - publish from the source module through its outbox and consume it in the target module through its inbox into a local read model. Use whenever a module needs data or a signal from another module.
---

# Nouvel événement d'intégration

**D'abord, choisir le mécanisme** (ADR-022). Un événement d'intégration sert à **réagir** à un changement d'un autre module ou à **garder une copie locale** justifiée (lecture très fréquente, historisation, disponibilité). Pour **lire ponctuellement** une donnée d'un autre module, ne pas la copier : utiliser (ou ajouter) son projet `<S>.Contracts`, lu depuis l'Application du module cible (exemple : `IProductCategoryReader`, lu par Pricing). Ce skill ne couvre que l'événement.

Schéma de référence (Pricing ne consomme plus d'événement de Catalog depuis l'ADR-025 : il lit par contrat ; Catalog publie toujours `ProductRegisteredIntegrationEvent` et les autres, sans abonné). Les extraits ci-dessous prennent cet événement comme exemple.

Flux : agrégat `Raise(DomainEvent)` → `SaveChanges` (l'intercepteur écrit l'outbox du module source) → `ProcessOutboxJob` → `DomainEventHandler` publie l'`IntegrationEvent` sur MassTransit → `IntegrationEventConsumer<T>` du module cible écrit dans son inbox → `ProcessInboxJob` → `IntegrationEventHandler<T>` (Presentation) → commande d'Application → upsert idempotent.

## Côté source (module `<S>`)

1. `Pricely.Modules.<S>.IntegrationEvents/<Fait>IntegrationEvent.cs` :

```csharp
public sealed class ProductRegisteredIntegrationEvent : IntegrationEvent
{
    public ProductRegisteredIntegrationEvent(Guid id, DateTime occurredOnUtc, Guid productId)
        : base(id, occurredOnUtc)
    {
        ProductId = productId;
    }

    public Guid ProductId { get; init; }

    public required string Name { get; init; }

    public string? Gtin { get; init; }
}
```

   - Nom au passé, contrat **stable** : ajouter des champs, ne jamais en renommer ni en retirer sans versionner.
   - Uniquement des types primitifs (`Guid`, `string`, `decimal`, `DateTime`, enums sérialisés en chaîne si besoin). Aucun type du domaine.
   - Au-delà de 7 paramètres de constructeur (S107) : passer le reste en `required init`.
   - Sérialisation Newtonsoft avec `TypeNameHandling.All` : ne pas renommer / déplacer un type déjà publié en production sans migration de l'outbox / inbox.
2. Événement de domaine correspondant levé par l'agrégat (skill `new-aggregate`).
3. `Application/<Agrégats>/<UseCase>/<Fait>DomainEventHandler.cs` (`internal sealed`, `DomainEventHandler<T>`) : recharge l'agrégat si besoin et `await eventBus.PublishAsync(new XIntegrationEvent(domainEvent.Id, domainEvent.OccurredOnUtc, ...), cancellationToken)`. Réutiliser `domainEvent.Id` comme identifiant : c'est la clé d'idempotence côté inbox.

## Côté cible (module `<C>`)

1. `Pricely.Modules.<C>.Presentation.csproj` et `Pricely.Modules.<C>.Infrastructure.csproj` : `ProjectReference` vers `Pricely.Modules.<S>.IntegrationEvents` (et **rien d'autre** du module source).
2. Read model local dans `<C>.Domain/<S>/` (nom `<S>Xxx`, par exemple `CatalogProduct`) : classe simple (pas `Entity`), `Create` / `Update`, repository `GetAsync` + `Insert`, configuration EF `ValueGeneratedNever()` sur l'`Id` (identifiant source).
3. Commande `Upsert<X>Command` dans `<C>.Application/<S>/` : `GetAsync` puis `Insert` ou `Update`, `SaveChangesAsync`. Doit être idempotente (rejouable).
4. `<C>.Presentation/<S>/<Fait>IntegrationEventHandler.cs` :

```csharp
internal sealed class ProductRegisteredIntegrationEventHandler(ISender sender)
    : IntegrationEventHandler<ProductRegisteredIntegrationEvent>
{
    public override async Task Handle(
        ProductRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(new UpsertCatalogProductCommand(...), cancellationToken);

        if (result.IsFailure)
        {
            throw new PricelyException(nameof(UpsertCatalogProductCommand), result.Error);
        }
    }
}
```

   L'exception est enregistrée dans `inbox_messages.error` par `ProcessInboxJob`.
5. `<C>Module.ConfigureConsumers` : `registrationConfigurator.AddConsumer<IntegrationEventConsumer<XIntegrationEvent>>();`. Si c'est le premier consommateur du module, créer l'inbox (copier `Pricing.Infrastructure/Inbox`), `AddIntegrationEventHandlers`, options `<C>:Inbox` et passer `<C>Module.ConfigureConsumers` à `AddInfrastructure` dans `Pricely.Sdk`.
6. Migration du module cible (skill `add-migration`).

## Vérifications

- `test/Pricely.ArchitectureTests` passe (aucune dépendance vers `<S>.Domain|Application|Infrastructure|Presentation`).
- Rejouer deux fois le même événement ne crée pas de doublon.
