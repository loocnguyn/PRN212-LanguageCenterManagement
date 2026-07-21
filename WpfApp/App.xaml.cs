using System.Windows;
using Services;

namespace WpfApp;

// ============================================================
//  App — WPF entry point. On startup, if a semester is in its
//  LEARNING phase, it locks enrollments on the transition day and
//  (idempotently) generates class sessions. DB errors are swallowed
//  so the app still opens to the login window.
// ============================================================
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
                if (today == active.SetupEndDate.AddDays(1))
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
