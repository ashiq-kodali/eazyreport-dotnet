namespace EazyReport.Model;

public class LogicRule
{
    public string Condition { get; set; } = "";
    public string Property { get; set; } = "";
    public object? Value { get; set; }
    public bool Enabled { get; set; } = true;
}
