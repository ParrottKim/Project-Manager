using Caliburn.Micro;
using ProjectManager.Models;
using ProjectManager.ViewModels.Dialogs;
using ProjectManager.ViewModels.UserControls;
using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectManager.ViewModels
{
    public class DashboardViewModel : Conductor<Screen>, IScreen, IHandle<UserModelSender>
    {
        #region Construction
        public DashboardViewModel(IWindowManager windowManager, IEventAggregator events)
        {
            _windowManager = windowManager;
            _events = events;
            _events.Subscribe(this);
        }

        public void Handle(UserModelSender sender)
        {
            IsEnabled = false;
            CurrentUser = sender.User;
            ActivateItem(new MainViewModel(new WindowManager(), new EventAggregator()));
        }
        #endregion

        #region Members
        private readonly IWindowManager _windowManager = new WindowManager();
        private readonly IEventAggregator _events;
        #endregion

        #region Properties
        private bool _isEnabled = false;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                NotifyOfPropertyChange(() => IsEnabled);
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

        private string _currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public string CurrentVersion
        {
            get { return _currentVersion; }
            set
            {
                _currentVersion = value;
                NotifyOfPropertyChange(() => CurrentVersion);
            }
        }

        private string _appVersion;
        public string AppVersion
        {
            get { return _appVersion; }
            set
            {
                _appVersion = value;
                NotifyOfPropertyChange(() => AppVersion);
            }
        }
        #endregion

        #region Methods

        public async Task CheckCurrentAppVersion()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT TOP 1 VERSION FROM VERSION ORDER BY VERSION_ID DESC";
                object pathObj = await cmd.ExecuteScalarAsync();
                try
                {
                    if (DBNull.Value != pathObj) 
                        AppVersion = pathObj.ToString();
                }
                catch { }
            }

            if (AppVersion != CurrentVersion)
            {
                var programUpdateViewModel = new ProgramUpdateViewModel(new WindowManager(), _events);
                _events.PublishOnUIThread(AppVersion);
                _windowManager.ShowDialog(programUpdateViewModel);
                if (programUpdateViewModel.IsUpdated)
                    TryClose();
            }
        }

        public async void OnLoaded()
        {
            await CheckCurrentAppVersion();
            if (CurrentUser == null)
            {
                IsEnabled = true;
                var loginViewModel = new LoginViewModel(_events);
                _windowManager.ShowDialog(loginViewModel);
            }
        }

        public void OnMainListPreviewMouseLeftButtonUp(object source)
        {
            ListView listView = source as ListView;
            if (listView.SelectedItem != null)
            {
                switch (listView.SelectedIndex)
                {
                    case 0:
                        ActivateItem(new MainViewModel(new WindowManager(), new EventAggregator()));
                        break;
                    case 3:
                        ActivateItem(new RecentViewModel(new WindowManager(), new EventAggregator()));
                        break;
                    case 4:
                        ActivateItem(new SiteViewModel(new WindowManager(), new EventAggregator()));
                        break;
                    case 7:
                        ActivateItem(new BookmarkViewModel(new WindowManager(), new EventAggregator()));
                        break;
                    case 8:
                        ActivateItem(new SearchViewModel(new WindowManager(), new EventAggregator()));
                        break;
                    default:
                        break;
                }
            }
        }

        public void OnSubListPreviewMouseLeftButtonUp(object source)
        {
            ListView listView = source as ListView;
            if (listView.SelectedItem != null)
            {
                switch (listView.SelectedIndex)
                {
                    case 0:
                        ActivateItem(new UserInfoViewModel(new WindowManager(), new EventAggregator()));
                        break;
                    case 1:
                        IsEnabled = true;
                        Properties.Settings.Default.IsAutoLogin = false;
                        Properties.Settings.Default.UserEmail = string.Empty;
                        Properties.Settings.Default.UserPassword = string.Empty;
                        Properties.Settings.Default.Save();
                        var loginViewModel = new LoginViewModel(_events);
                        _windowManager.ShowDialog(loginViewModel);
                        break;
                }
            }
        }

        public void LoginButton()
        {
            var loginViewModel = new LoginViewModel(_events);
            _windowManager.ShowDialog(loginViewModel);
        }

        public void TitleBarDoubleClick(object source, MouseButtonEventArgs e)
        {
            Window window = (Window)GetView();
            if (e.ClickCount == 2)
                if (window.WindowState == WindowState.Maximized)
                    window.WindowState = WindowState.Normal;
                else
                    window.WindowState = WindowState.Maximized;
        }

        public void MinimizeButton()
        {
            Window window = (Window)GetView();
            window.WindowState = WindowState.Minimized;
        }

        public void MaximizeButton()
        {
            Window window = (Window)GetView();
            if (window.WindowState == WindowState.Maximized)
                window.WindowState = WindowState.Normal;
            else
                window.WindowState = WindowState.Maximized;
        }

        public void CloseButton()
        {
            TryClose();
        }

        public void OnClosing()
        {
            System.Windows.Application.Current.Shutdown();
        }
        #endregion
    }
}
