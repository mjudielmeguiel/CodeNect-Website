using System.ComponentModel.DataAnnotations;

namespace CodeNect_Website.Models
{
    public class RegisterModel
    {
        [Required]
        public string ACCOUNT { get; set; } = string.Empty;

        public string? ADDRESS { get; set; }

        [Required]
        public string CONTACT { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string EMAIL { get; set; } = string.Empty;

        [Required]
        public string USERNAME { get; set; } = string.Empty; // gagamitin natin sa form, ilalagay sa RNAME
        // O kaya kung gusto mong RNAME na ang label sa form: palitan natin sa ibaba

        [Required, DataType(DataType.Password)]
        public string PASSWORD { get; set; } = string.Empty;

        public string? SERIAL_NUMBER { get; set; }
    }
}