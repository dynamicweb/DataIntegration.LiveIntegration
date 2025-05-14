using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Screens;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.UI.Commands;
using Dynamicweb.Ecommerce.UI.Models;
using Dynamicweb.Ecommerce.UI.Screens;
using System.Collections.Generic;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.UI.Injectors;

public sealed class OrderListScreenInjector : ListScreenInjector<OrderListScreen, OrderListDataModel, OrderDataModel>
{
    public override IEnumerable<ActionGroup> GetScreenActions()
    {
        if (SettingsManager.ActiveSettingsByShopId.Count == 0 ||
            Screen?.Model?.Data is null ||
            Screen?.Query is null ||
            !Screen.Model.Data.Any(o => SettingsManager.GetSettingsByShop(o.ShopId) is not null))
            return [];

        return new List<ActionGroup>()
                {
                    new()
                    {
                        Name = OrderEditScreenInjector.LiveIntegrationTab,
                        Title = OrderEditScreenInjector.LiveIntegrationTab,
                        Nodes = new List<ActionNode>()
                        {
                            new ActionNode()
                            {
                                Name = "Transfer via Live Integration", 
                                Title = "Transfer to ERP via Live Integration",
                                Icon = Icon.SignOutAlt,
                                NodeAction = ConfirmAction.For(RunCommandAction.For<TransferOrdersToErpCommand>().With(Screen.Query).WithReloadOnSuccess(),
                                    "Transfer to ERP via Live Integration?",
                                    "Transfer selected orders to ERP via Live Integration?")
                            }
                        }
                    }
                };

    }
}
