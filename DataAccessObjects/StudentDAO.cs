using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class StudentDAO
{
    public static List<Student> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Students.ToList();
    }

    public static Student? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Students.FirstOrDefault(x => x.StudentId == id);
    }

    public static void Save(Student entity)
    {
        using var context = new LanguageCenterContext();
        context.Students.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Student entity)
    {
        using var context = new LanguageCenterContext();
        context.Students.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.Students.Find(id);
        if (entity != null)
        {
            context.Students.Remove(entity);
            context.SaveChanges();
        }
    }
}

