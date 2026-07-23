using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  GradeTypeManagementWindow — course picker for grading structures.
//  Lists the available courses with how their grade components add up,
//  then opens CourseGradingStructureWindow for the chosen one.
//  CONTENTS:
//    1. Fields & load      — courses + their component count / total weight
//    2. Filter & paging    — search, active-only toggle, PagerBar
//    3. Configure          — open the per-course editor, refresh on close
//    4. CourseRow          — grid-facing view model (status badge fields)
// ============================================================
public partial class GradeTypeManagementWindow : Window
{
    private readonly ICourseService _courseService = new CourseService();
    private readonly IGradeTypeService _gradeTypeService = new GradeTypeService();

    private List<CourseRow> _all = new();
    private List<CourseRow> _filtered = new();

    public GradeTypeManagementWindow()
    {
        InitializeComponent();
        LoadCourses();
    }

    // ---- 1. Load -----------------------------------------------
    private void LoadCourses()
    {
        // One GetAll for the grade types, then group in memory — avoids a
        // per-course service round trip while building the list.
        var byCourse = _gradeTypeService.GetAll()
            .GroupBy(g => g.CourseId)
            .ToDictionary(g => g.Key, g => g.ToList());

        _all = _courseService.GetAll()
            .Select(c => new CourseRow(c, byCourse.GetValueOrDefault(c.CourseId) ?? new List<GradeType>()))
            .OrderBy(r => r.Name)
            .ToList();

        UpdateStats();
        ApplyFilter();
    }

    private void UpdateStats()
    {
        statTotal.Text = _all.Count.ToString();
        statBalanced.Text = _all.Count(r => r.StatusKind == "Balanced").ToString();
        statIncomplete.Text = _all.Count(r => r.StatusKind is "Incomplete" or "Over").ToString();
        statNotSetUp.Text = _all.Count(r => r.ComponentCount == 0).ToString();
    }

    // ---- 2. Filter & paging ------------------------------------
    private void ApplyFilter()
    {
        if (dgCourses == null) return;

        var kw = txtSearch.Text.Trim().ToLower();
        var onlyActive = chkOnlyActive.IsChecked == true;

        _filtered = _all
            .Where(r => !onlyActive || r.IsActive)
            .Where(r => string.IsNullOrEmpty(kw)
                        || r.Name.ToLower().Contains(kw)
                        || r.Code.ToLower().Contains(kw))
            .ToList();

        dgCourses.ItemsSource = pager.Slice(_filtered);
        emptyState.Visibility = _filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Pager_PageChanged(object sender, EventArgs e) => ApplyFilter();

    private void BtnSearch_Click(object sender, RoutedEventArgs e) { pager.Reset(); ApplyFilter(); }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        txtSearch.Text = "";
        chkOnlyActive.IsChecked = true;
        pager.Reset();
        ApplyFilter();
    }

    private void ChkOnlyActive_Click(object sender, RoutedEventArgs e) { pager.Reset(); ApplyFilter(); }

    // ---- 3. Configure ------------------------------------------
    private void DgCourses_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgCourses.SelectedItem is CourseRow) ConfigureSelected();
    }

    private void BtnConfigure_Click(object sender, RoutedEventArgs e) => ConfigureSelected();

    private void ConfigureSelected()
    {
        if (dgCourses.SelectedItem is not CourseRow row)
        {
            MessageBox.Show("Please select a course to configure.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        new CourseGradingStructureWindow(row.Course) { Owner = this }.ShowDialog();
        LoadCourses(); // weights may have changed
    }

    // ---- 4. CourseRow (grid-facing view model) -----------------
    private sealed record CourseRow(Course Course, List<GradeType> GradeTypes)
    {
        public string Name => Course.Name;
        public string Code => Course.Code;
        public bool IsActive => Course.IsActive;

        public int ComponentCount => GradeTypes.Count;
        public decimal TotalWeight => GradeTypes.Sum(g => g.WeightPercent);

        public string TotalWeightText => ComponentCount == 0 ? "—" : $"{TotalWeight:0.##}%";

        /// <summary>Drives the badge colour via DataTrigger; keep in sync with StatusText.</summary>
        public string StatusKind => ComponentCount switch
        {
            0 => "NotSetUp",
            _ => TotalWeight == 100 ? "Balanced" : TotalWeight < 100 ? "Incomplete" : "Over"
        };

        public string StatusText => StatusKind switch
        {
            "Balanced" => "✓ Balanced",
            "Incomplete" => $"{100 - TotalWeight:0.##}% missing",
            "Over" => $"Over by {TotalWeight - 100:0.##}%",
            _ => "Not set up"
        };
    }
}
