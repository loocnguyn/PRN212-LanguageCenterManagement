using BusinessObjects;

namespace DataAccessObjects;

public class SlotDAO
{
    public static List<Slot> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Slots.OrderBy(s => s.SlotNo).ToList();
    }

    public static Slot? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Slots.FirstOrDefault(s => s.SlotId == id);
    }

    public static void Save(Slot slot)
    {
        using var context = new LanguageCenterContext();
        context.Slots.Add(slot);
        context.SaveChanges();
    }

    public static void Update(Slot slot)
    {
        using var context = new LanguageCenterContext();
        context.Slots.Update(slot);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var slot = context.Slots.Find(id);
        if (slot != null)
        {
            context.Slots.Remove(slot);
            context.SaveChanges();
        }
    }
}
