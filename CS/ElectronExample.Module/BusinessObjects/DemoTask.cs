using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;

namespace ElectronExample.Module.BusinessObjects;

[DefaultClassOptions]
[ModelDefault("Caption", "Task")]
[Appearance("FontColorRed", AppearanceItemType = "ViewItem", TargetItems = "*", Context = "ListView", Criteria = "Status=='Completed'", FontStyle = DevExpress.Drawing.DXFontStyle.Strikeout)]
[RuleCriteria("Task_Status", DefaultContexts.Save, "IIF(Status != 'NotStarted' and Status != 'Deferred', AssignedTo is not null, True)", CustomMessageTemplate = @"The task must have an assignee when its Status is ""In progress"", ""Waiting for someone else"", or ""Completed"".", SkipNullOrEmptyValues = false)]
[RuleCriteria("TaskIsNotStarted", DefaultContexts.Save, "Status != 'NotStarted'", CustomMessageTemplate = "Cannot set the task completed because it's not started.", TargetContextIDs = "MarkCompleted")]
public class DemoTask : BaseObject {
    private string subject;
    private string description;
    private DateTime dueDate;
    private DateTime startDate;
    private Employee assignedTo;
    private TaskStatus status;
    private int percentCompleted;
    private DateTime dateCompleted;
    private Priority priority;
    private int estimatedWorkHours;
    private int actualWorkHours;
    public DemoTask(Session session)
        : base(session) {
    }
    [Action(ImageName = "State_Task_Completed")]
    public void MarkCompleted() {
        Status = TaskStatus.Completed;
    }
    public string Subject {
        get { return subject; }
        set { SetPropertyValue(nameof(Subject), ref subject, value); }
    }
    [Size(SizeAttribute.Unlimited)]
    public string Description {
        get { return description; }
        set { SetPropertyValue(nameof(Description), ref description, value); }
    }
    public DateTime DueDate {
        get { return dueDate; }
        set { SetPropertyValue(nameof(DueDate), ref dueDate, value); }
    }
    public DateTime StartDate {
        get { return startDate; }
        set { SetPropertyValue(nameof(StartDate), ref startDate, value); }
    }
    public Employee AssignedTo {
        get { return assignedTo; }
        set { SetPropertyValue(nameof(AssignedTo), ref assignedTo, value); }
    }
    public TaskStatus Status {
        get { return status; }
        set {
            if(SetPropertyValue(nameof(Status), ref status, value)) {
                if(IsLoading)
                    return;
                switch(status) {
                    case TaskStatus.NotStarted:
                        PercentCompleted = 0;
                        break;
                    case TaskStatus.Completed:
                        PercentCompleted = 100;
                        break;
                    case TaskStatus.InProgress:
                        if(PercentCompleted == 100)
                            PercentCompleted = 75;
                        break;
                    case TaskStatus.WaitingForSomeoneElse:
                    case TaskStatus.Deferred:
                        if(PercentCompleted == 100)
                            PercentCompleted = 0;
                        break;
                }
                CheckDateCompleted();
            }
        }
    }
    public int PercentCompleted {
        get { return percentCompleted; }
        set {
            if(SetPropertyValue(nameof(PercentCompleted), ref percentCompleted, value)) {
                if(IsLoading)
                    return;
                if(percentCompleted == 100)
                    status = TaskStatus.Completed;
                if(percentCompleted == 0)
                    status = TaskStatus.NotStarted;
                if((percentCompleted > 0) && (percentCompleted < 100))
                    status = TaskStatus.InProgress;
                CheckDateCompleted();
            }
        }
    }
    [ModelDefault("AllowEdit", "False")]
    public DateTime DateCompleted {
        get { return dateCompleted; }
        set { SetPropertyValue(nameof(DateCompleted), ref dateCompleted, value); }
    }
    private void CheckDateCompleted() {
        DateCompleted = Status == TaskStatus.Completed ? DateTime.Now : DateTime.MinValue;
    }
    [Appearance("PriorityBackColorPink", AppearanceItemType = "ViewItem", Criteria = "Priority=2", BackColor = "0x14FF0000")]
    public Priority Priority {
        get { return priority; }
        set { SetPropertyValue(nameof(Priority), ref priority, value); }
    }
    public override void AfterConstruction() {
        base.AfterConstruction();
        Priority = Priority.Normal;
    }
    [ToolTip("View, assign or remove employees for the current task")]
    [Association("Employee-DemoTask")]
    public XPCollection<Employee> Employees {
        get { return GetCollection<Employee>(nameof(Employees)); }
    }
    public override string ToString() {
        return this.Subject;
    }
    [Action(ToolTip = "Postpone the task to the next day", ImageName = "State_Task_Deferred")]
    public void Postpone() {
        if(DueDate == DateTime.MinValue) {
            DueDate = DateTime.Now;
        }
        DueDate = DueDate + TimeSpan.FromDays(1);
    }
    [RuleValueComparison("Task_EstimatedWorkHours", DefaultContexts.Save, ValueComparisonType.GreaterThanOrEqual, 0)]
    public int EstimatedWorkHours {
        get { return estimatedWorkHours; }
        set { SetPropertyValue<int>(nameof(EstimatedWorkHours), ref estimatedWorkHours, value); }
    }
    [RuleValueComparison("Task_ActualWorkHours", DefaultContexts.Save, ValueComparisonType.GreaterThanOrEqual, 0)]
    public int ActualWorkHours {
        get { return actualWorkHours; }
        set { SetPropertyValue<int>(nameof(ActualWorkHours), ref actualWorkHours, value); }
    }
}
public enum Priority {
    [ImageName("State_Priority_Low")]
    Low = 0,
    [ImageName("State_Priority_Normal")]
    Normal = 1,
    [ImageName("State_Priority_High")]
    High = 2
}
