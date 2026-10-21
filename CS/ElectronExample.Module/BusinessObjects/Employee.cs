using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;

namespace ElectronExample.Module.BusinessObjects;

[DefaultClassOptions]
[DefaultProperty(nameof(FullName))]
[ImageName("BO_Person")]
public class Employee : BaseObject {
    private string firstName;
    private string lastName;
    private string middleName;
    private DateTime? birthday;
    private string email;
    private byte[] photo;
    private string webPageAddress;
    private Employee manager;
    private string nickName;
    private string spouseName;
    private TitleOfCourtesy titleOfCourtesy;
    private string notes;
    private DateTime? anniversary;
    public Employee(Session session) :
        base(session) {
    }
    public string FirstName {
        get { return firstName; }
        set { SetPropertyValue(nameof(FirstName), ref firstName, value); }
    }
    public string LastName {
        get { return lastName; }
        set { SetPropertyValue(nameof(LastName), ref lastName, value); }
    }
    public string MiddleName {
        get { return middleName; }
        set { SetPropertyValue(nameof(MiddleName), ref middleName, value); }
    }
    [PersistentAlias("concat(FirstName, ' ', LastName)")]
    public string FullName {
        get { return Convert.ToString(EvaluateAlias(nameof(FullName))); }
    }
    public DateTime? Birthday {
        get { return birthday; }
        set { SetPropertyValue(nameof(Birthday), ref birthday, value); }
    }
    [Size(255)]
    public string Email {
        get { return email; }
        set { SetPropertyValue(nameof(Email), ref email, value); }
    }
    [ImageEditor(ListViewImageEditorCustomHeight = 75, DetailViewImageEditorFixedHeight = 150)]
    public byte[] Photo {
        get { return photo; }
        set { SetPropertyValue(nameof(Photo), ref photo, value); }
    }
    [RuleRegularExpression(@"(((http|https)\://)|(www.)[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*)|([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,6})", CustomMessageTemplate = @"Invalid ""Web Page Address"".")]
    public string WebPageAddress {
        get { return webPageAddress; }
        set { SetPropertyValue(nameof(WebPageAddress), ref webPageAddress, value); }
    }
    [DataSourceCriteria("Oid != '@This.Oid'")]
    public Employee Manager {
        get { return manager; }
        set { SetPropertyValue(nameof(Manager), ref manager, value); }
    }
    public string NickName {
        get { return nickName; }
        set { SetPropertyValue(nameof(NickName), ref nickName, value); }
    }
    public string SpouseName {
        get { return spouseName; }
        set { SetPropertyValue(nameof(SpouseName), ref spouseName, value); }
    }
    public TitleOfCourtesy TitleOfCourtesy {
        get { return titleOfCourtesy; }
        set { SetPropertyValue(nameof(TitleOfCourtesy), ref titleOfCourtesy, value); }
    }
    public DateTime? Anniversary {
        get { return anniversary; }
        set { SetPropertyValue(nameof(Anniversary), ref anniversary, value); }
    }
    [Size(4096)]
    public string Notes {
        get { return notes; }
        set { SetPropertyValue(nameof(Notes), ref notes, value); }
    }
    [Association("Employee-DemoTask")]
    public XPCollection<DemoTask> Tasks {
        get { return GetCollection<DemoTask>(nameof(Tasks)); }
    }
}
public enum TitleOfCourtesy {
    Dr,
    Miss,
    Mr,
    Mrs,
    Ms
};
