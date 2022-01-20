using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using ProjectManager.Models;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectManager.ViewModels
{
    public class LoginViewModel : Conductor<Screen>, IScreen
    {
        #region Construction
        public LoginViewModel(IEventAggregator events)
        {
            _events = events;
        }
        #endregion

        #region Members
        private readonly IEventAggregator _events;
        #endregion

        #region Properties
        private string _emailText;
        public string EmailText
        {
            get { return _emailText; }
            set
            {
                _emailText = value;
                NotifyOfPropertyChange(() => EmailText);
            }
        }

        private string _passwordText;
        public string PasswordText
        {
            get { return _passwordText; }
            set
            {
                _passwordText = value;
                NotifyOfPropertyChange(() => PasswordText);
            }
        }

        private UserModel _currentUser;
        public UserModel CurrentUser
        {
            get { return _currentUser; }
            set
            {
                _currentUser = value;
                NotifyOfPropertyChange(() => CurrentUser);
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

        private bool _isLoginValid = false;
        public bool IsLoginValid
        {
            get { return _isLoginValid; }
            set
            {
                _isLoginValid = value;
                NotifyOfPropertyChange(() => IsLoginValid);
            }
        }

        private bool _isAutoLogin = Properties.Settings.Default.IsAutoLogin;
        public bool IsAutoLogin
        {
            get { return _isAutoLogin; }
            set
            {
                _isAutoLogin = value;
                NotifyOfPropertyChange(() => IsAutoLogin);
            }
        }

        private string _ipAddressText = Properties.Settings.Default.IpAddress;
        public string IpAddressText
        {
            get { return _ipAddressText; }
            set
            {
                _ipAddressText = value;
                NotifyOfPropertyChange(() => IpAddressText);
            }
        }

        private int _serverPort = Properties.Settings.Default.ServerPort;
        public int ServerPort
        {
            get { return _serverPort; }
            set
            {
                _serverPort = value;
                NotifyOfPropertyChange(() => ServerPort);
            }
        }

        private int _databasePort = Properties.Settings.Default.DatabasePort;
        public int DatabasePort
        {
            get { return _databasePort; }
            set
            {
                _databasePort = value;
                NotifyOfPropertyChange(() => DatabasePort);
            }
        }
        #endregion

        #region Methods
        public void OnLoaded()
        {
            if (IsAutoLogin)
            {
                EmailText = Properties.Settings.Default.UserEmail;
                PasswordText = Properties.Settings.Default.UserPassword;

                SignInButton();
            }
        }

        public void PasswordKeyDown(object source, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SignIn();
        }

        public void SignInButton()
        {
            SignIn();
        }

        public void SignIn()
        {
            IsLoginValid = true;

            if (!string.IsNullOrEmpty(EmailText) && !string.IsNullOrEmpty(PasswordText))
            {
                UserModel user = new UserModel();

                string ipAddress = Properties.Settings.Default.IpAddress;
                string databasePort = Properties.Settings.Default.DatabasePort.ToString();
                string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

                using (SqlConnection conn = new SqlConnection(strConn))
                using (SqlCommand cmd = new SqlCommand())
                {
                    conn.Open();
                    cmd.Connection = conn;
                    cmd.CommandText = 
                        $@"SELECT *
                           FROM   userinfo
                           WHERE  email = '{EmailText}'
                                  AND password = '{PasswordText}' ";

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                user = new UserModel
                                {
                                    UserId = (int)rdr[0],
                                    Email = rdr["EMAIL"].ToString(),
                                    Password = rdr["PASSWORD"].ToString(),
                                    Auth = rdr["Auth"].ToString(),
                                    Username = rdr["USERNAME"].ToString(),
                                    Position = rdr["POSITION"].ToString(),
                                };
                            }
                            NoticeText = "로그인 성공";

                            Properties.Settings.Default.UserEmail = EmailText;
                            Properties.Settings.Default.UserAuth = user.Auth == "관리자" ? true : false;
                            if (IsAutoLogin)
                            {
                                Properties.Settings.Default.UserPassword = PasswordText;
                                Properties.Settings.Default.Save();
                            }

                            _events.PublishOnUIThread(new UserModelSender(user));
                            TryClose();
                        }
                        else
                        {
                            NoticeText = "로그인 실패";
                        }
                    }
                }
            }
            else if (string.IsNullOrEmpty(EmailText))
                NoticeText = "이메일을 입력하세요";
            else if (string.IsNullOrEmpty(PasswordText))
                NoticeText = "비밀번호를 입력하세요";

            EmailText = string.Empty;
            PasswordText = string.Empty;
        }

        public void SetIpAddressButton()
        {
            Properties.Settings.Default.IpAddress = IpAddressText;
            Properties.Settings.Default.ServerPort = ServerPort;
            Properties.Settings.Default.DatabasePort = DatabasePort;
            Properties.Settings.Default.Save();
        }

        public void OnChecked(object source)
        {
            CheckBox checkBox = source as CheckBox;
            Properties.Settings.Default.IsAutoLogin = (bool)checkBox.IsChecked;
            Properties.Settings.Default.Save();
        }

        public void ExitProgramButton()
        {
            System.Windows.Application.Current.Shutdown();
        }
        #endregion
    }
}
