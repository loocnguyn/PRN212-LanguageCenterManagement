using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class EnrollmentDAO
{
    public static List<Enrollment> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Enrollments.ToList();
    }

    public static Enrollment? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Enrollments.FirstOrDefault(x => x.EnrollmentId == id);
    }

    public static void Save(Enrollment entity)
    {
        using var context = new LanguageCenterContext();
        context.Enrollments.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Enrollment entity)
    {
        using var context = new LanguageCenterContext();
        context.Enrollments.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.Enrollments.Find(id);
        if (entity != null)
        {
            context.Enrollments.Remove(entity);
            context.SaveChanges();
        }
    }
}

