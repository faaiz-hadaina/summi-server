using System.ComponentModel.DataAnnotations;

namespace SummiServer.Models
{
    public class Contact
    {
        [Required]
        public string Id { get; set; } = System.Guid.NewGuid().ToString();

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;
    }
}
