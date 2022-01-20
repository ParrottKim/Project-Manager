using Caliburn.Micro;
using ProjectManager.Models;
using ProjectManager.ViewModels.Dialogs;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProjectManager.ViewModels.UserControls
{
    public class SearchViewModel : Conductor<Screen>, IScreen, IHandle<string>
    {
        #region Construction
        public SearchViewModel(IWindowManager windowManager, IEventAggregator events)
        {
            _windowManager = windowManager;
            _events = events;
            _events.Subscribe(this);
        }

        public void Handle(string sender)
        {
            ProjectManagerChecked = true;
            IsEnabled = true;
            Keyword = sender;
            SearchButton();
        }
        #endregion

        #region Members
        private readonly IWindowManager _windowManager = new WindowManager();
        private readonly IEventAggregator _events;
        #endregion

        #region Properties
        private bool _factoryChecked;
        public bool FactoryChecked
        {
            get { return _factoryChecked; }
            set
            {
                _factoryChecked = value;
                NotifyOfPropertyChange(() => FactoryChecked);
            }
        }

        private bool _projectNameChecked;
        public bool ProjectNameChecked
        {
            get { return _projectNameChecked; }
            set
            {
                _projectNameChecked = value;
                NotifyOfPropertyChange(() => ProjectNameChecked);
            }
        }

        private bool _projectManagerChecked;
        public bool ProjectManagerChecked
        {
            get { return _projectManagerChecked; }
            set
            {
                _projectManagerChecked = value;
                NotifyOfPropertyChange(() => ProjectManagerChecked);
            }
        }

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

        private string _keyword;
        public string Keyword
        {
            get { return _keyword; }
            set
            {
                _keyword = value;
                NotifyOfPropertyChange(() => Keyword);
            }
        }

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

        private bool _isDataGridVisible;
        public bool IsDataGridVisible
        {
            get { return _isDataGridVisible; }
            set
            {
                _isDataGridVisible = value;
                NotifyOfPropertyChange(() => IsDataGridVisible);
            }
        }
        #endregion

        #region Methods
        public async Task LoadProjectsAsync()
        {
            string ipAddress = Properties.Settings.Default.IpAddress;
            string databasePort = Properties.Settings.Default.DatabasePort.ToString();
            string strConn = $"Data Source={ipAddress},{databasePort};Initial Catalog=DOCUMENTS;Persist Security Info=True;User ID=sa;Password=eoladmin";

            using (SqlConnection conn = new SqlConnection(strConn))
            using (SqlCommand cmd = new SqlCommand())
            {
                await conn.OpenAsync();
                cmd.Connection = conn;
                if (FactoryChecked)
                    cmd.CommandText =
                        $@"SELECT PR.date,
                                  PR.project_id,
                                  PR.project_code,
                                  PR.project_name,
                                  PR.customer_id,
                                  PR.factory_id,
                                  Isnull(PR.user_id, 1000)                     USER_ID,
                                  (SELECT US.username
                                   FROM   userinfo AS US
                                   WHERE  PR.user_id = US.user_id)             USERNAME,
                                  (SELECT CU.customer_name
                                   FROM   customer AS CU
                                   WHERE  PR.customer_id = CU.customer_id)     CUSTOMER_NAME,
                                  (SELECT FA.factory_name
                                   FROM   factory AS FA
                                   WHERE  PR.factory_id = FA.factory_id)       FACTORY_NAME,
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
                           WHERE  (SELECT FA.factory_name
                                   FROM   factory AS FA
                                   WHERE  PR.factory_id = FA.factory_id) LIKE '%{Keyword}%'
                           ORDER  BY project_id DESC ";
                else if (ProjectNameChecked)
                    cmd.CommandText =
                        $@"SELECT PR.date,
                                  PR.project_id,
                                  PR.project_code,
                                  PR.project_name,
                                  PR.customer_id,
                                  PR.factory_id,
                                  Isnull(PR.user_id, 1000)                     USER_ID,
                                  (SELECT US.username
                                   FROM   userinfo AS US
                                   WHERE  PR.user_id = US.user_id)             USERNAME,
                                  (SELECT CU.customer_name
                                   FROM   customer AS CU
                                   WHERE  PR.customer_id = CU.customer_id)     CUSTOMER_NAME,
                                  (SELECT FA.factory_name
                                   FROM   factory AS FA
                                   WHERE  PR.factory_id = FA.factory_id)       FACTORY_NAME,
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
                           WHERE  project_name LIKE '%{Keyword}%'
                           ORDER  BY project_id DESC ";
                else if (ProjectManagerChecked)
                    cmd.CommandText =
                        $@"SELECT PR.date,
                                  PR.project_id,
                                  PR.project_code,
                                  PR.project_name,
                                  PR.customer_id,
                                  PR.factory_id,
                                  Isnull(PR.user_id, 1000)                     USER_ID,
                                  (SELECT US.username
                                   FROM   userinfo AS US
                                   WHERE  PR.user_id = US.user_id)             USERNAME,
                                  (SELECT CU.customer_name
                                   FROM   customer AS CU
                                   WHERE  PR.customer_id = CU.customer_id)     CUSTOMER_NAME,
                                  (SELECT FA.factory_name
                                   FROM   factory AS FA
                                   WHERE  PR.factory_id = FA.factory_id)       FACTORY_NAME,
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
                           WHERE  (SELECT US.username
                                   FROM   userinfo AS US
                                   WHERE  PR.user_id = US.user_id) LIKE '%{Keyword}%'
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

                        Projects.Add(new ProjectModel()
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

        public void OnChecked() => IsEnabled = true;

        public void SearchKeyDown(object source, KeyEventArgs e)
        {
            TextBox textBox = source as TextBox;
            string keyword = textBox.Text;

            if (e.Key == Key.Return)
            {
                Keyword = keyword;
                Search();
            }
        }

        public void SearchButton()
        {
            Search();
        }

        public async void Search()
        {
            Projects.Clear();
            if (!FactoryChecked && !ProjectNameChecked && !ProjectManagerChecked)
                return;
            else
            {
                IsDataGridVisible = true;
                await LoadProjectsAsync();
            }
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
                    await LoadProjectsAsync();
                }
            }
        }
        #endregion
    }
}
