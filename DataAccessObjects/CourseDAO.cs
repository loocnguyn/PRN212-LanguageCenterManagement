using BusinessObjects;

namespace DataAccessObjects;

public class CourseDAO
{
    public static List<Course> GetAll()
    {
        try
        {
            using var context = new LanguageCenterContext();
            return context.Courses.ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving courses: {ex.Message}", ex);
        }
    }

    public static Course? GetById(int id)
    {
        try
        {
            using var context = new LanguageCenterContext();
            return context.Courses.FirstOrDefault(x => x.CourseId == id);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving course {id}: {ex.Message}", ex);
        }
    }

    public static void Save(Course entity)
    {
        try
        {
            Validate(entity);
            using var context = new LanguageCenterContext();
            context.Courses.Add(entity);
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error saving course: {ex.Message}", ex);
        }
    }

    public static void Update(Course entity)
    {
        try
        {
            Validate(entity);
            using var context = new LanguageCenterContext();
            var existing = context.Courses.Find(entity.CourseId);
            if (existing == null)
                throw new Exception($"Course with ID {entity.CourseId} not found for update.");

            var originalCreatedAt = existing.CreatedAt;
            context.Entry(existing).CurrentValues.SetValues(entity);
            existing.CreatedAt = originalCreatedAt;
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error updating course {entity.CourseId}: {ex.Message}", ex);
        }
    }

    public static void Delete(int id)
    {
        try
        {
            using var context = new LanguageCenterContext();
            var existing = context.Courses.Find(id);
            if (existing == null)
                throw new Exception($"Course with ID {id} not found for delete.");
            context.Courses.Remove(existing);
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting course {id}: {ex.Message}", ex);
        }
    }

    private static void Validate(Course entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("Course code is required.");

        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("Course name is required.");

        if (string.IsNullOrWhiteSpace(entity.Language))
            throw new ArgumentException("Course language is required.");

        if (entity.DurationSessions <= 0)
            throw new ArgumentException("DurationSessions must be greater than 0.");

        if (entity.TuitionFee < 0)
            throw new ArgumentException("Tuition fee cannot be negative.");
    }
}
