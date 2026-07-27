using System.Windows;
using BusinessObjects;
using Microsoft.Win32;
using Services;

namespace WpfApp;

// ============================================================
//  StudentImportWindow — create student accounts from a class list.
//  CONTENTS:
//    1. Browse   — pick the file, read and check it
//    2. Preview  — every row, with the reason a bad one will be skipped
//    3. Import   — create accounts for the valid rows only
//
//  Nothing is written until the user has seen the preview. Invalid rows are
//  skipped rather than guessed at: an import that silently invents an email or
//  merges two people is worse than one that says which line to go and fix.
// ============================================================
public partial class StudentImportWindow : Window
{
    private readonly IStudentImportService _importService = new StudentImportService();
    private List<StudentImportRow> _rows = new();

    public StudentImportWindow()
    {
        InitializeComponent();
    }

    // ---- 1. Browse ---------------------------------------------
    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose a student list",
            Filter = "Student list (*.csv;*.xlsx)|*.csv;*.xlsx|CSV file (*.csv)|*.csv|Excel file (*.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            _rows = _importService.ReadAndValidate(dialog.FileName);
            tbFile.Text = dialog.FileName;
            ShowPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not read the file:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---- 2. Preview --------------------------------------------
    private void ShowPreview()
    {
        dgRows.ItemsSource = null;
        dgRows.ItemsSource = _rows;

        var valid = _rows.Count(r => r.IsValid);
        statTotal.Text = _rows.Count.ToString();
        statValid.Text = valid.ToString();
        statSkipped.Text = (_rows.Count - valid).ToString();

        summaryPanel.Visibility = Visibility.Visible;
        emptyState.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (_rows.Count == 0) emptyState.Text = "That file has no data rows.";

        btnImport.IsEnabled = valid > 0;
    }

    // ---- 3. Import ---------------------------------------------
    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        var password = txtPassword.Text.Trim();
        if (!ValidationHelper.IsValidPassword(password))
        {
            MessageBox.Show(
                $"The starting password must be at least {ValidationHelper.MinPasswordLength} characters.",
                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var valid = _rows.Count(r => r.IsValid);
        var skipped = _rows.Count - valid;

        var message = $"Create {valid} student account(s)?";
        if (skipped > 0) message += $"\n{skipped} row(s) will be skipped — see the Status column.";

        if (MessageBox.Show(message, "Confirm import",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        try
        {
            var created = _importService.Import(_rows, password);

            MessageBox.Show(
                $"{created} student account(s) created.\n" +
                $"They sign in with their email and the starting password, then must choose a new one.",
                "Import finished", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
