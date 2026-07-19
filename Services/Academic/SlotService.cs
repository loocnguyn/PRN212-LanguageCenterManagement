using BusinessObjects;
using Repositories;

namespace Services;

public class SlotService : ISlotService
{
    private readonly ISlotRepository _repo = new SlotRepository();

    public List<Slot> GetAll() => _repo.GetAll();
    public Slot? GetById(int id) => _repo.GetById(id);
    public void Save(Slot entity) => _repo.Save(entity);
    public void Update(Slot entity) => _repo.Update(entity);
    public void Delete(int id) => _repo.Delete(id);
}
