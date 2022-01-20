using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManager.Models
{
    public class ProjectModelSender
    {
        public ProjectModelSender(ProjectModel project)
        {
            Project = project;
        }
        public ProjectModel Project { get; set; }
    }
}
