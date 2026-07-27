using System.Text;
using System.Windows;
using System.Windows.Input;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  AiAssistantWindow — chat UI over AiAssistantService (Gemini).
//  CONTENTS:
//    1. Construction & Init    — resolve the student, greet
//    2. BuildContext           — everything about THIS student, as plain text
//    3. Chat flow              — send, remember the conversation, busy state
//
//  Two things the model needs and cannot get by itself:
//    · the student's data — rebuilt fresh before every question, so an answer
//      never quotes a balance that was paid off ten minutes ago;
//    · the conversation so far — sent along, so "what about the other class?"
//      still makes sense.
//  The service only ever sees the signed-in student's own records.
// ============================================================
public partial class AiAssistantWindow : Window
{
    private readonly User _currentUser;
    private readonly IStudentService _studentService = new StudentService();
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly IGradeService _gradeService = new GradeService();
    private readonly IInvoiceService _invoiceService = new InvoiceService();
    private readonly ISessionService _sessionService = new SessionService();
    private readonly IAttendanceService _attendanceService = new AttendanceService();
    private readonly IWalletService _walletService = new WalletService();
    private readonly IAiAssistantService _aiService = new AiAssistantService();

    private readonly List<ChatTurn> _history = new();
    private Student? _student;

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
            _student = _studentService.GetByUserId(_currentUser.Id);
            if (_student == null)
            {
                tbStudentName.Text = "(no student profile)";
                AppendLine("System", "No student profile is linked to this account, so I can't load your data.");
                DisableInput();
                return;
            }

            tbStudentName.Text = $"— {_student.FullName}";

            if (!_aiService.IsConfigured)
            {
                AppendLine("System",
                    "The AI assistant is not configured yet. A Google Gemini API key must be set in the \"Gemini\" " +
                    "section of appsettings.json before you can chat.");
                DisableInput();
                return;
            }

            AppendLine("Assistant",
                "Hi! Ask me anything about your classes, timetable, attendance, grades, tuition or wallet. 👋");
        }
        catch (Exception ex)
        {
            AppendLine("System", $"Error preparing assistant: {ex.Message}");
            DisableInput();
        }
    }

    // ---- 2. Context --------------------------------------------
    /// <summary>
    /// Everything the student can see about themselves elsewhere in the app, written
    /// out as plain text. Built fresh on every question so the answers track the
    /// database rather than whatever was true when the window opened.
    /// </summary>
    private string BuildContext(Student student)
    {
        var sb = new StringBuilder();
        var today = DateOnly.FromDateTime(DateTime.Today);

        // --- Profile ---
        sb.AppendLine("== PROFILE ==");
        sb.AppendLine($"Name: {student.FullName}");
        sb.AppendLine($"Email (login): {_currentUser.Email}");
        if (student.DateOfBirth.HasValue) sb.AppendLine($"Date of birth: {student.DateOfBirth:dd/MM/yyyy}");
        if (!string.IsNullOrWhiteSpace(student.Phone)) sb.AppendLine($"Phone: {student.Phone}");
        if (!string.IsNullOrWhiteSpace(student.Address)) sb.AppendLine($"Address: {student.Address}");
        sb.AppendLine($"Account status: {student.Status}");
        sb.AppendLine($"Today is {today:dd/MM/yyyy}.");
        sb.AppendLine();

        // ACTIVE enrollments only — that is what GetByStudentId returns. Finished
        // classes still reach the model through the grades and invoices below.
        var enrollments = _enrollmentService.GetByStudentId(student.StudentId);

        // --- Classes ---
        sb.AppendLine("== CLASSES ==");
        if (enrollments.Count == 0)
        {
            sb.AppendLine("(not enrolled in anything)");
        }
        else
        {
            foreach (var e in enrollments)
            {
                var c = e.Class;
                if (c == null) continue;

                sb.AppendLine(
                    $"- {c.Name} ({c.SnapCourseCode} {c.SnapCourseName}, {c.SnapLanguage} {c.SnapLevel}): " +
                    $"{c.StartDate:dd/MM/yyyy} to {c.EndDate:dd/MM/yyyy}, class is {c.Status}, " +
                    $"my enrollment is {e.Status}, room {c.Classroom?.Name ?? "n/a"}, " +
                    $"teacher {c.TeacherNames}, tuition {c.SnapTuitionFee:N0} VND");
            }
        }
        sb.AppendLine();

        // --- Timetable ---
        // The upcoming meetings, which is what "when is my next class" needs. Past
        // sessions are summarised by the attendance block below instead of listed.
        sb.AppendLine("== UPCOMING SESSIONS (next 15) ==");
        var classIds = enrollments
            .Where(e => e.Status != "DROPPED" && e.Class != null)
            .Select(e => e.ClassId)
            .Distinct()
            .ToList();

        var sessions = classIds.Count == 0
            ? new List<Session>()
            : _sessionService.GetByClassIds(classIds);

        var upcoming = sessions
            .Where(s => s.SessionDate >= today)
            .OrderBy(s => s.SessionDate)
            .Take(15)
            .ToList();

        if (upcoming.Count == 0)
        {
            sb.AppendLine("(no sessions scheduled from today onwards)");
        }
        else
        {
            foreach (var s in upcoming)
            {
                var className = enrollments.FirstOrDefault(e => e.ClassId == s.ClassId)?.Class?.Name ?? "class";
                var time = s.Schedule == null ? "" : $" {s.Schedule.StartTime:HH\\:mm}-{s.Schedule.EndTime:HH\\:mm}";
                sb.AppendLine($"- {s.SessionDate:dd/MM/yyyy}{time} {className}" +
                              (string.IsNullOrWhiteSpace(s.Topic) ? "" : $" — {s.Topic}") +
                              $" [{s.Status}]");
            }
        }
        sb.AppendLine();

        // --- Attendance ---
        sb.AppendLine("== ATTENDANCE ==");
        var attendances = _attendanceService.GetByStudentId(student.StudentId);
        if (attendances.Count == 0)
        {
            sb.AppendLine("(nothing recorded yet)");
        }
        else
        {
            var present = attendances.Count(a => a.Status == "PRESENT");
            var late = attendances.Count(a => a.Status == "LATE");
            var absent = attendances.Count(a => a.Status == "ABSENT");
            var excused = attendances.Count(a => a.Status == "EXCUSED");

            sb.AppendLine($"Out of {attendances.Count} recorded session(s): " +
                          $"{present} present, {late} late, {absent} absent, {excused} excused.");

            var missed = attendances
                .Where(a => a.Status == "ABSENT")
                .OrderByDescending(a => a.Session?.SessionDate)
                .Take(10);

            foreach (var a in missed)
                sb.AppendLine($"- absent on {a.Session?.SessionDate:dd/MM/yyyy}");
        }
        sb.AppendLine();

        // --- Grades ---
        sb.AppendLine("== GRADES ==");
        var grades = _gradeService.GetByStudentId(student.StudentId);
        if (grades.Count == 0)
        {
            sb.AppendLine("(no grades yet)");
        }
        else
        {
            foreach (var byClass in grades.GroupBy(g => g.Enrollment.Class.Name).OrderBy(g => g.Key))
            {
                sb.AppendLine($"{byClass.Key}:");

                decimal weighted = 0, weightSoFar = 0;
                foreach (var g in byClass.OrderBy(g => g.Component.SortOrder))
                {
                    sb.AppendLine($"  - {g.Component.Name}: {g.Score}/{g.MaxScore} " +
                                  $"(weight {g.Component.WeightPercent}%)");
                    weighted += g.Score / g.MaxScore * 10m * g.Component.WeightPercent;
                    weightSoFar += g.Component.WeightPercent;
                }

                // Same rule the teacher's grade screen uses: an average over part of the
                // weights is provisional, and must be labelled as such.
                if (weightSoFar > 0)
                {
                    var average = Math.Round(weighted / weightSoFar, 2);
                    sb.AppendLine(weightSoFar >= 100
                        ? $"  Final average: {average}/10"
                        : $"  Average so far: {average}/10 (only {weightSoFar}% of the marks are in)");
                }
            }
        }
        sb.AppendLine();

        // --- Money ---
        sb.AppendLine("== TUITION & PAYMENTS ==");
        var invoices = _invoiceService.GetAll().Where(i => i.StudentId == student.StudentId).ToList();
        if (invoices.Count == 0)
        {
            sb.AppendLine("(no invoices)");
        }
        else
        {
            decimal totalOwed = 0;
            foreach (var inv in invoices.OrderBy(i => i.CreatedAt))
            {
                var paid = _invoiceService.GetPaidAmount(inv.InvoiceId);
                var remaining = Math.Max(0, inv.Amount - paid);
                if (inv.Status != "CANCELLED") totalOwed += remaining;

                var className = enrollments.FirstOrDefault(e => e.EnrollmentId == inv.EnrollmentId)?.Class?.Name;

                sb.AppendLine(
                    $"- Invoice #{inv.InvoiceId}" + (className == null ? "" : $" for {className}") +
                    $": billed {inv.Amount:N0}" +
                    (inv.DiscountAmount > 0 ? $" (after {inv.DiscountAmount:N0} discount off {inv.OriginalAmount:N0})" : "") +
                    $", paid {paid:N0}, still owing {remaining:N0}, status {inv.Status}, " +
                    $"due {inv.DueDate?.ToString("dd/MM/yyyy") ?? "n/a"}");
            }
            sb.AppendLine($"Total still owing across all invoices: {totalOwed:N0} VND.");
        }

        sb.AppendLine($"Wallet balance: {_walletService.GetBalance(student.StudentId):N0} VND.");

        return sb.ToString();
    }

    // ---- 3. Chat flow ------------------------------------------
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
        if (string.IsNullOrEmpty(question) || _student == null) return;

        AppendLine("You", question);
        txtQuestion.Clear();
        SetBusy(true);

        try
        {
            // Fresh snapshot each time: the student may have just paid an invoice or
            // had a grade entered in another window.
            var context = BuildContext(_student);

            var result = await _aiService.AskAsync(question, context, _history);

            if (result.Success)
            {
                AppendLine("Assistant", result.Answer ?? "");

                _history.Add(new ChatTurn { IsUser = true, Text = question });
                _history.Add(new ChatTurn { IsUser = false, Text = result.Answer ?? "" });

                // Keep the last 10 exchanges. Older ones add cost without helping —
                // the data the answers come from is resent in full every time anyway.
                while (_history.Count > 20) _history.RemoveAt(0);
            }
            else
            {
                AppendLine("System", result.Error ?? "Unknown error.");
            }
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

    /// <summary>Asks one of the suggested questions from the chips above the input box.</summary>
    private async void SuggestionChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button) return;
        txtQuestion.Text = button.Tag?.ToString() ?? button.Content?.ToString() ?? "";
        await SendAsync();
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
        suggestions.IsEnabled = !busy;
        btnSend.Content = busy ? "…" : "Send";
    }

    private void DisableInput()
    {
        txtQuestion.IsEnabled = false;
        btnSend.IsEnabled = false;
        suggestions.IsEnabled = false;
    }
}
