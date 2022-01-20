using Caliburn.Micro;
using ProjectManager.Models;

namespace ProjectManager.Dialogs
{
    public class ChangeProjectManagerDialog : NotificationMessage
    {
        private BindableCollection<UserModel> _users = new BindableCollection<UserModel>();
        public BindableCollection<UserModel> Users
        {
            get => _users; set => _users = value;
        }

        private UserModel _selectedUser;
        public UserModel SelectedUser
        {
            get => _selectedUser; set => _selectedUser = value;
        }

        public ChangeProjectManagerDialog()
        {
            Title = "알림";
        }
    }
}