namespace SmartLogReader.Common;

public interface IListInclExclViewModel
{
    /// <summary>
    /// 
    /// </summary>
    public FilterCollection IncludeList { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public FilterCollection ExcludeList { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public void NotifyFilterChanged();
}
