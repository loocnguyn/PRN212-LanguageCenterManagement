using System;
using System.Linq;
using System.Windows;
using Services;

namespace WpfApp;

public partial class ClassRosterWindow : Window
{
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();

    public ClassRosterWindow()
    {
        InitializeComponent();
    }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(txtClassId.Text.Trim(), out int classId))
            {
                MessageBox.Show("Please enter a valid Class ID.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var enrollments = _enrollmentService.GetByClassId(classId);

            var rows = enrollments
                .Where(en => en.Status == "ACTIVE")
                .Select(en => new RosterRow
                {
                    StudentId = en.Student?.StudentId ?? 0,
                    FullName = en.Student?.FullName ?? "",
                    Gender = en.Student?.Gender ?? "",
                    Phone = en.Student?.Phone ?? "",
                    Email = en.Student?.Email ?? ""
                }).ToList();

            dgRoster.ItemsSource = rows;

            if (!rows.Any())
            {
                MessageBox.Show("No active enrollments found for this class.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        txtClassId.Text = "";
        dgRoster.ItemsSource = null;
    }
}

public class RosterRow
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
}
