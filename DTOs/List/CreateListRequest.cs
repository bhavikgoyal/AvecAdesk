using System.ComponentModel.DataAnnotations;

namespace AvecADeskApi.DTOs.List
{
    public class CreateListRequest
    {
        [Required]
        public int BoardID { get; set; }

        [Required]
        [MaxLength(255)]
        public string ListName { get; set; } = string.Empty;
    }
}