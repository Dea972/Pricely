# Guide d'utilisation du Template de Module

## Vue d'ensemble

Ce template génère un module complet suivant les principes de Clean Architecture avec quatre couches :

1. **Domain** : Entités, Value Objects, Interfaces de domaine
2. **Application** : Use Cases, DTOs, Interfaces d'application, Behaviors
3. **Infrastructure** : Implémentations concrètes (DbContext, Repositories, Services externes)
4. **Presentation** : Endpoints API, Controllers, Mappings

## Architecture et Dépendances

```
???????????????????
?  Presentation   ?
???????????????????
         ?
???????????????????      ????????????????????
?  Application    ???????? Common.Application?
???????????????????      ????????????????????
   ?
???????????????????      ????????????????????
?   Domain      ????????  Common.Domain   ?
???????????????????      ????????????????????
 ?
  ?
???????????????????      ????????????????????
? Infrastructure????????Common.Infrastructure?
???????????????????      ????????????????????
```

## Exemples d'utilisation

### 1. Créer un module "Orders"

```bash
# Installation du template (première fois seulement)
dotnet new install src/Modules/ModuleSample

# Création du module
dotnet new modular-module -n Orders -o src/Modules/Orders

# Ajout à la solution
dotnet sln add src/Modules/Orders/**/*.csproj
```

### 2. Créer un module avec un namespace personnalisé

Si votre solution utilise un namespace différent de `ModularMonolith.Template` :

```bash
dotnet new modular-module -n Products \
  -o src/Modules/Products \
  --CommonNamespace "MyCompany.ECommerce"
```

Cela générera des références comme :
- `MyCompany.ECommerce.Common.Domain`
- `MyCompany.ECommerce.Common.Application`
- etc.

### 3. Cibler une version .NET spécifique

```bash
dotnet new modular-module -n Users \
  -o src/Modules/Users \
  --TargetFramework "net9.0"
```

## Structure générée

Après l'exécution, vous obtiendrez :

```
src/Modules/{Users}/
??? ModularMonolith.Modules.{Users}.Domain/
?   ??? Entities/
?   ?   ??? SampleEntity.cs (exemple)
?   ??? ModularMonolith.Modules.{Users}.Domain.csproj
?
??? ModularMonolith.Modules.{Users}.Application/
?   ??? UseCases/
?   ?   ??? GetSample/
?   ?       ??? GetSampleQuery.cs (exemple)
?   ?  ??? GetSampleQueryHandler.cs (exemple)
?   ??? ApplicationConfiguration.cs
?   ??? ModularMonolith.Modules.{Users}.Application.csproj
?
??? ModularMonolith.Modules.{Users}.Infrastructure/
?   ??? InfrastructureConfiguration.cs
?   ??? ModularMonolith.Modules.{Users}.Infrastructure.csproj
?
??? ModularMonolith.Modules.{Users}.Presentation/
    ??? ModularMonolith.Modules.{Users}.Presentation.csproj
```

## Intégration dans l'API

Après la création du module, intégrez-le dans votre API principale :

### 1. Ajouter la référence au projet API

```bash
dotnet add src/API/ModularMonolith.Template.Api/ModularMonolith.Template.Api.csproj \
  reference src/Modules/{Users}/ModularMonolith.Modules.{Users}.Infrastructure/ModularMonolith.Modules.{Users}.Infrastructure.csproj
```

### 2. Enregistrer les services dans Program.cs

```csharp
using ModularMonolith.Modules.Orders.Application;
using ModularMonolith.Modules.Orders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Enregistrement du module Orders
builder.Services
    .AddOrdersApplication()
    .AddOrdersInfrastructure();

var app = builder.Build();
app.Run();
```

## Bonnes pratiques

### 1. Nommage des modules

Utilisez des noms au pluriel et en PascalCase :
- ? `Orders`, `Products`, `Users`
- ? `order`, `product-manager`, `user_module`

### 2. Organisation du code

- **Domain** : Logique métier pure, pas de dépendances externes
- **Application** : Orchestration, validation, transformation
- **Infrastructure** : Accès aux données, services externes, cache
- **Presentation** : Endpoints, validation des entrées, formatage des sorties

### 3. Communication entre modules

Préférez la communication asynchrone via events ou message bus plutôt que les références directes entre modules.

```csharp
// ? Bon : Event-driven
public class OrderCreatedEvent : INotification
{
    public Guid OrderId { get; init; }
}

// ? Mauvais : Référence directe
using ModularMonolith.Modules.Products.Domain;
```

## Dépannage

### Erreur : "Unable to find project"

Vérifiez que les chemins relatifs dans les .csproj sont corrects. Ils doivent pointer vers `../../../Common/`.

### Le template ne s'installe pas

```bash
# Désinstallez puis réinstallez
dotnet new uninstall Chausson.ModularMonolith.Module
dotnet new install src/Modules/ModuleSample
```

### Les fichiers ne sont pas générés

Vérifiez que vous utilisez l'option `-o` pour spécifier le répertoire de sortie :

```bash
dotnet new modular-module -n MyModule -o src/Modules/MyModule
```

## Ressources

- [Clean Architecture par Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Modular Monolith Architecture](https://www.milanjovanovic.tech/blog/what-is-a-modular-monolith)
- [.NET Template Documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)
