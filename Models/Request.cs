using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoneypotAPI.Models
{
    public class Request
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(45)]
        public string IpAddress { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(500)]
        public string Endpoint { get; set; }

        [Required]
        [MaxLength(10)]
        public string HttpMethod { get; set; }

        [Column(TypeName = "TEXT")]
        public string Headers { get; set; }

        [Column(TypeName = "TEXT")]
        public string? Payload { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Response Response { get; set; }
    }
}