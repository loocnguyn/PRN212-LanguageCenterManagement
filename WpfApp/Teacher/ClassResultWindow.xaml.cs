using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using ClosedXML.Excel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  ClassResultWindow — final results/summary for a teacher's class.
//  CONTENTS:
//    1. Construction & LoadTeacherClasses — the teacher's classes
//    2. Class select                      — compute & show results
//    3. Reset                             — clear the view
// ============================================================
public partial class ClassResultWindow : Window
{
    private readonly User _currentUser;
    private readonly IGradeService _gradeService = new GradeService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IClassService _classService = new ClassService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly ISemesterService _semesterService = new SemesterService();

    private Teacher? _teacher;
    private List<Class> _teacherClasses = new();
    private List<ExpandoObject> _rows = new();
    private List<ClassGradeComponent> _components = new();
    private string _activeSemesterName = "N/A";

    public ClassResultWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => LoadTeacherClasses();
    }

    private void LoadTeacherClasses()
    {
        try
        {
            _teacher = _teacherService.GetByUserId(_currentUser.Id);
            if (_teacher == null)
            {
                tbTeacherInfo.Text = "No teacher profile linked to this account.";
                return;
            }

            tbTeacherInfo.Text = $"Teacher: {_teacher.FullName}";

            var semester = _semesterService.GetActive();
            if (semester == null)
            {
                tbTeacherInfo.Text += " — No active semester";
                return;
            }
            _activeSemesterName = semester.Name;

            _teacherClasses = _classService.GetBySemesterId(semester.SemesterId)
                .Where(c => c.ClassTeachers.Any(ct => ct.TeacherId == _teacher.TeacherId))
                .ToList();

            cboClass.ItemsSource = _teacherClasses;
            cboClass.SelectedIndex = -1;

            if (!_teacherClasses.Any())
                tbTeacherInfo.Text += $" — No classes in {semester.Name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading teacher classes: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CboClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboClass.SelectedItem is not Class cls) return;

        try
        {
            // The class's OWN frozen structure — not the course template, which may
            // have been edited since this class opened.
            _components = _classService.GetGradeComponents(cls.ClassId);
            if (_components.Count == 0)
            {
                MessageBox.Show($"No grading structure recorded for class '{cls.Name}'.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var enrollments = _enrollmentService.GetByClassId(cls.ClassId);
            if (enrollments.Count == 0)
            {
                MessageBox.Show($"No active enrollments found for class '{cls.Name}'.", "No Data",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                SetRows(new List<ExpandoObject>());
                return;
            }

            // BATCH-LOAD grades for all enrollments in one query (fixes N+1)
            var enrollmentIds = enrollments.Select(e => e.EnrollmentId).ToList();
            var allGrades = _gradeService.GetByEnrollmentIds(enrollmentIds);
            var gradesByEnrollment = allGrades
                .GroupBy(g => g.EnrollmentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build rows
            var rows = new List<ExpandoObject>();
            foreach (var enrollment in enrollments)
            {
                dynamic row = new ExpandoObject();
                var dict = (IDictionary<string, object?>)row;

                row.StudentId = enrollment.Student?.StudentId ?? 0;
                row.StudentName = enrollment.Student?.FullName ?? "";

                var enrollmentGrades = gradesByEnrollment.TryGetValue(enrollment.EnrollmentId, out var gList)
                    ? gList
                    : new List<Grade>();

                decimal finalScore = 0m;
                foreach (var comp in _components)
                {
                    var grade = enrollmentGrades.FirstOrDefault(g => g.ComponentId == comp.ComponentId);
                    if (grade != null && grade.MaxScore > 0)
                    {
                        dict[comp.Name] = grade.Score;
                        finalScore += (grade.Score / grade.MaxScore) * (comp.WeightPercent / 100m);
                    }
                    else
                    {
                        dict[comp.Name] = null;
                    }
                }

                row.FinalScore = Math.Round(finalScore, 4);
                rows.Add(row);
            }

            // Build dynamic columns
            dgResults.Columns.Clear();

            dgResults.Columns.Add(new DataGridTextColumn
            {
                Header = "Student ID",
                Binding = new Binding("StudentId"),
                Width = 100
            });

            dgResults.Columns.Add(new DataGridTextColumn
            {
                Header = "Full Name",
                Binding = new Binding("StudentName"),
                Width = 180
            });

            foreach (var comp in _components)
            {
                dgResults.Columns.Add(new DataGridTextColumn
                {
                    Header = $"{comp.Name}\n({comp.WeightPercent}%)",
                    Binding = new Binding(comp.Name)
                    {
                        TargetNullValue = "-",
                        StringFormat = "N2"
                    },
                    Width = 100
                });
            }

            dgResults.Columns.Add(new DataGridTextColumn
            {
                Header = "Final Score\n(w/weight)",
                Binding = new Binding("FinalScore")
                {
                    StringFormat = "P2"
                },
                Width = 110
            });

            SetRows(rows);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading results: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        cboClass.SelectedIndex = -1;
        dgResults.Columns.Clear();
        SetRows(new List<ExpandoObject>());
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (cboClass.SelectedItem is not Class cls)
        {
            MessageBox.Show("Please select a class first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!_rows.Any())
        {
            MessageBox.Show("There are no grades to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Find project root folder by traversing upwards to find LanguageCenter.slnx
            string current = AppDomain.CurrentDomain.BaseDirectory;
            string root = current;
            while (!string.IsNullOrEmpty(root) && !File.Exists(Path.Combine(root, "LanguageCenter.slnx")))
            {
                var parent = Directory.GetParent(root);
                if (parent == null) break;
                root = parent.FullName;
            }

            if (string.IsNullOrEmpty(root) || !File.Exists(Path.Combine(root, "LanguageCenter.slnx")))
            {
                root = Directory.GetCurrentDirectory();
            }

            string exportFolder = Path.Combine(root, "export_grade");
            if (!Directory.Exists(exportFolder))
            {
                Directory.CreateDirectory(exportFolder);
            }

            string safeClassName = string.Join("_", cls.Name.Split(Path.GetInvalidFileNameChars()));
            string fileName = $"{safeClassName}_Grades_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(exportFolder, fileName);

            // Reuse cached components and metadata
            var components = _components;
            string teacherName = _teacher?.FullName ?? "N/A";
            string className = cls.Name;
            string courseName = $"{cls.SnapCourseCode} — {cls.SnapCourseName}";
            string languageAndLevel = $"{cls.SnapLanguage} {cls.SnapLevel}";
            string semesterName = _activeSemesterName;
            string exportDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // Perform Excel export
            ExportGradesToExcel(
                filePath,
                teacherName,
                className,
                courseName,
                languageAndLevel,
                semesterName,
                exportDate,
                components,
                _rows);

            MessageBox.Show($"Grades exported successfully to:\n{filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting grades: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportGradesToExcel(
        string filePath,
        string teacherName,
        string className,
        string courseName,
        string languageAndLevel,
        string semesterName,
        string exportDate,
        List<ClassGradeComponent> components,
        List<ExpandoObject> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Grades");

        // Title Block
        int lastColIndex = 3 + components.Count; // StudentID(1) + Name(2) + Components(N) + FinalScore(1)
        string lastColLetter = GetColumnLetter(lastColIndex);

        var titleRange = sheet.Range($"A1:{lastColLetter}1");
        titleRange.Merge();
        titleRange.Value = "CLASS GRADES REPORT";
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 14;
        titleRange.Style.Font.FontColor = XLColor.White;
        titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32"); // Dark green
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(1).Height = 35;

        // Metadata Info Block
        // Left Column (Col A & B)
        sheet.Cell("A3").Value = "Class:";
        sheet.Cell("A3").Style.Font.Bold = true;
        sheet.Cell("B3").Value = className;

        sheet.Cell("A4").Value = "Teacher:";
        sheet.Cell("A4").Style.Font.Bold = true;
        sheet.Cell("B4").Value = teacherName;

        sheet.Cell("A5").Value = "Export Date:";
        sheet.Cell("A5").Style.Font.Bold = true;
        sheet.Cell("B5").Value = exportDate;

        // Right Column (Col D & E)
        sheet.Cell("D3").Value = "Course:";
        sheet.Cell("D3").Style.Font.Bold = true;
        sheet.Cell("E3").Value = $"{courseName} ({languageAndLevel})";

        sheet.Cell("D4").Value = "Semester:";
        sheet.Cell("D4").Style.Font.Bold = true;
        sheet.Cell("E4").Value = semesterName;

        sheet.Cell("D5").Value = "Total Students:";
        sheet.Cell("D5").Style.Font.Bold = true;
        sheet.Cell("E5").Value = rows.Count;

        var infoRange = sheet.Range($"A3:{lastColLetter}5");
        infoRange.Style.Font.FontSize = 10;

        // Table Headers (Row 7)
        int headerRowIndex = 7;
        var headers = new List<string> { "Student ID", "Full Name" };
        foreach (var comp in components)
        {
            headers.Add($"{comp.Name} ({comp.WeightPercent}%)");
        }
        headers.Add("Final Score");

        for (int col = 0; col < headers.Count; col++)
        {
            var cell = sheet.Cell(headerRowIndex, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B5E20"); // Darker green
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        sheet.Row(headerRowIndex).Height = 26;

        // Data Rows (Row 8 onwards)
        int startRowIndex = 8;
        int currentRowIndex = startRowIndex;

        foreach (var r in rows)
        {
            var dict = (IDictionary<string, object?>)r;

            // Student ID (Col 1)
            var idCell = sheet.Cell(currentRowIndex, 1);
            idCell.Value = dict.TryGetValue("StudentId", out var studentId) ? XLCellValue.FromObject(studentId) : Blank.Value;
            idCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Student Name (Col 2)
            var nameCell = sheet.Cell(currentRowIndex, 2);
            nameCell.Value = dict.TryGetValue("StudentName", out var studentName) ? XLCellValue.FromObject(studentName) : Blank.Value;
            nameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            // Components (Col 3 to N)
            for (int col = 0; col < components.Count; col++)
            {
                var comp = components[col];
                var scoreCell = sheet.Cell(currentRowIndex, 3 + col);
                if (dict.TryGetValue(comp.Name, out var score) && score != null)
                {
                    scoreCell.Value = Convert.ToDouble(score);
                    scoreCell.Style.NumberFormat.Format = "0.00";
                }
                else
                {
                    scoreCell.Value = Blank.Value;
                }
                scoreCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            // Final Score (Col lastColIndex)
            var finalCell = sheet.Cell(currentRowIndex, lastColIndex);
            if (dict.TryGetValue("FinalScore", out var finalScoreObj) && finalScoreObj != null)
            {
                finalCell.Value = Convert.ToDouble(finalScoreObj);
                finalCell.Style.NumberFormat.Format = "0.00%";
            }
            else
            {
                finalCell.Value = Blank.Value;
            }
            finalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Zebra striping
            if (currentRowIndex % 2 == 0)
            {
                sheet.Range(currentRowIndex, 1, currentRowIndex, lastColIndex).Style.Fill.BackgroundColor = XLColor.FromHtml("#F4F8F4");
            }

            currentRowIndex++;
        }

        // Borders for table data
        var tableRange = sheet.Range(headerRowIndex, 1, currentRowIndex - 1, lastColIndex);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.OutsideBorderColor = XLColor.LightGray;
        tableRange.Style.Border.InsideBorderColor = XLColor.LightGray;

        // Summary Row
        int summaryRowIndex = currentRowIndex + 1;
        sheet.Cell(summaryRowIndex, 2).Value = "Class Average:";
        sheet.Cell(summaryRowIndex, 2).Style.Font.Bold = true;
        sheet.Cell(summaryRowIndex, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        for (int col = 0; col < components.Count; col++)
        {
            var avgCell = sheet.Cell(summaryRowIndex, 3 + col);
            string colLetter = GetColumnLetter(3 + col);
            avgCell.FormulaA1 = $"=AVERAGE({colLetter}{startRowIndex}:{colLetter}{currentRowIndex - 1})";
            avgCell.Style.Font.Bold = true;
            avgCell.Style.NumberFormat.Format = "0.00";
            avgCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        var finalAvgCell = sheet.Cell(summaryRowIndex, lastColIndex);
        string finalColLetter = GetColumnLetter(lastColIndex);
        finalAvgCell.FormulaA1 = $"=AVERAGE({finalColLetter}{startRowIndex}:{finalColLetter}{currentRowIndex - 1})";
        finalAvgCell.Style.Font.Bold = true;
        finalAvgCell.Style.NumberFormat.Format = "0.00%";
        finalAvgCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var summaryRange = sheet.Range(summaryRowIndex, 1, summaryRowIndex, lastColIndex);
        summaryRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        summaryRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
        summaryRange.Style.Border.TopBorderColor = XLColor.Black;
        summaryRange.Style.Border.BottomBorderColor = XLColor.Black;

        // Auto-fit columns
        sheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    private static string GetColumnLetter(int colNum)
    {
        int temp;
        string colLetter = string.Empty;
        while (colNum > 0)
        {
            temp = (colNum - 1) % 26;
            colLetter = (char)(65 + temp) + colLetter;
            colNum = (colNum - temp - 1) / 26;
        }
        return colLetter;
    }

    /// <summary>Swap the result set the grid is paging over and jump back to page 1.</summary>
    private void SetRows(List<ExpandoObject> rows)
    {
        _rows = rows;
        pager.Reset();
        BindPage();
    }

    private void BindPage() => dgResults.ItemsSource = pager.Slice(_rows);

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();
}