using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class StaffDAO
{
    public static List<Staff> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Staff.ToList();
    }

    public static Staff? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Staff.FirstOrDefault(x => x.StaffId == id);
    }

    public static void Save(Staff entity)
    {
        using var context = new LanguageCenterContext();
        context.Staff.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Staff entity)
    {
        using var context = new LanguageCenterContext();
        context.Staff.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.Staff.Find(id);
        if (entity != null)
        {
            context.Staff.Remove(entity);
            context.SaveChanges();
        }
    }
}


