using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// LevelDialog — add or edit one level of a given language. The language is
// fixed by the screen that opened this dialog, which is what stops "N5" being
// filed under English.
public partial class LevelDialog : Window
{
    private readonly ICatalogueService _service = new CatalogueService();
    private readonly Language _language;
    private readonly Level? _editing;

    public LevelDialog(Language language)
    {
        InitializeComponent();
        _language = language;
        tbLanguage.Text = language.Name;

        // Suggest the next rung so the common case needs no typing.
        txtSortOrder.Text = (_service.GetLevels(language.LanguageId, activeOnly: false)
            .Select(l => l.SortOrder)
            .DefaultIfEmpty(0)
            .Max() + 1).ToString();
    }

    public LevelDialog(Language language, Level level) : this(language)
    {
        _editing = level;
        Title = "Edit Level";
        txtName.Text = level.Name;
        txtSortOrder.Text = level.SortOrder.ToString();
        chkActive.IsChecked = level.IsActive;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtSortOrder.Text.Trim(), out var sortOrder) || sortOrder < 0)
        {
            MessageBox.Show("Order must be a non-negative whole number.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_editing == null)
            {
                _service.SaveLevel(new Level
                {
                    LanguageId = _language.LanguageId,
                    Name = txtName.Text,
                    SortOrder = sortOrder,
                    IsActive = chkActive.IsChecked ?? true
                });
            }
            else
            {
                _editing.Name = txtName.Text;
                _editing.SortOrder = sortOrder;
                _editing.IsActive = chkActive.IsChecked ?? true;
                _service.UpdateLevel(_editing);
            }

            DialogResult = true;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Cannot save", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save the level:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
