using BusinessObjects;
using Moq;
using Repositories;
using Services;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for the semester edit lock. The repository is mocked, so these run with no
/// database.
///
/// The rule under test: a semester's details can only be changed while it is still in SETUP.
/// Once teaching has started its dates are already baked into generated sessions, and once it
/// has ended it is a historical record.
/// </summary>
public class SemesterServiceTests
{
    private readonly Mock<ISemesterRepository> _repo = new();

    private SemesterService CreateService() => new(_repo.Object);

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Still in setup: teaching has not started yet.</summary>
    private static Semester InSetup() => new()
    {
        SemesterId = 1,
        Name = "Setup 2026",
        StartDate = Today.AddDays(-3),
        SetupEndDate = Today.AddDays(7),
        EndDate = Today.AddDays(60)
    };

    /// <summary>Teaching: setup ended yesterday, the semester still runs.</summary>
    private static Semester Teaching() => new()
    {
        SemesterId = 2,
        Name = "Teaching 2026",
        StartDate = Today.AddDays(-30),
        SetupEndDate = Today.AddDays(-1),
        EndDate = Today.AddDays(30)
    };

    /// <summary>Completed: the end date is in the past.</summary>
    private static Semester Completed() => new()
    {
        SemesterId = 3,
        Name = "Completed 2025",
        StartDate = Today.AddDays(-120),
        SetupEndDate = Today.AddDays(-110),
        EndDate = Today.AddDays(-30)
    };

    // ---- IsEditable --------------------------------------------
    [Fact]
    public void IsEditable_IsTrue_OnlyDuringSetup()
    {
        var service = CreateService();

        Assert.True(service.IsEditable(InSetup()));
        Assert.False(service.IsEditable(Teaching()));
        Assert.False(service.IsEditable(Completed()));
    }

    // ---- Update guard ------------------------------------------
    [Fact]
    public void Update_Succeeds_WhileStillInSetup()
    {
        var stored = InSetup();
        _repo.Setup(r => r.GetById(stored.SemesterId)).Returns(stored);
        _repo.Setup(r => r.NameExists(It.IsAny<string>(), It.IsAny<int?>())).Returns(false);
        _repo.Setup(r => r.GetOverlapping(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int?>()))
             .Returns(new List<Semester>());

        var edited = InSetup();
        edited.Name = "Renamed";

        CreateService().Update(edited);

        _repo.Verify(r => r.Update(edited), Times.Once);
    }

    [Fact]
    public void Update_IsRejected_OnceTeachingHasStarted()
    {
        var stored = Teaching();
        _repo.Setup(r => r.GetById(stored.SemesterId)).Returns(stored);

        var ex = Assert.Throws<InvalidOperationException>(() => CreateService().Update(Teaching()));

        Assert.Contains("teaching", ex.Message, StringComparison.OrdinalIgnoreCase);
        _repo.Verify(r => r.Update(It.IsAny<Semester>()), Times.Never);
    }

    [Fact]
    public void Update_IsRejected_OnceCompleted()
    {
        var stored = Completed();
        _repo.Setup(r => r.GetById(stored.SemesterId)).Returns(stored);

        var ex = Assert.Throws<InvalidOperationException>(() => CreateService().Update(Completed()));

        Assert.Contains("completed", ex.Message, StringComparison.OrdinalIgnoreCase);
        _repo.Verify(r => r.Update(It.IsAny<Semester>()), Times.Never);
    }

    /// <summary>
    /// The important one: editability is judged from the STORED row. Submitting future dates for
    /// a semester that is already teaching must not talk the guard into letting the edit through.
    /// </summary>
    [Fact]
    public void Update_IsRejected_EvenWhenSubmittedDatesLookLikeSetup()
    {
        var stored = Teaching();
        _repo.Setup(r => r.GetById(stored.SemesterId)).Returns(stored);

        var disguised = new Semester
        {
            SemesterId = stored.SemesterId,
            Name = stored.Name,
            StartDate = Today.AddDays(10),
            SetupEndDate = Today.AddDays(20),   // "still in setup" if judged on these
            EndDate = Today.AddDays(80)
        };

        Assert.Throws<InvalidOperationException>(() => CreateService().Update(disguised));
        _repo.Verify(r => r.Update(It.IsAny<Semester>()), Times.Never);
    }
}
