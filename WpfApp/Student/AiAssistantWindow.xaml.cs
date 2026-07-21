using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  AiAssistantWindow — chat UI over AiAssistantService (Gemini).
//  CONTENTS:
//    1. Construction & Init    — resolve student, greet
//    2. BuildContext           — student data fed to the model as context
//    3. Chat flow              — key handling, SendAsync, append, busy state
// ============================================================
public partial class AiAssistantWindow : Window
{
    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IGradeService _gradeService = new GradeService();
    private readonly IInvoiceService _invoiceService = new InvoiceService();
    private readonly IAiAssistantService _aiService = new AiAssistantService();

    private string _studentContext = "";

    public AiAssistantWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        Loaded += (_, _) => Init();
    }

    private void Init()
    {
        try
        {
            var student = _studentService.GetAll().FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null)
            {
                tbStudentName.Text = "(no student profile)";
                AppendLine("System", "No student profile is linked to this account, so I can't load your data.");
                DisableInput();
                return;
            }

            tbStudentName.Text = $"— {student.FullName}";
            _studentContext = BuildContext(student);

            if (!_aiService.IsConfigured)
            {
                AppendLine("System",
                    "The AI assistant is not configured yet. A Google Gemini API key must be set in the \"Gemini\" " +
                    "section of appsettings.json before you can chat.");
                DisableInput();
                return;
            }

            AppendLine("Assistant", "Hi! Ask me anything about your schedule, grades, or tuition. 👋");
        }
        catch (Exception ex)
        {
            AppendLine("System", $"Error preparing assistant: {ex.Message}");
            DisableInput();
        }
    }

    /// <summary>Snapshot of the student's own data, sent to the model as context so answers
    /// stay grounded in this student's records.</summary>
    private string BuildContext(Student student)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Student name: {student.FullName}");
        sb.AppendLine($"Email: {student.Email}");
        sb.AppendLine();

        var enrollments = _enrollmentService.GetByStudentId(student.StudentId);
        sb.AppendLine("Enrolled classes:");
        if (enrollments.Any())
        {
            foreach (var e in enrollments)
            {
                var courseName = e.Class?.Course?.Name ?? "Unknown course";
                sb.AppendLine($"- {e.Class?.Name} ({courseName}) — status {e.Status}");
            }
        }
        else
        {
            sb.AppendLine("- (none)");
        }
        sb.AppendLine();

        var grades = _gradeService.GetByStudentId(student.StudentId);
        sb.AppendLine("Grades:");
        if (grades.Any())
        {
            foreach (var g in grades.OrderBy(g => g.Enrollment.Class.Name).ThenBy(g => g.Component.Name))
            {
                sb.AppendLine(
                    $"- {g.Enrollment.Class.Name} / {g.Component.Name}: {g.Score}/{g.MaxScore} " +
                    $"(weight {g.Component.WeightPercent}%)");
            }
        }
        else
        {
            sb.AppendLine("- (no grades yet)");
        }
        sb.AppendLine();

        var invoices = _invoiceService.GetAll().Where(i => i.StudentId == student.StudentId).ToList();
        sb.AppendLine("Invoices:");
        if (invoices.Any())
        {
            foreach (var inv in invoices.OrderBy(i => i.CreatedAt))
            {
                var paid = _invoiceService.GetPaidAmount(inv.InvoiceId);
                var remaining = Math.Max(0, inv.Amount - paid);
                var due = inv.DueDate?.ToString("dd/MM/yyyy") ?? "n/a";
                sb.AppendLine(
                    $"- Invoice #{inv.InvoiceId}: amount {inv.Amount:N0}, paid {paid:N0}, " +
                    $"remaining {remaining:N0}, status {inv.Status}, due {due}");
            }
        }
        else
        {
            sb.AppendLine("- (no invoices)");
        }

        return sb.ToString();
    }

    private async void BtnSend_Click(object sender, RoutedEventArgs e) => await SendAsync();

    private async void TxtQuestion_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await SendAsync();
        }
    }

    private async Task SendAsync()
    {
        var question = txtQuestion.Text.Trim();
        if (string.IsNullOrEmpty(question)) return;

        AppendLine("You", question);
        txtQuestion.Clear();
        SetBusy(true);

        try
        {
            var result = await _aiService.AskAsync(question, _studentContext);
            if (result.Success)
                AppendLine("Assistant", result.Answer ?? "");
            else
                AppendLine("System", result.Error ?? "Unknown error.");
        }
        catch (Exception ex)
        {
            AppendLine("System", ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void AppendLine(string who, string text)
    {
        if (tbConversation.Text.Length > 0)
            tbConversation.Text += "\n\n";
        tbConversation.Text += $"{who}: {text}";
        conversationScroll.ScrollToEnd();
    }

    private void SetBusy(bool busy)
    {
        btnSend.IsEnabled = !busy;
        txtQuestion.IsEnabled = !busy;
        btnSend.Content = busy ? "…" : "Send";
    }

    private void DisableInput()
    {
        txtQuestion.IsEnabled = false;
        btnSend.IsEnabled = false;
    }
}
