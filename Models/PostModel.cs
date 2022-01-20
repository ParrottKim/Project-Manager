using Caliburn.Micro;

namespace ProjectManager.Models
{
    public class PostModel
    {
        public string Date { get; set; }
        public string ModifiedDate { get; set; }
        public int PostId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Content { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string ProjectCode { get; set; }
        public BindableCollection<AttachmentModel> Attachments { get; set; }
    }
}
