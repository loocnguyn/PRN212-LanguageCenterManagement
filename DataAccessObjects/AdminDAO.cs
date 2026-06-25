using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class AdminDAO
{
    public static List<Admin> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Admins.ToList();
    }

    public static Admin? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Admins.FirstOrDefault(x => x.AdminId == id);
    }

    public static void Save(Admin entity)
    {
        using var context = new LanguageCenterContext();
        context.Admins.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Admin entity)
    {
        using var context = new LanguageCenterContext();
        context.Admins.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.Admins.Find(id);
        if (entity != null)
        {
            context.Admins.Remove(entity);
            context.SaveChanges();
        }
    }
}

