using DevExpress.Persistent.Base;

namespace ElectronExample.Module.BusinessObjects;

public enum TaskStatus {
    [ImageName("State_Task_NotStarted")]
    NotStarted,
    [ImageName("State_Task_InProgress")]
    InProgress,
    [ImageName("State_Task_WaitingForSomeoneElse")]
    WaitingForSomeoneElse,
    [ImageName("State_Task_Deferred")]
    Deferred,
    [ImageName("State_Task_Completed")]
    Completed
}
