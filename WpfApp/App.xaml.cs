using System.Windows;
using Services;

namespace WpfApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var semesterService = new SemesterService();
            var active = semesterService.GetActive();
            if (active == null) return;

            var phase = semesterService.GetPhase(active);

            if (phase == BusinessObjects.Phase.LEARNING)
            {
                // Lock enrollments only on the first day after setup ends (transition day)
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (today == active.SetupEndDate.Value.AddDays(1))
                {
                    var enrollmentService = new EnrollmentService();
                    enrollmentService.LockEnrollmentsForSemester(active.SemesterId);
                }

                // Generate sessions on every startup during LEARNING (idempotent — CountByClassId guard)
                var sessionService = new SessionService();
                sessionService.EnsureSessionsForSemester(active.SemesterId);
            }
        }
        catch
        {
            // Silently handle startup errors — DB may not be connected yet.
            // Individual windows will show errors to the user when they try to access data.
        }
    }
}
