using BusinessObjects;
using Moq;
using Repositories;
using Services;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for the enrollment guard clauses (student/class existence, capacity, tuition
/// validity). All repositories are mocked, so these run with no database.
///
/// Note the price source: a class carries a FROZEN copy of its course
/// (Class.SnapTuitionFee), and enrollment bills from that. The course is never
/// consulted — repricing a course must not change what already-enrolled students
/// owe — which is why there is no course repository here at all.
/// </summary>
public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _enrollmentRepo = new();
    private readonly Mock<IClassRepository> _classRepo = new();
    private readonly Mock<IStudentRepository> _studentRepo = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();

    private EnrollmentService CreateService() => new(
        _enrollmentRepo.Object, _classRepo.Object, _studentRepo.Object, _invoiceRepo.Object);

    private static Student AStudent() => new() { StudentId = 1, FullName = "Test Student" };

    /// <summary>
    /// A class that is open for enrollment. Status is not settable — it follows the dates,
    /// so "open" is expressed by ending in the future rather than by a magic string.
    /// </summary>
    private static Class AClass(decimal snapFee = 3_500_000) => new()
    {
        ClassId = 5,
        CourseId = 3,
        Name = "A1-K01",
        MaxStudents = 20,
        StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),
        EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
        SnapCourseCode = "ENG-A1",
        SnapCourseName = "English A1",
        SnapLanguage = "English",
        SnapLevel = "A1",
        SnapDurationSessions = 40,
        SnapTuitionFee = snapFee
    };

    [Fact]
    public void Enroll_ThrowsWhenStudentNotFound()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns((Student?)null);
        var service = CreateService();

        var ex = Assert.Throws<InvalidOperationException>(() => service.Enroll(1, 5));
        Assert.Contains("Student 1 not found", ex.Message);
    }

    [Fact]
    public void Enroll_ThrowsWhenClassNotFound()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns(AStudent());
        _classRepo.Setup(r => r.GetById(5)).Returns((Class?)null);
        var service = CreateService();

        var ex = Assert.Throws<InvalidOperationException>(() => service.Enroll(1, 5));
        Assert.Contains("Class 5 not found", ex.Message);
    }

    [Fact]
    public void Enroll_ThrowsWhenClassHasNoRecordedTuition()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns(AStudent());
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass(snapFee: 0));
        var service = CreateService();

        var ex = Assert.Throws<InvalidOperationException>(() => service.Enroll(1, 5));
        Assert.Contains("no valid tuition fee", ex.Message);
    }

    [Fact]
    public void Enroll_ThrowsWhenClassIsFull()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns(AStudent());
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass());
        _enrollmentRepo.Setup(r => r.CountByClassId(5)).Returns(20); // MaxStudents == 20
        var service = CreateService();

        var ex = Assert.Throws<InvalidOperationException>(() => service.Enroll(1, 5));
        Assert.Contains("is full", ex.Message);
    }

    [Fact]
    public void Enroll_CreatesEnrollmentWithInvoiceOnHappyPath()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns(AStudent());
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass());
        _enrollmentRepo.Setup(r => r.CountByClassId(5)).Returns(3);
        _enrollmentRepo.Setup(r => r.GetByStudentAndClass(1, 5)).Returns((Enrollment?)null);
        var service = CreateService();

        service.Enroll(1, 5);

        _enrollmentRepo.Verify(r => r.EnrollWithInvoice(
            It.Is<Enrollment>(e => e.StudentId == 1 && e.ClassId == 5 && e.Status == "ACTIVE"),
            3_500_000m,
            It.IsAny<DateOnly>(),
            It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// The point of the snapshot: the class was sold at its own price, so that is what
    /// gets invoiced even though the course it came from has since been repriced.
    /// </summary>
    [Fact]
    public void Enroll_BillsTheClassSnapshotNotTheCurrentCoursePrice()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns(AStudent());
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass(snapFee: 3_500_000));
        _enrollmentRepo.Setup(r => r.CountByClassId(5)).Returns(0);
        _enrollmentRepo.Setup(r => r.GetByStudentAndClass(1, 5)).Returns((Enrollment?)null);
        var service = CreateService();

        service.Enroll(1, 5);

        // 9_900_000 would be the course's new price; the frozen 3_500_000 must win.
        _enrollmentRepo.Verify(r => r.EnrollWithInvoice(
            It.IsAny<Enrollment>(), 3_500_000m, It.IsAny<DateOnly>(), It.IsAny<string>()), Times.Once);
        _enrollmentRepo.Verify(r => r.EnrollWithInvoice(
            It.IsAny<Enrollment>(), 9_900_000m, It.IsAny<DateOnly>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Enroll_ThrowsWhenStudentAlreadyActivelyEnrolled()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns(AStudent());
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass());
        _enrollmentRepo.Setup(r => r.CountByClassId(5)).Returns(3);
        _enrollmentRepo.Setup(r => r.GetByStudentAndClass(1, 5))
            .Returns(new Enrollment { StudentId = 1, ClassId = 5, Status = "ACTIVE" });
        var service = CreateService();

        var ex = Assert.Throws<InvalidOperationException>(() => service.Enroll(1, 5));
        Assert.Contains("already enrolled", ex.Message);
    }

    // ============================================================
    //  GetEnrollableStudents — who the picker is allowed to offer.
    //  These exist to keep the offer list and the enroll guards in step: anyone the
    //  list shows must be someone Enroll would actually accept.
    // ============================================================

    private static Student AStudent(int id, string name, string status = "ACTIVE")
        => new() { StudentId = id, FullName = name, Status = status };

    [Fact]
    public void GetEnrollableStudents_ExcludesStudentsAlreadyHoldingAPlace()
    {
        _studentRepo.Setup(r => r.GetAll()).Returns(new List<Student>
        {
            AStudent(1, "Already Active"),
            AStudent(2, "Locked In"),
            AStudent(3, "Free Agent")
        });
        _enrollmentRepo.Setup(r => r.GetByClassId(5)).Returns(new List<Enrollment>
        {
            new() { StudentId = 1, ClassId = 5, Status = "ACTIVE" },
            new() { StudentId = 2, ClassId = 5, Status = "LOCKED" }
        });

        var result = CreateService().GetEnrollableStudents(5);

        Assert.Single(result);
        Assert.Equal(3, result[0].Student.StudentId);
    }

    /// <summary>
    /// Enrolling a dropped student revives their existing row, so they must stay on offer —
    /// but flagged, because they are not a fresh enrollment.
    /// </summary>
    [Fact]
    public void GetEnrollableStudents_KeepsDroppedStudentsAndFlagsThem()
    {
        _studentRepo.Setup(r => r.GetAll()).Returns(new List<Student> { AStudent(7, "Came Back") });
        _enrollmentRepo.Setup(r => r.GetByClassId(5)).Returns(new List<Enrollment>
        {
            new() { StudentId = 7, ClassId = 5, Status = "DROPPED" }
        });

        var result = CreateService().GetEnrollableStudents(5);

        Assert.Single(result);
        Assert.True(result[0].PreviouslyDropped);
    }

    [Fact]
    public void GetEnrollableStudents_ExcludesInactiveStudents()
    {
        _studentRepo.Setup(r => r.GetAll()).Returns(new List<Student>
        {
            AStudent(1, "Active One"),
            AStudent(2, "Deactivated", status: "INACTIVE")
        });
        _enrollmentRepo.Setup(r => r.GetByClassId(5)).Returns(new List<Enrollment>());

        var result = CreateService().GetEnrollableStudents(5);

        Assert.Single(result);
        Assert.Equal(1, result[0].Student.StudentId);
    }

    // ============================================================
    //  EnrollMany — batch enroll, deliberately not all-or-nothing.
    // ============================================================

    [Fact]
    public void EnrollMany_EnrollsEachStudentWithTheirOwnDiscount()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns(AStudent(1, "First"));
        _studentRepo.Setup(r => r.GetById(2)).Returns(AStudent(2, "Second"));
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass());
        _enrollmentRepo.Setup(r => r.CountByClassId(5)).Returns(0);
        _enrollmentRepo.Setup(r => r.GetByStudentAndClass(It.IsAny<int>(), 5)).Returns((Enrollment?)null);

        var result = CreateService().EnrollMany(5, new List<EnrollRequest>
        {
            new(1, null),
            new(2, null)
        });

        Assert.All(result, o => Assert.True(o.Success));
        _enrollmentRepo.Verify(r => r.EnrollWithInvoice(
            It.IsAny<Enrollment>(), It.IsAny<decimal>(),
            It.IsAny<DateOnly>(), It.IsAny<string>()), Times.Exactly(2));
    }

    /// <summary>
    /// One student being refused must not cost the others their place — the whole reason
    /// this is a loop of independent enrollments rather than a single transaction.
    /// </summary>
    [Fact]
    public void EnrollMany_KeepsGoingAfterOneStudentIsRefused()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns(AStudent(1, "Good One"));
        _studentRepo.Setup(r => r.GetById(2)).Returns(AStudent(2, "Duplicate"));
        _studentRepo.Setup(r => r.GetById(3)).Returns(AStudent(3, "Also Good"));
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass());
        _enrollmentRepo.Setup(r => r.CountByClassId(5)).Returns(0);
        _enrollmentRepo.Setup(r => r.GetByStudentAndClass(It.IsAny<int>(), 5)).Returns((Enrollment?)null);
        _enrollmentRepo.Setup(r => r.GetByStudentAndClass(2, 5))
            .Returns(new Enrollment { StudentId = 2, ClassId = 5, Status = "ACTIVE" });

        var result = CreateService().EnrollMany(5, new List<EnrollRequest>
        {
            new(1, null), new(2, null), new(3, null)
        });

        Assert.Equal(2, result.Count(o => o.Success));

        var refused = Assert.Single(result, o => !o.Success);
        Assert.Equal(2, refused.StudentId);
        Assert.Contains("already enrolled", refused.Error);

        // The two valid students still got in.
        _enrollmentRepo.Verify(r => r.EnrollWithInvoice(
            It.IsAny<Enrollment>(), It.IsAny<decimal>(),
            It.IsAny<DateOnly>(), It.IsAny<string>()), Times.Exactly(2));
    }

    /// <summary>
    /// Capacity is recounted inside every iteration, so a class filling up mid-batch admits
    /// the students who fit and refuses the rest — rather than reading a stale count once.
    /// </summary>
    [Fact]
    public void EnrollMany_StopsAdmittingOnceTheClassFillsUp()
    {
        _studentRepo.Setup(r => r.GetById(It.IsAny<int>()))
            .Returns((int id) => AStudent(id, $"Student {id}"));
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass());
        _enrollmentRepo.Setup(r => r.GetByStudentAndClass(It.IsAny<int>(), 5)).Returns((Enrollment?)null);

        // 19 of 20 seats gone, then full from the next check onwards.
        var counts = new Queue<int>(new[] { 19, 20, 20 });
        _enrollmentRepo.Setup(r => r.CountByClassId(5)).Returns(() => counts.Dequeue());

        var result = CreateService().EnrollMany(5, new List<EnrollRequest>
        {
            new(1, null), new(2, null), new(3, null)
        });

        Assert.True(result[0].Success);
        Assert.False(result[1].Success);
        Assert.False(result[2].Success);
        Assert.Contains("is full", result[1].Error);
    }

    [Fact]
    public void PreviewFinalAmount_ReturnsTheFrozenFeeWhenNoDiscountChosen()
    {
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass(snapFee: 3_500_000));

        Assert.Equal(3_500_000m, CreateService().PreviewFinalAmount(5, null));
    }
}
