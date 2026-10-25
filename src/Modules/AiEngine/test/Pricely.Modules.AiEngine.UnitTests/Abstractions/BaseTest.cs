using Bogus;
using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.UnitTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Faker Faker = new();

    public static T AssertDomainEventWasPublished<T>(Entity entity)
        where T : IDomainEvent
    {
        T? domainEvent = entity.DomainEvents.OfType<T>().SingleOrDefault()
            ?? throw new Exception($"{typeof(T).Name} was not published");

        return domainEvent;
    }
}
