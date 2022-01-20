using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManager.Models
{
    public class UserModelSender
    {
        public UserModelSender(UserModel user)
        {
            User = user;
        }
        public UserModel User { get; set; }
    }
}
