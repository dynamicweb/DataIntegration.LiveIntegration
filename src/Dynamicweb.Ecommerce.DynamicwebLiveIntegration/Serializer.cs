using System.IO;
using System.Xml.Serialization;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration
{
    /// <summary>
    /// Serializer class to serialize from objects to XML and back.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize or deserialize.</typeparam>
    internal class Serializer<T>
    {
        /// <summary>
        /// Deserializes the specified XML to an object of type T.
        /// </summary>
        /// <param name="xml">The XML to deserialize.</param>
        /// <returns>An object of type T when serialization succeeds.</returns>
        internal T Deserialize(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            T result;

            using (TextReader reader = new StringReader(xml))
            {
                result = (T)serializer.Deserialize(reader);
            }

            return result;
        }

        /// <summary>
        /// Serializes the specified object to XML.
        /// </summary>
        /// <param name="o">The object to serialize.</param>
        /// <returns>An XML representation of the specified object.</returns>
        internal string Serialize(T o)
        {
            var xmlSerializer = new XmlSerializer(o.GetType());
            using (var textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, o);
                return textWriter.ToString();
            }
        }
    }
}