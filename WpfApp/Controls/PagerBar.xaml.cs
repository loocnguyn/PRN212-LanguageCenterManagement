using System.Windows;
using System.Windows.Controls;

namespace WpfApp.Controls;

// ============================================================
//  PagerBar — reusable « ‹ Page x / y › » footer for list windows.
//  CONTENTS:
//    1. State           — ItemsCount / PageSize / Page
//    2. Paging helpers  — Skip, Slice, TotalPages
//    3. Navigation      — first / prev / next / last, raises PageChanged
//  USAGE (host window):
//    XAML : <ctl:PagerBar x:Name="pager" PageChanged="Pager_PageChanged"/>
//    CS   : pager.ItemsCount = _filtered.Count;                  // clamps page, no event
//           dg.ItemsSource   = pager.Slice(_filtered);
//    Reset to page 1 whenever the filter changes: pager.Reset().
// ============================================================
public partial class PagerBar : UserControl
{
    /// <summary>Raised when the user navigates. The host should re-slice and rebind.</summary>
    public event EventHandler? PageChanged;

    private int _itemsCount;
    private int _pageSize = 20;
    private int _page = 1;

    public PagerBar()
    {
        InitializeComponent();
        Refresh();
    }

    /// <summary>Rows per page. Defaults to 20.</summary>
    public int PageSize
    {
        get => _pageSize;
        set { _pageSize = Math.Max(1, value); Refresh(); }
    }

    /// <summary>
    /// Total rows across all pages. Setting this clamps <see cref="Page"/> into range
    /// and refreshes the label without raising <see cref="PageChanged"/>.
    /// </summary>
    public int ItemsCount
    {
        get => _itemsCount;
        set { _itemsCount = Math.Max(0, value); Refresh(); }
    }

    /// <summary>Current 1-based page number.</summary>
    public int Page
    {
        get => _page;
        set { _page = Math.Clamp(value, 1, TotalPages); Refresh(); }
    }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(_itemsCount / (double)_pageSize));

    /// <summary>Number of rows to skip to reach the current page.</summary>
    public int Skip => (_page - 1) * _pageSize;

    /// <summary>Jump back to page 1 — call after a search/filter change.</summary>
    public void Reset() { _page = 1; Refresh(); }

    /// <summary>
    /// Sets <see cref="ItemsCount"/> from <paramref name="source"/> and returns the current page's rows.
    /// </summary>
    public List<T> Slice<T>(IEnumerable<T> source)
    {
        var list = source as IList<T> ?? source.ToList();
        ItemsCount = list.Count;
        return list.Skip(Skip).Take(_pageSize).ToList();
    }

    private void Refresh()
    {
        // Called from the constructor before the template is applied.
        if (txtPage == null) return;

        var totalPages = TotalPages;
        if (_page > totalPages) _page = totalPages;

        txtPage.Text = $"Page {_page} / {totalPages}";

        if (_itemsCount == 0)
        {
            txtRange.Text = "No records";
        }
        else
        {
            var from = Skip + 1;
            var to = Math.Min(_itemsCount, Skip + _pageSize);
            txtRange.Text = $"{from}–{to} of {_itemsCount}";
        }

        btnFirst.IsEnabled = btnPrev.IsEnabled = _page > 1;
        btnNext.IsEnabled = btnLast.IsEnabled = _page < totalPages;
    }

    private void GoTo(int page)
    {
        var target = Math.Clamp(page, 1, TotalPages);
        if (target == _page) return;
        _page = target;
        Refresh();
        PageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BtnFirst_Click(object sender, RoutedEventArgs e) => GoTo(1);
    private void BtnPrev_Click(object sender, RoutedEventArgs e) => GoTo(_page - 1);
    private void BtnNext_Click(object sender, RoutedEventArgs e) => GoTo(_page + 1);
    private void BtnLast_Click(object sender, RoutedEventArgs e) => GoTo(TotalPages);
}
