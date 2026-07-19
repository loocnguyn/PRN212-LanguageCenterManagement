using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// StudentRewardRepository — thin pass-through from the service layer to StudentRewardDAO.
public class StudentRewardRepository : IStudentRewardRepository
{
    public List<StudentReward> GetAll() => StudentRewardDAO.GetAll();
    public List<StudentReward> GetBySemesterAndCourse(int semesterId, int courseId)
        => StudentRewardDAO.GetBySemesterAndCourse(semesterId, courseId);
    public bool Exists(int studentId, int semesterId, int courseId)
        => StudentRewardDAO.Exists(studentId, semesterId, courseId);
    public void Save(StudentReward entity) => StudentRewardDAO.Save(entity);
}
