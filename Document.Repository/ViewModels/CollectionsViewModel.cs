using Document.Repository.Models.Entities;

namespace Document.Repository.ViewModels
{
    public class CollectionsViewModel
    {
        public IEnumerable<TagCategory> TagCategories { get; set; }
        public int? SelectedTagId { get; set; }
        public IEnumerable<Project> Projects { get; set; }
    }

}
