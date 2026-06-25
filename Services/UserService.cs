using BusinessObjects;
using Repositories;

namespace Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repo = new UserRepository();

    public List<User> GetAll() => _repo.GetAll();

    public User? GetById(int id) => _repo.GetById(id);

    public User? GetByUsername(string username) => _repo.GetByUsername(username);

    public void Save(User user) => _repo.Save(user);

    public void Update(User user) => _repo.Update(user);

    public void Delete(int id) => _repo.Delete(id);

    public User? Login(string username, string password)
    {
        // TODO: thay bằng BCrypt.Verify khi team thống nhất hashing
        string passwordHash = password;
        return _repo.Login(username, passwordHash);
    }
}
