using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Collections.Generic;
using Dynamicweb.Rendering;
using Dynamicweb.Ecommerce.Orders;
using System.Xml.XPath;
using Dynamicweb.Security.UserManagement;
using Dynamicweb.Environment;
using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators.IntegrationCustomerCenter;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using System.Linq;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Core;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration
{
    /// <summary>
    /// Represents sort direction
    /// </summary>
    internal enum SortDirection
    {
        Asc = 0,
        Desc
    }

    /// <summary>
    /// Static manager to handle integration customer center requests.
    /// </summary>
    internal class IntegrationCustomerCenterHandler
    {
        private static readonly string OrderLinesLoop = "OrderLinesLoop";

        /// <summary>
        /// Retrieves integration customer center item details
        /// </summary>
        /// <param name="template">Template to render item details</param>
        /// <param name="callType">Item type(OpenOrder,Invoice,Credit,SalesShipment)</param>
        /// <param name="user">User</param>
        /// <param name="itemID">item id</param>
        /// <returns>Template with rendered item details</returns>
        internal static Template RetrieveItemDetailsFromRemoteSystem(Template template, string callType, User user, string itemID)
        {
            string shopId = Global.CurrentShopId;
            var currentSettings = SettingsManager.GetSettingsByShop(shopId);
            if (Global.IsIntegrationActive(currentSettings))
            {
                var logger = new Logger(currentSettings);
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.IntegrationCustomerCenterHandler.RetrieveItemDetailsFromRemoteSystem START");
                if (!string.IsNullOrEmpty(itemID) && !string.IsNullOrEmpty(Context.Current.Request.GetString("Redirect")) && Context.Current.Request.GetBoolean("addToCart"))
                {
                    HandleReorder(itemID, logger);
                }
                else
                {
                    if (!string.IsNullOrEmpty(callType) && user != null)
                    {
                        ItemDetailsXmlGeneratorSettings settings = new ItemDetailsXmlGeneratorSettings()
                        {
                            ItemType = callType,
                            CustomerId = user.CustomerNumber,
                            ItemId = itemID
                        };
                        string request = new ItemDetailsXmlGenerator().GenerateItemDetailsXml(settings);                        
                        logger.Log(ErrorLevel.DebugInfo, string.Format("Request RetrieveItemDetailsFromRemoteSystem sent: '{0}'.", request));
                        XmlDocument response = Connector.RetrieveDataFromRequestString(currentSettings, request, logger);
                        if (response != null && !string.IsNullOrEmpty(response.InnerXml))
                        {
                            logger.Log(ErrorLevel.DebugInfo, string.Format("Response RetrieveItemDetailsFromRemoteSystem received: '{0}'.", response.InnerXml));
                            if (template.Type != Template.TemplateType.Xslt)
                            {
                                ProcessItemDetailsResponse(response, template, shopId, logger);
                            }
                            else
                            {
                                MakeXslTransformation(response, template, logger);
                            }
                        }
                        else
                        {
                            logger.Log(ErrorLevel.ResponseError, "Response RetrieveItemDetailsFromRemoteSystem returned null.");
                        }
                    }
                }
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.IntegrationCustomerCenterHandler.RetrieveItemDetailsFromRemoteSystem END");
            }
            return template;
        }

        /// <summary>
        /// Retrieves integration customer center items list
        /// </summary>
        /// <param name="template">Template to render items list</param>
        /// <param name="callType">Item type(OpenOrder,Invoice,Credit,SalesShipment)</param>
        /// <param name="user">User</param>
        /// <param name="pageSize">List page size</param>
        /// <param name="pageIndex">List page index</param>
        /// <param name="totalItemsCount">Items count</param>
        /// <returns>Template with rendered items list</returns>
        internal static Template RetrieveItemsListFromRemoteSystem(Template template, string callType, User user, int pageSize, int pageIndex, out int totalItemsCount)
        {
            totalItemsCount = 0;
            string shopId = Global.CurrentShopId;
            var currentSettings = SettingsManager.GetSettingsByShop(shopId);
            if (Global.IsIntegrationActive(currentSettings))
            {                         
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.IntegrationCustomerCenterHandler.RetrieveItemsListFromRemoteSystem START");
                if (!string.IsNullOrEmpty(callType) && user != null)
                {
                    ItemListXmlGeneratorSettings settings = new ItemListXmlGeneratorSettings()
                    {
                        ItemType = callType,
                        CustomerId = user.CustomerNumber,
                        PageIndex = pageIndex,
                        PageSize = pageSize
                    };
                    string request = new ItemListXmlGenerator().GenerateItemListXml(settings);
                    var logger = new Logger(currentSettings);
                    logger.Log(ErrorLevel.DebugInfo, string.Format("Request RetrieveItemsListFromRemoteSystem sent: '{0}'.", request));
                    XmlDocument response = Connector.RetrieveDataFromRequestString(currentSettings, request, logger);
                    if (response != null && !string.IsNullOrEmpty(response.InnerXml))
                    {
                        logger.Log(ErrorLevel.DebugInfo, string.Format("Response RetrieveItemsListFromRemoteSystem received: '{0}'.", response.InnerXml));
                        if (template.Type != Template.TemplateType.Xslt)
                        {
                            ProcessItemsListResponse(response, template, callType, pageSize, pageIndex, out totalItemsCount, shopId, logger);
                        }
                        else
                        {
                            MakeXslTransformation(response, template, logger);
                        }
                    }
                    else
                    {
                        logger.Log(ErrorLevel.ResponseError, "Response RetrieveItemsListFromRemoteSystem returned null.");
                    }
                }
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.IntegrationCustomerCenterHandler.RetrieveItemsListFromRemoteSystem END");
            }
            return template;
        }
        
        private static void ProcessItemDetailsResponse(XmlDocument response, Template template, string shopId, Logger logger)
        {
            try
            {
                XmlNode orderNode = response.SelectSingleNode("//item [@table='EcomOrders']");
                //Set Order tags
                if (orderNode != null && orderNode.ChildNodes.Count > 0)
                {
                    foreach (XmlNode itemProperty in orderNode.ChildNodes)
                    {
                        if (itemProperty.Attributes["columnName"] != null)
                        {
                            string tagName = itemProperty.Attributes["columnName"].Value;
                            template.SetTag(tagName, itemProperty.InnerText);
                        }
                    }
                }
                //Set OrderLines loop
                XmlNodeList orderLinesNodes = response.SelectNodes("//item [@table='EcomOrderLines']");
                if (orderLinesNodes != null && orderLinesNodes.Count > 0)//Process OrderLines
                {
                    Template orderLinesLoop = template.GetLoop(OrderLinesLoop);

                    foreach (XmlNode orderLineNode in orderLinesNodes)
                    {
                        foreach (XmlNode itemProperty in orderLineNode.ChildNodes)
                        {
                            if (itemProperty.Attributes["columnName"] != null)
                            {
                                string tagName = itemProperty.Attributes["columnName"].Value;
                                orderLinesLoop.SetTag(tagName, itemProperty.InnerText);
                            }
                        }
                        orderLinesLoop.CommitLoop();
                    }
                }
                SetUrlTags(template, shopId);
            }
            catch (Exception e)
            {
                logger.Log(ErrorLevel.Error, string.Format("Response does not match schema: '{0}'.", e.Message));
            }
        }

        private static void ProcessItemsListResponse(XmlDocument response, Template template, string callType, int pageSize, int pageIndex, out int totalItemsCount, string shopId, Logger logger)
        {
            totalItemsCount = 0;
            string tagPrefix = string.Format("Ecom:IntegrationCustomerCenter.{0}", callType);
            try
            {
                XmlNodeList items = response.SelectNodes("//item");

                if (items != null && items.Count > 0)
                {
                    XmlNode itemsNode = response.SelectSingleNode("Items");
                    bool isTotalCountAttributePresent = itemsNode != null && itemsNode.Attributes["totalCount"] != null;
                    if (isTotalCountAttributePresent)
                    {
                        Int32.TryParse(itemsNode.Attributes["totalCount"].Value, out totalItemsCount);
                    }
                    if (totalItemsCount <= 0)
                    {
                        totalItemsCount = items.Count;
                    }

                    string itemsLoopName = string.Format("{0}Loop", tagPrefix);
                    Template itemsLoop = template.GetLoop(itemsLoopName);

                    int index = 0;
                    bool usePaging = totalItemsCount > pageSize;
                    pageIndex = pageIndex > 0 ? pageIndex - 1 : 0;
                    int startIndex = pageSize * pageIndex;
                    int endIndex = startIndex + pageSize;

                    foreach (XmlNode item in items)
                    {
                        if (index >= startIndex || !usePaging || isTotalCountAttributePresent)
                        {
                            foreach (XmlNode itemProperty in item.ChildNodes)
                            {
                                if (itemProperty.Attributes["columnName"] != null)
                                {
                                    string tagName = itemProperty.Attributes["columnName"].Value;
                                    itemsLoop.SetTag(tagName, itemProperty.InnerText);
                                }
                            }
                            SetUrlTags(itemsLoop, shopId);
                            itemsLoop.CommitLoop();
                        }
                        index++;
                        if (usePaging && index >= endIndex)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    //set empty list tag
                    template.SetTag(string.Format("{0}.EmptyList", tagPrefix), true);
                }
            }
            catch (Exception e)
            {
                logger.Log(ErrorLevel.Error, string.Format("Response does not match schema: '{0}'.", e.Message));
            }
        }

        private static void MakeXslTransformation(XmlDocument doc, Template template, Logger logger)
        {
            try
            {
                XslCompiledTransform xslTransform = new XslCompiledTransform();
                StringWriter writer = new StringWriter();
                xslTransform.Load(new XPathDocument(new StringReader(template.Html)));
                xslTransform.Transform(doc.CreateNavigator(), null, writer);
                template.Html = writer.ToString();
            }
            catch (Exception ex)
            {
                string msg = string.Format("Error in xsl transformation: '{0}'.", ex.Message);
                template.Html = msg;
                logger.Log(ErrorLevel.Error, msg);
            }
        }

        private static void SetUrlTags(Template template, string shopId)
        {
            var user = Helpers.GetCurrentExtranetUser();
            string userID = user != null ? user.ID.ToString() : string.Empty;            
            template.SetTag("UrlUserID", userID);            
            template.SetTag("UrlShopID", shopId);
        }

        private static void HandleReorder(string itemID, Logger logger)
        {
            OrderService orderService = new OrderService();
            Order order = orderService.GetById(itemID);
            if (order != null)
            {
                Order ecomCart;
                if (!Frontend.Cart.CartCatch.CartIsPresent())
                {
                    ecomCart = new Order(Common.Context.Currency, Common.Context.Country, Common.Context.Language)
                    {
                        IsCart = true
                    };
                    Common.Context.SetCart(ecomCart);
                }

                ecomCart = Common.Context.Cart;

                foreach (OrderLine orderLineToCopy in order.OrderLines)
                {
                    var OrderlineBuilder = new OrderLineBuilderConfig()
                    {
                        ProductId = orderLineToCopy.ProductId,
                        Quantity = orderLineToCopy.Quantity,
                        VariantId = orderLineToCopy.ProductVariantId,
                        VariantText = orderLineToCopy.ProductVariantText,
                        UnitId = orderLineToCopy.UnitId,
                        OrderLineType = orderLineToCopy.OrderLineType,
                        ReferenceUrl = orderLineToCopy.Reference,
                        PageId = orderLineToCopy.PageId,
                        ProductName = string.IsNullOrEmpty(orderLineToCopy.ProductId) ? orderLineToCopy.ProductName : string.Empty,
                        ProductNumber = string.IsNullOrEmpty(orderLineToCopy.ProductId) ? orderLineToCopy.ProductNumber : string.Empty,
                        OrderLineFieldValues = orderLineToCopy.OrderLineFieldValues,
                    };

                    Frontend.Cart.CartService service = new Frontend.Cart.CartService();
                    var newOrderLine = service.OrderLineBuilder(ecomCart, OrderlineBuilder);

                    Frontend.Cart.CartCatch.SaveCart();                    

                    NotificationManager.Notify(Ecommerce.Notifications.Ecommerce.Cart.Line.Added, 
                        new Ecommerce.Notifications.Ecommerce.Cart.Line.AddedArgs(newOrderLine, ecomCart));
                }
                ecomCart.Comment = order.Comment;
                ecomCart.CustomerComment = order.CustomerComment;
                OrderFieldValueCollection orderFieldValues = new OrderFieldValueCollection();
                foreach (OrderFieldValue orderField in order.OrderFieldValues)
                {
                    orderFieldValues.Add(new OrderFieldValue(orderField.OrderField, orderField.Value));
                }
                ecomCart.OrderFieldValues = orderFieldValues;                
                orderService.Save(ecomCart);
                orderService.RemoveNoneActiveProducts(ecomCart);                

                string redirectUrl = Context.Current.Request.GetString("Redirect");
                if (Context.Current.Request.GetInt32("Redirect") != 0)
                {
                    redirectUrl = "/Default.aspx?ID=" + Context.Current.Request.GetString("Redirect");
                }
                Context.Current.Response.Redirect(redirectUrl);
                Context.Current.Response.End();
            }
            else
            {
                logger.Log(ErrorLevel.Error, string.Format("Can not find order with ID = '{0}'", itemID));
            }
        }

        /// <summary>
        /// Retrieves order information in pdf
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="response">Response</param>
        /// <param name="securityKey">Security key</param>
        internal static void RetrievePDF(IRequest request, IResponse response)
        {
            string shopId = Global.CurrentShopId;
            var currentSettings = SettingsManager.GetSettingsByShop(shopId);
            if (Global.IsIntegrationActive(currentSettings))
            {
                string id = request["id"];
                User user = Helpers.GetCurrentExtranetUser();
                if (user == null)
                {
                    SetAccessDenied(response);
                    return;
                }
                RetrievePdfXmlGeneratorSettings settings = new RetrievePdfXmlGeneratorSettings()
                {
                    Type = request["type"],
                    CustomerId = user.CustomerNumber,
                    ItemId = id
                };
                string requestString = new RetrievePdfXmlGenerator().GenerateXml(settings);
                string base64EncodedPDF = Connector.RetrievePDF(currentSettings, requestString);

                MemoryStream inputStream = new MemoryStream();
                StreamWriter writer = new StreamWriter(inputStream);
                writer.Write(base64EncodedPDF);
                writer.Flush();
                inputStream.Position = 0;
                response.ContentType = "application/pdf";

                bool forceDownload = Context.Current.Request.GetBoolean("forceDownload");
                if (forceDownload)
                {
                    string fileName = string.Format("IntegrationCustomerCenterItem{0}.pdf", id);
                    if (!string.IsNullOrEmpty(request["Filename"])){
                        fileName = request["Filename"];
                        fileName = Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
                    }
                    string filePath = SystemInformation.MapPath("/" + Dynamicweb.DataIntegration.ProviderHelpers.FileHelper.FilesFolderName + "/System/Log/LiveIntegration/" + fileName);
                    using (Stream stream = File.OpenWrite(filePath))
                    {
                        DecodeStream(inputStream, stream);
                    }
                    response.Clear();
                    response.AddHeader("content-disposition", string.Format("attachment;filename={0}", fileName));
                    response.BinaryWrite(File.ReadAllBytes(filePath));
                    response.Flush();
                    File.Delete(filePath);
                }
                else
                {
                    DecodeStream(inputStream, response.OutputStream);
                    response.OutputStream.Flush();
                    response.OutputStream.Close();
                }
            }
        }

        /// <summary>
        /// Decodes the stream.
        /// </summary>
        /// <param name="inStream">The in stream.</param>
        /// <param name="output">The output.</param>
        private static void DecodeStream(Stream inStream, Stream output)
        {
            using (System.Security.Cryptography.ICryptoTransform transform = new System.Security.Cryptography.FromBase64Transform())
            {
                using (var cryptStream = new System.Security.Cryptography.CryptoStream(inStream, transform, System.Security.Cryptography.CryptoStreamMode.Read))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = cryptStream.Read(buffer, 0, buffer.Length);
                    while (bytesRead > 0)
                    {
                        output.Write(buffer, 0, bytesRead);
                        bytesRead = cryptStream.Read(buffer, 0, buffer.Length);
                    }
                }
            }
        }

        private static void SetAccessDenied(IResponse response)
        {
            response.ClearHeaders();
            response.Status = "403 Access denied";
            response.StatusCode = 403;
            response.StatusDescription = "Access denied";
            response.End();
        }        
    }
}
