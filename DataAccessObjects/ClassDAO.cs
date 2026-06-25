using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class ClassDAO
{
    public static List<Class> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Classes.ToList();
    }

    public static Class? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Classes.FirstOrDefault(x => x.ClassId == id);
    }

    public static void Save(Class entity)
    {
        using var context = new LanguageCenterContext();
        context.Classes.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Class entity)
    {
        using var context = new LanguageCenterContext();
        context.Classes.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.Classes.Find(id);
        if (entity != null)
        {
            context.Classes.Remove(entity);
            context.SaveChanges();
        }
    }
}


