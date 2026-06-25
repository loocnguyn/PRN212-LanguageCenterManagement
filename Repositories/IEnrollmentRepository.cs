using BusinessObjects;

namespace Repositories;

public interface IEnrollmentRepository
{
    List<Enrollment> GetAll();
    Enrollment? GetById(int id);
    void Save(Enrollment entity);
    void Update(Enrollment entity);
    void Delete(int id);
}


