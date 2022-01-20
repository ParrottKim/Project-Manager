using Caliburn.Micro;
using ProjectManager.Models;
using ProjectManager.ViewModels.Dialogs;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProjectManager.ViewModels.UserControls
{
    public class SiteViewModel : Conductor<Screen>, IScreen
    {
        #region Construction
        public SiteViewModel(IWindowManager windowManager, IEventAggregator events)
        {
            _windowManager = windowManager;
            _events = events;
        }
        #endregion

        #region Members
        private readonly IWindowManager _windowManager = new WindowManager();
        private readonly IEventAggregator _events;
        #endregion

        #region Properties
        private BindableCollection<ProjectModel> _projects = new BindableCollection<ProjectModel>();
        public BindableCollection<ProjectModel> Projects
        {
            get { return _projects; }
            set
            {
                _projects = value;
                NotifyOfPropertyChange(() => Projects);
            }
        }

        private ProjectModel _selectedProject;
        public ProjectModel SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                _selectedProject = value;
                NotifyOfPropertyChange(() => SelectedProject);
            }
        }

        private bool _isFactorySelectable = false;
        public bool IsFactorySelectable
        {
            get { return _isFactorySelectable; }
            set
            {
                _isFactorySelectable = value;
                NotifyOfPropertyChange(() => IsFactorySelectable);
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

        private bool _isSearchEnabled = false;
        public bool IsSearchEnabled
        {
            get { return _isSearchEnabled; }
            set
            {
                _isSearchEnabled = value;
                NotifyOfPropertyChange(() => IsSearchEnabled);
            }
        }

        private bool _isDataGridVisible = false;
        public bool IsDataGridVisible
        {
            get { return _isDataGridVisible; }
            set
            {
                _isDataGridVisible = value;
                NotifyOfPropertyChange(() => IsDataGridVisible);
            }
        }

        private bool _isAuthorized = false;
        public bool IsAuthorized
        {
            get { return _isAuthorized; }
            set
            {
                _isAuthorized = value;
                NotifyOfPropertyChange(() => IsAuthorized);
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
                    @"SELECT CUSTOMER_NAME 
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

        public static async Task LoadProjectsAsync(string selectedCustomer, string selectedFactory, BindableCollection<ProjectModel> projects)
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
                    $@"SELECT PR.date,
                              PR.project_id,
                              PR.project_code,
                              PR.project_name,
                              PR.customer_id,
                              PR.factory_id,
                              Isnull(PR.user_id, 1000)                     USER_ID,
                              (SELECT username
                               FROM   userinfo
                               WHERE  user_id = PR.user_id)                USERNAME,
                              (SELECT customer_name
                               FROM   customer
                               WHERE  customer_id = PR.customer_id)        CUSTOMER_NAME,
                              (SELECT factory_name
                               FROM   factory
                               WHERE  factory_id = PR.factory_id)          FACTORY_NAME,
                              Isnull((SELECT TOP 1 A.category_id
                                      FROM   category A
                                             INNER JOIN content B
                                                     ON A.category_id = B.category_id
                                                        AND B.project_id = (SELECT project_id
                                                                            FROM   project
                                                                            WHERE
                                                            project_code = PR.project_code
                                                            AND project_id = PR.project_id)
                                      ORDER  BY A.category_priority DESC,
                                                A.category_id DESC), 1000) CATEGORY_ID,
                              Isnull((SELECT TOP 1 A.category_priority
                                      FROM   category A
                                             INNER JOIN content B
                                                     ON A.category_id = B.category_id
                                                        AND B.project_id = (SELECT project_id
                                                                            FROM   project
                                                                            WHERE
                                                            project_code = PR.project_code
                                                            AND project_id = PR.project_id)
                                      ORDER  BY A.category_priority DESC,
                                                B.content_id DESC), 1000)  CATEGORY_PRIORITY,
                              (SELECT TOP 1 A.category_name
                               FROM   category A
                                      INNER JOIN content B
                                              ON A.category_id = B.category_id
                                                 AND B.project_id = (SELECT project_id
                                                                     FROM   project
                                                                     WHERE
                                                     project_code = PR.project_code
                                                     AND project_id = PR.project_id)
                               ORDER  BY A.category_priority DESC,
                                         A.category_id DESC)               CATEGORY_NAME,
                              (SELECT TOP 1 A.category_color
                               FROM   category A
                                      INNER JOIN content B
                                              ON A.category_id = B.category_id
                                                 AND B.project_id = (SELECT project_id
                                                                     FROM   project
                                                                     WHERE
                                                     project_code = PR.project_code
                                                     AND project_id = PR.project_id)
                               ORDER  BY A.category_priority DESC,
                                         A.category_id DESC)               CATEGORY_COLOR,
                              (SELECT logo
                               FROM   customer
                               WHERE  customer_id = PR.customer_id)        LOGO
                       FROM   project PR
                       WHERE  customer_id = (SELECT customer_id
                                             FROM   customer
                                             WHERE  customer_name = '{selectedCustomer}')
                              AND factory_id = (SELECT factory_id
                                                FROM   factory
                                                WHERE  factory_name = '{selectedFactory}')
                       ORDER  BY project_id DESC ";

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (await rdr.ReadAsync())
                    {
                        byte[] imageBlob = rdr["LOGO"] as byte[];
                        MemoryStream ms = new MemoryStream(imageBlob);
                        ms.Seek(0, SeekOrigin.Begin);

                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = ms;
                        image.EndInit();

                        var bc = new BrushConverter();

                        projects.Add(new ProjectModel()
                        {
                            Date = rdr["DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["DATE"]).ToString("yyyy.MM.dd HH:mm") : "-",

                            ProjectId = (int)rdr["PROJECT_ID"],
                            ProjectCode = rdr["PROJECT_CODE"].ToString(),
                            ProjectName = rdr["PROJECT_NAME"].ToString(),

                            UserId = rdr["USER_ID"] != DBNull.Value ? (int)rdr["USER_ID"] : 1000,
                            Username = rdr["USERNAME"].ToString(),

                            CustomerId = (int)rdr["CUSTOMER_ID"],
                            CustomerName = rdr["CUSTOMER_NAME"].ToString(),
                            CustomerLogo = image,

                            FactoryId = (int)rdr["FACTORY_ID"],
                            FactoryName = rdr["FACTORY_NAME"].ToString(),

                            CategoryId = (int)rdr["CATEGORY_ID"],
                            CategoryPriority = (int)rdr["CATEGORY_PRIORITY"],
                            CategoryName = rdr["CATEGORY_NAME"].ToString(),
                            Color = rdr["CATEGORY_COLOR"] != DBNull.Value ? (Brush)bc.ConvertFrom(rdr["CATEGORY_COLOR"]) : null,
                        });
                    }
                    rdr.Close();
                }
            }
        }

        public async void OnLoaded()
        {
            await LoadCustomersAsync(Customers);
        }

        public async void OnCustomerSelectionChanged(object source)
        {
            IsDataGridVisible = false;
            IsFactorySelectable = true;

            Factories = new BindableCollection<string>();
            SelectedFactory = null;

            await LoadFactoriesAsync(SelectedCustomer, Factories);
        }

        public void OnFactorySelectionChanged(object source)
        {
            IsDataGridVisible = false;
            IsSearchEnabled = true;
        }

        public async void OnMouseDoubleClick()
        {
            if (SelectedProject != null)
            {
                var detailProjectViewModel = new DetailProjectViewModel(new WindowManager(), _events);
                _events.PublishOnUIThread(new ProjectModelSender(SelectedProject));
                _windowManager.ShowDialog(detailProjectViewModel);
                if (detailProjectViewModel.IsUpdated)
                {
                    Projects.Clear();
                    await LoadProjectsAsync(SelectedCustomer, SelectedFactory, Projects);
                }
            }
        }

        public async void SearchProjectsButton()
        {
            IsDataGridVisible = true;
            Projects.Clear();
            await LoadProjectsAsync(SelectedCustomer, SelectedFactory, Projects);
        }

        public void AddProjectButton()
        {
            if (Properties.Settings.Default.UserAuth)
            {
                var addProjectViewModel = new AddProjectViewModel();
                _windowManager.ShowDialog(addProjectViewModel);
                if (addProjectViewModel.IsUploaded)
                {
                    Projects.Clear();
                }
            }
            else
                IsAuthorized = true;
        }

        public void OnCloseDialogClick()
        {
            IsAuthorized = false;
        }
        #endregion
    }
}
