using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricely.Common.Presentation.Swagger;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SwaggerParameterDescriptionAttribute : Attribute
{
    public string Parameter { get; }

    public string Value { get; }

    public SwaggerParameterDescriptionAttribute(string parameter, string value)
    {
        Parameter = parameter;
        Value = value;
    }
}
