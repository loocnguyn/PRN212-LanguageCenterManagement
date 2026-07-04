using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Services;

namespace WpfApp;

public partial class ClassRosterWindow : Window
{
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IClassService _classService = new ClassService();

    public ClassRosterWindow()
    {
        InitializeComponent();
    }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(txtClassId.Text.Trim(), out var classId))
            {
                MessageBox.Show("Please enter a valid Class ID.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cls = _classService.GetById(classId);
            if (cls == null)
            {
                MessageBox.Show($"Class {classId} not found.", "Not Found",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var enrollments = _enrollmentService.GetByClassId(classId);
            if (!enrollments.Any())
            {
                MessageBox.Show($"No active enrollments for class '{cls.Name}'.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                dgRoster.ItemsSource = null;
                return;
            }

            var students = enrollments
                .Where(en => en.Student != null)
                .Select(en => new RosterRow
                {
                    StudentId = en.Student!.StudentId,
                    FullName = en.Student.FullName,
                    Gender = en.Student.Gender ?? "",
                    Phone = en.Student.Phone ?? "",
                    Email = en.Student.Email ?? ""
                })
                .ToList();

            dgRoster.ItemsSource = students;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading roster: {ex.Message}", "Error",
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
