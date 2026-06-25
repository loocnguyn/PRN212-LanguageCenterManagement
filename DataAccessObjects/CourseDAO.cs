using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class CourseDAO
{
    public static List<Course> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Courses.ToList();
    }

    public static Course? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Courses.FirstOrDefault(x => x.CourseId == id);
    }

    public static void Save(Course entity)
    {
        using var context = new LanguageCenterContext();
        context.Courses.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Course entity)
    {
        using var context = new LanguageCenterContext();
        context.Courses.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.Courses.Find(id);
        if (entity != null)
        {
            context.Courses.Remove(entity);
            context.SaveChanges();
        }
    }
}

