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
        var existing = context.Students.Find(entity.StudentId)
            ?? throw new Exception("Student not found.");
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Students.Find(id)
            ?? throw new Exception("Student not found.");
        context.Students.Remove(existing);
        context.SaveChanges();
    }
}
