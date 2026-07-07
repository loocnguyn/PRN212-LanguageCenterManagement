using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class UserRepository : IUserRepository
{
    public List<User> GetAll() => UserDAO.GetAll();

    public User? GetById(int id) => UserDAO.GetById(id);

    public User? GetByUsername(string username) => UserDAO.GetByUsername(username);

    public void Save(User user) => UserDAO.Save(user);

    public void Update(User user) => UserDAO.Update(user);

    public void Delete(int id) => UserDAO.Delete(id);

    public User? Login(string username, string passwordHash)
    {
        var user = UserDAO.GetByUsername(username);
        if (user == null || !user.IsActive) return null;
        return user.PasswordHash == passwordHash ? user : null;
    }
}
