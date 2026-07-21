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

    private static Class AClass(decimal snapFee = 3_500_000) => new()
    {
        ClassId = 5,
        CourseId = 3,
        Name = "A1-K01",
        MaxStudents = 20,
        Status = "ACTIVE",
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
}
