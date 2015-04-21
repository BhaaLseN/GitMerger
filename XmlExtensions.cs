using System;
using System.Xml.Linq;

namespace GitMerger
{
    public static class XmlExtensions
    {
        public static string AttributeValue(this XElement element, XName name)
        {
            if (element == null)
                throw new ArgumentNullException("element", "element is null.");
            var attribute = element.Attribute(name);
            return attribute != null ? attribute.Value : string.Empty;
        }
        public static XElement ElementPath(this XElement element, params XName[] names)
        {
            if (element == null)
                throw new ArgumentNullException("element", "element is null.");
            foreach (XName name in names)
            {
                element = element.Element(name);
                if (element == null)
                    break;
            }
            return element;
        }
        public static string ElementValue(this XElement element, params XName[] names)
        {
            var targetElement = element.ElementPath(names);
            return targetElement != null ? targetElement.Value : string.Empty;
        }
        public static string ElementValue(this XElement element, XName name)
        {
            var targetElement = element.Element(name);
            return targetElement != null ? targetElement.Value : string.Empty;
        }
    }
}