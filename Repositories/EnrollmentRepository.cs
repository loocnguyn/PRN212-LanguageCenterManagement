using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    public List<Enrollment> GetAll() => EnrollmentDAO.GetAll();
    public Enrollment? GetById(int id) => EnrollmentDAO.GetById(id);
    public void Save(Enrollment entity) => EnrollmentDAO.Save(entity);
    public void Update(Enrollment entity) => EnrollmentDAO.Update(entity);
    public void Delete(int id) => EnrollmentDAO.Delete(id);
}


