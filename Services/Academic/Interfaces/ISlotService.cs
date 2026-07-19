using BusinessObjects;

namespace Services;

public interface ISlotService
{
    List<Slot> GetAll();
    Slot? GetById(int id);
    void Save(Slot entity);
    void Update(Slot entity);
    void Delete(int id);
}
