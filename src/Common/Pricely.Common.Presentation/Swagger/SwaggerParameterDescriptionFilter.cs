using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Pricely.Common.Presentation.Swagger;
public class SwaggerParameterDescriptionFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null || !operation.Parameters.Any())
        {
            return;
        }

        context.ApiDescription.TryGetMethodInfo(out MethodInfo? x);
        if (x is null)
        {
            return;
        }

        var attributes = x!.GetCustomAttributes<SwaggerParameterDescriptionAttribute>().ToList();
        if (!attributes.Any())
        {
            return;
        }

        foreach (IOpenApiParameter? parameter in operation.Parameters)
        {
            SwaggerParameterDescriptionAttribute? attr = attributes.FirstOrDefault(it => it.Parameter == parameter.Name);
            if (attr != null && !string.IsNullOrWhiteSpace(attr.Value))
            {
                parameter.Description = attr.Value;
            }
        }
    }
}
