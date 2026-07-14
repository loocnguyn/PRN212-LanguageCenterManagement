using BusinessObjects;
using Moq;
using Repositories;
using Services;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for the per-course grade-weight aggregation that the (upcoming) GradeType
/// management window relies on to validate that a course's weights total 100%.
/// The repository is mocked, so these run with no database.
/// </summary>
public class GradeTypeServiceTests
{
    private static GradeType Gt(int id, decimal weight) =>
        new() { GradeTypeId = id, CourseId = 1, Name = $"GT{id}", WeightPercent = weight };

    [Fact]
    public void GetTotalWeightPercent_SumsAllWeightsForCourse()
    {
        var repo = new Mock<IGradeTypeRepository>();
        repo.Setup(r => r.GetByCourseId(1))
            .Returns(new List<GradeType> { Gt(1, 10), Gt(2, 30), Gt(3, 60) });

        var service = new GradeTypeService(repo.Object);

        Assert.Equal(100, service.GetTotalWeightPercent(1));
    }

    [Fact]
    public void GetTotalWeightPercent_ExcludesTheGivenGradeType()
    {
        var repo = new Mock<IGradeTypeRepository>();
        repo.Setup(r => r.GetByCourseId(1))
            .Returns(new List<GradeType> { Gt(1, 10), Gt(2, 30), Gt(3, 60) });

        var service = new GradeTypeService(repo.Object);

        // Excluding GT #2 (weight 30) — e.g. the row currently being edited — leaves 70.
        Assert.Equal(70, service.GetTotalWeightPercent(1, excludeGradeTypeId: 2));
    }

    [Fact]
    public void GetTotalWeightPercent_ReturnsZeroWhenCourseHasNoGradeTypes()
    {
        var repo = new Mock<IGradeTypeRepository>();
        repo.Setup(r => r.GetByCourseId(99)).Returns(new List<GradeType>());

        var service = new GradeTypeService(repo.Object);

        Assert.Equal(0, service.GetTotalWeightPercent(99));
    }
}
