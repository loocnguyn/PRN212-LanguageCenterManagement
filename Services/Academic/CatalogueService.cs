using BusinessObjects;
using DataAccessObjects;

namespace Services;

// ============================================================
//  CatalogueService — languages the centre teaches and the levels of each.
//
//  Rules enforced here rather than in the windows, so the catalogue stays
//  consistent whatever calls it:
//    * A language name is unique; a level name is unique WITHIN its language
//      (both "English B1" and "German B1" are fine).
//    * Nothing still in use can be deleted — the counts say what is blocking.
// ============================================================

public interface ICatalogueService
{
    List<Language> GetLanguages();
    Language? GetLanguage(int id);

    /// <summary>Levels for one language, in the order they were added.</summary>
    List<Level> GetLevels(int languageId);

    List<Level> GetAllLevels();
    Level? GetLevel(int id);

    void SaveLanguage(Language entity);
    void UpdateLanguage(Language entity);

    /// <summary>Throws InvalidOperationException if the language is still referenced.</summary>
    void DeleteLanguage(int id);

    void SaveLevel(Level entity);
    void UpdateLevel(Level entity);

    /// <summary>Throws InvalidOperationException if any course still uses the level.</summary>
    void DeleteLevel(int id);

    int CountCoursesUsingLanguage(int languageId);
    int CountLevelsInLanguage(int languageId);
    int CountCoursesUsingLevel(int levelId);
}

public class CatalogueService : ICatalogueService
{
    // ---- Reads -------------------------------------------------
    public List<Language> GetLanguages() => CatalogueDAO.GetLanguages();
    public Language? GetLanguage(int id) => CatalogueDAO.GetLanguage(id);
    public List<Level> GetLevels(int languageId) => CatalogueDAO.GetLevels(languageId);
    public List<Level> GetAllLevels() => CatalogueDAO.GetAllLevels();
    public Level? GetLevel(int id) => CatalogueDAO.GetLevel(id);

    public int CountCoursesUsingLanguage(int languageId) => CatalogueDAO.CountCoursesUsingLanguage(languageId);
    public int CountLevelsInLanguage(int languageId) => CatalogueDAO.CountLevelsInLanguage(languageId);
    public int CountCoursesUsingLevel(int levelId) => CatalogueDAO.CountCoursesUsingLevel(levelId);

    // ---- Language writes ---------------------------------------
    public void SaveLanguage(Language entity)
    {
        ValidateLanguage(entity, excludeId: null);
        CatalogueDAO.SaveLanguage(entity);
    }

    public void UpdateLanguage(Language entity)
    {
        ValidateLanguage(entity, excludeId: entity.LanguageId);
        CatalogueDAO.UpdateLanguage(entity);
    }

    private static void ValidateLanguage(Language entity, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new InvalidOperationException("Language name is required.");

        entity.Name = entity.Name.Trim();

        if (CatalogueDAO.LanguageNameExists(entity.Name, excludeId))
            throw new InvalidOperationException($"\"{entity.Name}\" is already in the catalogue.");
    }

    public void DeleteLanguage(int id)
    {
        var courses = CatalogueDAO.CountCoursesUsingLanguage(id);
        var levels = CatalogueDAO.CountLevelsInLanguage(id);

        if (courses > 0 || levels > 0)
        {
            var parts = new List<string>();
            if (courses > 0) parts.Add($"{courses} course(s)");
            if (levels > 0) parts.Add($"{levels} level(s)");

            throw new InvalidOperationException(
                $"Cannot delete this language — it still has {string.Join(" and ", parts)}.\n" +
                "Remove those first.");
        }

        CatalogueDAO.DeleteLanguage(id);
    }

    // ---- Level writes ------------------------------------------
    public void SaveLevel(Level entity)
    {
        ValidateLevel(entity, excludeId: null);
        CatalogueDAO.SaveLevel(entity);
    }

    public void UpdateLevel(Level entity)
    {
        ValidateLevel(entity, excludeId: entity.LevelId);
        CatalogueDAO.UpdateLevel(entity);
    }

    private static void ValidateLevel(Level entity, int? excludeId)
    {
        if (entity.LanguageId <= 0)
            throw new InvalidOperationException("Pick the language this level belongs to.");

        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new InvalidOperationException("Level name is required.");

        entity.Name = entity.Name.Trim();

        // Scoped to the language: "B1" may exist for both English and German.
        if (CatalogueDAO.LevelNameExists(entity.LanguageId, entity.Name, excludeId))
            throw new InvalidOperationException($"This language already has a level named \"{entity.Name}\".");
    }

    public void DeleteLevel(int id)
    {
        var courses = CatalogueDAO.CountCoursesUsingLevel(id);
        if (courses > 0)
            throw new InvalidOperationException(
                $"Cannot delete this level — {courses} course(s) still use it.");

        CatalogueDAO.DeleteLevel(id);
    }
}
