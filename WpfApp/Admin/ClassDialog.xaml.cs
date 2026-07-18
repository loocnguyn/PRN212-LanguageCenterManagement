using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class ClassDialog : Window
{
    public Class? Result { get; private set; }
    private readonly int? _editId;
    private readonly ICourseService _courseService = new CourseService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IClassroomService _classroomService = new ClassroomService();

    private static readonly string[] Statuses = { "UPCOMING", "ONGOING", "COMPLETED", "CANCELLED" };

    public ClassDialog()
    {
        InitializeComponent();
        LoadDropdowns();
    }

    public ClassDialog(Class cls)
    {
        InitializeComponent();
        _editId = cls.ClassId;
        LoadDropdowns();
        txtName.Text = cls.Name;
        cboCourse.SelectedValue = cls.CourseId;
        cboTeacher.SelectedValue = cls.TeacherId;
        cboClassroom.SelectedValue = cls.ClassroomId;
        txtMaxStudents.Text = cls.MaxStudents.ToString();
        if (cls.StartDate.HasValue) dpStartDate.SelectedDate = cls.StartDate.Value.ToDateTime(TimeOnly.MinValue);
        if (cls.EndDate.HasValue) dpEndDate.SelectedDate = cls.EndDate.Value.ToDateTime(TimeOnly.MinValue);
        cboStatus.SelectedItem = cls.Status;
    }

    private void LoadDropdowns()
    {
        cboCourse.ItemsSource = _courseService.GetAll();
        cboTeacher.ItemsSource = _teacherService.GetAll();
        cboClassroom.ItemsSource = _classroomService.GetAll();
        cboStatus.ItemsSource = Statuses;
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(txtName.Text.Trim()))
            return "Name is required.";
        if (cboCourse.SelectedValue == null)
            return "Please select a Course.";
        if (cboTeacher.SelectedValue == null)
            return "Please select a Teacher.";
        if (cboClassroom.SelectedValue == null)
            return "Please select a Classroom.";
        if (!int.TryParse(txtMaxStudents.Text.Trim(), out var max) || max < 1)
            return "Max Students must be an integer >= 1.";
        if (cboStatus.SelectedItem == null)
            return "Please select a Status.";
        return null;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var error = Validate();
        if (error != null) { MessageBox.Show(error, "Validation"); return; }

        Result = new Class
        {
            ClassId = _editId ?? 0,
            Name = txtName.Text.Trim(),
            CourseId = (int)cboCourse.SelectedValue,
            TeacherId = (int)cboTeacher.SelectedValue,
            ClassroomId = (int)cboClassroom.SelectedValue,
            MaxStudents = int.Parse(txtMaxStudents.Text.Trim()),
            StartDate = dpStartDate.SelectedDate.HasValue ? DateOnly.FromDateTime(dpStartDate.SelectedDate.Value) : null,
            EndDate = dpEndDate.SelectedDate.HasValue ? DateOnly.FromDateTime(dpEndDate.SelectedDate.Value) : null,
            Status = (string)cboStatus.SelectedItem,
            CreatedAt = DateTime.Now
        };
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
