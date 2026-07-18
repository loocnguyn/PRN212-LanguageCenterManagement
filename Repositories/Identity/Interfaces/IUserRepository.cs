using BusinessObjects;

namespace Repositories;

public interface IUserRepository
{
    List<User> GetAll();
    User? GetById(int id);
    User? GetByUsername(string username);
    void Save(User user);
    void Update(User user);
    void Delete(int id);
    User? Login(string username, string passwordHash);
}
