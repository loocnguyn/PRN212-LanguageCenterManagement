using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  AccountViewWindow — read-only detail view of one account.
//  CONTENTS:
//    1. Construction & Load    — role-colored header + sections
//    2. LoadProfile            — role-specific profile rows
//    3. AddRow / helpers       — label/value row builder, initials, brush
// ============================================================
public partial class AccountViewWindow : Window
{
    private readonly IStudentService _studentService = new StudentService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IStaffService _staffService = new StaffService();
    private readonly IAdminService _adminService = new AdminService();

    public AccountViewWindow(User user)
    {
        InitializeComponent();
        Load(user);
    }

    private void Load(User user)
    {
        var accent = RoleBrush(user.Role);
        header.Background = accent;
        rolePill.Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
        txtAvatar.Foreground = accent;
        txtAvatar.Text = Initials(user.Email);
        txtHeaderUser.Text = user.Email;
        txtHeaderRole.Text = user.Role;

        // Account section (same for every role)
        AddRow(accountPanel, "Account ID", $"#{user.Id}");
        AddRow(accountPanel, "Email (login)", user.Email);
        AddRow(accountPanel, "Role", user.Role);
        AddRow(accountPanel, "Status", user.IsActive ? "Active" : "Inactive");
        AddRow(accountPanel, "Created At", user.CreatedAt.ToString("dd/MM/yyyy HH:mm"));

        // Role-specific profile section
        var displayName = LoadProfile(user);
        txtHeaderName.Text = string.IsNullOrWhiteSpace(displayName) ? user.Email : displayName;
    }

    /// <summary>Fills profilePanel from the role-specific profile record; returns the full name for the header.</summary>
    private string LoadProfile(User user)
    {
        switch (user.Role)
        {
            case "STUDENT":
                var student = _studentService.GetAll().FirstOrDefault(x => x.UserId == user.Id);
                if (student == null) return ShowMissingProfile();
                AddRow(profilePanel, "Full name", student.FullName);
                AddRow(profilePanel, "Date of birth", student.DateOfBirth?.ToString("dd/MM/yyyy"));
                AddRow(profilePanel, "Gender", student.Gender);
                AddRow(profilePanel, "Phone", student.Phone);
                AddRow(profilePanel, "Email", student.Email);
                AddRow(profilePanel, "Address", student.Address);
                AddRow(profilePanel, "Wallet balance", student.Balance.ToString("#,0") + " đ");
                AddRow(profilePanel, "Status", student.Status);
                return student.FullName;

            case "TEACHER":
                var teacher = _teacherService.GetByUserId(user.Id);
                if (teacher == null) return ShowMissingProfile();
                AddRow(profilePanel, "Full name", teacher.FullName);
                AddRow(profilePanel, "Date of birth", teacher.DateOfBirth?.ToString("dd/MM/yyyy"));
                AddRow(profilePanel, "Gender", teacher.Gender);
                AddRow(profilePanel, "Phone", teacher.Phone);
                AddRow(profilePanel, "Email", teacher.Email);
                AddRow(profilePanel, "Specialization", teacher.Specialization);
                AddRow(profilePanel, "Degree", teacher.Degree);
                AddRow(profilePanel, "Status", teacher.Status);
                return teacher.FullName;

            case "STAFF":
                var staff = _staffService.GetAll().FirstOrDefault(x => x.UserId == user.Id);
                if (staff == null) return ShowMissingProfile();
                AddRow(profilePanel, "Full name", staff.FullName);
                AddRow(profilePanel, "Date of birth", staff.DateOfBirth?.ToString("dd/MM/yyyy"));
                AddRow(profilePanel, "Gender", staff.Gender);
                AddRow(profilePanel, "Phone", staff.Phone);
                AddRow(profilePanel, "Email", staff.Email);
                AddRow(profilePanel, "Department", staff.Department);
                return staff.FullName;

            case "ADMIN":
                var admin = _adminService.GetAll().FirstOrDefault(x => x.UserId == user.Id);
                if (admin == null) return ShowMissingProfile();
                AddRow(profilePanel, "Full name", admin.FullName);
                AddRow(profilePanel, "Phone", admin.Phone);
                AddRow(profilePanel, "Email", admin.Email);
                return admin.FullName;

            default:
                return ShowMissingProfile();
        }
    }

    private string ShowMissingProfile()
    {
        profilePanel.Children.Add(new TextBlock
        {
            Text = "No profile record linked to this account.",
            Foreground = (Brush)FindResource("TextSecondaryBrush"),
            FontStyle = FontStyles.Italic
        });
        return "";
    }

    private void AddRow(Panel panel, string label, string? value)
    {
        var row = new Grid { Margin = new Thickness(0, 5, 0, 5) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock
        {
            Text = label,
            Foreground = (Brush)FindResource("TextSecondaryBrush"),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Top
        };
        Grid.SetColumn(lbl, 0);

        var val = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(val, 1);

        row.Children.Add(lbl);
        row.Children.Add(val);
        panel.Children.Add(row);
    }

    private Brush RoleBrush(string role) => role switch
    {
        "ADMIN" => (Brush)FindResource("RoleAdminBrush"),
        "TEACHER" => (Brush)FindResource("RoleTeacherBrush"),
        "STUDENT" => (Brush)FindResource("RoleStudentBrush"),
        _ => (Brush)FindResource("RoleStaffBrush"),
    };

    private static string Initials(string username)
        => new InitialsConverter().Convert(username, typeof(string), null!, null!)?.ToString() ?? "?";
}
