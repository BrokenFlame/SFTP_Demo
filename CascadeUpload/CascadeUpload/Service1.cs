using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using Renci.SshNet;


namespace CascadeUpload
{
    public partial class Service1 : ServiceBase
    {
        CascadeController control;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            control = new CascadeController();
        }

        protected override void OnStop()
        {
            control.Dispose();
        }
    }

    class CascadeController: IDisposable
    {
        private FileSystemWatcher watcher;
        private string header;
        private string host;
        private string username;
        private string password;
        private string sftpDir;
        private int port;


        public CascadeController()
        {
            watcher = new FileSystemWatcher();
            header = System.Configuration.ConfigurationManager.AppSettings["header"];
            host = System.Configuration.ConfigurationManager.AppSettings["host"];
            username = System.Configuration.ConfigurationManager.AppSettings["username"];
            password = System.Configuration.ConfigurationManager.AppSettings["password"];
            sftpDir = System.Configuration.ConfigurationManager.AppSettings["sftpDir"];
            port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["port"]);
            watcher.Path = System.Configuration.ConfigurationManager.AppSettings["directory"];

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.csv";
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
        }


        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            InsertHeaderIntoCsvFile(e.FullPath, header);
            UploadFileToSftpServer(host, port, username, password, sftpDir, e.FullPath);
        }

        private void InsertHeaderIntoCsvFile(string filepath, string header)
        {
            string[] csvContents = File.ReadAllLines(filepath);
            List<string> csvContentsAsList = csvContents.ToList();
            csvContentsAsList.Insert(0, header);
            File.WriteAllLines(filepath, csvContentsAsList.ToArray());
        }

        private void UploadFileToSftpServer(string host, int port, string username, string password, string sftpDir, string filepath)
        {
            using (var client = new SftpClient(host, port, username, password))
            {
                client.Connect();
                client.ChangeDirectory(sftpDir);
                using (var fileStream = new FileStream(filepath, FileMode.Open))
                {
                    client.BufferSize = 4 * 1024; // bypass Payload error large files
                    client.UploadFile(fileStream, Path.GetFileName(filepath));
                }
                client.Disconnect();
            }
        }

        public void Dispose()
        {
            watcher.Dispose();
        }
    }
}
