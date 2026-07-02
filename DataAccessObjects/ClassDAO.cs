using BusinessObjects;

namespace DataAccessObjects;

public class ClassDAO
{
    public static List<Class> GetAll()
    {
        try
        {
            using var context = new LanguageCenterContext();
            return context.Classes.ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving classes: {ex.Message}", ex);
        }
    }

    public static Class? GetById(int id)
    {
        try
        {
            using var context = new LanguageCenterContext();
            return context.Classes.FirstOrDefault(x => x.ClassId == id);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving class {id}: {ex.Message}", ex);
        }
    }

    public static void Save(Class entity)
    {
        try
        {
            Validate(entity);
            using var context = new LanguageCenterContext();
            context.Classes.Add(entity);
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error saving class: {ex.Message}", ex);
        }
    }

    public static void Update(Class entity)
    {
        try
        {
            Validate(entity);
            using var context = new LanguageCenterContext();
            var existing = context.Classes.Find(entity.ClassId);
            if (existing == null)
                throw new Exception($"Class with ID {entity.ClassId} not found for update.");

            var originalCreatedAt = existing.CreatedAt;
            context.Entry(existing).CurrentValues.SetValues(entity);
            existing.CreatedAt = originalCreatedAt;
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error updating class {entity.ClassId}: {ex.Message}", ex);
        }
    }

    public static void Delete(int id)
    {
        try
        {
            using var context = new LanguageCenterContext();
            var existing = context.Classes.Find(id);
            if (existing == null)
                throw new Exception($"Class with ID {id} not found for delete.");
            context.Classes.Remove(existing);
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting class {id}: {ex.Message}", ex);
        }
    }

    private static void Validate(Class entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("Class name is required.");

        if (entity.MaxStudents <= 0)
            throw new ArgumentException("MaxStudents must be greater than 0.");

        if (string.IsNullOrWhiteSpace(entity.Status))
            throw new ArgumentException("Class status is required.");

        if (entity.StartDate.HasValue && entity.EndDate.HasValue
            && entity.StartDate > entity.EndDate)
            throw new ArgumentException("Start date cannot be later than end date.");
    }
}
