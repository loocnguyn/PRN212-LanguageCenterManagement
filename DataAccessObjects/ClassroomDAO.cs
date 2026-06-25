using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class ClassroomDAO
{
    public static List<Classroom> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Classrooms.ToList();
    }

    public static Classroom? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Classrooms.FirstOrDefault(x => x.ClassroomId == id);
    }

    public static void Save(Classroom entity)
    {
        using var context = new LanguageCenterContext();
        context.Classrooms.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Classroom entity)
    {
        using var context = new LanguageCenterContext();
        context.Classrooms.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.Classrooms.Find(id);
        if (entity != null)
        {
            context.Classrooms.Remove(entity);
            context.SaveChanges();
        }
    }
}

