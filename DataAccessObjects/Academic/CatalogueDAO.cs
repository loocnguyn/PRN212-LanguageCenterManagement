using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

// ============================================================
//  CatalogueDAO — the centre's Languages and their Levels.
//  CONTENTS:
//    1. Reads              — languages, levels per language
//    2. Language CRUD
//    3. Level CRUD
//    4. Usage counts       — what would break if a row were deleted
//
//  Reference data driving the course dialog: pick a language, then one of that
//  language's levels. Levels are per-language because "N5" only means anything
//  for Japanese and "B1" only for the CEFR languages.
//
//  Levels list by level_id, i.e. the order they were added — so a language's
//  levels should be entered beginner-first.
// ============================================================
public class CatalogueDAO
{
    // ---- 1. Reads ----------------------------------------------
    public static List<Language> GetLanguages()
    {
        using var context = new LanguageCenterContext();
        return context.Languages.OrderBy(l => l.Name).ToList();
    }

    public static Language? GetLanguage(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Languages.FirstOrDefault(l => l.LanguageId == id);
    }

    /// <summary>Levels belonging to one language, in the order they were added.</summary>
    public static List<Level> GetLevels(int languageId)
    {
        using var context = new LanguageCenterContext();
        return context.Levels
            .Where(l => l.LanguageId == languageId)
            .OrderBy(l => l.LevelId)
            .ToList();
    }

    public static List<Level> GetAllLevels()
    {
        using var context = new LanguageCenterContext();
        return context.Levels
            .Include(l => l.Language)
            .OrderBy(l => l.Language.Name).ThenBy(l => l.LevelId)
            .ToList();
    }

    // ---- 2. Language CRUD --------------------------------------
    public static bool LanguageNameExists(string name, int? excludeId = null)
    {
        using var context = new LanguageCenterContext();
        return context.Languages.Any(l => l.Name == name && (excludeId == null || l.LanguageId != excludeId));
    }

    public static void SaveLanguage(Language entity)
    {
        using var context = new LanguageCenterContext();
        context.Languages.Add(entity);
        context.SaveChanges();
    }

    public static void UpdateLanguage(Language entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Languages.Find(entity.LanguageId);
        if (existing == null) return;
        existing.Name = entity.Name;
        context.SaveChanges();
    }

    public static void DeleteLanguage(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Languages.Find(id);
        if (existing == null) return;
        context.Languages.Remove(existing);
        context.SaveChanges();
    }

    // ---- 3. Level CRUD -----------------------------------------
    public static Level? GetLevel(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Levels.Include(l => l.Language).FirstOrDefault(l => l.LevelId == id);
    }

    public static bool LevelNameExists(int languageId, string name, int? excludeId = null)
    {
        using var context = new LanguageCenterContext();
        return context.Levels.Any(l => l.LanguageId == languageId && l.Name == name
                                       && (excludeId == null || l.LevelId != excludeId));
    }

    public static void SaveLevel(Level entity)
    {
        using var context = new LanguageCenterContext();
        context.Levels.Add(entity);
        context.SaveChanges();
    }

    public static void UpdateLevel(Level entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Levels.Find(entity.LevelId);
        if (existing == null) return;
        existing.Name = entity.Name;
        context.SaveChanges();
    }

    public static void DeleteLevel(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Levels.Find(id);
        if (existing == null) return;
        context.Levels.Remove(existing);
        context.SaveChanges();
    }

    // ---- 4. Usage counts ---------------------------------------
    // Checked before deleting: the FKs would refuse anyway, but a counted,
    // named reason beats a raw constraint violation in the UI.

    public static int CountCoursesUsingLanguage(int languageId)
    {
        using var context = new LanguageCenterContext();
        return context.Courses.Count(c => c.LanguageId == languageId);
    }

    public static int CountLevelsInLanguage(int languageId)
    {
        using var context = new LanguageCenterContext();
        return context.Levels.Count(l => l.LanguageId == languageId);
    }

    public static int CountCoursesUsingLevel(int levelId)
    {
        using var context = new LanguageCenterContext();
        return context.Courses.Count(c => c.LevelId == levelId);
    }
}
