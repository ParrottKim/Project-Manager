using Caliburn.Micro;
using ProjectManager.Extensions;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace ProjectManager.ViewModels.Dialogs
{
    public class ProgramUpdateViewModel : Conductor<Screen>, IScreen, IHandle<string>
    {
        #region Construction
        public ProgramUpdateViewModel(IWindowManager windowManager, IEventAggregator events)
        {
            _windowManager = windowManager;
            _events = events;
            _events.Subscribe(this);

            outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);
        }

        public void Handle(string sender)
        {
            AppVersion = sender;
        }
        #endregion

        #region Members
        private readonly IWindowManager _windowManager = new WindowManager();
        private readonly IEventAggregator _events;
        private FtpRequest _ftpRequset = new FtpRequest();
        private string outputFolder;
        #endregion

        #region Properties
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
        #endregion

        #region Methods
        //public void DownloadUpdateFileAsync()
        //{
        //    string ftpPath = $"ftp://{Properties.Settings.Default.IpAddress}:12000/프로그램";
        //    //string ftpPath = $"ftp://127.0.0.1:12000/프로그램";
        //    string user = "user";
        //    string pwd = "dantech";

        //    FtpWebRequest req = (FtpWebRequest)WebRequest.Create($"{ftpPath}/publish_{AppVersion}.zip");
        //    req.EnableSsl = true;
        //    System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
        //    req.Method = WebRequestMethods.Ftp.DownloadFile;
        //    req.Credentials = new NetworkCredential(user, pwd);

        //    using (Stream sourceStream = req.GetResponse().GetResponseStream())
        //    using (Stream targetStream = File.Create(Path.Combine(outputFolder, $"publish_{AppVersion}.zip")))
        //    {
        //        //ftpStream.CopyTo(fileStream);
        //        byte[] buffer = new byte[10240];
        //        int read;
        //        int fullSize = (int)GetFullSize($"{ftpPath}/publish_{AppVersion}.zip");

        //        var progressViewModel = new ProgressViewModel();
        //        _windowManager.ShowWindow(progressViewModel);
        //        progressViewModel.Title = "다운로드 중...";
        //        //IsServerRunning = false;

        //        while ((read = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
        //        {
        //            targetStream.Write(buffer, 0, read);
        //            int position = (int)targetStream.Position;

        //            // ProgressBar Value 설정
        //            // TextBlock 설정

        //            //progressViewModel.MaximumProgress = fullSize;
        //            //progressViewModel.CurrentProgress = position;
        //            //progressViewModel.InfoText = "Downloading... [" + targetStream.Position + "/" + fullSize + "]";
        //        }
        //        //progressViewModel.InfoText = "Download Completed";
        //        progressViewModel.TryClose();
        //        //IsServerRunning = true;
        //    }
        //}

        private long GetFullSize(string fileUrl)
        {
            string user = "user";
            string pwd = "dantech";

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fileUrl);
            request.EnableSsl = true;
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
            request.Credentials = new NetworkCredential(user, pwd);
            request.Method = WebRequestMethods.Ftp.GetFileSize;

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                long size = (long)response.ContentLength;
                return size;
            }
        }

        public async void CheckButton()
        {
            IsServerRunning = true;
            await Task.Run(() => _ftpRequset.FtpDownloadRequest($"publish_{AppVersion}.zip", "프로그램", outputFolder, true));

            string unzipDir = $"{outputFolder}\\{AppVersion}";
            string unzipFile = $"{outputFolder}\\publish_{AppVersion}.zip";

            if (!Directory.Exists(unzipDir))
                Directory.CreateDirectory(unzipDir);
            try
            {
                ZipFile.ExtractToDirectory(unzipFile, unzipDir);
            }
            catch { }

            File.Delete(unzipFile);
            System.Diagnostics.Process.Start(Path.Combine(unzipDir, $"ProjectManager.Package.msi"));
            IsServerRunning = false;
            IsUpdated = true;
            TryClose();
        }

        public void CloseButton()
        {
            TryClose();
        }
        #endregion
    }
}
