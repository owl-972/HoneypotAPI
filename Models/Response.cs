using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoneypotAPI.Models
{
    public class Response
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequestId { get; set; }

        [Required]
        public int ResponseStatus { get; set; }

        [Column(TypeName = "TEXT")]
        public string ResponsePayload { get; set; }

        [Required]
        public int ResponseTime { get; set; } // in milliseconds

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("RequestId")]
        public Request Request { get; set; }
    }
}