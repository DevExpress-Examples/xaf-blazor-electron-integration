using DevExpress.ExpressApp;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Security.Strategy;
using DevExpress.Xpo;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using ElectronExample.Module.BusinessObjects;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using TaskStatus = ElectronExample.Module.BusinessObjects.TaskStatus;

namespace ElectronExample.Module.DatabaseUpdate;

// For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater
public class Updater : ModuleUpdater {
    public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
        base(objectSpace, currentDBVersion) {
    }
    public override void UpdateDatabaseAfterUpdateSchema() {
        base.UpdateDatabaseAfterUpdateSchema();
        //string name = "MyName";
        //DomainObject1 theObject = ObjectSpace.FirstOrDefault<DomainObject1>(u => u.Name == name);
        //if(theObject == null) {
        //    theObject = ObjectSpace.CreateObject<DomainObject1>();
        //    theObject.Name = name;
        //}

        // The code below creates users and roles for testing purposes only.
        // In production code, you can create users and assign roles to them automatically, as described in the following help topic:
        // https://docs.devexpress.com/eXpressAppFramework/119064/data-security-and-safety/security-system/authentication
#if !RELEASE
        // If a role doesn't exist in the database, create this role
        var defaultRole = CreateDefaultRole();
        var adminRole = CreateAdminRole();

        ObjectSpace.CommitChanges(); //This line persists created object(s).

        UserManager userManager = ObjectSpace.ServiceProvider.GetRequiredService<UserManager>();

        // If a user named 'User' doesn't exist in the database, create this user
        if(userManager.FindUserByName<ApplicationUser>(ObjectSpace, "User") == null) {
            // Set a password if the standard authentication type is used
            string EmptyPassword = "";
            _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "User", EmptyPassword, (user) => {
                // Add the Users role to the user
                user.Roles.Add(defaultRole);
            });
        }

        // If a user named 'Admin' doesn't exist in the database, create this user
        if(userManager.FindUserByName<ApplicationUser>(ObjectSpace, "Admin") == null) {
            // Set a password if the standard authentication type is used
            string EmptyPassword = "";
            _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "Admin", EmptyPassword, (user) => {
                // Add the Administrators role to the user
                user.Roles.Add(adminRole);
            });
        }

        ObjectSpace.CommitChanges(); //This line persists created object(s).
#endif

        CreateEmployees();
        ObjectSpace.CommitChanges();

        IList<Employee> employees = ObjectSpace.GetObjects<Employee>();
        IList<DemoTask> taskList = GenerateTask(employees);
        if(taskList.Count > 0) {
            Random rndGenerator = new Random();
            foreach(Employee employee in employees) {
                if(taskList.Count == 1) {
                    employee.Tasks.Add(taskList[0]);
                }
                else if(taskList.Count == 2) {
                    employee.Tasks.Add(taskList[0]);
                    employee.Tasks.Add(taskList[1]);
                }
                else {
                    int index = rndGenerator.Next(1, taskList.Count - 2);
                    employee.Tasks.Add(taskList[index]);
                    employee.Tasks.Add(taskList[index - 1]);
                    employee.Tasks.Add(taskList[index + 1]);
                }
            }
        }
        ObjectSpace.CommitChanges();
    }
    private void CreateEmployees() {
        DataTable employeesTable = GetEmployeesDataTable();
        foreach(DataRow employeeRow in employeesTable.Rows) {
            string email = Convert.ToString(employeeRow["EmailAddress"]);
            Employee employee = ObjectSpace.FirstOrDefault<Employee>(e => e.Email == email, true);
            if(employee == null) {
                employee = ObjectSpace.CreateObject<Employee>();
                employee.Email = email;
                employee.FirstName = Convert.ToString(employeeRow["FirstName"]);
                employee.LastName = Convert.ToString(employeeRow["LastName"]);
                employee.Birthday = Convert.ToDateTime(employeeRow["BirthDate"]);
                employee.Photo = Convert.FromBase64String(Convert.ToString(employeeRow["ImageData"]));
                string titleOfCourtesyText = Convert.ToString(employeeRow["Title"]).ToLower();
                if(!string.IsNullOrEmpty(titleOfCourtesyText)) {
                    titleOfCourtesyText = titleOfCourtesyText.Replace(".", "");
                    if(Enum.TryParse(titleOfCourtesyText, true, out TitleOfCourtesy titleOfCourtesy)) {
                        employee.TitleOfCourtesy = titleOfCourtesy;
                    }
                }
            }
        }
    }
    private List<DemoTask> GenerateTask(IList<Employee> employees) {
        Random rndGenerator = new Random();
        List<DemoTask> taskList = new List<DemoTask>();
        if(employees.Count == 0) {
            return taskList;
        }
        DataTable tasksTable = GetTasksDataTable();
        foreach(DataRow taskRow in tasksTable.Rows) {
            string taskSubject = UpdateContent(Convert.ToString(taskRow["Subject"]));
            if(ObjectSpace.FirstOrDefault<DemoTask>(t => t.Subject == taskSubject) == null) {
                DemoTask task = ObjectSpace.CreateObject<DemoTask>();
                task.Subject = taskSubject;
                task.Description = UpdateContent(Convert.ToString(taskRow["Description"]));
                int rndStatus = rndGenerator.Next(0, 5);
                task.Status = (TaskStatus)rndStatus;
                task.DueDate = DateTime.Now.AddHours((90 - rndStatus * 9) + 24).Date;
                task.EstimatedWorkHours = rndGenerator.Next(10, 20);
                if(task.Status == TaskStatus.WaitingForSomeoneElse ||
                    task.Status == TaskStatus.Completed ||
                    task.Status == TaskStatus.InProgress) {
                    task.StartDate = DateTime.Now.AddHours(-rndGenerator.Next(720)).Date;
                    task.ActualWorkHours = rndGenerator.Next(task.EstimatedWorkHours - 10, task.EstimatedWorkHours + 10);
                }
                if(task.Status == TaskStatus.WaitingForSomeoneElse ||
                    task.Status == TaskStatus.InProgress) {
                    task.PercentCompleted = rndGenerator.Next(1, 99);
                }
                if(task.Status == TaskStatus.Completed) {
                    task.PercentCompleted = 100;
                }
                task.DueDate = DateTime.Now.AddHours((90 - rndStatus * 9) + 24).Date;
                task.AssignedTo = employees[rndGenerator.Next(0, employees.Count)];
                task.Priority = (Priority)rndGenerator.Next(3);
                taskList.Add(task);
            }
        }
        return taskList;
    }
    private DataTable GetEmployeesDataTable() {
        DataSet ds = new DataSet();
        ds.ReadXml(GetResourceByName("EmployeesWithPhoto.xml"));
        return ds.Tables["Employee"];
    }
    private DataTable GetTasksDataTable() {
        DataSet ds = new DataSet();
        ds.ReadXml(GetResourceByName("Tasks.xml"));
        return ds.Tables["Task"];
    }
    private Stream GetResourceByName(string shortName) {
        string embeddedResourceName = Array.Find(GetType().Assembly.GetManifestResourceNames(), s => s.Contains(shortName));
        Stream stream = embeddedResourceName == null ? null : GetType().Assembly.GetManifestResourceStream(embeddedResourceName);
        return stream ?? throw new Exception(string.Format("Cannot read data from the {0} file!", shortName));
    }
    private static string UpdateContent(string content) {
        if(!string.IsNullOrEmpty(content)) {
            return content.Replace("LAST_YEAR", (DateTime.Now.Year - 1).ToString()).Replace("CURRENT_YEAR", DateTime.Now.Year.ToString());
        }
        return content;
    }
    public override void UpdateDatabaseBeforeUpdateSchema() {
        base.UpdateDatabaseBeforeUpdateSchema();
        //if(CurrentDBVersion < new Version("1.1.0.0") && CurrentDBVersion > new Version("0.0.0.0")) {
        //    RenameColumn("DomainObject1Table", "OldColumnName", "NewColumnName");
        //}
    }
    PermissionPolicyRole CreateAdminRole() {
        PermissionPolicyRole adminRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Administrators");
        if(adminRole == null) {
            adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
            adminRole.Name = "Administrators";
            adminRole.IsAdministrative = true;
        }
        return adminRole;
    }
    PermissionPolicyRole CreateDefaultRole() {
        PermissionPolicyRole defaultRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(role => role.Name == "Default");
        if(defaultRole == null) {
            defaultRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
            defaultRole.Name = "Default";

            defaultRole.AddObjectPermissionFromLambda<ApplicationUser>(SecurityOperations.Read, cm => cm.Oid == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
            defaultRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);
            defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "ChangePasswordOnFirstLogon", cm => cm.Oid == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
            defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "StoredPassword", cm => cm.Oid == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
            defaultRole.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read, SecurityPermissionState.Deny);
            defaultRole.AddObjectPermission<ModelDifference>(SecurityOperations.ReadWriteAccess, "UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
            defaultRole.AddObjectPermission<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess, "Owner.UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
            defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create, SecurityPermissionState.Allow);
            defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create, SecurityPermissionState.Allow);
            defaultRole.AddTypePermission<AuditDataItemPersistent>(SecurityOperations.Read, SecurityPermissionState.Deny);
            defaultRole.AddObjectPermissionFromLambda<AuditDataItemPersistent>(SecurityOperations.Read, a => a.UserId == CurrentUserIdOperator.CurrentUserId().ToString(), SecurityPermissionState.Allow);
            defaultRole.AddTypePermission<AuditedObjectWeakReference>(SecurityOperations.Read, SecurityPermissionState.Allow);
        }
        return defaultRole;
    }
}
