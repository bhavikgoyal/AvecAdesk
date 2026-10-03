namespace AvecADeskApi.DTOs.Label
{
    public class LabelResponse
    {
        public int LabelID { get; set; }
        public int CardID { get; set; }
        public string LabelName { get; set; } = string.Empty;
        public string? Color { get; set; }
    }

    public class CreateLabelRequest
    {
        public int CardID { get; set; }
        public string LabelName { get; set; } = string.Empty;
        public string? Color { get; set; }
    }

    public class BoardLabelResponse
    {
        public int BoardLabelID { get; set; }
        public int? BoardID { get; set; }
        public string LabelName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool IsAssigned { get; set; }
        public int? CardLabelID { get; set; }
    }

    public class CreateBoardLabelRequest
    {
        public int CardID { get; set; }
        public string? LabelName { get; set; }
        public string? Color { get; set; }
        public bool AssignToCard { get; set; } = true;
    }

    public class UpdateBoardLabelRequest
    {
        public string? LabelName { get; set; }
        public string? Color { get; set; }
    }

    public class SetCardBoardLabelRequest
    {
        public int CardID { get; set; }
        public int BoardLabelID { get; set; }
        public bool IsAssigned { get; set; }
    }
}
