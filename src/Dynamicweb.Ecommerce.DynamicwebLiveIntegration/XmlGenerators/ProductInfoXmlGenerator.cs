using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Extensibility.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators
{
    /// <summary>
    /// Generates XML for product info requests.
    /// </summary>
    /// <seealso cref="XmlGenerator" />
    internal class ProductInfoXmlGenerator : XmlGenerator
    {
        /// <summary>
        /// Generates the product information XML.
        /// </summary>
        /// <param name="products">The products.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>System.String.</returns>
        internal string GenerateProductInfoXml(Settings currentSettings, List<PriceProductSelection> products, ProductInfoXmlGeneratorSettings settings, Logger logger)
        {
            NotificationManager.Notify(Notifications.ProductInfo.OnBeforeGenerateProductInfoXml, new Notifications.ProductInfo.OnBeforeGenerateProductInfoXmlArgs(products, settings, currentSettings, logger));
            var xmlDocument = BuildXmlDocument();

            var xmlRoot = xmlDocument.CreateElement("GetEcomData");
            var user = settings.Context.User;

            xmlRoot.SetAttribute("ExternalUserId", !string.IsNullOrWhiteSpace(user?.CustomerNumber) ? user.CustomerNumber : currentSettings.AnonymousUserKey);
            xmlRoot.SetAttribute("AccessUserCustomerNumber", !string.IsNullOrWhiteSpace(user?.CustomerNumber) ? user.CustomerNumber : currentSettings.AnonymousUserKey);
            xmlRoot.SetAttribute("VatCountryCode", !string.IsNullOrWhiteSpace(settings?.Context?.PriceContext?.Country?.Code2) ? settings.Context.PriceContext.Country.Code2 :
                !string.IsNullOrWhiteSpace(settings?.Context?.Country?.Code2) ? settings?.Context?.Country?.Code2 :                
                Services.Countries.GetCountries().FirstOrDefault().Code2);
            xmlRoot.SetAttribute("VatPostingGroup", !string.IsNullOrWhiteSpace(settings?.Context?.PriceContext?.Country?.VatPostingGroup) ? settings.Context.PriceContext.Country.VatPostingGroup : Services.Countries.GetCountries().FirstOrDefault().VatPostingGroup);

            var tablesNode = xmlDocument.CreateElement("tables");
            tablesNode.AppendChild(BuildProductInfoXml(currentSettings, xmlDocument, products, settings, logger));

            xmlRoot.AppendChild(tablesNode);
            xmlDocument.AppendChild(xmlRoot);
            NotificationManager.Notify(Notifications.ProductInfo.OnAfterGenerateProductInfoXml, new Notifications.ProductInfo.OnAfterGenerateProductInfoXmlArgs(products, settings, xmlDocument, currentSettings, logger));
            if (settings.Beautify)
            {
                return xmlDocument.Beautify();
            }
            else
            {
                return xmlDocument.InnerXml;
            }
        }

        /// <summary>
        /// Builds the product information XML.
        /// </summary>
        /// <param name="xmlDocument">The XML document.</param>
        /// <param name="products">The products.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>XmlNode.</returns>
        private XmlNode BuildProductInfoXml(Settings currentSettings, XmlDocument xmlDocument, List<PriceProductSelection> products, ProductInfoXmlGeneratorSettings settings, Logger logger)
        {
            if (products == null || !products.Any())
            {
                return null;
            }

            var currencyCode = settings.Context?.Currency?.Code;
            if(string.Equals(currentSettings.LcyCurrency, currencyCode, StringComparison.OrdinalIgnoreCase))
            {
                currencyCode = "";
            }

            var tableNode = xmlDocument.CreateElement("Products");
            tableNode.SetAttribute("type", "filter");
            if (settings.GetUnitPrices)
            {
                tableNode.SetAttribute("unitPrices", "true");
            }

            bool doCalculatePriceUsingProductNumber = currentSettings.CalculatePriceUsingProductNumber;

            foreach (var productWithQuantity in products)
            {
                Product product = productWithQuantity.Product;
                var itemNode = xmlDocument.CreateElement("Product");
                tableNode.AppendChild(itemNode);

                if (product != null)
                {
                    AddChildXmlNode(itemNode, "ProductId", doCalculatePriceUsingProductNumber ? product.Number : product.Id);
                    AddChildXmlNode(itemNode, "ProductVariantId", ProductManager.ProductProvider.GetProductVariantIdIdentifier(product));
                    AddChildXmlNode(itemNode, "ProductNumber", product.Number);
                    AddChildXmlNode(itemNode, "ProductUnitId", settings.GetUnitPrices ? productWithQuantity.UnitId : product.DefaultUnitId ?? string.Empty);
                    AddChildXmlNode(itemNode, "ProductIdentifier", ProductManager.ProductProvider.GetProductIdentifier(currentSettings, product, productWithQuantity.UnitId));
                    AddChildXmlNode(itemNode, "CurrencyCode", currencyCode);
                    AddChildXmlNode(itemNode, "Quantity", productWithQuantity.Quantity.ToIntegrationString(currentSettings, logger));

                    if (settings.AddProductFieldsToRequest && product.ProductFieldValues.Count > 0)
                    {
                        AppendProductFields(currentSettings, product, itemNode, logger);
                    }
                }
            }

            return tableNode;
        }

        /// <summary>
        /// Appends the product fields.
        /// </summary>
        /// <param name="product">The product.</param>
        /// <param name="productNode">The product node.</param>
        private void AppendProductFields(Settings settings, Product product, XmlElement productNode, Logger logger)
        {
            foreach (var field in product.ProductFieldValues)
            {
                string value = field.Value?.ToIntegrationString(settings, logger) ?? string.Empty;
                AddChildXmlNode(productNode, field.ProductField.SystemName, value, isCustomField: true);
            }
        }

        private void AddChildXmlNode(XmlElement parent, string nodeName, string nodeValue, bool isCustomField = false)
        {
            if (parent.OwnerDocument == null)
            {
                throw new ArgumentException("parent element must have an OwnerDocument", nameof(parent));
            }

            var node = parent.OwnerDocument.CreateElement(nodeName);
            if (isCustomField)
            {
                node.SetAttribute("isCustomField", true.ToString());
            }

            node.InnerText = nodeValue;
            parent.AppendChild(node);
        }
    }
}