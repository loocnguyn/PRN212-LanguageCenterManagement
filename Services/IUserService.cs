using BusinessObjects;

namespace Services;

public interface IUserService
{
    List<User> GetAll();
    User? GetById(int id);
    User? GetByUsername(string username);
    void Save(User user, string plainPassword);
    void Update(User user);
    void UpdatePassword(int id, string newPlainPassword);
    void Delete(int id);
    User? Login(string username, string password);
}
