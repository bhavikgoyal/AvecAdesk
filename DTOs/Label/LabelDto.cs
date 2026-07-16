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
}
