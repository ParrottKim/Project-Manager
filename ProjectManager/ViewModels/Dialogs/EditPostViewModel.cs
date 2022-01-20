using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using ProjectManager.Extensions;
using ProjectManager.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ProjectManager.ViewModels.Dialogs
{
    public class EditPostViewModel : Screen, IHandle<ProjectModelSender>, IHandle<PostModelSender>
    {
        #region Construction
        public EditPostViewModel(IWindowManager windowManager, IEventAggregator events)
        {
            _windowManager = windowManager;
            _events = events;
            _events.Subscribe(this);
        }

        public void Handle(PostModelSender sender)
        {
            CurrentContent = sender.Content;
        }

        public void Handle(ProjectModelSender sender)
        {
            CurrentProject = sender.Project;
        }
        #endregion

        #region Members
        private readonly IWindowManager _windowManager = new WindowManager();
        private readonly IEventAggregator _events;
        private FtpRequest _ftpRequest = new FtpRequest();
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

        private PostModel _currentContent;
        public PostModel CurrentContent
        {
            get { return _currentContent; }
            set
            {
                _currentContent = value;
                NotifyOfPropertyChange(() => CurrentContent);
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

        private BindableCollection<string> _uploadableFiles = new BindableCollection<string>();
        public BindableCollection<string> UploadableFiles
        {
            get { return _uploadableFiles; }
            set
            {
                _uploadableFiles = value;
                NotifyOfPropertyChange(() => UploadableFiles);
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

        public bool _isUpdated = false;
        public bool IsUpdated
        {
            get { return _isUpdated; }
            set
            {
                _isUpdated = value;
                NotifyOfPropertyChange(() => IsUpdated);
            }
        }

        private bool _userAuth = false;
        public bool UserAuth
        {
            get { return _userAuth; }
            set
            {
                _userAuth = value;
                NotifyOfPropertyChange(() => UserAuth);
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

        public void AddNewAttachments()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if ((bool)dialog.ShowDialog())
                foreach (string fileName in dialog.FileNames)
                    UploadableFiles.Add(fileName);
        }

        public async Task DeleteAttachmentAsync(string fileName)
        {
            IsServerRunning = true;
            MessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(8000));
            await Task.Run(async () =>
            {
                if (_ftpRequest.CheckFtpConnection() && _ftpRequest.FtpDeleteRequest(fileName, $"{CurrentProject.CustomerId}/{CurrentProject.FactoryId}/{CurrentProject.ProjectId}/{CurrentContent.PostId}"))
                {
                    string ipAddress = Properties.Settings.Default.IpAddress;
                    string databasePort = Properties.Settings.Default.DatabasePort.ToString();
                    string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

                    using (SqlConnection conn = new SqlConnection(strConn))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        await conn.OpenAsync();
                        cmd.Connection = conn;
                        cmd.CommandText = $"DELETE FROM ATTACHMENT WHERE CONTENT_ID = {CurrentContent.PostId} AND FILE_NAME = '{fileName}'";
                        await cmd.ExecuteNonQueryAsync();
                    }
                    Attachments.Remove(Attachments.Where(i => i == fileName).Single());
                    MessageQueue.Enqueue($"{fileName} 삭제 완료");
                }
                else
                {
                    MessageQueue.Enqueue($"{fileName} 삭제 실패");
                }
            });
            IsServerRunning = false;
        }

        public async Task UpdatePostAsync()
        {
            if (_ftpRequest.CheckFtpConnection())
            {
                string ipAddress = Properties.Settings.Default.IpAddress;
                string databasePort = Properties.Settings.Default.DatabasePort.ToString();
                string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

                int categoryId = 0;

                using (SqlConnection conn = new SqlConnection(strConn))
                using (SqlCommand cmd = new SqlCommand())
                {
                    await conn.OpenAsync();
                    cmd.Connection = conn;
                    cmd.CommandText = $"SELECT CATEGORY_ID FROM CATEGORY WHERE CATEGORY_NAME = '{CurrentContent.CategoryName}'";
                    object pathObj = await cmd.ExecuteScalarAsync();
                    try
                    {
                        if (DBNull.Value != pathObj)
                            categoryId = (int)pathObj;
                    }
                    catch { }
                }

                if (categoryId != 0)
                {
                    using (SqlConnection conn = new SqlConnection(strConn))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        await conn.OpenAsync();
                        cmd.Connection = conn;
                        cmd.CommandText = $"UPDATE CONTENT SET CATEGORY_ID = {categoryId}, CONTENTS = '{CurrentContent.Content}', MODIFIED_DATE = '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' WHERE CONTENT_ID = {CurrentContent.PostId}";
                        await cmd.ExecuteNonQueryAsync();
                    }

                    if (UploadableFiles.Count > 0)
                    {
                        IsServerRunning = true;
                        foreach (var attachment in UploadableFiles)
                        {
                            await Task.Run(async () =>
                            {
                                if (_ftpRequest.FtpUploadRequest(attachment, $"{CurrentProject.CustomerId}/{CurrentProject.FactoryId}/{CurrentProject.ProjectId}/{CurrentContent.PostId}"))
                                {
                                    using (SqlConnection conn = new SqlConnection(strConn))
                                    using (SqlCommand cmd = new SqlCommand())
                                    {
                                        await conn.OpenAsync();
                                        cmd.Connection = conn;
                                        cmd.CommandText = $"INSERT INTO ATTACHMENT (CONTENT_ID, FILE_NAME) VALUES({CurrentContent.PostId}, '{Path.GetFileName(attachment).Replace("'", "''")}')";
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("업로드 실패!\n서버 상태를 확인해주십시오.", "오류");
                                    return;
                                }
                            });
                            IsServerRunning = false;
                        }
                    }
                }

                IsUpdated = true;
                TryClose();
            }
            else
            {
                MessageBox.Show("FTP 서버와의 연결이 끊어졌습니다.");
            }
        }

        public async void OnLoaded()
        {
            if (Properties.Settings.Default.UserEmail == CurrentContent.Email || Properties.Settings.Default.UserAuth)
                UserAuth = true;

            foreach (var attachment in CurrentContent.Attachments)
                Attachments.Add(attachment.FileName);

            await LoadCategoriesAsync(Categories);
        }

        public void AddAttachmentButton()
        {
            AddNewAttachments();
        }

        public async void OnDeleteAttachmentClick(object source)
        {
            Button button = source as Button;
            string fileName = button.DataContext.ToString();

            await DeleteAttachmentAsync(fileName);
        }

        public void OnDeleteUploadableClick(object source)
        {
            Button button = source as Button;
            string fileName = button.DataContext.ToString();

            UploadableFiles.Remove(UploadableFiles.Where(i => i == fileName).Single());
        }

        public async void UploadButton()
        {
            if (!string.IsNullOrEmpty(CurrentContent.CategoryName) && !string.IsNullOrEmpty(CurrentContent.Content))
            {
                foreach (var attachment in Attachments)
                    if (File.Exists(@attachment))
                        UploadableFiles.Add(attachment);

                await UpdatePostAsync();
            }
            IsServerRunning = false;
        }

        public void CancelButton()
        {
            TryClose();
        }
        #endregion
    }
}
