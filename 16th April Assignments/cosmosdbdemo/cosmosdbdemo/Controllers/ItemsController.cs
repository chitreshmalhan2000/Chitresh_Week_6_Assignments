using cosmosdbdemo.Data;
using cosmosdbdemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace cosmosdbdemo.Controllers
{
    public class ItemsController : Controller
    {
        private readonly CosmosDbService _cosmosDbService;

        public ItemsController(CosmosDbService cosmosdbservice)
        {
            _cosmosDbService = cosmosdbservice;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _cosmosDbService.GetItemsAsync("SELECT * FROM c");
            return View(items);
        }

        public async Task<IActionResult> Details(string id)
        {
            var item = await _cosmosDbService.GetItemAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Itemmodel item)
        {
            if (ModelState.IsValid)
            {
                await _cosmosDbService.AddItemAsync(item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var item = await _cosmosDbService.GetItemAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Itemmodel item)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _cosmosDbService.UpdateItemAsync(id, item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var item = await _cosmosDbService.GetItemAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _cosmosDbService.DeleteItemAsync(id);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Search(string query)
        {
            var items = await _cosmosDbService.GetItemsAsync($"SELECT * FROM c WHERE CONTAINS(LOWER(c.name),'{query}')");
            return View("Index", items);
        }
    }
}

