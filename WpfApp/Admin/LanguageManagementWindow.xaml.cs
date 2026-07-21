using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  LanguageManagementWindow — the languages the centre teaches.
//  CONTENTS:
//    1. Load          — languages + how many levels/courses hang off each
//    2. Add / edit / delete
//    3. Levels        — drill into one language's levels
//    4. LanguageRow   — grid-facing view model
// ============================================================
public partial class LanguageManagementWindow : Window
{
    private readonly ICatalogueService _service = new CatalogueService();
    private List<LanguageRow> _all = new();

    public LanguageManagementWindow()
    {
        InitializeComponent();
        LoadData();
    }

    // ---- 1. Load -----------------------------------------------
    private void LoadData()
    {
        // activeOnly: false — admins manage the whole catalogue, including
        // languages they have retired from new courses.
        _all = _service.GetLanguages(activeOnly: false)
            .Select(l => new LanguageRow(
                l,
                _service.CountLevelsInLanguage(l.LanguageId),
                _service.CountCoursesUsingLanguage(l.LanguageId)))
            .ToList();

        emptyState.Visibility = _all.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        BindPage();
    }

    private void BindPage() => dgLanguages.ItemsSource = pager.Slice(_all);

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    // ---- 2. Add / edit / delete --------------------------------
    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        if (new LanguageDialog { Owner = this }.ShowDialog() == true) LoadData();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e) => EditSelected();

    private void DgLanguages_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgLanguages.SelectedItem is LanguageRow) OpenLevels();
    }

    private void EditSelected()
    {
        if (Selected() is not LanguageRow row) return;
        if (new LanguageDialog(row.Language) { Owner = this }.ShowDialog() == true) LoadData();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (Selected() is not LanguageRow row) return;

        var confirm = MessageBox.Show($"Delete language \"{row.Name}\"?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _service.DeleteLanguage(row.Language.LanguageId);
            LoadData();
        }
        catch (InvalidOperationException ex)
        {
            // Still referenced — the service message explains by what.
            MessageBox.Show(ex.Message, "Cannot delete", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete the language:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---- 3. Levels ---------------------------------------------
    private void BtnLevels_Click(object sender, RoutedEventArgs e) => OpenLevels();

    private void OpenLevels()
    {
        if (Selected() is not LanguageRow row) return;

        new LevelManagementWindow(row.Language.LanguageId) { Owner = this }.ShowDialog();
        LoadData(); // level counts may have changed
    }

    private LanguageRow? Selected()
    {
        if (dgLanguages.SelectedItem is LanguageRow row) return row;
        MessageBox.Show("Please select a language.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        return null;
    }

    // ---- 4. LanguageRow ----------------------------------------
    private sealed record LanguageRow(Language Language, int LevelCount, int CourseCount)
    {
        public string Name => Language.Name;
        public bool IsActive => Language.IsActive;
    }
}
