using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Document.Repository.Models.Entities
{
    public class SliderImage
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        public required string Title { get; set; }

        public string Description { get; set; }  = string.Empty;

        [Required(ErrorMessage = "ImageUpload is required.")]
        public required string ImageUpload { get; set; }

        [NotMapped]

        public IFormFile? ImageFile { get; set; }
    }
}
