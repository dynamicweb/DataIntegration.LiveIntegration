using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.Validation;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.UI.Commands;

public sealed class TransferOrderToErpCommand : CommandBase
{
    [Required]
    public string OrderId { get; set; }

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
        if (settings is null)
        {
            return new CommandResult
            {
                Message = $"No active Dynamicweb Live integration instance found for Order id: {order.Id} and Shop Id: {order.ShopId}.",
                Status = CommandResult.ResultType.NotFound
            };
        }

        bool result = OrderHandler.UpdateOrder(settings, order, SubmitType.ManualSubmit) ?? false;
        return new CommandResult
        {
            Message = result ? "Order successfully transferred to ERP" : "Error creating order in ERP. Check the LiveIntegration log for details",
            Status = result ? CommandResult.ResultType.Ok : CommandResult.ResultType.Error
        };
    }
}

