using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace GitMerger
{
    public static class JsonHelper
    {
        private static readonly XmlDictionaryReaderQuotas InfiniteQuotas = new XmlDictionaryReaderQuotas
        {
            MaxArrayLength = int.MaxValue,
            MaxBytesPerRead = int.MaxValue,
            MaxDepth = int.MaxValue,
            MaxNameTableCharCount = int.MaxValue,
            MaxStringContentLength = int.MaxValue
        };

        public static void SerializeTo(XDocument content, Stream target)
        {
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(target))
                content.Save(writer);
        }

        public static XDocument DeserializeFrom(string jsonString)
        {
            try
            {
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(jsonString), InfiniteQuotas))
                    return XDocument.Load(reader);
            }
            catch
            {
                return null;
            }
        }
    }
}
