using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Document.Repository.Models.Entities
{
    public class Notice
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Notice Title is required.")]
        public required string Title { get; set; }


        [Required(ErrorMessage = "Notice Upload is required.")]
        public string? FilePath { get; set; }

        [NotMapped]

        public IFormFile? File { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; }

    }
}
