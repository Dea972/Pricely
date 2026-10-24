using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace Pricely.Common.Presentation.Swagger;

public class SwaggerDefaultValueFilter : IOperationFilter
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

        var attributes = x!.GetCustomAttributes<SwaggerDefaultValueAttribute>().ToList();
        if (!attributes.Any())
        {
            return;
        }

        foreach (OpenApiParameter? parameter in operation.Parameters.OfType<OpenApiParameter?>())
        {
            if (parameter is null)
            {
                continue;
            }

            SwaggerDefaultValueAttribute? attr = attributes.FirstOrDefault(it => it.Parameter == parameter.Name);
            if (attr != null)
            {
                if (!string.IsNullOrWhiteSpace(attr.Value))
                {
                    parameter.Example = JsonValue.Create(attr.Value);
                }

                if (attr.Values is not null)
                {
                    var jsonArray = new JsonArray();
                    foreach (string value in attr.Values)
                    {
                        jsonArray.Add(JsonValue.Create(value));
                    }
                    parameter.Example = jsonArray;
                }
            }
        }
    }
}
