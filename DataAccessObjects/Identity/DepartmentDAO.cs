using BusinessObjects;

namespace DataAccessObjects;

// DepartmentDAO — EF data access for staff departments (static CRUD helpers).
public class DepartmentDAO
{
    public static List<Department> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Departments.OrderBy(d => d.Name).ToList();
    }

    public static Department? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Departments.FirstOrDefault(x => x.DepartmentId == id);
    }

    public static void Save(Department entity)
    {
        using var context = new LanguageCenterContext();
        context.Departments.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Department entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Departments.Find(entity.DepartmentId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Departments.Find(id);
        if (existing == null) return;
        context.Departments.Remove(existing);
        context.SaveChanges();
    }
}
