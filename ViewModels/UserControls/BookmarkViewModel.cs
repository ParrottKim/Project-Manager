using Caliburn.Micro;
using ProjectManager.Models;
using ProjectManager.ViewModels.Dialogs;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProjectManager.ViewModels.UserControls
{
    public class BookmarkViewModel : Conductor<Screen>, IScreen
    {
        #region Construction
        public BookmarkViewModel(IWindowManager windowManager, IEventAggregator events)
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
        #endregion

        #region Methods
        public static async Task LoadProjectsAsync(BindableCollection<ProjectModel> projects)
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
                    $@"SELECT A.date,
                              A.project_id,
                              A.project_code,
                              A.project_name,
                              A.customer_id,
                              A.factory_id,
                              Isnull(A.user_id, 1000)                      USER_ID,
                              (SELECT username
                               FROM   userinfo
                               WHERE  user_id = A.user_id)                 USERNAME,
                              (SELECT customer_name
                               FROM   customer
                               WHERE  customer_id = A.customer_id)         CUSTOMER_NAME,
                              (SELECT factory_name
                               FROM   factory
                               WHERE  factory_id = A.factory_id)           FACTORY_NAME,
                              Isnull((SELECT TOP 1 C.category_id
                                      FROM   category C
                                             INNER JOIN content D
                                                     ON C.category_id = D.category_id
                                                        AND D.project_id = (SELECT project_id
                                                                            FROM   project
                                                                            WHERE
                                                            project_code = A.project_code
                                                            AND project_id = A.project_id)
                                      ORDER  BY C.category_priority DESC,
                                                C.category_id DESC), 1000) CATEGORY_ID,
                              Isnull((SELECT TOP 1 C.category_priority
                                      FROM   category C
                                             INNER JOIN content D
                                                     ON C.category_id = D.category_id
                                                        AND B.project_id = (SELECT project_id
                                                                            FROM   project
                                                                            WHERE
                                                            project_code = A.project_code
                                                            AND project_id = A.project_id)
                                      ORDER  BY C.category_priority DESC,
                                                D.content_id DESC), 1000)  CATEGORY_PRIORITY,
                              (SELECT TOP 1 C.category_name
                               FROM   category C
                                      INNER JOIN content D
                                              ON C.category_id = D.category_id
                                                 AND D.project_id = (SELECT project_id
                                                                     FROM   project
                                                                     WHERE
                                                     project_code = A.project_code
                                                     AND project_id = A.project_id)
                               ORDER  BY C.category_priority DESC,
                                         C.category_id DESC)               CATEGORY_NAME,
                              (SELECT TOP 1 C.category_color
                               FROM   category C
                                      INNER JOIN content D
                                              ON C.category_id = D.category_id
                                                 AND D.project_id = (SELECT project_id
                                                                     FROM   project
                                                                     WHERE
                                                     project_code = A.project_code
                                                     AND project_id = A.project_id)
                               ORDER  BY C.category_priority DESC,
                                         C.category_id DESC)               CATEGORY_COLOR,
                              (SELECT CU.logo
                               FROM   customer AS CU
                               WHERE  A.customer_id = CU.customer_id)      LOGO
                       FROM   project A
                              INNER JOIN bookmark B
                                      ON A.project_id = B.project_id
                       WHERE  B.user_id = (SELECT user_id
                                           FROM   userinfo
                                           WHERE  email = '{Properties.Settings.Default.UserEmail}') ";

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

        public static async Task DeleteBookmarkAsync(string projectCode)
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
                    $@"DELETE FROM bookmark
                       WHERE  user_id = (SELECT user_id
                                         FROM   userinfo
                                         WHERE  email = '{Properties.Settings.Default.UserEmail}')
                              AND project_id = (SELECT project_id
                                                FROM   project
                                                WHERE  project_code = '{projectCode}') ";
                cmd.ExecuteNonQuery();
            }
        }

        public async void OnLoaded()
        {
            await LoadProjectsAsync(Projects);
        }

        public async void OnDeleteButtonClick(object source)
        {
            Button button = source as Button;
            ProjectModel project = (ProjectModel)button.DataContext;

            await DeleteBookmarkAsync(project.ProjectCode);

            Projects.Clear();
            await LoadProjectsAsync(Projects);
        }

        public async void OnMouseDoubleClick()
        {
            if (SelectedProject != null)
            {
                var detailPostViewModel = new DetailProjectViewModel(new WindowManager(), _events);
                _events.PublishOnUIThread(new ProjectModelSender(SelectedProject));
                _windowManager.ShowDialog(detailPostViewModel);
                if (!detailPostViewModel.IsBookmarked)
                {
                    Projects.Clear();
                    await LoadProjectsAsync(Projects);
                }
            }
        }
        #endregion
    }
}
