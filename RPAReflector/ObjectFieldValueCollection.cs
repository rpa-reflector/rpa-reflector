using System.Collections.Generic;
using System.IO;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace RPAReflector
{
    [XmlRoot("ObjectFieldValueCollection", Namespace = "")]
    public class ObjectFieldValueCollection : Dictionary<string, string>, IXmlSerializable
    {
        public void ReadXml(XmlReader reader)
        {
            var root = (XElement)XNode.ReadFrom(reader);
            foreach (XElement keyValue in root.Elements("KeyValue"))
            {
                XElement keyEl   = keyValue.Element("Key");
                XElement valueEl = keyValue.Element("Value");

                if (keyEl == null)
                    throw new XmlException("KeyValue element is missing a <Key> child element");
                if (valueEl == null)
                    throw new XmlException("KeyValue element is missing a <Value> child element");

                Add(keyEl.Value, valueEl.Value);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var kvp in this)
            {
                writer.WriteStartElement("KeyValue");
                writer.WriteElementString("Key", kvp.Key);
                writer.WriteElementString("Value", kvp.Value);
                writer.WriteEndElement();
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public string ToXml()
        {
            var serializer = new XmlSerializer(typeof(ObjectFieldValueCollection));

            // Create a namespace with an empty namespace URI to remove namespaces from the XML output.
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            using (var stringWriter = new StringWriter())
            {
                // Serialize the collection to XML.
                serializer.Serialize(stringWriter, this, namespaces);
                return stringWriter.ToString();
            }
        }
    }
}
