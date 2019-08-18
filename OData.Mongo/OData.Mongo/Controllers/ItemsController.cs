using Microsoft.AspNetCore.Mvc;
using OData.Mongo.Controllers.Abstract;
using OData.Mongo.Repositories;

namespace OData.Mongo.Controllers
{
    [Route("api/{itemType}")]
    [ApiController]
    public class ItemsController : ODataController
    {
        private readonly IItemsRepository _itemsRepository;

        public ItemsController(IItemsRepository itemsRepository)
        {
            _itemsRepository = itemsRepository;
        }

        [HttpGet("{id}")]
        public IActionResult Get(string itemType, string id)
        {
            return Ok(itemType);
        }

        [HttpGet]
        public IActionResult Get(string itemType)
        {
            return ODataResult(_itemsRepository.Get(itemType));
        }
    }
}
