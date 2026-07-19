using BusinessObjects;

namespace Repositories;

public interface ISlotRepository
{
    List<Slot> GetAll();
    Slot? GetById(int id);
    void Save(Slot entity);
    void Update(Slot entity);
    void Delete(int id);
}
