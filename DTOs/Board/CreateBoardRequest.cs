using System.ComponentModel.DataAnnotations;

namespace AvecADeskApi.DTOs.Board
{
    public class CreateBoardRequest
    {
        [Required]
        [MaxLength(255)]
        public string BoardName { get; set; } = string.Empty;
    }
}