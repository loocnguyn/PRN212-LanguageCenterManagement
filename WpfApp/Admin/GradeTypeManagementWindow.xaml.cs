using System.Windows;

namespace WpfApp;

// TODO (Nhân): Design and implement the per-course grading structure editor here.
// Backend groundwork already exists — Services/IGradeTypeService.cs:
//   - GetByCourseId(courseId)
//   - GetTotalWeightPercent(courseId, excludeGradeTypeId?)
public partial class GradeTypeManagementWindow : Window
{
    public GradeTypeManagementWindow()
    {
        InitializeComponent();
    }
}
