using Caliburn.Micro;
using ProjectManager.Models;
using ProjectManager.ViewModels.Dialogs;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProjectManager.ViewModels.UserControls
{
    public class MainViewModel : Conductor<Screen>, IScreen
    {
        #region Construction
        public MainViewModel(IWindowManager windowManager, IEventAggregator events)
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
        private BindableCollection<DetailPostModel> _posts = new BindableCollection<DetailPostModel>();
        public BindableCollection<DetailPostModel> Posts
        {
            get { return _posts; }
            set
            {
                _posts = value;
                NotifyOfPropertyChange(() => Posts);
            }
        }

        private DetailPostModel _selectedPost;
        public DetailPostModel SelectedPost
        {
            get { return _selectedPost; }
            set
            {
                _selectedPost = value;
                NotifyOfPropertyChange(() => SelectedPost);
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

        private ManagerCountModel _selectedManagerCounts;
        public ManagerCountModel SelectedManagerCounts
        {
            get { return _selectedManagerCounts; }
            set
            {
                _selectedManagerCounts = value;
                NotifyOfPropertyChange(() => SelectedManagerCounts);
            }
        }

        private BindableCollection<ManagerCountModel> _managerCounts = new BindableCollection<ManagerCountModel>();
        public BindableCollection<ManagerCountModel> ManagerCounts
        {
            get { return _managerCounts; }
            set
            {
                _managerCounts = value;
                NotifyOfPropertyChange(() => ManagerCounts);
            }
        }

        private string _appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public string AppVersion
        {
            get { return _appVersion; }
            set
            {
                _appVersion = value;
                NotifyOfPropertyChange(() => AppVersion);
            }
        }

        private string _updateHistory;
        public string UpdateHistory
        {
            get { return _updateHistory; }
            set
            {
                _updateHistory = value;
                NotifyOfPropertyChange(() => UpdateHistory);
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
                cmd.CommandText =
                    @"SELECT TOP 20 (SELECT customer_name
                                     FROM   customer
                                     WHERE  customer_id = (SELECT customer_id
                                                           FROM   project
                                                           WHERE  project_id = CO.project_id))
                                    CUSTOMER_NAME,
                                    (SELECT factory_name
                                     FROM   factory
                                     WHERE  factory_id = (SELECT factory_id
                                                          FROM   project
                                                          WHERE  project_id = CO.project_id))
                                    FACTORY_NAME,
                                    (SELECT project_code
                                     FROM   project
                                     WHERE  project_id = CO.project_id)
                                    PROJECT_CODE,
                                    (SELECT project_name
                                     FROM   project
                                     WHERE  project_id = CO.project_id)
                                    PROJECT_NAME,
                                    (SELECT category_name
                                     FROM   category
                                     WHERE  category_id = CO.category_id)
                                    CATEGORY_NAME,
                                    (SELECT category_color
                                     FROM   category
                                     WHERE  category_id = CO.category_id)
                                    CATEGORY_COLOR,
                                    (SELECT logo
                                     FROM   customer
                                     WHERE  customer_id = (SELECT customer_id
                                                           FROM   project
                                                           WHERE  project_id = CO.project_id)) LOGO
                      FROM   content CO
                      ORDER  BY content_id DESC ";

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

                        Posts.Add(new DetailPostModel()
                        {
                            CustomerName = rdr["CUSTOMER_NAME"].ToString(),
                            FactoryName = rdr["FACTORY_NAME"].ToString(),
                            ProjectCode = rdr["PROJECT_CODE"].ToString(),
                            ProjectName = rdr["PROJECT_NAME"].ToString(),
                            CategoryName = rdr["CATEGORY_NAME"].ToString(),
                            CustomerLogo = image,
                            Color = rdr["CATEGORY_COLOR"] != DBNull.Value ? (Brush)bc.ConvertFrom(rdr["CATEGORY_COLOR"]) : null,
                        });
                    }
                    rdr.Close();
                }
            }
        }

        public async Task LoadSelectedPostAsync()
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
                       WHERE  project_code = '{SelectedPost.ProjectCode}'
                              AND project_name = '{SelectedPost.ProjectName}'
                              AND customer_id = (SELECT customer_id
                                                 FROM   customer
                                                 WHERE  customer_name = '{SelectedPost.CustomerName}')
                              AND factory_id = (SELECT factory_id
                                                FROM   factory
                                                WHERE  factory_name = '{SelectedPost.FactoryName}') ";

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

                        SelectedProject = new ProjectModel()
                        {
                            Date = rdr["DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["DATE"]).ToString("yyyy.MM.dd HH:mm") : "-",

                            ProjectId = (int)rdr["PROJECT_ID"],
                            ProjectCode = rdr["PROJECT_CODE"].ToString(),
                            ProjectName = rdr["PROJECT_NAME"].ToString(),

                            UserId = (int)rdr["USER_ID"],
                            Username = rdr["USERNAME"].ToString(),

                            CustomerId = (int)rdr["CUSTOMER_ID"],
                            CustomerName = rdr["CUSTOMER_NAME"].ToString(),
                            CustomerLogo = image,

                            FactoryId = (int)rdr["FACTORY_ID"],
                            FactoryName = rdr["FACTORY_NAME"].ToString(),

                            CategoryId = (int)rdr["CATEGORY_ID"],
                            CategoryName = rdr["CATEGORY_NAME"].ToString(),
                            Color = rdr["CATEGORY_COLOR"] != DBNull.Value ? (Brush)bc.ConvertFrom(rdr["CATEGORY_COLOR"]) : null,
                        };

                        if (SelectedProject != null)
                        {
                            var detailProjectViewModel = new DetailProjectViewModel(new WindowManager(), _events);
                            _events.PublishOnUIThread(new ProjectModelSender(SelectedProject));
                            _windowManager.ShowDialog(detailProjectViewModel);
                            if (detailProjectViewModel.IsUpdated)
                            {
                                Posts.Clear();
                                await LoadPostsAsync();
                            }
                        }
                    }
                    rdr.Close();
                }
            }
        }

        public async Task LoadManagerCountsAsync()
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
                    @"SELECT QRY2.username,
                             QRY2.position,
                             Isnull(count, 0) COUNT,
                             Isnull(total, 0) TOTAL
                      FROM   (SELECT Isnull((SELECT username
                                             FROM   userinfo
                                             WHERE  user_id = P.user_id), '미지정') USERNAME,
                                     (SELECT position
                                      FROM   userinfo
                                      WHERE  user_id = P.user_id)                      POSITION,
                                     Count(*)                                          COUNT
                              FROM   project P
                              WHERE  (SELECT TOP 1 A.category_priority
                                      FROM   category A
                                             INNER JOIN content B
                                                     ON A.category_id = B.category_id
                                                        AND B.project_id = (SELECT project_id
                                                                            FROM   project
                                                                            WHERE
                                                            project_code = P.project_code
                                                            AND project_id = P.project_id)
                                      ORDER  BY A.category_priority DESC,
                                                A.category_id DESC) <= 5
                              GROUP  BY user_id) QRY1
                             RIGHT OUTER JOIN (SELECT Isnull((SELECT username
                                                              FROM   userinfo
                                                              WHERE  user_id = P.user_id), '미지정'
                                                      )
                                                                               USERNAME,
                                                      (SELECT position
                                                       FROM   userinfo
                                                       WHERE  user_id = P.user_id)
                                                                               POSITION,
                                                      Count(*)
                                                      TOTAL
                                               FROM   project P
                                               GROUP  BY user_id) QRY2
                                           ON QRY1.username = QRY2.username ";

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (await rdr.ReadAsync())
                    {
                        ManagerCounts.Add(new ManagerCountModel()
                        {
                            Name = rdr["USERNAME"].ToString(),
                            Position = rdr["POSITION"].ToString(),
                            Count = (int)rdr["COUNT"],
                            Total = (int)rdr["TOTAL"]
                        });
                    }
                    rdr.Close();
                }
            }
        }

        public async Task LoadUpdateHistoryAsync()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                string appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                await conn.OpenAsync();
                cmd.Connection = conn;
                cmd.CommandText = 
                    $@"SELECT history
                       FROM version
                       WHERE version = '{appVersion}'
                       ORDER BY version_id DESC ";

                object pathObj = await cmd.ExecuteScalarAsync();
                try
                {
                    if (pathObj != null)
                    {
                        UpdateHistory = pathObj.ToString();
                    }
                }
                catch { }
            }
        }

        public async void OnLoaded()
        {
            var tasks = new Task[]
            {
                LoadPostsAsync(),
                LoadManagerCountsAsync(),
                LoadUpdateHistoryAsync(),
            };
            await Task.WhenAll(tasks);
        }

        public async void OnPostDataGridMouseDoubleClick()
        {
            await LoadSelectedPostAsync();
        }

        public void OnManagerCountsDataGridMouseDoubleClick()
        {
            var parent = (Conductor<Screen>)this.Parent;
            parent.ActivateItem(new SearchViewModel(_windowManager, _events));
            _events.PublishOnUIThread(SelectedManagerCounts.Name);
        }
        #endregion
    }
}
