using Caliburn.Micro;
using ProjectManager.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ProjectManager.ViewModels.Dialogs
{
    public class EditProjectViewModel : Screen, IHandle<ProjectModelSender>
    {
        #region Construction
        public EditProjectViewModel(IWindowManager windowManager, IEventAggregator events)
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

        private ProjectModel _changedProject;
        public ProjectModel ChangedProject
        {
            get { return _changedProject; }
            set
            {
                _changedProject = value;
                NotifyOfPropertyChange(() => ChangedProject);
            }
        }

        private BindableCollection<string> _customers = new BindableCollection<string>();
        public BindableCollection<string> Customers
        {
            get { return _customers; }
            set
            {
                _customers = value;
                NotifyOfPropertyChange(() => Customers);
            }
        }

        private BindableCollection<string> _factories = new BindableCollection<string>();
        public BindableCollection<string> Factories
        {
            get { return _factories; }
            set
            {
                _factories = value;
                NotifyOfPropertyChange(() => Factories);
            }
        }

        private string _selectedCustomer;
        public string SelectedCustomer
        {
            get { return _selectedCustomer; }
            set
            {
                _selectedCustomer = value;
                NotifyOfPropertyChange(() => SelectedCustomer);
            }
        }

        private string _selectedFactory;
        public string SelectedFactory
        {
            get { return _selectedFactory; }
            set
            {
                _selectedFactory = value;
                NotifyOfPropertyChange(() => SelectedFactory);
            }
        }

        private string _projectCode;
        public string ProjectCode
        {
            get { return _projectCode; }
            set
            {
                _projectCode = value;
                NotifyOfPropertyChange(() => ProjectCode);
            }
        }

        private string _projectName;
        public string ProjectName
        {
            get { return _projectName; }
            set
            {
                _projectName = value;
                NotifyOfPropertyChange(() => ProjectName);
            }
        }

        private bool _isSelectable = true;
        public bool IsSelectable
        {
            get { return _isSelectable; }
            set
            {
                _isSelectable = value;
                NotifyOfPropertyChange(() => IsSelectable);
            }
        }

        private bool _isUploadable = false;
        public bool IsUploadable
        {
            get { return _isUploadable; }
            set
            {
                _isUploadable = value;
                NotifyOfPropertyChange(() => IsUploadable);
            }
        }

        private bool _isDuplicated = false;
        public bool IsDuplicated
        {
            get { return _isDuplicated; }
            set
            {
                _isDuplicated = value;
                NotifyOfPropertyChange(() => IsDuplicated);
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
        #endregion

        #region Methods
        public static async Task LoadCustomersAsync(BindableCollection<string> customers)
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText =
                    $@"SELECT customer_name
                       FROM   customer ";

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (await rdr.ReadAsync())
                    {
                        customers.Add(rdr["CUSTOMER_NAME"].ToString());
                    }
                    rdr.Close();
                }
            }
        }

        public static async Task LoadFactoriesAsync(string customer, BindableCollection<string> factories)
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText =
                    $@"SELECT factory_name
                       FROM   factory CU
                       WHERE  customer_id = (SELECT customer_id
                                             FROM   customer
                                             WHERE  customer_name = '{customer}') ";

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (await rdr.ReadAsync())
                    {
                        factories.Add(rdr["FACTORY_NAME"].ToString());
                    }
                    rdr.Close();
                }
            }
        }

        //public async Task CheckDuplication(string code)
        //{
        //    string ipAddress = Properties.Settings.Default.IpAddress;
        //    string databasePort = Properties.Settings.Default.DatabasePort.ToString();
        //    string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

        //    using (SqlConnection conn = new SqlConnection(strConn))
        //    using (SqlCommand cmd = new SqlCommand())
        //    {
        //        await conn.OpenAsync();
        //        cmd.Connection = conn;
        //        cmd.CommandText =
        //            $@"SELECT Count(*)
        //               FROM   project
        //               WHERE  project_code = '{code}' ";

        //        object pathObj = await cmd.ExecuteScalarAsync();
        //        {
        //            if (DBNull.Value != null)
        //            {
        //                if ((int)pathObj >= 1)
        //                {
        //                    IsDuplicated = true;
        //                    IsUploadable = false;
        //                }
        //                else
        //                {
        //                    IsDuplicated = false;
        //                    IsUploadable = true;
        //                }
        //            }
        //        }
        //    }
        //}

        public async Task ChangeProjectAsync()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            int customerId = 0;
            string customerName = string.Empty;
            int factoryId = 0;
            string factoryName = string.Empty;
            BitmapImage logo = new BitmapImage();

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = 
                    $@"SELECT FA.customer_id,
                              (SELECT customer_name
                               FROM   customer
                               WHERE  customer_id = FA.customer_id) CUSTOMER_NAME,
                              FA.factory_id,
                              FA.factory_name,
                              (SELECT logo
                               FROM   customer
                               WHERE  customer_id = FA.customer_id) LOGO
                       FROM   factory FA
                       WHERE  FA.factory_name = '{SelectedFactory}' ";

                try
                {
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        await rdr.ReadAsync();

                        byte[] imageBlob = rdr["LOGO"] as byte[];
                        MemoryStream ms = new MemoryStream(imageBlob);
                        ms.Seek(0, SeekOrigin.Begin);

                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = ms;
                        image.EndInit();

                        customerId = (int)rdr["CUSTOMER_ID"];
                        customerName = (string)rdr["CUSTOMER_NAME"];
                        factoryId = (int)rdr["FACTORY_ID"];
                        factoryName = (string)rdr["FACTORY_NAME"];
                        logo = image;
                    }
                }
                catch { }
            }

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText =
                    $@"UPDATE project
                       SET    project_code = '{ProjectCode}',
                              project_name = '{ProjectName}',
                              customer_id = {customerId},
                              factory_id = {factoryId}
                       WHERE  project_id = {CurrentProject.ProjectId} ";
                await cmd.ExecuteNonQueryAsync();
            }

            ChangedProject = new ProjectModel()
            {
                Date = CurrentProject.Date,

                ProjectId = CurrentProject.ProjectId,
                ProjectCode = ProjectCode,
                ProjectName = ProjectName,

                UserId = CurrentProject.UserId,
                Username = CurrentProject.Username,

                CustomerId = customerId,
                CustomerLogo = logo,
                CustomerName = customerName,

                FactoryId = factoryId,
                FactoryName = factoryName,

                CategoryId = CurrentProject.CategoryId,
                CategoryPriority = CurrentProject.CategoryPriority,
                CategoryName = CurrentProject.CategoryName,

                Color = CurrentProject.Color
            };

            IsUpdated = true;
        }

        public async void OnLoaded()
        {
            SelectedCustomer = CurrentProject.CustomerName;
            SelectedFactory = CurrentProject.FactoryName;
            ProjectCode = CurrentProject.ProjectCode;
            ProjectName = CurrentProject.ProjectName;
            await LoadCustomersAsync(Customers);
            await LoadFactoriesAsync(SelectedCustomer, Factories);
        }

        public async void OnCustomerSelectionChanged(object source)
        {
            IsSelectable = true;

            Factories = new BindableCollection<string>();
            SelectedFactory = null;

            await LoadFactoriesAsync(SelectedCustomer, Factories);
        }

        public void OnTextChanged()
        {
            if (SelectedCustomer != null && SelectedFactory != null && !string.IsNullOrEmpty(ProjectCode) && !string.IsNullOrEmpty(ProjectName))
            {
                IsUploadable = true;
            }
            else
            {
                IsUploadable = false;
            }
        }

        public async void CheckButton()
        {
            await ChangeProjectAsync();
            TryClose();
            
            //var detailProjectViewModel = new DetailProjectViewModel(_windowManager, _events);
            _events.PublishOnUIThread(ChangedProject);
        }

        public void CloseButton()
        {
            TryClose();
        }
        #endregion
    }
}
