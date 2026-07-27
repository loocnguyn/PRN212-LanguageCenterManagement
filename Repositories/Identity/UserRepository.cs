using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

// UserRepository — thin pass-through from the service layer to UserDAO.

public class UserRepository : IUserRepository
{
    public List<User> GetAll() => UserDAO.GetAll();

    public User? GetById(int id) => UserDAO.GetById(id);

    public User? GetByEmail(string email) => UserDAO.GetByEmail(email);

    public bool IsEmailTaken(string email, int? exceptUserId = null)
        => UserDAO.IsEmailTaken(email, exceptUserId);

    public void Save(User user) => UserDAO.Save(user);

    public void Update(User user) => UserDAO.Update(user);

    public void Delete(int id) => UserDAO.Delete(id);
}
