using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class TeacherDAO
{
    public static List<Teacher> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Teachers.ToList();
    }

    public static Teacher? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Teachers.FirstOrDefault(x => x.TeacherId == id);
    }

    public static void Save(Teacher entity)
    {
        using var context = new LanguageCenterContext();
        context.Teachers.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Teacher entity)
    {
        using var context = new LanguageCenterContext();
        context.Teachers.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.Teachers.Find(id);
        if (entity != null)
        {
            context.Teachers.Remove(entity);
            context.SaveChanges();
        }
    }
}

