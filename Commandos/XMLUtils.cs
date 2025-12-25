using System.Reflection;
using System.Xml.Linq;

namespace Commandos;


static class XmlUtils
{
    static readonly XDocument Doc = XDocument.Load("Commandos.xml");

    public static string GetSummary(MethodInfo method)
    {
        string memberName = GetMemberName(method);

        var member = Doc.Descendants("member").FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

        if (member == null) return "No description provided.";

        string summary = member.Element("summary")?.Value ?? "";
        return NormalizeXmlText(summary);
    }
    
    private static string GetMemberName(MethodInfo method)
    {
        var typeName = method.DeclaringType!.FullName;
        var parameters = method.GetParameters();

        if (parameters.Length == 0) return $"M:{typeName}.{method.Name}";

        string paramList = string.Join(",", parameters.Select(p => p.ParameterType.FullName));

        return $"M:{typeName}.{method.Name}({paramList})";
    }
    
    private static string NormalizeXmlText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        
        var lines = text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0);
        
        return string.Join("\n", lines);
    }
}