using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProjectManager.Models
{
    public class DetailPostModel
    {
        public BitmapImage CustomerLogo { get; set; }
        public string CustomerName { get; set; }
        public string FactoryName { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public string CategoryName { get; set; }
        public Brush Color { get; set; }
    }
}
