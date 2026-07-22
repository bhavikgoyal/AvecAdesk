namespace AvecADeskApi.DTOs.Card
{
    public class CardMemberResponse
    {
        public int CardLabelID { get; set; }
        public int CardID { get; set; }
        public int UserID { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class AddCardMemberRequest
    {
        public int UserID { get; set; }
    }
}