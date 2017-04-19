using System.ComponentModel.DataAnnotations;

namespace LineBot.Models
{
    public class KeyDictionary
    {
        [Key]
        [MaxLength(50)]
        public string Key { get; set; }
        
        [Required]
        public string Value { get; set; }
    }
}
