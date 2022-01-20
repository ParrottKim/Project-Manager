using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProjectManager.Models
{
    public class ProjectModel
    {
        public string Date { get; set; }

        public int ProjectId { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }

        public int UserId { get; set; }
        public string Username { get; set; }

        public int CustomerId { get; set; }
        public BitmapImage CustomerLogo { get; set; }
        public string CustomerName { get; set; }

        public int FactoryId { get; set; }
        public string FactoryName { get; set; }

        public int CategoryId { get; set; }
        public int CategoryPriority { get; set; }
        public string CategoryName { get; set; }
        public Brush Color { get; set; }
    }
}
