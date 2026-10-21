using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.AspNetCore.DesignTime;
using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using DevExpress.ExpressApp.Security.Authentication.ClientServer;
using DevExpress.ExpressApp.ApplicationBuilder;

if(ContainsArgument(args, "help") || ContainsArgument(args, "h")) {
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
DevExpress.ExpressApp.FrameworkSettings.DefaultSettingsCompatibilityMode = DevExpress.ExpressApp.FrameworkSettingsCompatibilityMode.Latest;
DevExpress.ExpressApp.Security.SecurityStrategy.AutoAssociationReferencePropertyMode = DevExpress.ExpressApp.Security.ReferenceWithoutAssociationPermissionsMode.AllMembers;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddXafMiddleTier(builder.Configuration, xafBuilder => {
    xafBuilder.ConfigureDataServer(options => {
        options.UseConnectionString(builder.Configuration.GetConnectionString("ConnectionString"));
        options.UseDataStorePool(true);
    });
    xafBuilder.Modules
        .AddAuditTrailXpo()
        .AddReports(options => {
            options.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.ReportDataV2);
        })
        .Add<ElectronExample.Module.ElectronExampleModule>();


    xafBuilder.Security
        .UseIntegratedMode(options => {
            options.Lockout.Enabled = true;

            options.RoleType = typeof(PermissionPolicyRole);
            // ApplicationUser descends from PermissionPolicyUser and supports the OAuth authentication. For more information, refer to the following topic: https://docs.devexpress.com/eXpressAppFramework/402197
            // If your application uses PermissionPolicyUser or a custom user type, set the UserType property as follows:
            options.UserType = typeof(ElectronExample.Module.BusinessObjects.ApplicationUser);
            options.UserTokenType = typeof(UserToken);
            // ApplicationUserLoginInfo is only necessary for applications that use the ApplicationUser user type.
            // If you use PermissionPolicyUser or a custom user type, comment out the following line:
            options.UserLoginInfoType = typeof(ElectronExample.Module.BusinessObjects.ApplicationUserLoginInfo);
            options.UseXpoPermissionsCaching();
            options.Events.OnSecurityStrategyCreated += securityStrategy => {
                //((SecurityStrategy)securityStrategy).AnonymousAllowedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.ModelDifference));
                //((SecurityStrategy)securityStrategy).AnonymousAllowedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.ModelDifferenceAspect));
                ((SecurityStrategy)securityStrategy).PermissionsReloadMode = PermissionsReloadMode.CacheOnFirstAccess;
            };
        })
        .AddPasswordAuthentication(options => {
            options.IsSupportChangePassword = true;
        });

    xafBuilder.AddBuildStep(application => {
        application.ApplicationName = "SetupApplication.ElectronExample";
        application.CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema;
        //application.CreateCustomModelDifferenceStore += (s, e) => {
            //    e.Store = new ModelDifferenceDbStore((XafApplication)sender!, typeof(ModelDifference), true, "Win");
            //    e.Handled = true;
        //};
#if !RELEASE
        if(application.CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema) {
            application.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
            application.DatabaseVersionMismatch += (s, e) => {
                e.Updater.Update();
                e.Handled = true;
            };
        }
#endif
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters() {
            ValidateIssuerSigningKey = true,
            //ValidIssuer = builder.Configuration["Authentication:Jwt:Issuer"],
            //ValidAudience = builder.Configuration["Authentication:Jwt:Audience"],
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Authentication:Jwt:IssuerSigningKey"])),
            AuthenticationType = JwtBearerDefaults.AuthenticationScheme
        };
});

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(
        new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireXafAuthentication()
            .Build());
WebApplication app = builder.Build();
if(ContainsArgument(args, "updateDatabase")) {
    using(var serviceScope = app.Services.CreateScope()) {
        return serviceScope.ServiceProvider.GetRequiredService<DevExpress.ExpressApp.Utils.IDBUpdater>().Update(ContainsArgument(args, "forceUpdate"), ContainsArgument(args, "silent"));
    }
}
if(!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. To change this for production scenarios, see: https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
#if RELEASE
app.UseHttpsRedirection();
#endif
app.UseRequestLocalization();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseXafMiddleTier();
app.MapControllers();
app.Run();
return 0;
bool ContainsArgument(string[] args, string argument) {
    return args.Any(arg => arg.TrimStart('/').TrimStart('-').ToLower() == argument.ToLower());
}
