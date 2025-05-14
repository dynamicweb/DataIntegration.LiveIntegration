using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Dynamicweb.Ecommerce.UI.Screens;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.UI.Injectors;

public sealed class OrderOverviewScreenInjector : ScreenInjector<OrderOverviewScreen>
{
    public override void OnAfter(OrderOverviewScreen screen, UiComponentBase content)
    {
        if (!content.TryGet<ScreenLayout>(out var layout))
            return;        

        var actions = OrderEditScreenInjector.GetOrderScreenActions(Screen?.Model?.Id, Screen?.Model?.IntegrationOrderId);
        if (actions.Count > 0)
        {
            layout.ContextActionGroups.AddRange(actions);
        }
    }
}
