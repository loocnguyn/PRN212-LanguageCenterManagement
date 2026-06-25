using BusinessObjects;
using Repositories;

namespace Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _repo = new EnrollmentRepository();

    public List<Enrollment> GetAll() => _repo.GetAll();
    public Enrollment? GetById(int id) => _repo.GetById(id);
    public void Save(Enrollment entity) => _repo.Save(entity);
    public void Update(Enrollment entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}


