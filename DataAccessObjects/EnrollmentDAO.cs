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
        var existing = context.Enrollments.Find(entity.EnrollmentId)
            ?? throw new Exception("Enrollment not found.");
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Enrollments.Find(id)
            ?? throw new Exception("Enrollment not found.");
        context.Enrollments.Remove(existing);
        context.SaveChanges();
    }
}
