using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  CatalogueWindow — languages and their levels, on one screen.
//  CONTENTS:
//    1. Languages (master)  — load, add / edit / delete
//    2. Levels (detail)     — load, add / edit / delete
//    3. View models         — LanguageRow / LevelRow
//
//  Master/detail rather than two separate screens: a level only means anything
//  inside a language ("N5" is Japanese-only), so the language is always visible
//  while its levels are edited — which is also what stops a level being filed
//  under the wrong one.
//
//  The Levels/Courses counts on the left are not decoration: they are exactly
//  what blocks deleting a language, so the reason is visible before the click.
// ============================================================
public partial class CatalogueWindow : Window
{
    private readonly ICatalogueService _service = new CatalogueService();
    private List<LevelRow> _levels = new();

    public CatalogueWindow()
    {
        InitializeComponent();
        LoadLanguages();
    }

    // ---- 1. Languages ------------------------------------------
    private void LoadLanguages()
    {
        // Remember the selection across a reload so editing does not bounce the
        // user back to the first language.
        var selectedId = SelectedLanguage()?.LanguageId;

        var rows = _service.GetLanguages()
            .Select(l => new LanguageRow(
                l,
                _service.CountLevelsInLanguage(l.LanguageId),
                _service.CountCoursesUsingLanguage(l.LanguageId)))
            .ToList();

        lstLanguages.ItemsSource = rows;

        if (rows.Count == 0)
        {
            LoadLevels();
            return;
        }

        lstLanguages.SelectedItem = rows.FirstOrDefault(r => r.LanguageId == selectedId) ?? rows[0];
    }

    private void LstLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadLevels();

    private void LstLanguages_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (lstLanguages.SelectedItem is LanguageRow) EditLanguage();
    }

    /// <summary>
    /// The language on the left, or null. Says nothing when there is none — used
    /// where an empty selection is a normal state, such as reloading the list.
    ///
    /// Naming rule in this file: <c>Selected*</c> is silent, <c>Require*</c> tells
    /// the user to pick something first. Reach for Require in a button handler.
    /// </summary>
    private Language? SelectedLanguage() => (lstLanguages?.SelectedItem as LanguageRow)?.Language;

    private void BtnAddLanguage_Click(object sender, RoutedEventArgs e)
    {
        if (new LanguageDialog { Owner = this }.ShowDialog() == true) LoadLanguages();
    }

    private void BtnEditLanguage_Click(object sender, RoutedEventArgs e) => EditLanguage();

    private void EditLanguage()
    {
        if (RequireLanguage() is not Language language) return;
        if (new LanguageDialog(language) { Owner = this }.ShowDialog() == true) LoadLanguages();
    }

    private void BtnDeleteLanguage_Click(object sender, RoutedEventArgs e)
    {
        if (RequireLanguage() is not Language language) return;

        var confirm = MessageBox.Show($"Delete language \"{language.Name}\"?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _service.DeleteLanguage(language.LanguageId);
            LoadLanguages();
        }
        catch (InvalidOperationException ex)
        {
            // Still referenced by levels or courses — the service names which.
            MessageBox.Show(ex.Message, "Cannot delete", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete the language:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Language? RequireLanguage()
    {
        var language = SelectedLanguage();
        if (language == null)
            MessageBox.Show("Please select a language.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        return language;
    }

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

        _levels = _service.GetLevels(language.LanguageId)
            .Select(l => new LevelRow(l, _service.CountCoursesUsingLevel(l.LevelId)))
            .ToList();

        tbLanguageHint.Text = _levels.Count == 0
            ? "No levels defined yet."
            : $"{_levels.Count} level(s), listed in the order they were added.";

        dgLevels.ItemsSource = _levels;
        emptyState.Visibility = _levels.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnAddLevel_Click(object sender, RoutedEventArgs e)
    {
        if (RequireLanguage() is not Language language) return;

        if (new LevelDialog(language) { Owner = this }.ShowDialog() == true)
        {
            LoadLevels();
            LoadLanguages(); // the level count on the left moved
        }
    }

    private void BtnEditLevel_Click(object sender, RoutedEventArgs e) => EditLevel();

    private void DgLevels_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgLevels.SelectedItem is LevelRow) EditLevel();
    }

    private void EditLevel()
    {
        if (RequireLevel() is not LevelRow row) return;
        if (SelectedLanguage() is not Language language) return;

        if (new LevelDialog(language, row.Level) { Owner = this }.ShowDialog() == true) LoadLevels();
    }

    private void BtnDeleteLevel_Click(object sender, RoutedEventArgs e)
    {
        if (RequireLevel() is not LevelRow row) return;

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

    private LevelRow? RequireLevel()
    {
        if (dgLevels.SelectedItem is LevelRow row) return row;
        MessageBox.Show("Please select a level.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        return null;
    }

    // ---- 3. View models ----------------------------------------
    private sealed record LanguageRow(Language Language, int LevelCount, int CourseCount)
    {
        public int LanguageId => Language.LanguageId;
        public string Name => Language.Name;

        /// <summary>"6 level(s) · 2 course(s)" — also the reason Delete would be refused.</summary>
        public string Summary
        {
            get
            {
                var levels = LevelCount == 0 ? "no levels" : $"{LevelCount} level(s)";
                var courses = CourseCount == 0 ? "no courses" : $"{CourseCount} course(s)";
                return $"{levels} · {courses}";
            }
        }
    }

    private sealed record LevelRow(Level Level, int CourseCount)
    {
        public int LevelId => Level.LevelId;
        public string Name => Level.Name;
    }
}
