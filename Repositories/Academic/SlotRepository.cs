using BusinessObjects;
using DataAccessObjects;

namespace Repositories;

public class SlotRepository : ISlotRepository
{
    public List<Slot> GetAll() => SlotDAO.GetAll();
    public Slot? GetById(int id) => SlotDAO.GetById(id);
    public void Save(Slot entity) => SlotDAO.Save(entity);
    public void Update(Slot entity) => SlotDAO.Update(entity);
    public void Delete(int id) => SlotDAO.Delete(id);
}
