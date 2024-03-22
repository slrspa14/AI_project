using System.Windows;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AI_project_server
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private string bindIP = "10.10.20.113";
        private const int bindPort = 9196;
        private TcpListener server = null;
        private IPEndPoint localAddress;
        TcpClient client;
        NetworkStream stream;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Open_btn_Click(object sender, RoutedEventArgs e)
        {
            //서버열기
            //listen
            try
            {
                localAddress = new IPEndPoint(IPAddress.Parse(bindIP), bindPort);
                server = new TcpListener(localAddress);
                server.Start();
                MessageBox.Show("Server Open");
                Thread t1 = new Thread(new ThreadStart(Connect));
                t1.Start();
                //Connect_test();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                server.Stop();
            }
        }
        private void Connect()
        {
            while (true)
            {
                client = server.AcceptTcpClient();
                stream = client.GetStream();
                int length;
                byte[] data = new byte[128];
                while ((length = stream.Read(data, 0, data.Length)) != 0)//파일크기 받고 파일받기
                {
                    //연결 확인용
                    string test = Encoding.Default.GetString(data);
                    MessageBox.Show(test);
                    byte[] test_msg = new byte[128];
                    test_msg = Encoding.Default.GetBytes("가나요");//잘 옴
                    stream.Write(test_msg, 0, test_msg.Length);
                }
            }
        }
        //private async void Connect_test()
        //{
        //    await Task.Run(async () =>
        //    {
        //        while (true)
        //        {
        //            await Task.Delay(1000);
        //            client = server.AcceptTcpClient();
        //            stream = client.GetStream();
        //            int length;
        //            byte[] data = new byte[128];
        //            while ((length = stream.Read(data, 0, data.Length)) != 0)//파일크기 받고 파일받기//클라가 연결 끊었을 때
        //            {
        //                //연결 확인용
        //                string test = Encoding.Default.GetString(data);
        //                MessageBox.Show(test);
        //                byte[] test_msg = new byte[128];
        //                test_msg = Encoding.Default.GetBytes("가나요");//잘 옴
        //                stream.Write(test_msg, 0, test_msg.Length);
        //            }
        //        }
        //    });
            
        //}
    }
}
