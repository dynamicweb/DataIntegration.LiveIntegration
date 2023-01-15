using System;
using System.Collections.Generic;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators
{
    /// <summary>
    /// Class XmlGenerator.
    /// </summary>
    public abstract class XmlGenerator
    {
        /// <summary>
        /// Adds the child XML node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeName">Name of the node.</param>
        /// <param name="nodeValue">The node value.</param>
        /// <param name="isInformationalOnly">if set to <c>true</c> [is informational only].</param>
        /// <param name="isCustomField">if set to <c>true</c> [is custom field].</param>
        /// <exception cref="InvalidOperationException">Cannot call this method without an xml document associated with the parent element.</exception>
        protected static void AddChildXmlNode(XmlElement parent, string nodeName, string nodeValue, bool isInformationalOnly = false, bool isCustomField = false)
        {
            if (parent.OwnerDocument == null)
            {
                throw new InvalidOperationException("Cannot call this method without an xml document associated with the parent element.");
            }

            var node = parent.OwnerDocument.CreateElement("column");
            node.SetAttribute("columnName", nodeName);

            Dictionary<string, string> attributes = BuildAttributes(isInformationalOnly, isCustomField);
            foreach (var attribute in attributes)
            {
                node.SetAttribute(attribute.Key, attribute.Value);
            }

            node.InnerText = nodeValue;
            parent.AppendChild(node);
        }

        /// <summary>
        /// Builds the XML document.
        /// </summary>
        /// <returns>XmlDocument.</returns>
        protected static XmlDocument BuildXmlDocument()
        {
            var xDoc = new XmlDocument();
            var xDec = xDoc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            xDoc.AppendChild(xDec);
            return xDoc;
        }

        /// <summary>
        /// Creates the and append item node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>XmlElement.</returns>
        /// <exception cref="InvalidOperationException">Cannot call this method without an xml document associated with the parent element.</exception>
        protected static XmlElement CreateAndAppendItemNode(XmlNode parent, string tableName)
        {
            if (parent.OwnerDocument == null)
            {
                throw new InvalidOperationException("Cannot call this method without an xml document associated with the parent element.");
            }

            var itemNode = parent.OwnerDocument.CreateElement("item");
            itemNode.SetAttribute("table", tableName);
            parent.AppendChild(itemNode);
            return itemNode;
        }

        /// <summary>
        /// Creates the and append tables node.
        /// </summary>
        /// <param name="xmlDocument">The XML document.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>XmlElement.</returns>
        protected static XmlElement CreateAndAppendTablesNode(XmlDocument xmlDocument, XmlGeneratorSettings settings)
        {
            var xmlRoot = xmlDocument.CreateElement("tables");
            xmlRoot.SetAttribute("source", "LiveIntegration");
            xmlRoot.SetAttribute("submitType", settings.LiveIntegrationSubmitType.ToString());
            xmlRoot.SetAttribute("referenceName", settings.ReferenceName);
            xmlDocument.AppendChild(xmlRoot);
            return xmlRoot;
        }

        /// <summary>
        /// Creates the table node.
        /// </summary>
        /// <param name="xmlDocument">The XML document.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>XmlElement.</returns>
        protected static XmlElement CreateTableNode(XmlDocument xmlDocument, string tableName)
        {
            var ordersNode = xmlDocument.CreateElement("table");
            ordersNode.SetAttribute("tableName", tableName);
            return ordersNode;
        }

        /// <summary>
        /// Builds the attributes.
        /// </summary>
        /// <param name="isInformationalOnly">if set to <c>true</c> [is informational only].</param>
        /// <param name="isCustomField">if set to <c>true</c> [is custom field].</param>
        /// <returns>Dictionary&lt;System.String, System.String&gt;.</returns>
        private static Dictionary<string, string> BuildAttributes(bool isInformationalOnly, bool isCustomField)
        {
            var attributes = new Dictionary<string, string>();
            if (isInformationalOnly)
            {
                attributes.Add("isInformationalOnly", true.ToString());
            }

            if (isCustomField)
            {
                attributes.Add("isCustomField", true.ToString());
            }

            return attributes;
        }
    }
}