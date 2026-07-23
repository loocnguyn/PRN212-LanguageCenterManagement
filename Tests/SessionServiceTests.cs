using BusinessObjects;
using Moq;
using Repositories;
using Services;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for session generation. All repositories are mocked, so these run with
/// no database.
///
/// The rule under test: the COURSE decides how many meetings a class runs (frozen onto
/// the class as SnapDurationSessions); the weekly schedule and the semester only decide
/// when they fall. Generation therefore stops at that count instead of running to the
/// end of the semester, which is what it used to do.
/// </summary>
public class SessionServiceTests
{
    private readonly Mock<ISessionRepository> _sessionRepo = new();
    private readonly Mock<IClassRepository> _classRepo = new();
    private readonly Mock<ISemesterRepository> _semesterRepo = new();
    private readonly Mock<IClassScheduleRepository> _scheduleRepo = new();

    private SessionService CreateService() => new(
        _sessionRepo.Object, _classRepo.Object, _semesterRepo.Object, _scheduleRepo.Object);

    // Teaching runs 08/06/2026 (Mon) .. 12/07/2026 — five whole weeks.
    private static Semester ASemester() => new()
    {
        SemesterId = 2,
        Name = "Test 2026",
        StartDate = new DateOnly(2026, 6, 1),
        SetupEndDate = new DateOnly(2026, 6, 7),
        EndDate = new DateOnly(2026, 7, 12)
    };

    private static Class AClass(int duration) => new()
    {
        ClassId = 5,
        SemesterId = 2,
        CourseId = 3,
        Name = "A1-K01",
        MaxStudents = 20,

        SnapCourseCode = "ENG-A1",
        SnapCourseName = "English A1",
        SnapLanguage = "English",
        SnapDurationSessions = duration,
        SnapTuitionFee = 3_500_000
    };

    /// <summary>Mon + Wed, the pattern from the user's example.</summary>
    private static List<ClassSchedule> MonWed() => new()
    {
        new ClassSchedule { ScheduleId = 1, ClassId = 5, DayOfWeek = 1,
                            StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(9, 15) },
        new ClassSchedule { ScheduleId = 2, ClassId = 5, DayOfWeek = 3,
                            StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(9, 15) }
    };

    private void Arrange(Class cls, List<ClassSchedule> schedules)
    {
        _classRepo.Setup(r => r.GetById(5)).Returns(cls);
        _semesterRepo.Setup(r => r.GetById(2)).Returns(ASemester());
        _scheduleRepo.Setup(r => r.GetByClassId(5)).Returns(schedules);
        _sessionRepo.Setup(r => r.CountByClassId(5)).Returns(0);
    }

    private List<Session> CaptureSaved()
    {
        var saved = new List<Session>();
        _sessionRepo.Setup(r => r.BulkSave(It.IsAny<List<Session>>()))
            .Callback<List<Session>>(saved.AddRange);
        return saved;
    }

    [Fact]
    public void AvailableDates_CountsEveryMeetingThePatternFits()
    {
        Arrange(AClass(duration: 10), MonWed());

        var available = CreateService().GetAvailableSessionDates(5);

        // Five weeks x two days a week.
        Assert.Equal(10, available.Count);
    }

    [Fact]
    public void AvailableDates_AreChronologicalAcrossSlots()
    {
        Arrange(AClass(duration: 10), MonWed());

        var dates = CreateService().GetAvailableSessionDates(5).Select(p => p.Date).ToList();

        // Not grouped slot-by-slot: every Monday must be followed by that week's Wednesday.
        Assert.Equal(dates.OrderBy(d => d).ToList(), dates);
        Assert.Equal(new DateOnly(2026, 6, 8), dates[0]);   // first Monday after setup
        Assert.Equal(new DateOnly(2026, 6, 10), dates[1]);  // that same week's Wednesday
    }

    [Fact]
    public void Generate_StopsAtTheCoursesSessionCount()
    {
        // The pattern could run 10 meetings, but the course only wants 6.
        Arrange(AClass(duration: 6), MonWed());
        var saved = CaptureSaved();

        CreateService().GenerateSessionsForClass(5);

        Assert.Equal(6, saved.Count);
        Assert.Equal(new DateOnly(2026, 6, 24), saved.Last().SessionDate); // week 3 Wednesday
    }

    /// <summary>
    /// Capping must not drop a weekday: taking the first N of a slot-ordered list would
    /// have filled the quota with Mondays and scheduled no Wednesday at all.
    /// </summary>
    [Fact]
    public void Generate_KeepsBothWeekdaysWhenCapping()
    {
        Arrange(AClass(duration: 4), MonWed());
        var saved = CaptureSaved();

        CreateService().GenerateSessionsForClass(5);

        Assert.Equal(4, saved.Count);
        Assert.Equal(2, saved.Count(s => s.SessionDate.DayOfWeek == DayOfWeek.Monday));
        Assert.Equal(2, saved.Count(s => s.SessionDate.DayOfWeek == DayOfWeek.Wednesday));
    }

    /// <summary>
    /// The short-schedule case the editor warns about: the semester simply cannot hold
    /// 40 meetings at two a week, so generation delivers what fits rather than throwing.
    /// </summary>
    [Fact]
    public void Generate_ProducesWhatFitsWhenTheScheduleIsTooSparse()
    {
        Arrange(AClass(duration: 40), MonWed());
        var saved = CaptureSaved();

        CreateService().GenerateSessionsForClass(5);

        Assert.Equal(10, saved.Count);
    }

    [Fact]
    public void Generate_NoOpsWhenSessionsAlreadyExist()
    {
        Arrange(AClass(duration: 10), MonWed());
        _sessionRepo.Setup(r => r.CountByClassId(5)).Returns(10);

        CreateService().GenerateSessionsForClass(5);

        _sessionRepo.Verify(r => r.BulkSave(It.IsAny<List<Session>>()), Times.Never);
    }
}
