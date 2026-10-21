<!-- default badges list -->
![](https://img.shields.io/endpoint?url=https://codecentral.devexpress.com/api/v1/VersionRange/1428168384/26.1.5%2B)
[![](https://img.shields.io/badge/Open_in_DevExpress_Support_Center-FF7200?style=flat-square&logo=DevExpress&logoColor=white)](https://supportcenter.devexpress.com/ticket/details/T1337084)
[![](https://img.shields.io/badge/📖_How_to_use_DevExpress_Examples-e9f6fc?style=flat-square)](https://docs.devexpress.com/GeneralInformation/403183)
[![](https://img.shields.io/badge/💬_Leave_Feedback-feecdd?style=flat-square)](#does-this-example-address-your-development-requirementsobjectives)
<!-- default badges end -->
# How to: Run an XAF Blazor Application as a Desktop Application with Electron.NET

This topic describes how to host an [XAF Blazor](https://docs.devexpress.com/eXpressAppFramework/401675/overview/supported-ui-platforms) application inside an [Electron](https://www.electronjs.org/) desktop shell using the [ElectronNET.Core](https://github.com/ElectronNET/Electron.NET) library.

> [!IMPORTANT]
> This example references ElectronNET.Core **0.6.0** and may not be updated for newer versions. Electron.NET is a third-party library and its setup instructions, APIs, and package names can change between releases. Before you integrate Electron.NET into your application, check the [official Electron.NET documentation](https://github.com/ElectronNET/Electron.NET) for the current instructions.

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 8.0 or later |
| Node.js | 22.x or later (must be available in `PATH`) |
| XAF Blazor Server application | Created with the [Template Kit](https://docs.devexpress.com/eXpressAppFramework/405447/installation-upgrade-version-history/visual-studio-integration/template-kit) |

To check the installed Node.js version, run the following command:

```powershell
node --version
```

## Step 1. Add NuGet Packages

Add the following packages to the *YourApplicationName.Blazor.Server* project:

```powershell
dotnet add package ElectronNET.Core
dotnet add package ElectronNET.Core.AspNet
```

The project file should contain the following references:

```xml
<PackageReference Include="ElectronNET.Core" Version="0.6.0" />
<PackageReference Include="ElectronNET.Core.AspNet" Version="0.6.0" />
```

> [!NOTE]
> ElectronNET.Core does not require the legacy `electronize` CLI tool or the `electron.manifest.json` file. The library configures the Electron host through MSBuild.

## Step 2. Register Electron in the Application Builder

Open the *YourApplicationName.Blazor.Server/Program.cs* file and add the following namespaces:

```csharp
using ElectronNET.API;
using ElectronNET.API.Entities;
```

Register Electron services conditionally. Add the following code after the `WebApplication.CreateBuilder` call and the `AddRazorComponents` registration, and before the `AddXaf` call:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

bool useElectron = builder.Configuration.GetValue<bool>("USE_ELECTRON");
if (useElectron) {
	builder.Services.AddElectron();
	builder.UseElectron(args, async () => {
		var options = new BrowserWindowOptions {
			Show = false,
			IsRunningBlazor = true,
		};
		if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux()) {
			options.AutoHideMenuBar = true;
		}
		var browserWindow = await Electron.WindowManager.CreateWindowAsync(options);
		browserWindow.OnReadyToShow += () => browserWindow.Show();
	});
}

builder.Services.AddXaf(builder.Configuration, b => {
	// ...
});
```

The code above does the following:

- `IsRunningBlazor = true` configures the Electron renderer so that the Blazor SignalR circuit works correctly.
- `Show = false` in combination with the `OnReadyToShow` event handler prevents an empty window from being displayed while the XAF application loads.
- The `USE_ELECTRON` check keeps the default browser hosting unchanged. Do not call `UseElectron` if the setting is not specified.

## Step 3. Resolve the App Component Name Conflict

The `ElectronNET.API` namespace contains an `App` type. After you add the `using ElectronNET.API;` directive, the `App` identifier conflicts with the Blazor root component and the build fails with the [CS0104](https://learn.microsoft.com/en-us/dotnet/csharp/misc/cs0104) error.

To resolve this conflict, specify the fully qualified name of the root component in the `MapRazorComponents` call:

```csharp
app.MapRazorComponents<YourApplicationName.Blazor.Server.Components.App>()
	.AddInteractiveServerRenderMode()
	.AddAdditionalAssemblies(typeof(BlazorApplication).Assembly);
```

The rest of the XAF pipeline (`AddXaf`, Security System configuration, `UseXaf`, and `MapXafEndpoints`) does not require changes.

### Disable HTTPS Redirection in Electron Mode

logs the "Failed to determine the https port for redirect" warning.

```csharp
if (!useElectron) {
	app.UseHttpsRedirection();
}
```

Traffic between the Electron window and Kestrel does not leave the local machine, so HTTPS is not required. `UseHsts` does not need changes: the HSTS middleware ignores `localhost` by default.

## Step 4. Add an Electron Launch Profile

Open the *YourApplicationName.Blazor.Server/Properties/launchSettings.json* file and add a profile that sets the `USE_ELECTRON` environment variable and disables browser launch:

```json
"Electron": {
  "commandName": "Project",
  "dotnetRunMessages": true,
  "launchBrowser": false,
  "applicationUrl": "http://localhost:5000",
  "environmentVariables": {
	"ASPNETCORE_ENVIRONMENT": "Development",
	"USE_ELECTRON": "true"
  }
}
```

## Step 5. Run the Application

1. If your solution uses the [Middle Tier Security](https://docs.devexpress.com/eXpressAppFramework/404389/data-security-and-safety/security-system/security-tiers/middle-tier-security), start the *YourApplicationName.MiddleTierWebApi* project first.
2. Run the *YourApplicationName.Blazor.Server* project with the **Electron** launch profile:
   - In Visual Studio, select the **Electron** profile and press **F5**.
   - In the command line, run `dotnet run --launch-profile Electron`.
3. The first run restores and installs Electron npm packages, so it takes longer than subsequent runs.
4. A desktop window opens and displays the XAF application's logon page.

To run the application in a browser, use any other launch profile. The application then behaves as a standard XAF Blazor application.

## Step 6. (Optional) Use Native Electron APIs from XAF Actions

In this setup, the XAF Blazor application and the Electron shell run on the same machine. The .NET process hosts the Kestrel server, and Electron displays it in a native window. This allows XAF Controllers to call the Electron.NET API (the static `Electron` class) and access native OS features that a regular browser cannot reach: message boxes, file dialogs, notifications, the system tray, the clipboard, and so on.

How it works:

1. A user clicks an XAF Action in the Electron window.
2. The Action's `Execute` event handler runs on the server (the local .NET process), as in any XAF Blazor application.
3. The handler calls an Electron.NET API, for example, `Electron.Dialog.ShowMessageBoxAsync`.
4. Electron.NET forwards the call to the Electron main process, which displays a native OS dialog.

> [!NOTE]
> Native APIs work only when the application runs with the **Electron** launch profile. In a regular browser, the Electron process does not exist. Use the `HybridSupport.IsElectronActive` property to deactivate Electron-specific Actions in this case.

### Example: Show a Native Message Box

The *ElectronExample.Blazor.Server/Controllers/ElectronMessageBoxController.cs* file in the example solution implements a Window Controller with the **Native Message Box** [SimpleAction](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Actions.SimpleAction). The Action shows a native Electron message box:

```csharp
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace YourApplicationName.Blazor.Server.Controllers;

public class ElectronMessageBoxController : WindowController {
    public ElectronMessageBoxController() {
        TargetWindowType = WindowType.Main;
        var showMessageBoxAction = new SimpleAction(this, "ShowElectronMessageBox", PredefinedCategory.Tools) {
            Caption = "Native Message Box",
            ImageName = "Action_AboutInfo"
        };
        showMessageBoxAction.Execute += ShowMessageBoxAction_Execute;
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
```

How the Controller works:

- **Declare an XAF Action.** The Controller creates a `SimpleAction` and handles its `Execute` event. XAF renders the Action in the toolbar. You do not need custom Blazor markup.
- **Activate the Controller in Electron mode only.** `Active["ElectronActive"] = HybridSupport.IsElectronActive` hides the Action when the application runs in a regular browser, where native APIs are unavailable.
- **Call the native API in the `Execute` handler.** Configure the dialog with `MessageBoxOptions` and pass it to `Electron.Dialog.ShowMessageBoxAsync`. Electron.NET forwards the call to the Electron main process, which displays the native OS dialog.
- **Pass the parent window.** `Electron.WindowManager.BrowserWindows.FirstOrDefault()` returns the application window. When you pass it to `ShowMessageBoxAsync`, the dialog is modal to this window.
- **Handle exceptions.** The handler is `async void` because the `Execute` event is synchronous. Wrap the code in `try/catch` and log errors with `Tracing.Tracer.LogError`. Otherwise, an unhandled exception can terminate the application.

To configure the dialog, use the `Type` (`info`, `warning`, `error`, `question`), `Title`, `Buttons`, `DefaultId`, `CancelId`, and `Detail` properties of `MessageBoxOptions`. `ShowMessageBoxAsync` returns a `MessageBoxResult` object. Its `Response` property contains the index of the clicked button. Use this value to implement a confirmation dialog:

```csharp
var result = await Electron.Dialog.ShowMessageBoxAsync(window, new MessageBoxOptions("Delete the record?") {
    Type = MessageBoxType.question,
    Buttons = new[] { "Yes", "No" },
    CancelId = 1
});
if (result.Response == 0) {
    // The user clicked "Yes".
}
```

To test the Action, run the application with the **Electron** launch profile and click **Native Message Box** in the toolbar.

### Other Native APIs

You can use the same pattern to call other Electron.NET APIs from XAF Controllers:

| API | Purpose |
|---|---|
| `Electron.Dialog.ShowOpenDialogAsync` / `ShowSaveDialogAsync` | Native file open and save dialogs. |
| `Electron.Notification.Show` | OS notifications. |
| `Electron.Tray` | A system tray icon and menu. |
| `Electron.Clipboard` | Read and write the system clipboard. |
| `Electron.Shell.OpenExternalAsync` | Open a URL in the default browser. |
| `Electron.Menu.SetApplicationMenu` | A native application menu. |

## See Also
- [ElectronNET.Core on GitHub](https://github.com/ElectronNET/Electron.NET)
- [Electron Documentation](https://www.electronjs.org/docs/latest)
<!-- feedback -->
## Does This Example Address Your Development Requirements/Objectives?

[<img src="https://www.devexpress.com/support/examples/i/yes-button.svg"/>](https://www.devexpress.com/support/examples/survey.xml?utm_source=github&utm_campaign=xaf-blazor-electron-integration&~~~was_helpful=yes) [<img src="https://www.devexpress.com/support/examples/i/no-button.svg"/>](https://www.devexpress.com/support/examples/survey.xml?utm_source=github&utm_campaign=xaf-blazor-electron-integration&~~~was_helpful=no)

(you will be redirected to DevExpress.com to submit your response)
<!-- feedback end -->
