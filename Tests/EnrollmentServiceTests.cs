using BusinessObjects;
using Moq;
using Repositories;
using Services;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for the enrollment guard clauses (student/class existence, capacity, tuition
/// validity). All five repositories are mocked, so these run with no database.
/// </summary>
public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _enrollmentRepo = new();
    private readonly Mock<IClassRepository> _classRepo = new();
    private readonly Mock<IStudentRepository> _studentRepo = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<ICourseRepository> _courseRepo = new();

    private EnrollmentService CreateService() => new(
        _enrollmentRepo.Object, _classRepo.Object, _studentRepo.Object,
        _invoiceRepo.Object, _courseRepo.Object);

    private static Student AStudent() => new() { StudentId = 1, FullName = "Test Student" };
    private static Class AClass() => new() { ClassId = 5, CourseId = 3, Name = "A1-K01", MaxStudents = 20, Status = "ACTIVE" };
    private static Course ACourse() => new() { CourseId = 3, Name = "English A1", TuitionFee = 3_500_000 };

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
    public void Enroll_ThrowsWhenCourseHasNoValidTuition()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns(AStudent());
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass());
        _courseRepo.Setup(r => r.GetById(3)).Returns(new Course { CourseId = 3, Name = "Free", TuitionFee = 0 });
        var service = CreateService();

        var ex = Assert.Throws<InvalidOperationException>(() => service.Enroll(1, 5));
        Assert.Contains("does not have a valid tuition fee", ex.Message);
    }

    [Fact]
    public void Enroll_ThrowsWhenClassIsFull()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns(AStudent());
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass());
        _courseRepo.Setup(r => r.GetById(3)).Returns(ACourse());
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
        _courseRepo.Setup(r => r.GetById(3)).Returns(ACourse());
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

    [Fact]
    public void Enroll_ThrowsWhenStudentAlreadyActivelyEnrolled()
    {
        _studentRepo.Setup(r => r.GetById(1)).Returns(AStudent());
        _classRepo.Setup(r => r.GetById(5)).Returns(AClass());
        _courseRepo.Setup(r => r.GetById(3)).Returns(ACourse());
        _enrollmentRepo.Setup(r => r.CountByClassId(5)).Returns(3);
        _enrollmentRepo.Setup(r => r.GetByStudentAndClass(1, 5))
            .Returns(new Enrollment { StudentId = 1, ClassId = 5, Status = "ACTIVE" });
        var service = CreateService();

        var ex = Assert.Throws<InvalidOperationException>(() => service.Enroll(1, 5));
        Assert.Contains("already enrolled", ex.Message);
    }
}
