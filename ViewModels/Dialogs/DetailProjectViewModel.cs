using Caliburn.Micro;
using ProjectManager.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using ProjectManager.Dialogs;
using Outlook = Microsoft.Office.Interop.Outlook;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using System.Net;
using System.Windows.Threading;
using ProjectManager.Extensions;

namespace ProjectManager.ViewModels.Dialogs
{
    public class DetailProjectViewModel : Screen, IHandle<ProjectModelSender>, IHandle<ProjectModel>
    {
        #region Construction
        public DetailProjectViewModel(IWindowManager windowManager, IEventAggregator events)
        {
            _windowManager = windowManager;
            _events = events;
            _events.Subscribe(this);
        }

        public void Handle(ProjectModelSender sender)
        {
            CurrentProject = sender.Project;
        }

        public void Handle(ProjectModel sender)
        {
            CurrentProject = sender;
        }
        #endregion

        #region Members
        private readonly IWindowManager _windowManager = new WindowManager();
        private readonly IEventAggregator _events;
        private FtpRequest _ftpReqeust = new FtpRequest();
        private string outputFolder;

        private ChangeProjectManagerDialog change = new ChangeProjectManagerDialog();
        private AuthErrorMessageDialog authError = new AuthErrorMessageDialog();
        private MailErrorMessageDialog mailError = new MailErrorMessageDialog();
        #endregion

        #region Properties
        private ProjectModel _currentProject;
        public ProjectModel CurrentProject
        {
            get { return _currentProject; }
            set
            {
                _currentProject = value;
                NotifyOfPropertyChange(() => CurrentProject);
            }
        }

        private BindableCollection<PostModel> _contents = new BindableCollection<PostModel>();
        public BindableCollection<PostModel> Contents
        {
            get { return _contents; }
            set
            {
                _contents = value;
                NotifyOfPropertyChange(() => Contents);
            }
        }

        private BindableCollection<UserModel> _users = new BindableCollection<UserModel>();
        public BindableCollection<UserModel> Users
        {
            get { return _users; }
            set
            {
                _users = value;
                NotifyOfPropertyChange(() => Users);
            }
        }

        private UserModel _selectedUser;
        public UserModel SelectedUser
        {
            get { return _selectedUser; }
            set
            {
                _selectedUser = value;
                NotifyOfPropertyChange(() => SelectedUser);
            }
        }

        private bool _isBookmarked;
        public bool IsBookmarked
        {
            get { return _isBookmarked; }
            set
            {
                _isBookmarked = value;
                NotifyOfPropertyChange(() => IsBookmarked);
            }
        }

        private bool _isEditable;
        public bool IsEditable
        {
            get { return _isEditable; }
            set
            {
                _isEditable = value;
                NotifyOfPropertyChange(() => IsEditable);
            }
        }

        private bool _isServerRunning;
        public bool IsServerRunning
        {
            get { return _isServerRunning; }
            set
            {
                _isServerRunning = value;
                NotifyOfPropertyChange(() => IsServerRunning);
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

        private SnackbarMessageQueue _messageQueue;
        public SnackbarMessageQueue MessageQueue
        {
            get { return _messageQueue; }
            set
            {
                _messageQueue = value;
                NotifyOfPropertyChange(() => MessageQueue);
            }
        }
        #endregion

        #region Methods
        public async Task LoadPostsAsync()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = $@"SELECT CO.DATE, CO.MODIFIED_DATE, CO.CONTENT_ID, CO.CATEGORY_ID, 
                    (SELECT CA.CATEGORY_NAME FROM CATEGORY CA WHERE CA.CATEGORY_ID = CO.CATEGORY_ID) CATEGORY_NAME, 
                    (SELECT PR.PROJECT_CODE FROM PROJECT PR WHERE PR.PROJECT_ID = CO.PROJECT_ID) PROJECT_CODE, 
                    CO.CONTENTS,  
                    (SELECT US.USERNAME FROM USERINFO US WHERE US.USER_ID = CO.USER_ID) USERNAME, 
                    (SELECT US.EMAIL FROM USERINFO US WHERE US.USER_ID = CO.USER_ID) EMAIL 
                    FROM CONTENT CO 
                    WHERE CO.PROJECT_ID = (SELECT PR.PROJECT_ID FROM PROJECT PR WHERE PR.PROJECT_CODE = '{CurrentProject.ProjectCode}') 
                    ORDER BY CO.CONTENT_ID DESC";

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (await rdr.ReadAsync())
                    {
                        Contents.Add(new PostModel()
                        {
                            Date = rdr["DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["DATE"]).ToString("yyyy.MM.dd HH:mm") : "-",
                            ModifiedDate = rdr["MODIFIED_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["MODIFIED_DATE"]).ToString("yyyy.MM.dd HH:mm") : "-",
                            PostId = (int)rdr["CONTENT_ID"],
                            CategoryId = (int)rdr["CATEGORY_ID"],
                            CategoryName = rdr["CATEGORY_NAME"].ToString(),
                            ProjectCode = rdr["PROJECT_CODE"].ToString(),
                            Content = rdr["CONTENTS"].ToString(),
                            Username = rdr["USERNAME"].ToString(),
                            Email = rdr["EMAIL"].ToString(),
                            Attachments = new BindableCollection<AttachmentModel>()
                        });
                    }
                    rdr.Close();
                }
            }

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = $"SELECT * FROM USERINFO WHERE USER_ID = {CurrentProject.UserId}";

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (await rdr.ReadAsync())
                    {
                        SelectedUser = new UserModel()
                        {
                            UserId = (int)rdr["USER_ID"],
                            Email = rdr["EMAIL"].ToString(),
                            Username = rdr["USERNAME"].ToString(),
                            Position = rdr["POSITION"].ToString()
                        };
                    }
                }
            }

            foreach (var content in Contents)
            {
                using (SqlConnection conn = new SqlConnection(strConn))
                using (SqlCommand cmd = new SqlCommand())
                {
                    await conn.OpenAsync();
                    cmd.Connection = conn;
                    cmd.CommandText = $"SELECT * FROM ATTACHMENT WHERE CONTENT_ID = {content.PostId}";

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (await rdr.ReadAsync())
                                content.Attachments.Add(new AttachmentModel()
                                {
                                    Id = (int)rdr["FILE_ID"],
                                    ContentId = (int)rdr["CONTENT_ID"],
                                    FileName = rdr["FILE_NAME"].ToString()
                                });
                            rdr.Close();
                        }
                    }
                }
            }
        }

        public async Task DownloadAttachmentAsync(AttachmentModel attachment)
        {
            if (_ftpReqeust.CheckFtpConnection())
            {
                string ipAddress = Properties.Settings.Default.IpAddress;
                string databasePort = Properties.Settings.Default.DatabasePort.ToString();
                string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

                string selectedCategory = "";

                using (SqlConnection conn = new SqlConnection(strConn))
                using (SqlCommand cmd = new SqlCommand())
                {
                    await conn.OpenAsync();
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT (SELECT CA.CATEGORY_ID FROM CATEGORY AS CA WHERE CA.CATEGORY_ID = CO.CATEGORY_ID) " +
                        "FROM CONTENT AS CO " +
                        $"WHERE CO.CONTENT_ID = {attachment.ContentId}";
                    object pathObj = await cmd.ExecuteScalarAsync();
                    try
                    {
                        if (DBNull.Value != pathObj)
                            selectedCategory = pathObj.ToString();
                    }
                    catch { }
                }

                IsServerRunning = true;
                MessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(8000));
                await Task.Run(() =>
                {
                    if (_ftpReqeust.CheckFtpConnection() && _ftpReqeust.FtpDownloadRequest(attachment.FileName, $"{CurrentProject.CustomerId}/{CurrentProject.FactoryId}/{CurrentProject.ProjectId}/{attachment.ContentId}", outputFolder, false))
                        MessageQueue.Enqueue($"{attachment.FileName} 다운로드 완료");
                    else
                        MessageQueue.Enqueue($"{attachment.FileName} 다운로드 실패");
                });
                IsServerRunning = false;
            }
            else
            {
                MessageBox.Show("FTP 서버와의 연결이 끊어졌습니다.");
            }
        }

        public async Task LoadUsernamesAsync()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT * FROM USERINFO";

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (await rdr.ReadAsync())
                    {
                        Users.Add(new UserModel()
                        {
                            UserId = (int)rdr["USER_ID"],
                            Email = rdr["EMAIL"].ToString(),
                            Username = rdr["USERNAME"].ToString(),
                            Position = rdr["POSITION"].ToString()
                        });
                    }
                    rdr.Close();
                }
            }
        }

        public async Task UpdateUsername()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = $"UPDATE PROJECT SET USER_ID = (SELECT USER_ID FROM USERINFO WHERE USERNAME = '{change.SelectedUser.Username}' AND EMAIL = '{change.SelectedUser.Email}') WHERE PROJECT_ID = {CurrentProject.ProjectId}";
                CurrentProject.Username = change.SelectedUser.Username;
                SelectedUser = change.SelectedUser;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<string>> LoadUserEmailsAsync()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            List<string> list = new List<string>();

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = $"SELECT EMAIL FROM USERINFO";

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (await rdr.ReadAsync())
                    {
                        list.Add(rdr["EMAIL"].ToString());
                    }
                    rdr.Close();
                }
            }

            int index = list.IndexOf(Properties.Settings.Default.UserEmail);
            list.RemoveAt(index);

            return list;
        }

        public async Task LoadIsBookmarkedAsync()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT COUNT(*) FROM BOOKMARK " +
                    $"WHERE USER_ID = (SELECT USER_ID FROM USERINFO WHERE EMAIL = '{Properties.Settings.Default.UserEmail}') " +
                    $"AND PROJECT_ID = (SELECT PROJECT_ID FROM PROJECT WHERE PROJECT_CODE = '{CurrentProject.ProjectCode}')";
                object scalarValue = cmd.ExecuteScalar();
                if ((int)scalarValue == 0)
                    IsBookmarked = false;
                else
                    IsBookmarked = true;
            }
        }

        public async Task SetBookmarkAsync()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;

                if (!IsBookmarked)
                {
                    cmd.CommandText = $"INSERT INTO BOOKMARK (USER_ID, PROJECT_ID) VALUES ((SELECT USER_ID FROM USERINFO WHERE EMAIl = '{Properties.Settings.Default.UserEmail}'), (SELECT PROJECT_ID FROM PROJECT WHERE PROJECT_CODE = '{CurrentProject.ProjectCode}'))";
                }
                else
                {
                    cmd.CommandText = "DELETE FROM BOOKMARK " +
                        $"WHERE USER_ID = (SELECT USER_ID FROM USERINFO WHERE EMAIL = '{Properties.Settings.Default.UserEmail}') " +
                        $"AND PROJECT_ID = (SELECT PROJECT_ID FROM PROJECT WHERE PROJECT_CODE = '{CurrentProject.ProjectCode}')";
                }
                cmd.ExecuteNonQuery();
            }
        }

        public async void OnLoaded()
        {
            await LoadIsBookmarkedAsync();
            await LoadUsernamesAsync();
            await LoadPostsAsync();
        }

        public async void AddButton()
        {
            var addPostViewModel = new AddPostViewModel(new WindowManager(), _events);
            _events.PublishOnUIThread(new ProjectModelSender(CurrentProject));
            _windowManager.ShowDialog(addPostViewModel);
            if (addPostViewModel.IsUploaded)
            {
                IsUpdated = true;
                Contents.Clear();
                await LoadPostsAsync();
            }
        }

        public async void EditButton()
        {
            if (Properties.Settings.Default.UserAuth)
            {
                var editProjectViewModel = new EditProjectViewModel(new WindowManager(), _events);
                _events.PublishOnUIThread(new ProjectModelSender(CurrentProject));
                _windowManager.ShowDialog(editProjectViewModel);
                if (editProjectViewModel.IsUpdated)
                {
                    IsUpdated = true;
                }
            }
            else
            {
                await DialogHost.Show(authError, "RootDialog");
            }
        }

        public async void OnOpenFileClick(object source)
        {
            Button button = source as Button;

            Properties.Settings.Default.IsFileOpen = true;
            Properties.Settings.Default.Save();

            outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);
            await DownloadAttachmentAsync((AttachmentModel)button.DataContext);
        }

        public async void OnDownloadClick(object source)
        {
            Button button = source as Button;

            Properties.Settings.Default.IsFileOpen = false;
            Properties.Settings.Default.Save();

            await Task.Delay(100);
            if (!Properties.Settings.Default.IsFileOpen)
            {
                System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
                dialog.Title = "문서 저장";
                dialog.FileName = ((AttachmentModel)button.DataContext).FileName.ToString();
                dialog.Filter = "All files (*.*)|*.*";
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    outputFolder = Path.GetDirectoryName(dialog.FileName);
                    await Task.Delay(100);
                    await DownloadAttachmentAsync((AttachmentModel)button.DataContext);
                }
                else return;
            }
        }

        public async void BookmarkButton()
        {
            await SetBookmarkAsync();
            await LoadIsBookmarkedAsync();
        }

        public async void ChangeUserButton()
        {
            change.Users = Users;
            change.SelectedUser = SelectedUser;

            await DialogHost.Show(change, "RootDialog");
        }

        public async void ChangeButton()
        {
            await UpdateUsername();
            IsUpdated = true;
        }

        public async void OnEditPostButtonClick(object source)
        {
            Button button = source as Button;
            PostModel selectedContent = (PostModel)button.DataContext;

            var editPostViewModel = new EditPostViewModel(new WindowManager(), _events);
            _events.PublishOnUIThread(new ProjectModelSender(CurrentProject));
            _events.PublishOnUIThread(new PostModelSender(selectedContent));
            _windowManager.ShowDialog(editPostViewModel);
            if (editPostViewModel.IsUpdated)
            {
                Contents.Clear();
                await LoadPostsAsync();
            }
        }

        public async void OnSendEmailButtonClick(object source)
        {
            Button button = source as Button;
            PostModel selectedContent = (PostModel)button.DataContext;

            if (SelectedUser != null)
            {
                List<string> emails = await LoadUserEmailsAsync();
                Outlook.Application outlook = new Outlook.Application();
                Outlook._MailItem mailItem = (Outlook._MailItem)outlook.CreateItem(Outlook.OlItemType.olMailItem);
                Outlook.Inspector inspector = mailItem.GetInspector;
                mailItem.HTMLBody = null;

                Outlook.Recipient recipTo = mailItem.Recipients.Add(SelectedUser.Email);
                recipTo.Type = (int)Outlook.OlMailRecipientType.olTo;

                foreach (string email in emails)
                {
                    if (email != Properties.Settings.Default.UserEmail && email != SelectedUser.Email)
                    {
                        Outlook.Recipient recipCC = mailItem.Recipients.Add(email);
                        recipCC.Type = (int)Outlook.OlMailRecipientType.olCC;
                    }
                }
                mailItem.Recipients.ResolveAll();

                mailItem.Subject = $"{SelectedUser.Username} | {CurrentProject.CustomerName} | {CurrentProject.FactoryName} | {CurrentProject.ProjectCode} | {CurrentProject.ProjectName} | {selectedContent.CategoryName}";
                mailItem.Body += selectedContent.Content;
                if (selectedContent.Attachments.Count > 0)
                {
                    mailItem.Body += "\n[첨부 파일 목록]";
                    foreach (var attachments in selectedContent.Attachments)
                    {
                        mailItem.Body += attachments.FileName + "\n";
                    }
                }

                mailItem.Display(false);
            }
            else
            {
                await DialogHost.Show(mailError, "RootDialog");
            }
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
        #endregion
    }
}
