using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.Validation;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators;
using Dynamicweb.Ecommerce.Orders;
using System.IO;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.UI.Commands;

public sealed class DownloadOrderXmlCommand : CommandBase
{
    [Required]
    public string OrderId { get; set; }

    [Required]
    public bool GetOriginalXml { get; set; }

    public override CommandResult Handle()
    {
        var order = Services.Orders.GetById(OrderId);
        if (order is null)
        {
            return new CommandResult
            {
                Message = $"The order with id: '{OrderId}' was not found",
                Status = CommandResult.ResultType.NotFound
            };
        }

        Settings settings = SettingsManager.GetSettingsByShop(order.ShopId);
        if (settings is null && !GetOriginalXml)
        {
            return new CommandResult
            {
                Message = $"No active Dynamicweb Live integration instance found for Order id: {order.Id} and Shop Id: {order.ShopId}.",
                Status = CommandResult.ResultType.NotFound
            };
        }

        var fileName = $"Order_{order.Id}.xml";
        var xml = GetOriginalXml ? GetOrderOriginalXml(order) : GetOrderCurrentXml(settings, order);

        var stream = new MemoryStream();
        using (var writeFile = new StreamWriter(stream, leaveOpen: true))
        {
            writeFile.Write(xml);
        }
        stream.Position = 0;

        return new CommandResult
        {
            Status = CommandResult.ResultType.Ok,
            Model = new FileResult
            {
                FileStream = stream,
                ContentType = "application/octet-stream",
                FileDownloadName = fileName
            }
        };
    }

    /// <summary>
    /// Occurs when the button was clicked from edit order page. Gets the XML for the order and returns it to the browser.
    /// </summary>        
    private static string GetOrderCurrentXml(Settings settings, Order order)
    {
        var logger = new Logger(settings);
        var xmlGeneratorSettings = new OrderXmlGeneratorSettings
        {
            AddOrderLineFieldsToRequest = settings.AddOrderLineFieldsToRequest,
            AddOrderFieldsToRequest = settings.AddOrderFieldsToRequest,
            CreateOrder = true,
            Beautify = true,
            LiveIntegrationSubmitType = SubmitType.DownloadedFromBackEnd,
            ReferenceName = "OrdersPut",
            ErpControlsDiscount = settings.ErpControlsDiscount,
            ErpControlsShipping = settings.ErpControlsShipping,
            ErpShippingItemKey = settings.ErpShippingItemKey,
            ErpShippingItemType = settings.ErpShippingItemType,
            CalculateOrderUsingProductNumber = settings.CalculateOrderUsingProductNumber
        };
        return new OrderXmlGenerator().GenerateOrderXml(settings, order, xmlGeneratorSettings, logger);
    }

    /// <summary>
    /// Gets the original XML by reading the original file from disk.
    /// </summary>        
    private static string GetOrderOriginalXml(Order order) => File.ReadAllText(BuildXmlFileName(order));

    internal static string BuildXmlFileName(Order order)
    {
        return OrderHandler.BuildXmlCopyPath(order.Id, OrderHandler.GetLogFolderForXmlCopies(order.CompletedDate));
    }
}

