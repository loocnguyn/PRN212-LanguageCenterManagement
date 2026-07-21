using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  LevelManagementWindow — master/detail over the catalogue.
//  CONTENTS:
//    1. Load languages   — master list on the left
//    2. Load levels      — detail for the selected language
//    3. Add / edit / delete
//    4. Reorder          — move a level up/down its language's ladder
//
//  Levels belong to one language, so they are always managed in that context —
//  there is no flat "all levels" list to accidentally add "N5" under English.
// ============================================================
public partial class LevelManagementWindow : Window
{
    private readonly ICatalogueService _service = new CatalogueService();

    private readonly int? _preselectLanguageId;
    private List<LevelRow> _levels = new();

    public LevelManagementWindow(int? preselectLanguageId = null)
    {
        InitializeComponent();
        _preselectLanguageId = preselectLanguageId;
        LoadLanguages();
    }

    // ---- 1. Languages ------------------------------------------
    private void LoadLanguages()
    {
        var selectedId = SelectedLanguage()?.LanguageId ?? _preselectLanguageId;

        var rows = _service.GetLanguages(activeOnly: false)
            .Select(l => new LanguageRow(l, _service.CountLevelsInLanguage(l.LanguageId)))
            .ToList();

        lstLanguages.ItemsSource = rows;

        if (rows.Count == 0) return;

        lstLanguages.SelectedItem = rows.FirstOrDefault(r => r.LanguageId == selectedId) ?? rows[0];
    }

    private void LstLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadLevels();

    private Language? SelectedLanguage() => (lstLanguages?.SelectedItem as LanguageRow)?.Language;

    // ---- 2. Levels ---------------------------------------------
    private void LoadLevels()
    {
        if (dgLevels == null) return;

        var language = SelectedLanguage();
        if (language == null)
        {
            _levels = new List<LevelRow>();
            dgLevels.ItemsSource = null;
            tbLanguageName.Text = "Select a language";
            tbLanguageHint.Text = "Its levels will appear below.";
            emptyState.Visibility = Visibility.Collapsed;
            return;
        }

        tbLanguageName.Text = language.Name;

        // activeOnly: false — this screen manages the whole ladder, retired rungs included.
        _levels = _service.GetLevels(language.LanguageId, activeOnly: false)
            .Select(l => new LevelRow(l, _service.CountCoursesUsingLevel(l.LevelId)))
            .ToList();

        tbLanguageHint.Text = _levels.Count == 0
            ? "No levels defined yet."
            : $"{_levels.Count} level(s), shown in teaching order.";

        dgLevels.ItemsSource = _levels;
        emptyState.Visibility = _levels.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ---- 3. Add / edit / delete --------------------------------
    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedLanguage() is not Language language)
        {
            MessageBox.Show("Select a language first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (new LevelDialog(language) { Owner = this }.ShowDialog() == true)
        {
            LoadLevels();
            LoadLanguages(); // level count on the master list
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e) => EditSelected();

    private void DgLevels_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgLevels.SelectedItem is LevelRow) EditSelected();
    }

    private void EditSelected()
    {
        if (SelectedLevel() is not LevelRow row) return;
        if (SelectedLanguage() is not Language language) return;

        if (new LevelDialog(language, row.Level) { Owner = this }.ShowDialog() == true) LoadLevels();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedLevel() is not LevelRow row) return;

        var confirm = MessageBox.Show($"Delete level \"{row.Name}\"?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _service.DeleteLevel(row.Level.LevelId);
            LoadLevels();
            LoadLanguages();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Cannot delete", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete the level:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---- 4. Reorder --------------------------------------------
    private void BtnMoveUp_Click(object sender, RoutedEventArgs e) => Move(-1);
    private void BtnMoveDown_Click(object sender, RoutedEventArgs e) => Move(+1);

    /// <summary>
    /// Swaps this level's sort order with its neighbour, so A1/A2/B1 keep reading in
    /// teaching order in every course dropdown.
    /// </summary>
    private void Move(int direction)
    {
        if (SelectedLevel() is not LevelRow row) return;

        var index = _levels.FindIndex(l => l.Level.LevelId == row.Level.LevelId);
        var targetIndex = index + direction;
        if (index < 0 || targetIndex < 0 || targetIndex >= _levels.Count) return;

        var a = _levels[index].Level;
        var b = _levels[targetIndex].Level;

        (a.SortOrder, b.SortOrder) = (b.SortOrder, a.SortOrder);

        try
        {
            _service.UpdateLevel(a);
            _service.UpdateLevel(b);
            LoadLevels();

            // Keep the moved row selected so repeated clicks keep walking it along.
            var moved = dgLevels.ItemsSource?.Cast<LevelRow>()
                .FirstOrDefault(l => l.Level.LevelId == a.LevelId);
            if (moved != null) dgLevels.SelectedItem = moved;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not reorder the levels:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private LevelRow? SelectedLevel()
    {
        if (dgLevels.SelectedItem is LevelRow row) return row;
        MessageBox.Show("Please select a level.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        return null;
    }

    // ---- View models -------------------------------------------
    private sealed record LanguageRow(Language Language, int LevelCount)
    {
        public int LanguageId => Language.LanguageId;
        public string Name => Language.Name;

        public string Summary =>
            (LevelCount == 0 ? "no levels" : $"{LevelCount} level(s)")
            + (Language.IsActive ? "" : " · inactive");
    }

    private sealed record LevelRow(Level Level, int CourseCount)
    {
        public string Name => Level.Name;
        public int SortOrder => Level.SortOrder;
        public bool IsActive => Level.IsActive;
    }
}
