using BusinessObjects;

namespace Services;

// ISlotService — service contract for Slot operations.

public interface ISlotService
{
    List<Slot> GetAll();
    Slot? GetById(int id);
    void Save(Slot entity);
    void Update(Slot entity);
    void Delete(int id);
}
