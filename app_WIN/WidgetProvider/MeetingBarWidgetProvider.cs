using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;
namespace MeetingBar.WidgetProvider;
[ComVisible(true),ComDefaultInterface(typeof(IWidgetProvider)),Guid("66C23F7C-0FCF-4D2B-AEB1-7335A78F2D90")]
public sealed class MeetingBarWidgetProvider : IWidgetProvider
{
    private static readonly object Gate=new(); private static readonly Dictionary<string,MeetingBarWidget> Widgets=new(); private static bool _recovered;
    public MeetingBarWidgetProvider()=>Recover();
    public void CreateWidget(WidgetContext context){var widget=new MeetingBarWidget(context.Id);lock(Gate)Widgets[context.Id]=widget;widget.Update(true);}
    public void DeleteWidget(string id,string state){lock(Gate){if(Widgets.Remove(id,out var widget))widget.Dispose();}}
    public void OnActionInvoked(WidgetActionInvokedArgs args){MeetingBarWidget? widget;lock(Gate)Widgets.TryGetValue(args.WidgetContext.Id,out widget);widget?.Invoke(args.Verb);}
    public void OnWidgetContextChanged(WidgetContextChangedArgs args){MeetingBarWidget? widget;lock(Gate)Widgets.TryGetValue(args.WidgetContext.Id,out widget);widget?.Update(true);}
    public void Activate(WidgetContext context){MeetingBarWidget? widget;lock(Gate)Widgets.TryGetValue(context.Id,out widget);widget?.Activate();}
    public void Deactivate(string id){MeetingBarWidget? widget;lock(Gate)Widgets.TryGetValue(id,out widget);widget?.Deactivate();}
    private static void Recover(){lock(Gate){if(_recovered)return;_recovered=true;try{foreach(var info in WidgetManager.GetDefault().GetWidgetInfos()){var context=info.WidgetContext;if(context.DefinitionId==MeetingBarWidget.DefinitionId&&!Widgets.ContainsKey(context.Id))Widgets[context.Id]=new MeetingBarWidget(context.Id);}}catch{}}}
}
