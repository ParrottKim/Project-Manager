using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using ProjectManager.Extensions;
using ProjectManager.Models;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace ProjectManager.ViewModels.Dialogs
{
    public class AddPostViewModel : Screen, IHandle<ProjectModelSender>
    {
        #region Construction
        public AddPostViewModel(IWindowManager windowManager, IEventAggregator events)
        {
            _windowManager = windowManager;
            _events = events;
            _events.Subscribe(this);
        }

        public void Handle(ProjectModelSender sender)
        {
            CurrentProject = sender.Project;
        }
        #endregion

        #region Members
        private readonly IWindowManager _windowManager = new WindowManager();
        private readonly IEventAggregator _events;
        private FtpRequest _ftpReqeust = new FtpRequest();
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

        private BindableCollection<string> _categories = new BindableCollection<string>();
        public BindableCollection<string> Categories
        {
            get { return _categories; }
            set
            {
                _categories = value;
                NotifyOfPropertyChange(() => Categories);
            }
        }

        private string _selectedCategory;
        public string SelectedCategory
        {
            get { return _selectedCategory; }
            set
            {
                _selectedCategory = value;
                NotifyOfPropertyChange(() => SelectedCategory);
            }
        }

        private string _content;
        public string Content
        {
            get { return _content; }
            set
            {
                _content = value;
                NotifyOfPropertyChange(() => Content);
            }
        }

        private BindableCollection<string> _attachments = new BindableCollection<string>();
        public BindableCollection<string> Attachments
        {
            get { return _attachments; }
            set
            {
                _attachments = value;
                NotifyOfPropertyChange(() => Attachments);
            }
        }

        public int _uploadContentId;
        public int UploadContentId
        {
            get { return _uploadContentId; }
            set
            {
                _uploadContentId = value;
                NotifyOfPropertyChange(() => UploadContentId);
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

        public bool _isUploaded = false;
        public bool IsUploaded
        {
            get { return _isUploaded; }
            set
            {
                _isUploaded = value;
                NotifyOfPropertyChange(() => IsUploaded);
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

        #region Method
        public static async Task LoadCategoriesAsync(BindableCollection<string> categories)
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT CATEGORY_NAME FROM CATEGORY";

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (await rdr.ReadAsync())
                    {
                        categories.Add(rdr["CATEGORY_NAME"].ToString());
                    }
                    rdr.Close();
                }
            }
        }

        public void AddNewAttachments(ProjectModel project, BindableCollection<string> attachments)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if ((bool)dialog.ShowDialog())
                foreach (string fileName in dialog.FileNames)
                    attachments.Add(fileName);
        }

        public async Task UploadPostAsync()
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (_ftpReqeust.CheckFtpConnection())
            {
                string ipAddress = Properties.Settings.Default.IpAddress;
                string databasePort = Properties.Settings.Default.DatabasePort.ToString();
                string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

                int userId = 0;
                int categoryId = 0;

                using (SqlConnection conn = new SqlConnection(strConn))
                using (SqlCommand cmd = new SqlCommand())
                {
                    await conn.OpenAsync();
                    cmd.Connection = conn;
                    cmd.CommandText = $"SELECT USER_ID FROM USERINFO WHERE EMAIL = '{Properties.Settings.Default.UserEmail}'";
                    object pathObj = await cmd.ExecuteScalarAsync();
                    try
                    {
                        if (DBNull.Value != pathObj)
                            userId = (int)pathObj;
                    }
                    catch { }
                }

                using (SqlConnection conn = new SqlConnection(strConn))
                using (SqlCommand cmd = new SqlCommand())
                {
                    await conn.OpenAsync();
                    cmd.Connection = conn;
                    cmd.CommandText = $"SELECT CATEGORY_ID FROM CATEGORY WHERE CATEGORY_NAME = '{SelectedCategory}'";
                    object pathObj = await cmd.ExecuteScalarAsync();
                    try
                    {
                        if (DBNull.Value != pathObj)
                            categoryId = (int)pathObj;
                    }
                    catch { }
                }

                if (userId != 0 && categoryId != 0)
                {
                    using (SqlConnection conn = new SqlConnection(strConn))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        await conn.OpenAsync();
                        cmd.Connection = conn;
                        cmd.CommandText = $"INSERT INTO CONTENT (USER_ID, CATEGORY_ID, PROJECT_ID, CONTENTS, DATE) VALUES ({userId}, {categoryId}, {CurrentProject.ProjectId}, N'{Content.Replace("'", "''")}', '{date}')";
                        await cmd.ExecuteNonQueryAsync();
                    }

                    if (Attachments.Count > 0)
                    {
                        using (SqlConnection conn = new SqlConnection(strConn))
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            await conn.OpenAsync();
                            cmd.Connection = conn;
                            cmd.CommandText = $"SELECT CONTENT_ID FROM CONTENT WHERE USER_ID = {userId} AND CATEGORY_ID = {categoryId} AND PROJECT_ID = {CurrentProject.ProjectId} AND CONTENTS = N'{Content}' AND DATE = '{date}'";
                            object pathObj = await cmd.ExecuteScalarAsync();
                            try
                            {
                                if (DBNull.Value != pathObj)
                                    UploadContentId = (int)pathObj;
                            }
                            catch { }
                        }

                        IsServerRunning = true;
                        foreach (var attachment in Attachments)
                        {
                            await Task.Run(async () =>
                            {
                                if (_ftpReqeust.FtpUploadRequest(attachment, $"{CurrentProject.CustomerId}/{CurrentProject.FactoryId}/{CurrentProject.ProjectId}/{UploadContentId}"))
                                {
                                    using (SqlConnection conn = new SqlConnection(strConn))
                                    using (SqlCommand cmd = new SqlCommand())
                                    {
                                        await conn.OpenAsync();
                                        cmd.Connection = conn;
                                        cmd.CommandText = $"INSERT INTO ATTACHMENT (CONTENT_ID, FILE_NAME) VALUES({UploadContentId}, '{Path.GetFileName(attachment).Replace("'", "''")}')";
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("업로드 실패!\n서버 상태를 확인해주십시오.", "오류");
                                    return; 
                                }
                            });
                        }
                        IsServerRunning = false;
                    }
                }

                IsUploaded = true;
                TryClose();
            }
            else
            {
                MessageBox.Show("FTP 서버와의 연결이 끊어졌습니다.");
            }
        }

        public async void OnLoaded()
        {
            await LoadCategoriesAsync(Categories);
        }

        public void AddAttachmentButton()
        {
            AddNewAttachments(CurrentProject, Attachments);
        }

        public async void UploadButton()
        {
            if (!string.IsNullOrEmpty(SelectedCategory) && !string.IsNullOrEmpty(Content))
            {
                await UploadPostAsync();
            }
            else
                MessageBox.Show($"미작성 요소가 있습니다\nCategory: {SelectedCategory}\nContent: {Content}");
        }

        public void CancelButton()
        {
            TryClose();
        }
        #endregion
    }
}
