using System.Windows;

namespace SmartLogReader.Common;

/// <summary>
/// 
/// </summary>
public class IndexValuePair : Notifier
{
    /// <summary>
    /// 
    /// </summary>
    public IndexValuePair(int propertyIndex, string propertyValue)
    {
        PropertyIndex = propertyIndex;
        PropertyValue = propertyValue;
    }
    int PropertyIndex;

    /// <summary>
    /// 
    /// </summary>
    public string PropertyName
    {
        get { return Filter.PropertyNames[PropertyIndex]; }
        set { }
    }

    /// <summary>
    /// 
    /// </summary>
    public string PropertyValue { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public override string ToString()
    {
        return string.Format("IndexValuePair[{0},{1},{2}]", PropertyIndex, PropertyName, PropertyValue);
    }

    /// <summary>
    /// 
    /// </summary>
    public IListInclExclViewModel ViewModel { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsIncluded
    {
        get { return includedFilter != null; }
        set
        {
            OnPropertyChanged();
            ModifyList(ViewModel.IncludeList, ref includedFilter, ref FirstIncluded, value);
        }
    }
    Filter includedFilter;
    public static int FirstIncluded = -1;

    /// <summary>
    /// 
    /// </summary>
    public bool IsExcluded
    {
        get { return excludedFilter != null; }
        set
        {
            OnPropertyChanged();
            ModifyList(ViewModel.ExcludeList, ref excludedFilter, ref FirstExcluded, value);
        }
    }
    Filter excludedFilter;
    public static int FirstExcluded = -1;

    /// <summary>
    /// 
    /// </summary>
    void ModifyList(FilterCollection filterList, ref Filter filter, ref int indexOfFirst, bool createNewFilter)
    {
        if (createNewFilter)
        {
            if (PropertyIndex == 8 && PropertyValue.IndexOf('\n') > 0)
            {
                MessageBox.Show("Only a single JSON property can be specified in this version.\r\nPlease delete all lines except one.", "We're sorry!", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            filter = new Filter(PropertyIndex, 0, PropertyValue);
            filter.AndNext = true;
            filterList.Add(filter);
        }
        else
        {
            filterList.Remove(filter);
            filter = null;
        }

        ViewModel.NotifyFilterChanged();
    }
}
