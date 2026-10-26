---
name: new-use-case
description: Add a CQRS use case to a Pricely module - command or query with handler, validator, response, HTTP endpoint and tests. Use for any new operation exposed by a module.
---

# Nouveau cas d'usage

Références : commande `Pricing.Application/Offers/ConfirmOffer`, commande avec résultat `Catalog.Application/Products/RegisterProduct`, requête Dapper `Pricing.Infrastructure/Offers/GetProductPriceComparisonQueryHandler.cs`, endpoint `Pricing.Presentation/Offers/ConfirmOffer.cs`.

## Commande

Dossier `Application/<Agrégats>/<UseCase>/` :

```csharp
public sealed record ConfirmOfferCommand(Guid OfferId) : ICommand;            // ou ICommand<Guid>

internal sealed class ConfirmOfferCommandValidator : AbstractValidator<ConfirmOfferCommand>
{
    public ConfirmOfferCommandValidator()
    {
        RuleFor(c => c.OfferId).NotEmpty();
    }
}

internal sealed class ConfirmOfferCommandHandler(
    IOfferRepository offerRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmOfferCommand>
{
    public async Task<Result> Handle(ConfirmOfferCommand request, CancellationToken cancellationToken)
    {
        Offer? offer = await offerRepository.GetAsync(request.OfferId, cancellationToken);
        if (offer is null)
        {
            return Result.Failure(OfferErrors.NotFound(request.OfferId));
        }

        Result result = offer.Confirm(dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

- Le handler orchestre, la logique métier est dans le domaine. Une transaction = un agrégat (exception documentée : `CollectedOfferRecorder` sauve offre + observation).
- Heure : `IDateTimeProvider`, jamais `DateTime.UtcNow` dans un handler.
- Plus de 7 dépendances (S107) : extraire des services d'application `public sealed class` enregistrés dans `<M>Module` (ex. `OfferCollectionPlanner`, `CollectedOfferRecorder`).

## Requête

- `Application/<Agrégats>/<UseCase>/<UseCase>Query.cs` : `public sealed record GetXQuery(...) : IQuery<XResponse>;`
- Response : `sealed record` positionnel jusqu'à 7 champs, au-delà `sealed class` à propriétés `{ get; init; }` (mappée par Dapper).
- Handler dans **Infrastructure** `/<Agrégats>/<UseCase>QueryHandler.cs` (`internal sealed`), Dapper sur `IDbConnectionFactory`, PostgreSQL (voir « PostgreSQL : pièges connus » de `CLAUDE.md`), schéma explicite (`pricing.offers`), colonnes snake_case aliasées par `nameof(XResponse.Prop)`, `new CommandDefinition(sql, request, cancellationToken: cancellationToken)`.
- Collections : `return Result.Success<IReadOnlyCollection<XResponse>>(rows.ToList());` (la conversion implicite vers une interface ne compile pas).
- Requête qui a besoin de règles du domaine : handler dans Application (ex. `GetEffectiveCollectionProfileQueryHandler`).

## Endpoint

`Presentation/<Agrégats>/<UseCase>.cs` :

```csharp
internal sealed class ConfirmOffer : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "pricing/offers/{offerId:guid}/confirm",
            async (Guid offerId, ISender sender, CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(new ConfirmOfferCommand(offerId), cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .RequireAuthorization()
        .WithTags(Tags.Offers);
    }
}
```

Route préfixée par le module (`catalog/...`, `pricing/...`), `/api` est ajouté par le SDK. Corps JSON : classe imbriquée `internal sealed class Request { public string Name { get; init; } }`. Découverte automatique (`AddEndpoints`).

## Tests

Tests unitaires du domaine touché. Les tests d'architecture vérifient nommage (`Command`, `CommandHandler`, `Query`, `QueryHandler`, `Validator`), `sealed` et visibilité.
