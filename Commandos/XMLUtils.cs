using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualBasic;

namespace Commandos;

static class XmlUtils
{
    static readonly XDocument Doc = XDocument.Load("Commandos.xml");

    public static string GetSummary(MethodInfo method)
    {
        var member = GetMember(method);
        if (member == null) return "No description provided.";

        return NormalizeXmlText(member.Element("summary")?.Value ?? "");
    }

    public static IReadOnlyList<MethodParameter> GetParameters(MethodInfo method)
    {
        var member = GetMember(method);
        if (member == null) return Array.Empty<MethodParameter>();

        var xmlParams = member.Elements("param")
            .ToDictionary(
                p => p.Attribute("name")!.Value,
                p => NormalizeXmlText(p.Value)
            );

        return method.GetParameters()
            .Select(p => new MethodParameter(
                Type: GetFriendlyType(p.ParameterType),
                Name: p.Name!,
                Description: xmlParams.GetValueOrDefault(p.Name!, ""),
                DefaultValue: p.HasDefaultValue ? p.DefaultValue : null
            ))
            .ToList();

    }

    private static XElement? GetMember(MethodInfo method)
    {
        string memberName = GetMemberName(method);

        return Doc.Descendants("member")
            .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);
    }

    private static string GetMemberName(MethodInfo method)
    {
        var typeName = method.DeclaringType!.FullName!;
        var parameters = method.GetParameters();

        if (parameters.Length == 0)
            return $"M:{typeName}.{method.Name}";

        string paramList = string.Join(
            ",",
            parameters.Select(p => p.ParameterType.FullName)
        );

        return $"M:{typeName}.{method.Name}({paramList})";
    }

    private static readonly Dictionary<Type, string> TypeAliases = new()
    {
        { typeof(void), "void" },
        { typeof(bool), "bool" },
        { typeof(byte), "byte" },
        { typeof(short), "short" },
        { typeof(int), "int" },
        { typeof(long), "long" },
        { typeof(float), "float" },
        { typeof(double), "double" },
        { typeof(decimal), "decimal" },
        { typeof(char), "char" },
        { typeof(string), "string" },
        { typeof(object), "object" }
    };

    private static string GetFriendlyType(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return $"{GetFriendlyType(underlying)}?";
        }
        
        if (TypeAliases.TryGetValue(type, out var alias))
        {
            return alias;
        }
        
        if (type.IsArray)
        {
            return $"{GetFriendlyType(type.GetElementType()!)}[]";
        }
        
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return $"{GetFriendlyType(type.GetGenericArguments()[0])}[]";
        }
        
        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            var args = string.Join(
                ", ",
                type.GetGenericArguments().Select(GetFriendlyType)
            );
            return $"{name}<{args}>";
        }

        return type.Name;
    }


    private static string NormalizeXmlText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return string.Join(
            "\n",
            text.Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
        );
    }

    public static string ParametersToString(IEnumerable<MethodParameter> parameters)
    {
        var sb = new StringBuilder();

        foreach (var p in parameters)
        {
            sb.Append((p.DefaultValue != null ? "[Opt.] " : "") + $"({p.Type})<{p.Name}>");

            if (p.DefaultValue != null)
                sb.Append($" = {FormatDefaultValue(p.DefaultValue)}");

            if (!string.IsNullOrWhiteSpace(p.Description))
                sb.Append($" - {p.Description}");

            sb.Append('\n');
        }

        if (sb.Length > 0)
            sb.Length--;
        
        return string.IsNullOrWhiteSpace(sb.ToString()) ? "None" : sb.ToString();
    }

    private static string FormatDefaultValue(object value)
    {
        return value switch
        {
            string s => $"\"{s}\"",
            char c => $"'{c}'",
            bool b => b.ToString().ToLower(),
            _ => value.ToString()!
        };
    }

}

public record MethodParameter(string Type, string Name, string Description, object? DefaultValue);
