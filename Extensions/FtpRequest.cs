using Caliburn.Micro;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Web;

namespace ProjectManager.Extensions
{
    public class FtpRequest
    {
        string user = "user";
        string pwd = "dantech";

        public bool FtpUploadRequest(string attachment, string filePath)
        {
            string ftpPath = $"ftp://{Properties.Settings.Default.IpAddress}:12000/{filePath}";
            bool isDirectoryExist;

            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpPath);
                request.EnableSsl = true;
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                request.Credentials = new NetworkCredential(user, pwd);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                isDirectoryExist = true;
            }
            catch (Exception)
            {
                isDirectoryExist = false;
            }

            if (isDirectoryExist)
            {
                try
                {
                    FtpWebRequest reqUpload = (FtpWebRequest)WebRequest.Create($"{ftpPath}/{Path.GetFileName(attachment)}");
                    reqUpload.EnableSsl = true;
                    System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                    reqUpload.Credentials = new NetworkCredential(user, pwd);
                    reqUpload.Method = WebRequestMethods.Ftp.UploadFile;

                    using (Stream fileStream = File.OpenRead(attachment))
                    using (Stream ftpUploadStream = reqUpload.GetRequestStream())
                    {
                        fileStream.CopyTo(ftpUploadStream);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if (!isDirectoryExist)
            {
                try
                {
                    FtpWebRequest reqDir = (FtpWebRequest)FtpWebRequest.Create(ftpPath);
                    reqDir.Method = WebRequestMethods.Ftp.MakeDirectory;
                    reqDir.EnableSsl = true;
                    System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                    reqDir.Credentials = new NetworkCredential(user, pwd);
                    reqDir.Method = WebRequestMethods.Ftp.MakeDirectory;

                    FtpWebResponse response = (FtpWebResponse)reqDir.GetResponse();
                    Stream ftpDirStream = response.GetResponseStream();

                    ftpDirStream.Close();
                    response.Close();
                }
                catch (Exception) 
                {
                    return false;
                }

                try
                {
                    FtpWebRequest reqUpload = (FtpWebRequest)WebRequest.Create($"{ftpPath}/{Path.GetFileName(attachment)}");
                    reqUpload.EnableSsl = true;
                    System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                    reqUpload.Credentials = new NetworkCredential(user, pwd);
                    reqUpload.Method = WebRequestMethods.Ftp.UploadFile;

                    using (Stream fileStream = File.OpenRead(attachment))
                    using (Stream ftpUploadStream = reqUpload.GetRequestStream())
                    {
                        fileStream.CopyTo(ftpUploadStream);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }

        public bool FtpDownloadRequest(string fileName, string filePath, string outputFolder, bool isUpdating)
        {
            UriBuilder uriBuilder = new UriBuilder("ftp", Properties.Settings.Default.IpAddress, 12000, $"{filePath}/{fileName}");

            try
            {
                FtpWebRequest req = (FtpWebRequest)WebRequest.Create(uriBuilder.Uri);
                req.EnableSsl = true;
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                req.Method = WebRequestMethods.Ftp.DownloadFile;
                req.Credentials = new NetworkCredential(user, pwd);

                if (File.Exists(Path.Combine(outputFolder, fileName)))
                {
                    int indexOfDot = fileName.LastIndexOf(".");
                    string strName = fileName.Substring(0, indexOfDot);
                    string strExt = fileName.Substring(indexOfDot);

                    bool bExist = true;
                    int fileCount = 0;

                    string dirMapPath = string.Empty;

                    while (bExist)
                    {
                        string pathCombine = Path.Combine(outputFolder, fileName);

                        if (File.Exists(pathCombine))
                        {
                            fileCount++;
                            fileName = strName + "_(" + fileCount + ")" + strExt;
                        }
                        else
                        {
                            bExist = false;
                        }
                    }
                }

                using (Stream sourceStream = req.GetResponse().GetResponseStream())
                using (Stream targetStream = File.Create(Path.Combine(outputFolder, fileName)))
                {
                    //ftpStream.CopyTo(fileStream);
                    byte[] buffer = new byte[10240];
                    int read;
                    //int fullSize = (int)GetFullSize($"{ftpPath}/{fileName}");

                    while ((read = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        targetStream.Write(buffer, 0, read);
                        //int position = (int)targetStream.Position;

                        //Debug.WriteLine($"[{position}/{fullSize}]");

                        // ProgressBar Value 설정
                        // TextBlock 설정

                        //progressViewModel.MaximumProgress = fullSize;
                        //progressViewModel.CurrentProgress = position;
                        //progressViewModel.InfoText = "Downloading... [" + targetStream.Position + "/" + fullSize + "]";
                    }
                }

                if (!isUpdating && Properties.Settings.Default.IsFileOpen)
                {
                    Process.Start(Path.Combine(outputFolder, fileName));
                    Properties.Settings.Default.IsFileOpen = false;
                    Properties.Settings.Default.Save();
                } 
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool FtpDeleteRequest(string fileName, string filePath)
        {
            string ftpPath = $"ftp://{Properties.Settings.Default.IpAddress}:12000/{filePath}";

            try
            {
                FtpWebRequest req = (FtpWebRequest)WebRequest.Create($"{ftpPath}/{fileName}");
                req.EnableSsl = true;
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                req.Method = WebRequestMethods.Ftp.DeleteFile;
                req.Credentials = new NetworkCredential(user, pwd);

                FtpWebResponse response = (FtpWebResponse)req.GetResponse();
                response.Close();
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool FtpRenameRequest(string fileName, string filePath, string destination)
        {
            string ftpPath = $"ftp://{Properties.Settings.Default.IpAddress}:12000/{filePath}/{fileName}";

            try
            {
                FtpWebRequest req = (FtpWebRequest)WebRequest.Create(filePath);
                req.EnableSsl = true;
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                req.Method = WebRequestMethods.Ftp.Rename;
                req.Credentials = new NetworkCredential(user, pwd);
                req.RenameTo = $"../{destination}/{fileName}";

                FtpWebResponse response = (FtpWebResponse)req.GetResponse();
                response.Close();
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        private long GetFullSize(string fileUrl)
        {
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

        public bool CheckFtpConnection()
        {
            FtpWebRequest requestDir = (FtpWebRequest)FtpWebRequest.Create($"ftp://{Properties.Settings.Default.IpAddress}:12000/");
            requestDir.EnableSsl = true;
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
            requestDir.Credentials = new NetworkCredential(user, pwd);
            requestDir.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            try
            {
                FtpWebResponse response = (FtpWebResponse)requestDir.GetResponse();
                response.Close();
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
