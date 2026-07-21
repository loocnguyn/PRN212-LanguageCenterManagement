using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// LanguageDialog — add or rename a language. Uniqueness is enforced by
// CatalogueService, so this dialog only relays its message.
public partial class LanguageDialog : Window
{
    private readonly ICatalogueService _service = new CatalogueService();
    private readonly Language? _editing;

    public LanguageDialog()
    {
        InitializeComponent();
    }

    public LanguageDialog(Language language) : this()
    {
        _editing = language;
        Title = "Edit Language";
        txtName.Text = language.Name;
        chkActive.IsChecked = language.IsActive;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_editing == null)
            {
                _service.SaveLanguage(new Language
                {
                    Name = txtName.Text,
                    IsActive = chkActive.IsChecked ?? true
                });
            }
            else
            {
                _editing.Name = txtName.Text;
                _editing.IsActive = chkActive.IsChecked ?? true;
                _service.UpdateLanguage(_editing);
            }

            DialogResult = true;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Cannot save", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save the language:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
