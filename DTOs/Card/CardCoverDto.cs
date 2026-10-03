namespace AvecADeskApi.DTOs.Card
{
    public class CardCoverResponse
    {
        public int CardID { get; set; }
        public string? Color { get; set; }
        public string? HexCode { get; set; }
        public string? TextHexCode { get; set; }
        public string? ImageUrl { get; set; }
        public string Size { get; set; } = "normal";
        public string Brightness { get; set; } = "light";
    }

    public class SaveCardCoverRequest
    {
        public int CardID { get; set; }
        public string? Color { get; set; }
        public string? ImageUrl { get; set; }
        public string? Size { get; set; }
        public string? Brightness { get; set; }
    }

    public class CardCoverColorResponse
    {
        public string ColorKey { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string HexCode { get; set; } = string.Empty;
        public string TextHexCode { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
