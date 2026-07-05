using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

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

            if (cls.TeacherId != teacher.TeacherId)
            {
                MessageBox.Show($"You are not authorized to {actionName} for this class.\n" +
                    $"Class is assigned to Teacher ID {cls.TeacherId}.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        return true;
    }
}
