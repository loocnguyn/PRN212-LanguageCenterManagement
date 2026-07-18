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

    public void Save(User user, string plainPassword)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        _repo.Save(user);
    }

    public void UpdatePassword(int id, string newPlainPassword)
    {
        var user = _repo.GetById(id);
        if (user == null) return;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPlainPassword);
        _repo.Update(user);
    }

    public User? Login(string username, string password)
    {
        var user = _repo.GetByUsername(username);
        if (user == null || !user.IsActive) return null;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }
}
