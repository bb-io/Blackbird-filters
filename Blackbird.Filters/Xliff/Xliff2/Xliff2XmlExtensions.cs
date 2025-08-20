using Blackbird.Filters.Enums;
using Blackbird.Filters.Transformations;
using System.Linq;
using System.Xml.Linq;

namespace Blackbird.Filters.Xliff.Xliff2;
public static class Xliff2XmlExtensions
{
    public static string? Get(this XElement element, XName name, Optionality optional = Optionality.Optional)
    {
        var value = element.Attribute(name)?.Value;
        if (value == null && optional == Optionality.Required)
        {
            throw new Exception($"The {name.LocalName} attribute is required but not found in {element.Name}");
        }
        return value;
    }

    public static void Set(this XElement element, XName name, string? value)
    {
        if (value != null)
        {
            element.SetAttributeValue(name, value);
        }
    }

    public static bool? GetBool(this XElement element, XName name, Optionality optional = Optionality.Optional)
    {
        var value = element.Get(name, optional);
        return value != null ? value == "yes" : null;
    }

    public static void SetBool(this XElement element, XName name, bool? value)
    {
        if (value.HasValue)
        {
            element.SetAttributeValue(name, value.Value ? "yes" : "no");
        }
    }

    public static Direction? GetDirection(this XElement element, XName name, Optionality optional = Optionality.Optional)
    {
        var value = element.Get(name, optional);
        return value?.ToDirection();
    }

    public static void SetDirection(this XElement element, XName name, Direction? value)
    {
        if (value.HasValue)
        {
            element.SetAttributeValue(name, value.Value.Serialize());
        }
    }

    public static int? GetInt(this XElement element, XName name, Optionality optional = Optionality.Optional)
    {
        var value = element.Get(name, optional);
        return value != null ? int.Parse(value) : null;
    }

    public static void SetInt(this XElement element, XName name, int? value)
    {
        if (value.HasValue)
        {
            element.SetAttributeValue(name, value.Value);
        }
    }

    public static LanguageTarget? GetLanguageTarget(this XElement element, XName name, Optionality optional = Optionality.Optional)
    {
        var value = element.Get(name, optional);
        return value?.ToLanguageTarget();
    }

    public static void SetLanguageTarget(this XElement element, XName name, LanguageTarget? value)
    {
        if (value.HasValue)
        {
            element.SetAttributeValue(name, value.Value.Serialize());
        }
    }

    public static SegmentState? GetState(this XElement element, XName name, Optionality optional = Optionality.Optional)
    {
        var value = element.Get(name, optional);
        return value?.ToSegmentState();
    }

    public static void SetState(this XElement element, XName name, SegmentState? value)
    {
        if (value.HasValue)
        {
            element.SetAttributeValue(name, value.Value.Serialize());
        }
    }

    public static InlineTagType? GetInlineType(this XElement element, XName name, Optionality optional = Optionality.Optional)
    {
        var value = element.Get(name, optional);
        return value?.ToInlineTagType();
    }

    public static void SetInlineType(this XElement element, XName name, InlineTagType? value)
    {
        if (value.HasValue)
        {
            element.SetAttributeValue(name, value.Value.Serialize());
        }
    }

    public static Reorder? GetReorder(this XElement element, XName name, Optionality optional = Optionality.Optional)
    {
        var value = element.Get(name, optional);
        return value?.ToReorder();
    }

    public static void SetReorder(this XElement element, XName name, Reorder? value)
    {
        if (value.HasValue)
        {
            element.SetAttributeValue(name, value.Value.Serialize());
        }
    }

    public static Xliff2Version GetXliffVersion(this XElement element, XName name, Optionality optional = Optionality.Required)
    {
        var value = element.Get(name, optional);
        return value?.ToXliff2Version() ?? Xliff2Version.Xliff22;
    }

    public static void SetXliffVersion(this XElement element, XName name, Xliff2Version? value)
    {
        if (value.HasValue)
        {
            element.SetAttributeValue(name, value.Value.Serialize());
        }
    }

    public static WhiteSpaceHandling GetWhiteSpaceHandling(this XElement element, WhiteSpaceHandling fromParent = WhiteSpaceHandling.Default)
    {
        XNamespace xml = "http://www.w3.org/XML/1998/namespace";
        var xmlSpace = element.Attribute(xml + "space");
        if (xmlSpace != null)
        {
            if (xmlSpace.Value == "preserve") return WhiteSpaceHandling.Preserve;
            return WhiteSpaceHandling.Default;
        }

        return fromParent;
    }

    public static void SetWhiteSpaceHandling(this XElement element, WhiteSpaceHandling handling)
    {
        XNamespace xml = "http://www.w3.org/XML/1998/namespace";
        element.SetAttributeValue(xml + "space", handling == WhiteSpaceHandling.Default ? "default" : "preserve");
    }

    public static List<XAttribute> GetRemaining(this IEnumerable<XAttribute> attributes, XName[] usedAttributes)
    {
        return attributes.Where(a => !usedAttributes.Contains(a.Name)).ToList();
    }

    public static List<XElement> GetRemaining(this IEnumerable<XElement> elements, XName[] usedElements)
    {
        return elements.Where(a => !usedElements.Contains(a.Name)).ToList();
    }
}