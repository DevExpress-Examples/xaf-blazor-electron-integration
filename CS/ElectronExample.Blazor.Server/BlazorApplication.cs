using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Xpo;
using ElectronExample.Blazor.Server.Services;

namespace ElectronExample.Blazor.Server;

public class ElectronExampleBlazorApplication : BlazorApplication {
    public ElectronExampleBlazorApplication() {
        ApplicationName = "ElectronExample";
        CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema;
        DatabaseVersionMismatch += ElectronExampleBlazorApplication_DatabaseVersionMismatch;
    }
    void ElectronExampleBlazorApplication_DatabaseVersionMismatch(object sender, DatabaseVersionMismatchEventArgs e) {
        string message = "Application cannot connect to the specified database.";

        CompatibilityDatabaseIsOldError isOldError = e.CompatibilityError as CompatibilityDatabaseIsOldError;
        if(isOldError != null && isOldError.Module != null) {
            message = "The client application cannot connect to the Middle Tier Application Server and its database. " +
                      "To avoid this error, ensure that both the client and the server have the same modules set. Problematic module: " + isOldError.Module.Name +
                      ". For more information, see https://docs.devexpress.com/eXpressAppFramework/113439/concepts/security-system/middle-tier-security-wcf-service#troubleshooting";
        }
        if(e.CompatibilityError == null) {
            message = "You probably tried to update the database in Middle Tier Security mode from the client side. " +
                      "In this mode, the server application updates the database automatically. " +
                      "To disable the automatic database update, set the XafApplication.DatabaseUpdateMode property to the DatabaseUpdateMode.Never value in the client application.";
        }
        throw new InvalidOperationException(message);
    }
}
