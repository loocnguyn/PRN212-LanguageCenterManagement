using BusinessObjects;

namespace Repositories;

// IStudentRewardRepository — repository contract for StudentReward persistence.
public interface IStudentRewardRepository
{
    List<StudentReward> GetAll();
    List<StudentReward> GetBySemesterAndCourse(int semesterId, int courseId);
    bool Exists(int studentId, int semesterId, int courseId);
    void Save(StudentReward entity);
}
