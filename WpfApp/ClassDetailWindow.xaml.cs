using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class ClassDetailWindow : Window
{
    private readonly IClassService _classService = new ClassService();
    private readonly ICourseService _courseService = new CourseService();
    private readonly ITeacherService _teacherService = new TeacherService();
    private readonly IClassroomService _classroomService = new ClassroomService();
    private readonly Class? _editClass;

    public ClassDetailWindow(Class? classEntity = null)
    {
        InitializeComponent();
        _editClass = classEntity;

        cmbCourse.ItemsSource = _courseService.GetAll();
        cmbTeacher.ItemsSource = _teacherService.GetAll();
        cmbClassroom.ItemsSource = _classroomService.GetAll();

        if (classEntity != null)
        {
            Title = "Edit Class";
            txtName.Text = classEntity.Name;
            cmbCourse.SelectedValue = classEntity.CourseId;
            cmbTeacher.SelectedValue = classEntity.TeacherId;
            cmbClassroom.SelectedValue = classEntity.ClassroomId;
            txtMaxStudents.Text = classEntity.MaxStudents.ToString();
            if (classEntity.StartDate.HasValue)
                dpStartDate.SelectedDate = classEntity.StartDate.Value.ToDateTime(new TimeOnly(0, 0));
            if (classEntity.EndDate.HasValue)
                dpEndDate.SelectedDate = classEntity.EndDate.Value.ToDateTime(new TimeOnly(0, 0));
            SelectComboItem(cmbStatus, classEntity.Status);
        }
        else
        {
            cmbStatus.SelectedIndex = 0;
        }
    }

    private static void SelectComboItem(System.Windows.Controls.ComboBox cmb, string? value)
    {
        if (value == null) return;
        foreach (var item in cmb.Items)
        {
            if (item is System.Windows.Controls.ComboBoxItem ci && ci.Content.ToString() == value)
            {
                cmb.SelectedItem = ci;
                return;
            }
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var name = txtName.Text.Trim();
        var courseId = cmbCourse.SelectedValue as int?;
        var teacherId = cmbTeacher.SelectedValue as int?;
        var classroomId = cmbClassroom.SelectedValue as int?;
        var maxText = txtMaxStudents.Text.Trim();
        var status = (cmbStatus.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();

        // Validation
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Class name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (courseId == null || courseId <= 0)
        {
            MessageBox.Show("Please select a course.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (teacherId == null || teacherId <= 0)
        {
            MessageBox.Show("Please select a teacher.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (classroomId == null || classroomId <= 0)
        {
            MessageBox.Show("Please select a classroom.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(maxText, out var maxStudents) || maxStudents <= 0)
        {
            MessageBox.Show("Max Students must be a positive integer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Check MaxStudents vs Classroom capacity
        var selectedClassroom = _classroomService.GetById(classroomId.Value);
        if (selectedClassroom != null && maxStudents > selectedClassroom.Capacity)
        {
            MessageBox.Show($"MaxStudents ({maxStudents}) exceeds classroom capacity ({selectedClassroom.Capacity}).",
                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(status))
        {
            MessageBox.Show("Status is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DateOnly? startDate = dpStartDate.SelectedDate.HasValue
            ? DateOnly.FromDateTime(dpStartDate.SelectedDate.Value)
            : null;
        DateOnly? endDate = dpEndDate.SelectedDate.HasValue
            ? DateOnly.FromDateTime(dpEndDate.SelectedDate.Value)
            : null;

        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            MessageBox.Show("Start date cannot be later than end date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_editClass == null)
        {
            var classEntity = new Class
            {
                Name = name,
                CourseId = courseId.Value,
                TeacherId = teacherId.Value,
                ClassroomId = classroomId.Value,
                MaxStudents = maxStudents,
                StartDate = startDate,
                EndDate = endDate,
                Status = status,
                CreatedAt = System.DateTime.Now
            };
            _classService.Save(classEntity);
        }
        else
        {
            _editClass.Name = name;
            _editClass.CourseId = courseId.Value;
            _editClass.TeacherId = teacherId.Value;
            _editClass.ClassroomId = classroomId.Value;
            _editClass.MaxStudents = maxStudents;
            _editClass.StartDate = startDate;
            _editClass.EndDate = endDate;
            _editClass.Status = status;
            _classService.Update(_editClass);
        }

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
