using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricely.Common.Presentation.Swagger;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SwaggerDefaultValueAttribute : Attribute
{
    public string Parameter { get; }

    public string Value { get; }
    public string[]? Values { get; }

    public SwaggerDefaultValueAttribute(string parameter, string value)
    {
        Parameter = parameter;
        Value = value;
    }

    public SwaggerDefaultValueAttribute(string parameter, string[] values)
    {
        Parameter = parameter;
        Values = values;
    }
}
