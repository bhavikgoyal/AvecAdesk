using System;

namespace AvecADeskApi.DTOs.Card
{
   
    public class CardResponse
    {
        public int CardID { get; set; }
        public int? ListID { get; set; }
        public string? CardTitle { get; set; }
        public string? Description { get; set; }
        public int? Position { get; set; }
        public string? Color { get; set; }
        public DateTime? DueDate { get; set; }
        public int? CreatedUserID { get; set; }
        public string? CreatedUserName { get; set; }
        public int? AssignedUserID { get; set; }
        public string? AssignedUserName { get; set; }
        public bool? IsArchived { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? CardStatusID { get; set; }
        public string? StatusName { get; set; }
        public int? CPID { get; set; }
        public string? PriorityName { get; set; }
        public string? SheetType { get; set; }
        // public string? TrelloID { get; set; }
        public int ChecklistTotal { get; set; }
        public int ChecklistCompleted { get; set; }
    }

    
    public class BoardColumnResponse
    {
        public int CardStatusID { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<CardResponse> Cards { get; set; } = new();
    }

    public class CreateCardRequest
    {
        public int? ListID { get; set; }
        public string CardTitle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public DateTime? DueDate { get; set; }
        public int? AssignedUserID { get; set; }
        public int CardStatusID { get; set; }
        public int? CPID { get; set; }
        public string? SheetType { get; set; }
    }

    public class UpdateCardRequest
    {
        public int CardID { get; set; }
        public string CardTitle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public DateTime? DueDate { get; set; }
        public int? AssignedUserID { get; set; }
        public int? CardStatusID { get; set; }
        public int? CPID { get; set; }
    }

  
    public class MoveCardRequest
    {
        public int CardID { get; set; }
        public int NewCardStatusID { get; set; }
        public int NewPosition { get; set; }
    }

    public class CardStatusResponse
    {
        public int CardStatusID { get; set; }
        public string? StatusName { get; set; }
    }
}
