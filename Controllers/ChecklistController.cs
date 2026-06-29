using AvecADeskApi.DTOs;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.Cardlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    
        [ApiController]
        [Route("api/[controller]")]
        [Authorize]
        public class ChecklistController : ControllerBase
        {
            private readonly IChecklistRepository _repo;
            private readonly ILogger<ChecklistController> _logger;

            public ChecklistController(IChecklistRepository repo, ILogger<ChecklistController> logger)
            {
                _repo = repo;
                _logger = logger;
            }

           
            [HttpGet("card/{cardId:int}")]
        public async Task<IActionResult> GetByCard(int cardId)
        {
            try
            {
                List<ChecklistModel> result = await _repo.GetByCardIdAsync(cardId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checklists for CardId: {CardId}", cardId);
                return StatusCode(500, new { message = "Error retrieving checklists" });
            }
        }

        
        [HttpPost("create")]
            public async Task<IActionResult> Create([FromBody] CreateChecklistRequest request)
            {
                if (string.IsNullOrWhiteSpace(request.ChecklistTitle))
                    return BadRequest(new { message = "Checklist title is required." });

                try
                {
                    var id = await _repo.CreateChecklistAsync(request);
                    return Ok(new { message = "Checklist created successfully", checklistId = id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating checklist for CardId: {CardId}", request.CardID);
                    return StatusCode(500, new { message = "Error creating checklist" });
                }
            }

            
            [HttpDelete("delete/{checklistId:int}")]
            public async Task<IActionResult> Delete(int checklistId)
            {
                try
                {
                    var deleted = await _repo.DeleteChecklistAsync(checklistId);
                    if (!deleted) return NotFound(new { message = "Checklist not found." });
                    return Ok(new { message = "Checklist deleted successfully." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting checklist: {ChecklistId}", checklistId);
                    return StatusCode(500, new { message = "Error deleting checklist" });
                }
            }


        //[HttpPost("item/create")]
        //public async Task<IActionResult> CreateItem([FromBody] CreateChecklistItemRequest request)
        //{
        //    if (string.IsNullOrWhiteSpace(request.ItemName))
        //        return BadRequest(new { message = "Item name is required." });

        //    try
        //    {
        //        var id = await _repo.CreateChecklistItemAsync(request);
        //        return Ok(new { message = "Item created successfully", checklistItemId = id });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error creating checklist item for ChecklistId: {ChecklistId}", request.ChecklistID);
        //        return StatusCode(500, new { message = "Error creating checklist item" });
        //    }
        //}
        [HttpPost("item/create")]
        public async Task<IActionResult> CreateItem([FromBody] CreateChecklistItemRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ItemName))
                return BadRequest(new { message = "Item name is required." });

            if (request.ChecklistID <= 0)
                return BadRequest(new { message = "Valid ChecklistID is required." });

            if (request.AssignedUserID <= 0)         
                return BadRequest(new { message = "Valid AssignedUserID is required." });

            try
            {
                var id = await _repo.CreateChecklistItemAsync(request);
                return Ok(new { message = "Item created successfully", checklistItemId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checklist item for ChecklistId: {ChecklistId}", request.ChecklistID);
                return StatusCode(500, new { message = "Error creating checklist item" });
            }
        }

        //[HttpPut("item/update-assignee")]
        //public async Task<IActionResult> UpdateItemAssignee([FromBody] UpdateItemAssigneeRequest request)
        //{
        //    if (request.ChecklistItemID <= 0 || request.AssignedUserID <= 0)
        //        return BadRequest(new { message = "Valid ChecklistItemID and AssignedUserID are required." });

        //    try
        //    {
        //        await _repo.UpdateChecklistItemAssigneeAsync(request.ChecklistItemID, request.AssignedUserID);
        //        return Ok(new { message = "Assignee updated successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error updating assignee for ItemId: {ItemId}", request.ChecklistItemID);
        //        return StatusCode(500, new { message = "Error updating checklist item assignee" });
        //    }
        //}

        [HttpPatch("item/toggle/{checklistItemId:int}")]
            public async Task<IActionResult> ToggleItem(int checklistItemId, [FromBody] UpdateChecklistItemStatusRequest request)
            {
                try
                {
                    var updated = await _repo.UpdateChecklistItemStatusAsync(checklistItemId, request.IsCompleted);
                    if (!updated) return NotFound(new { message = "Checklist item not found." });
                    return Ok(new { message = "Status updated successfully." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error toggling checklist item: {ItemId}", checklistItemId);
                    return StatusCode(500, new { message = "Error updating checklist item status" });
            }
        }


        [HttpDelete("item/delete/{checklistItemId:int}")]
        public async Task<IActionResult> DeleteItem(int checklistItemId)
        {
            try
            {
                var deleted = await _repo.DeleteChecklistItemAsync(checklistItemId);
                if (!deleted) return NotFound(new { message = "Checklist item not found." });
                return Ok(new { message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting checklist item: {ItemId}", checklistItemId);
                return StatusCode(500, new { message = "Error deleting checklist item" });
            }
        }

        [HttpPut("item/update/{checklistItemId:int}")]
        public async Task<IActionResult> UpdateItemName(int checklistItemId, [FromBody] UpdateChecklistItemNameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ItemName))
                return BadRequest(new { message = "Item name is required." });

            try
            {
                var updated = await _repo.UpdateChecklistItemNameAsync(checklistItemId, request.ItemName);
                if (!updated) return NotFound(new { message = "Checklist item not found." });
                return Ok(new { message = "Item updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating checklist item name: {ItemId}", checklistItemId);
                return StatusCode(500, new { message = "Error updating checklist item" });
            }
        }

    }
}

