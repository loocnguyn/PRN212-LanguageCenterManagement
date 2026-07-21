using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// AuthorizationHelper — small role/ownership checks reused by windows to gate actions.
public static class AuthorizationHelper
{
    public static bool AuthorizeTeacherForClass(User currentUser, ITeacherService teacherService, Class cls, string actionName)
    {
        if (currentUser.Role == "TEACHER")
        {
            var teacher = teacherService.GetByUserId(currentUser.Id);
            if (teacher == null)
            {
                MessageBox.Show("Teacher profile not found for your account.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // A class can be co-taught, so ownership is "am I one of its teachers".
            // Needs ClassTeachers Included — ClassDAO.GetById and the by-semester
            // queries both do that.
            if (!cls.ClassTeachers.Any(ct => ct.TeacherId == teacher.TeacherId))
            {
                var assigned = cls.TeacherNames;
                MessageBox.Show($"You are not authorized to {actionName} for this class.\n" +
                    $"Class is assigned to: {(assigned.Length > 0 ? assigned : "nobody")}.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        return true;
    }
}
