using Microsoft.VisualStudio.TestTools.UnitTesting;
using RPAReflector;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace UnitTestProject
{
    [TestClass]
    public class ObjectFieldValueCollectionTests
    {
        // --- ToXml() ---

        [TestMethod]
        public void ToXml_SingleEntry_ProducesExpectedXml()
        {
            var collection = new ObjectFieldValueCollection { { "key1", "value1" } };

            string xml = collection.ToXml();

            StringAssert.Contains(xml, "<Key>key1</Key>");
            StringAssert.Contains(xml, "<Value>value1</Value>");
            StringAssert.Contains(xml, "<KeyValue>");
        }

        [TestMethod]
        public void ToXml_EmptyCollection_ProducesRootElementOnly()
        {
            var collection = new ObjectFieldValueCollection();

            string xml = collection.ToXml();

            StringAssert.Contains(xml, "<ObjectFieldValueCollection");
            Assert.IsFalse(xml.Contains("<KeyValue>"), "Empty collection should not contain KeyValue elements");
        }

        [TestMethod]
        public void ToXml_MultipleEntries_ContainsAllKeys()
        {
            var collection = new ObjectFieldValueCollection
            {
                { "alpha", "1" },
                { "beta",  "2" },
                { "gamma", "3" }
            };

            string xml = collection.ToXml();

            StringAssert.Contains(xml, "<Key>alpha</Key>");
            StringAssert.Contains(xml, "<Key>beta</Key>");
            StringAssert.Contains(xml, "<Key>gamma</Key>");
        }

        [TestMethod]
        public void ToXml_OutputHasNoXmlNamespace()
        {
            var collection = new ObjectFieldValueCollection { { "k", "v" } };

            string xml = collection.ToXml();

            // The XmlSerializerNamespaces(empty) call should suppress xmlns attributes
            Assert.IsFalse(xml.Contains("xmlns"), "Output should not contain namespace declarations");
        }

        // --- Round-trip: WriteXml / ReadXml ---

        [TestMethod]
        public void RoundTrip_SingleEntry_DeserializesCorrectly()
        {
            var original = new ObjectFieldValueCollection { { "name", "Alice" } };
            string xml = original.ToXml();

            var deserialized = DeserializeFromXml(xml);

            Assert.IsTrue(deserialized.ContainsKey("name"));
            Assert.AreEqual("Alice", deserialized["name"]);
        }

        [TestMethod]
        public void RoundTrip_MultipleEntries_PreservesAllPairs()
        {
            var original = new ObjectFieldValueCollection
            {
                { "foo", "bar" },
                { "baz", "qux" }
            };
            string xml = original.ToXml();

            var deserialized = DeserializeFromXml(xml);

            Assert.AreEqual(2, deserialized.Count);
            Assert.AreEqual("bar", deserialized["foo"]);
            Assert.AreEqual("qux", deserialized["baz"]);
        }

        [TestMethod]
        public void RoundTrip_EmptyCollection_DeserializesToEmptyCollection()
        {
            var original = new ObjectFieldValueCollection();
            string xml = original.ToXml();

            var deserialized = DeserializeFromXml(xml);

            Assert.AreEqual(0, deserialized.Count);
        }

        [TestMethod]
        public void RoundTrip_ValueWithSpecialXmlCharacters_PreservesValue()
        {
            var original = new ObjectFieldValueCollection { { "query", "<search>&\"test\"</search>" } };
            string xml = original.ToXml();

            var deserialized = DeserializeFromXml(xml);

            Assert.AreEqual("<search>&\"test\"</search>", deserialized["query"]);
        }

        // --- WriteXml directly ---

        [TestMethod]
        public void WriteXml_ProducesKeyValueElements()
        {
            var collection = new ObjectFieldValueCollection { { "mykey", "myval" } };

            var sw = new StringWriter();
            using (var writer = XmlWriter.Create(sw))
            {
                writer.WriteStartElement("ObjectFieldValueCollection");
                collection.WriteXml(writer);
                writer.WriteEndElement();
            }
            string xml = sw.ToString();

            StringAssert.Contains(xml, "<KeyValue>");
            StringAssert.Contains(xml, "<Key>mykey</Key>");
            StringAssert.Contains(xml, "<Value>myval</Value>");
        }

        // --- Helper ---

        private static ObjectFieldValueCollection DeserializeFromXml(string xml)
        {
            var serializer = new XmlSerializer(typeof(ObjectFieldValueCollection));
            using (var reader = new StringReader(xml))
            {
                return (ObjectFieldValueCollection)serializer.Deserialize(reader);
            }
        }
    }
}
