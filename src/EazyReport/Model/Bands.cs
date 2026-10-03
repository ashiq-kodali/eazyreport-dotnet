namespace EazyReport.Model;

public class Band
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    public string Type { get; set; } = "band";
    public string Name { get; set; } = "";
    public double Height { get; set; } = 20.0;
    public bool CanGrow { get; set; } = false;
    public bool CanShrink { get; set; } = false;
    public string VisibleExpr { get; set; } = "";
    public string Fill { get; set; } = "";
    public bool StartNewPage { get; set; } = false;
    public bool PrintOnFirstPage { get; set; } = true;
    public bool PrintOnLastPage { get; set; } = true;
    public bool RepeatOnEveryPage { get; set; } = false;
    public List<LayoutElement> Elements { get; set; } = new();
}

public class GroupBand : Band
{
    public string Expression { get; set; } = "";
    public bool ResetPageNumber { get; set; } = false;
    public bool KeepTogether { get; set; } = false;
}

public class ChildBand : Band
{
    public bool KeepWithParent { get; set; } = false;
    public bool FillUnusedSpace { get; set; } = false;
}

public class DataBand : Band
{
    public string Dataset { get; set; } = "";
    public string DataPath { get; set; } = "";
    public string Alias { get; set; } = "item";
    public string FilterExpr { get; set; } = "";
    public string SortField { get; set; } = "";
    public string SortDir { get; set; } = "asc"; // asc, desc
    public bool KeepWithNext { get; set; } = false;
    public bool KeepTogether { get; set; } = false;
    public bool FillUnusedSpace { get; set; } = false;
    public int ColumnCount { get; set; } = 1;
    public double ColumnSpacing { get; set; } = 0.0;
    public double ColumnWidth { get; set; } = 0.0;

    public Band? Header { get; set; }
    public Band? Footer { get; set; }
    public List<GroupBand> GroupHeaders { get; set; } = new();
    public List<GroupBand> GroupFooters { get; set; } = new();
    public List<ChildBand> ChildBands { get; set; } = new();
}
