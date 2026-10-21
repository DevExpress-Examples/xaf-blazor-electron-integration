using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.DesignTime;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Blazor;
using DevExpress.AIIntegration;
using DevExpress.AspNetCore.Reporting;
using DevExpress.Blazor.Reporting;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.MiddleTier;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.Persistent.Base;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;
using DevExpress.ExpressApp.Xpo;
using ElectronExample.Blazor.Server.Services;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Microsoft.Extensions.AI;
using ElectronExample.Blazor.Server;
using ElectronExample.Blazor.Server.Components;
using ElectronNET.API;
using ElectronNET.API.Entities;

if (ContainsArgument(args, "help") || ContainsArgument(args, "h")) {
    Console.WriteLine("Updates the database when its version does not match the application's version.");
    Console.WriteLine();
    Console.WriteLine($"    {Assembly.GetExecutingAssembly().GetName().Name}.exe --updateDatabase [--forceUpdate --silent]");
    Console.WriteLine();
    Console.WriteLine("--forceUpdate - Marks that the database must be updated whether its version matches the application's version or not.");
    Console.WriteLine("--silent - Marks that database update proceeds automatically and does not require any interaction with the user.");
    Console.WriteLine();
    Console.WriteLine($"Exit codes: 0 - {DBUpdaterStatus.UpdateCompleted}");
    Console.WriteLine($"            1 - {DBUpdaterStatus.UpdateError}");
    Console.WriteLine($"            2 - {DBUpdaterStatus.UpdateNotNeeded}");
    return 0;
}
DevExpress.ExpressApp.Blazor.Editors.LookupPropertyEditor.DefaultUseViewMode = true;
DevExpress.ExpressApp.FrameworkSettings.DefaultSettingsCompatibilityMode = DevExpress.ExpressApp.FrameworkSettingsCompatibilityMode.Latest;
DevExpress.ExpressApp.Security.SecurityStrategy.AutoAssociationReferencePropertyMode = DevExpress.ExpressApp.Security.ReferenceWithoutAssociationPermissionsMode.AllMembers;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(typeof(Microsoft.AspNetCore.SignalR.HubConnectionHandler<>), typeof(ProxyHubConnectionHandler<>));

#region AI Chat Client
builder.Services.AddSingleton<IChatClient>(services => {
    string azureOpenAIEndpoint = null;
    string azureOpenAIKey = null;
    string azureOpenAIDeployment = null;
    if (string.IsNullOrEmpty(azureOpenAIEndpoint) || string.IsNullOrEmpty(azureOpenAIKey) || string.IsNullOrEmpty(azureOpenAIDeployment)) {
        var message = "Credentials for AI Client were not found." +
            " Specify azureOpenAIEndpoint, Key and Deployment at Program.cs" +
            "\nSee more: https://docs.devexpress.com/Blazor/405228/ai-powered-extensions#prerequisites";
        throw new InvalidOperationException(message);
    }
    var azureOpenAIV1Endpoint = new Uri($"{azureOpenAIEndpoint.TrimEnd('/')}/openai/v1");
    var azureOpenAIClient = new OpenAI.OpenAIClient(
        new System.ClientModel.ApiKeyCredential(azureOpenAIKey),
        new OpenAI.OpenAIClientOptions() { Endpoint = azureOpenAIV1Endpoint }
    );
    return azureOpenAIClient.GetChatClient(azureOpenAIDeployment).AsIChatClient();
});
#endregion

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

WebApplication app = null;
bool useElectron = builder.Configuration.GetValue<bool>("USE_ELECTRON");
if (useElectron) {
    builder.Services.AddElectron(); // <-- might be useful to set up DI
    builder.UseElectron(args, async () => {
        var options = new BrowserWindowOptions {
            Show = false,
            IsRunningBlazor = true, // <-- crucial
        };
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            options.AutoHideMenuBar = true;
        var browserWindow = await Electron.WindowManager.CreateWindowAsync(options);
        browserWindow.OnReadyToShow += () => browserWindow.Show();
    });
}
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CircuitHandler, CircuitHandlerProxy>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddXaf(builder.Configuration, xafBuilder => {
    xafBuilder.UseApplication<ElectronExampleBlazorApplication>();
    xafBuilder.Modules
        .AddAuditTrailXpo()
        .AddCloning()
        .AddConditionalAppearance()
        .AddDashboards(options => {
            options.DashboardDataType = typeof(DevExpress.Persistent.BaseImpl.DashboardData);
        })
        .AddFileAttachments()
        .AddNotifications()
        .AddOffice()
        .AddReports(options => {
            options.EnableInplaceReports = true;
            options.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.ReportDataV2);
            options.ReportStoreMode = DevExpress.ExpressApp.ReportsV2.ReportStoreModes.XML;
        })
        .AddScheduler()
        .AddStateMachine(options => {
            options.StateMachineStorageType = typeof(DevExpress.ExpressApp.StateMachine.Xpo.XpoStateMachine);
        })
        .AddValidation(options => {
            options.AllowValidationDetailsAccess = false;
        })
        .AddViewVariants()
        .AddAI((options, aiOptions) => {
            aiOptions.AddBlazorReportingAIIntegration(reportingOptions =>
                reportingOptions
                    .AddSummarization(summarizeOptions =>
                        summarizeOptions.SetSummarizationMode(SummarizationMode.Abstractive)
                            .SetTemperature(0.1f))
                    .AddTranslation(translateOptions =>
                        translateOptions.SetLanguages(options.Languages)
                            .EnableTranslation()
                            .EnableInlineTranslation())
            );
            aiOptions.AddWebReportingAIIntegration(reportingOptions =>
                reportingOptions
                   .AddPromptToReportConverter()
                   .AddPromptToExpressionConverter()
                   .AddLocalization()
                   .AddTestDataSource()
                   .AddSummarization(summarizeOptions =>
                        summarizeOptions.SetSummarizationMode(SummarizationMode.Abstractive))
                   .AddTranslation(translationCfg =>
                       translationCfg.SetLanguages(options.Languages)
                           .EnableTranslation()
                           .EnableInlineTranslation())
            );
        })
        .Add<ElectronExample.Module.ElectronExampleModule>()
        .Add<ElectronExampleBlazorModule>();
    xafBuilder.ObjectSpaceProviders
        .AddNonPersistent();
    xafBuilder.Security
        .UseMiddleTierMode(options => {
#if !RELEASE
            options.WaitForMiddleTierServerReady();
            options.BaseAddress = new Uri("http://localhost:5002/");
#else
            options.BaseAddress = new Uri("https://localhost:44350/");
#endif
        })
        .AddPasswordAuthentication();
});
var authentication = builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});
authentication.AddCookie(options => {
    options.LoginPath = "/LoginPage";
});

app = builder.Build();
if (ContainsArgument(args, "updateDatabase")) {
    using (var serviceScope = app.Services.CreateScope()) {
        return serviceScope.ServiceProvider.GetRequiredService<DevExpress.ExpressApp.Utils.IDBUpdater>().Update(ContainsArgument(args, "forceUpdate"), ContainsArgument(args, "silent"));
    }
}
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. To change this for production scenarios, see: https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), appBuilder => appBuilder.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
if (!useElectron) {
    app.UseHttpsRedirection();
}
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseXaf();
app.MapStaticAssets();
app.MapRazorComponents<ElectronExample.Blazor.Server.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorApplication).Assembly);
app.MapXafEndpoints();
app.MapControllers();
app.Run();
return 0;

bool ContainsArgument(string[] args, string argument) {
    return args.Any(arg => arg.TrimStart('/').TrimStart('-').ToLower() == argument.ToLower());
}
