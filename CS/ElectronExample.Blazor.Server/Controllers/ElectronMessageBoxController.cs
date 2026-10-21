using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace ElectronExample.Blazor.Server.Controllers;

public class ElectronMessageBoxController : WindowController {
    public ElectronMessageBoxController() {
        TargetWindowType = WindowType.Main;
        var showMessageBoxAction = new SimpleAction(this, "ShowElectronMessageBox", PredefinedCategory.Tools) {
            Caption = "Native Message Box",
            ImageName = "Action_AboutInfo"
        };
        showMessageBoxAction.Execute += ShowMessageBoxAction_Execute;
    }

    protected override void OnActivated() {
        base.OnActivated();
        Active["ElectronActive"] = HybridSupport.IsElectronActive;
    }

    private async void ShowMessageBoxAction_Execute(object sender, SimpleActionExecuteEventArgs e) {
        var options = new MessageBoxOptions("Hello from the native Electron message box!") {
            Type = MessageBoxType.info,
            Title = "ElectronExample",
            Buttons = new[] { "OK" }
        };
        try {
            var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
            if (window != null)
                await Electron.Dialog.ShowMessageBoxAsync(window, options);
            else
                await Electron.Dialog.ShowMessageBoxAsync(options);
        }
        catch (Exception ex) {
            Tracing.Tracer.LogError(ex);
        }
    }
}
