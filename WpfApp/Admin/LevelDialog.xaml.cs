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
    }

    public LevelDialog(Language language, Level level) : this(language)
    {
        _editing = level;
        Title = "Edit Level";
        txtName.Text = level.Name;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_editing == null)
            {
                _service.SaveLevel(new Level
                {
                    LanguageId = _language.LanguageId,
                    Name = txtName.Text
                });
            }
            else
            {
                _editing.Name = txtName.Text;
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
