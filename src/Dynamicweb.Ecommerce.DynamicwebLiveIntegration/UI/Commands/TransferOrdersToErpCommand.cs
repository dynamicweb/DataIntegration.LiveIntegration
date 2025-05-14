using Dynamicweb.CoreUI.Data;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.UI.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.UI.Commands;

public sealed class TransferOrdersToErpCommand : OrderBulkActionCommand
{
    public override CommandResult Handle()
    {
        var orders = GetSelectedOrders().ToList();
        if (orders.Count == 0)
        {
            return new()
            {
                Status = CommandResult.ResultType.NotFound,
                Message = "The selected Orders could not be found"
            };
        }

        List<string> exportedOrders = new List<string>();
        List<string> alreadyExportedOrders = new List<string>();

        foreach (var order in orders)
        {
            if (string.IsNullOrEmpty(order.IntegrationOrderId))
            {
                var settings = SettingsManager.GetSettingsByShop(order.ShopId);
                if (settings is null)
                    continue;

                bool exported = OrderHandler.UpdateOrder(settings, order, SubmitType.ManualSubmit) ?? false;
                if (exported)
                {
                    exportedOrders.Add(order.Id);
                }
            }
            else
            {
                alreadyExportedOrders.Add(order.Id);
            }
        }

        var message = GetExportedOrdersMessage(orders, exportedOrders, alreadyExportedOrders, out var success);        

        return new()
        {
            Status = success ? CommandResult.ResultType.Ok : CommandResult.ResultType.Error,
            Message = message
        };
    }

    /// <summary>
    /// Gets the exported orders message.
    /// </summary>    
    /// <returns>System.String.</returns>
    private static string GetExportedOrdersMessage(List<Order> orders, List<string> exportedOrders, List<string> alreadyExportedOrders, out bool success)
    {
        string output = string.Empty;
        success = false;

        if (alreadyExportedOrders.Count > 0 && alreadyExportedOrders.Count == orders.Count())
        {
            output = "All selected orders are already transferred to ERP.";
            success = true;
        }
        else if (exportedOrders.Count > 0 || alreadyExportedOrders.Count > 0)
        {
            if ((exportedOrders.Count + alreadyExportedOrders.Count) == orders.Count())
            {
                output = "All selected orders were successfully transferred to ERP.";
                success = true;
            }
            else
            {
                if (alreadyExportedOrders.Count > 0)
                {
                    output += $"Orders with IDs [{string.Join(",", alreadyExportedOrders)}] were already transferred to ERP. ";
                }

                if (exportedOrders.Count > 0)
                {
                    output += $"Orders with IDs [{string.Join(",", exportedOrders)}] were successfully transferred to ERP. ";
                }

                output += $"Orders with IDs [{string.Join(",", orders
                    .Where(o => !(exportedOrders.Contains(o.Id) ||
                        alreadyExportedOrders.Contains(o.Id)))
                    .Select(o => o.Id).Distinct().ToArray())}] were not transferred to ERP. Check the LiveIntegration log for details";
            }
        }
        else
        {
            output = "None of the selected orders were transferred to ERP. Check the LiveIntegration log for details.";
        }

        return output;
    }
}
