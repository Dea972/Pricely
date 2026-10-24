using Microsoft.Extensions.Hosting;

namespace Pricely.Infrastructure;

public static class EnvironmentExtensions
{
    public static bool IsTest(this IHostEnvironment environment)
    {
        return environment.IsEnvironment("Test") || environment.EnvironmentName == "Test";
    }
}
