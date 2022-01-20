using Caliburn.Micro;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ProjectManager.ViewModels.Dialogs
{
    public class ChangePasswordViewModel : Screen
    {
        #region Construction
        public ChangePasswordViewModel()
        {

        }
        #endregion

        #region Members
        #endregion

        #region Properties
        private bool _isError = false;
        public bool IsError
        {
            get { return _isError; }
            set
            {
                _isError = value;
                NotifyOfPropertyChange(() => IsError);
            }
        }

        private bool _isConfirmed = false;
        public bool IsConfirmed
        {
            get { return _isConfirmed; }
            set
            {
                _isConfirmed = value;
                NotifyOfPropertyChange(() => IsConfirmed);
            }
        }

        private bool _isUpdated = false;
        public bool IsUpdated
        {
            get { return _isUpdated; }
            set
            {
                _isUpdated = value;
                NotifyOfPropertyChange(() => IsUpdated);
            }
        }

        private string _currentPassword;
        public string CurrentPassword
        {
            get { return _currentPassword; }
            set
            {
                _currentPassword = value;
                NotifyOfPropertyChange(() => CurrentPassword);
            }
        }

        private string _changedPassword;
        public string ChangedPassword
        {
            get { return _changedPassword; }
            set
            {
                _changedPassword = value;
                NotifyOfPropertyChange(() => ChangedPassword);
            }
        }

        private string _noticeText;
        public string NoticeText
        {
            get { return _noticeText; }
            set
            {
                _noticeText = value;
                NotifyOfPropertyChange(() => NoticeText);
            }
        }
        #endregion

        #region Methods
        public async Task CheckPasswordAsync()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = $"SELECT COUNT(*) FROM USERINFO WHERE EMAIL = '{Properties.Settings.Default.UserEmail}' AND PASSWORD = '{CurrentPassword}'";
                object pathObj = await cmd.ExecuteScalarAsync();
                try
                {
                    if (DBNull.Value != null)
                    {
                        if ((int)pathObj == 1)
                        {
                            IsConfirmed = true;
                            IsError = false;
                        }
                        else
                        {
                            IsConfirmed = false;
                            IsError = true;
                            NoticeText = "비밀번호가 잘못되었습니다";
                        }
                    }
                }
                catch { }
            }
        }

        public async Task ChangePasswordAsync()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = $"UPDATE USERINFO SET PASSWORD = '{ChangedPassword}' WHERE EMAIL = '{Properties.Settings.Default.UserEmail}'";
                await cmd.ExecuteNonQueryAsync();
                IsUpdated = true;
            }
        }

        public async void CheckCurrentPasswordButton()
        {
            await CheckPasswordAsync();
        }

        public async void ChangePasswordButton()
        {
            if (!string.IsNullOrEmpty(ChangedPassword))
                await ChangePasswordAsync();
        }

        public void OnCloseButtonClick()
        {
            System.Windows.Forms.Application.Restart();
            System.Windows.Application.Current.Shutdown();
        }

        public void CloseButton() => TryClose();
        #endregion
    }
}
