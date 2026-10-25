---
name: new-aggregate
description: Add a DDD aggregate (or entity / value object) to a Pricely module with its errors, domain events, repository, EF Core configuration and unit tests. Use when modelling a new business concept.
---

# Nouvel agrégat

Références : `Pricing.Domain/Offers/Offer.cs` (cycle de vie + politique), `Pricing.Domain/CollectionProfiles/CollectionProfile.cs` (collection possédée), `Catalog.Domain/Products/Gtin.cs` et `Pricing.Domain/Offers/Money.cs` (value objects).

Un agrégat = une frontière transactionnelle. Si deux concepts n'ont pas d'invariant commun, ce sont deux agrégats liés par identifiant (ex. `Offer` / `PriceObservation`, séparés parce que l'historique grossit).

## Domaine (`Pricely.Modules.<M>.Domain/<Agrégats>/`)

```csharp
public sealed class Thing : Entity
{
    public const int NameMaxLength = 200;

    private Thing()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public static Result<Thing> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            return Result.Failure<Thing>(ThingErrors.InvalidName);
        }

        Thing thing = new() { Id = Guid.CreateVersion7(), Name = name.Trim() };

        thing.Raise(new ThingCreatedDomainEvent(thing.Id));

        return thing;
    }
}
```

- Constructeur privé sans paramètre **et aucun constructeur public** (testé par `DomainTests`).
- Factory statique : `Result<T>` si elle valide, `T` sinon. Méthodes de comportement nommées avec le langage ubiquitaire (`Confirm`, `Reject`, `Propose`, pas `SetStatus`).
- `ThingErrors` : `static class`, `Error.Problem / NotFound / Conflict`, codes `"Things.Cas"`. Constantes de longueur réutilisées par les validateurs et la configuration EF.
- Événement : `public sealed class ThingCreatedDomainEvent(Guid thingId) : DomainEvent` avec `{ get; init; }`. Suffixe `DomainEvent` obligatoire.
- Value object : `sealed record` à constructeur privé + `static Result<T> Create`. Collection possédée : `private readonly List<X> _xs = [];` exposée en `IReadOnlyList<X>` ; mettre à jour les éléments existants plutôt que vider et recréer (conflits de clé EF).
- Règle sans état qui implique plusieurs objets : `static class XxxPolicy` dans le domaine (ex. `OfferMatchingPolicy`, `PriceAnomalyPolicy`, `CollectionProfileResolver`).
- `IThingRepository` dans le domaine : `GetAsync`, requêtes métier précises, `Insert`. Pas de `Update` (suivi de modifications EF) ni de `Delete` sans besoin.

## Infrastructure (`Pricely.Modules.<M>.Infrastructure/<Agrégats>/`)

- `ThingConfiguration : IEntityTypeConfiguration<Thing>` (`internal sealed`) : `ToTable("things")`, `HasKey`, **`builder.Ignore(t => t.DomainEvents)`**, `HasMaxLength(Thing.NameMaxLength)`, enums `HasConversion<string>()`, VO `ComplexProperty(... HasColumnName("..."))`, index uniques métier. Clé vers un autre module : simple colonne + commentaire, pas de `HasOne`.
- `ThingRepository(<M>DbContext context)` (`internal sealed`), méthodes en corps expression quand c'est possible.
- Ajouter le `DbSet` (`internal`) et `ApplyConfiguration` au `DbContext`, le repository dans `<M>Module`.
- Puis migration : skill `add-migration`.

## Tests (`<M>.UnitTests/<Agrégats>/ThingTests.cs`)

Tester chaque invariant, chaque transition et chaque événement levé (`AssertDomainEventWasPublished<T>`). Noms `Method_ShouldX_WhenY`. Rappel analyseurs : `var id = Guid.NewGuid();`, `var thing = Thing.Create(...)` seulement si `Create` retourne `Thing` (sinon type explicite `Result<Thing>`).
