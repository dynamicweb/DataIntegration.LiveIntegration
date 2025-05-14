using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Screens;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.UI.Commands;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.UI.Models;
using Dynamicweb.Ecommerce.UI.Screens;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.UI.Injectors
{
    public sealed class OrderEditScreenInjector : EditScreenInjector<OrderEditScreen, OrderDataModel>
    {
        internal static readonly string LiveIntegrationTab = "Live Integration";

        public override IEnumerable<ActionGroup> GetScreenActions()
        {
            return GetOrderScreenActions(Screen?.Model?.Id, Screen?.Model?.IntegrationOrderId);
        }

        internal static List<ActionGroup> GetOrderScreenActions(string orderId, string integrationOrderId)
        {
            var order = Services.Orders.GetById(orderId);
            if (order is null)
                return [];

            var settings = SettingsManager.GetSettingsByShop(order.ShopId);
            bool isLiveIntegrationFound = settings is not null;

            if (isLiveIntegrationFound)
            {
                bool exported = !string.IsNullOrEmpty(integrationOrderId);
                var actionNodes = new List<ActionNode>()
                {
                    new()
                    {
                        Name = "Transfer via Live Integration",
                        Title = exported ? "Order already transferred" : "Transfer to ERP via Live Integration",
                        Icon = Icon.SignOutAlt,
                        NodeAction = ConfirmAction.For(RunCommandAction.For(new TransferOrderToErpCommand { OrderId = orderId }).WithReloadOnSuccess(),
                            "Transfer to ERP via Live Integration?",
                            exported ? $"Order {orderId} is already in the ERP, update again?" : $"Transfer order {orderId} to ERP via Live Integration?")
                    }
                };
                actionNodes.AddRange(GetOrderExportToXmlActions(settings, order));

                return new List<ActionGroup>()
                {
                    new()
                    {
                        Name = LiveIntegrationTab,
                        Title = LiveIntegrationTab,
                        Nodes =  actionNodes
                    }
                };
            }

            return [];
        }        

        private static IEnumerable<ActionNode> GetOrderExportToXmlActions(Settings settings, Order order)
        {
            bool saveOrderXml = settings.SaveCopyOfOrderXml;

            bool enableButton = saveOrderXml && System.IO.File.Exists(DownloadOrderXmlCommand.BuildXmlFileName(order));

            return [
                new ActionNode()
                {
                    Icon = Icon.FileDownload,
                    Name = "Original XML",
                    Title = enableButton ?
                        "Downloads the original XML for an order as sent to the ERP" :
                        !saveOrderXml ?
                            "This option is not available because saving XML files is not enabled in the Live Integration setup." :
                            "This option is not available because the XML file does not exist.",
                    Disabled = !enableButton,
                    NodeAction = enableButton ? DownloadFileAction.Using(new DownloadOrderXmlCommand { OrderId = order.Id, GetOriginalXml = true })  : null
                },

                new ActionNode()
                {
                    Icon = Icon.FileDownload,
                    Name = "Current XML",
                    Title = "Exports the order to an XML document",
                    NodeAction = DownloadFileAction.Using(new DownloadOrderXmlCommand { OrderId = order.Id, GetOriginalXml = false })
                }
            ];
        }
    }
}

