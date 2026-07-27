using MyModbus.UI.Models;
using MyModbus.UI.Repositories;

namespace MyModbus.UI.Services
{
    public class CollectDataService
    {
        CollectDataRepository _collectDataRepository;
        public CollectDataService(CollectDataRepository collectDataRepository)
        {
            _collectDataRepository = collectDataRepository;
        }

        public int Insert(CollectData collectData)
        {
            return _collectDataRepository.Insert(collectData);
        }
    }
}
