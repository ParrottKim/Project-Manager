using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;

namespace ProjectManager.ViewModels.Dialogs
{
    public class AddProjectViewModel : Screen
    {
        #region Construction
        public AddProjectViewModel()
        {

        }
        #endregion

        #region Members
        #endregion

        #region Properties
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

        private bool _isSelectable = false;
        public bool IsSelectable
        {
            get { return _isSelectable; }
            set
            {
                _isSelectable = value;
                NotifyOfPropertyChange(() => IsSelectable);
            }
        }

        private bool _isDuplicated = true;
        public bool IsDuplicated
        {
            get { return _isDuplicated; }
            set
            {
                _isDuplicated = value;
                NotifyOfPropertyChange(() => IsDuplicated);
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

        private PackIconKind _noticeIcon = PackIconKind.Close;
        public PackIconKind NoticeIcon
        {
            get { return _noticeIcon; }
            set
            {
                _noticeIcon = value;
                NotifyOfPropertyChange(() => NoticeIcon);
            }
        }

        private Brush _noticeColor = Brushes.Green;
        public Brush NoticeColor
        {
            get { return _noticeColor; }
            set
            {
                _noticeColor = value;
                NotifyOfPropertyChange(() => NoticeColor);
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
                    $@"SELECT CUSTOMER_NAME 
                       FROM   CUSTOMER";

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

        public async Task CheckDuplication()
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
                    $@"SELECT Count(*)
                       FROM   project
                       WHERE  project_code = '{ProjectCode}' ";
                object pathObj = await cmd.ExecuteScalarAsync();
                {
                    if (DBNull.Value != null)
                    {
                        if ((int)pathObj >= 1)
                        {
                            IsDuplicated = true;
                            NoticeIcon = PackIconKind.Close;
                        }
                        else
                        {
                            IsDuplicated = false;
                            NoticeIcon = PackIconKind.CircleOutline;

                            if (!string.IsNullOrEmpty(ProjectName))
                                IsUploadable = true;
                        }
                    }
                }
            }
        }

        public static async Task UploadNewProjectAsync(string customer, string factory, string projectCode, string projectName)
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            int customerId = 0;
            int factoryId = 0;

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText =
                    $@"SELECT customer_id,
                              factory_id
                       FROM   factory
                       WHERE  factory_name = '{factory}' ";
                try
                {
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        await rdr.ReadAsync();
                        customerId = (int)rdr["CUSTOMER_ID"];
                        factoryId = (int)rdr["FACTORY_ID"];
                    }
                }
                catch { }
            }

            if (customerId != 0 && factoryId != 0)
            {
                using (SqlConnection conn = new SqlConnection(strConn))
                using (SqlCommand cmd = new SqlCommand())
                {
                    await conn.OpenAsync();
                    cmd.Connection = conn;
                    cmd.CommandText =
                        $@"INSERT INTO project
                                       (
                                                   project_code,
                                                   project_name,
                                                   customer_id,
                                                   factory_id,
                                                   date
                                       )
                                       VALUES
                                       (
                                                   '{projectCode}',
                                                   '{projectName.Replace("'", "''")}', 
		                              	           {customerId}, 
		                              	           {factoryId}, 
		                              	           '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}'
	                                   )";

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async void OnLoaded()
        {
            await LoadCustomersAsync(Customers);
        }

        public async void OnCustomerSelectionChanged(object source)
        {
            IsSelectable = true;

            Factories = new BindableCollection<string>();
            SelectedFactory = null;

            await LoadFactoriesAsync(SelectedCustomer, Factories);
        }

        public async void DuplicationCheckButton()
        {
            await CheckDuplication();
        }

        public void OnTextChanged()
        {
            if (SelectedCustomer != null && SelectedFactory != null && !IsDuplicated && !string.IsNullOrEmpty(ProjectName))
            {
                IsUploadable = true;
            }
            else
            {
                IsUploadable = false;
            }
        }

        public void CloseButton()
        {
            TryClose();
        }

        public async void CheckButton()
        {
            if (!string.IsNullOrEmpty(SelectedCustomer) && !string.IsNullOrEmpty(SelectedFactory) && !string.IsNullOrEmpty(ProjectCode) && !string.IsNullOrEmpty(ProjectName))
            {
                await UploadNewProjectAsync(SelectedCustomer, SelectedFactory, ProjectCode, ProjectName);
                IsUploaded = true;
                TryClose();
            }
            else MessageBox.Show("미작성 요소가 있습니다");
        }
        #endregion
    }
}
