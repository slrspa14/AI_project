using System.Windows;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

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
        string[] divide;//구분용
        string msg;//받아온 데이터 변환용
        byte[] file_length;//수신한 파일 길이용
        byte[] file_data;//수신한 파일내용
        FileStream filestr;
        int fileLength;
        int cnt = 0;

        //클라구분용
        Dictionary<string, NetworkStream> client_Distinguish = new Dictionary<string, NetworkStream>();

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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                server.Stop();
            }
        }
        private void Connect()
        {
            client = server.AcceptTcpClient();
            stream = client.GetStream();
            while (true)
            {
                int length;
                byte[] data = new byte[128];
                while ((length = stream.Read(data, 0, data.Length)) != 0)//파일크기 받고 파일받기
                {
                    try
                    {
                        byte[] size = new byte[4];
                        stream.Read(size, 0, size.Length);
                        fileLength = BitConverter.ToInt32(size, 0);

                        filestr = new FileStream("../../test" + cnt + ".png", FileMode.Create, FileAccess.Write);
                        cnt++;
                        int byteread;//리드한 파일 크기 담을거

                        BinaryWriter write = new BinaryWriter(filestr);

                        byte[] buffer = new byte[fileLength];
                        //MessageBox.Show("여기");
                        byteread = stream.Read(buffer, 0, buffer.Length);
                        write.Write(buffer, 0, byteread);
                        filestr.Close();
                        Array.Clear(size, 0, size.Length);
                        Array.Clear(buffer, 0, buffer.Length);
                        //데이터 받고 구분자로 파이썬, WPF 구분하기
                        //msg = Encoding.Default.GetString(data);
                        //divide = msg.Split('/');
                        //if(divide[0] == "1")
                        //{
                        //    //WPF client 정보저장하기
                        //    //client_Distinguish.Add(divide[1], stream);
                        //    //파일수신 및 파이썬한테 전송하기
                        //    if(divide[1] == "1")
                        //    {
                        //        //파일수신 테스트
                        //        stream.Read(size, 0, size.Length);
                        //        fileLength = BitConverter.ToInt32(size, 0);

                        //        string lineNumber = lineNameAndStream.FirstOrDefault(x => x.Value == stream).Key;

                        //        fileStr = new FileStream("../../Fimg.png", FileMode.Create, FileAccess.Write);
                        //        string a = "Fimg" + lineNumber + imageFileNumberCount.ToString() + ".png";
                        //        int byteread;//리드한 파일 크기 담을거

                        //        BinaryWriter write = new BinaryWriter(fileStr);

                        //        byte[] buffer = new byte[fileLength];

                        //        byteread = stream.Read(buffer, 0, buffer.Length);
                        //        write.Write(buffer, 0, byteread);
                        //        fileStr.Close();
                        //    }

                        //}
                        //else if(divide[0] == "2")
                        //{
                        //    //python 정보저장하기
                        //    client_Distinguish.Add(divide[1], stream);
                        //    //검사결과 및 파일수신하기, DB저장하기

                        //}
                    }
                    catch(Exception s)
                    {
                        MessageBox.Show(s.ToString());
                    }

                }
                client.Close();
                stream.Close();
            }
        }
        private async void send_python(byte[] file_length, byte[] file_data)
        {
            await Task.Run(async () =>
            {
                await Task.Delay(1000);
                //파일크기 보내주기
                stream.Write(file_length, 0, file_length.Length);
                //파일데이터 보내주기
                stream.Write(file_data, 0, file_data.Length);

                Array.Clear(file_length, 0, file_length.Length);
                Array.Clear(file_data, 0, file_data.Length);
            });
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
