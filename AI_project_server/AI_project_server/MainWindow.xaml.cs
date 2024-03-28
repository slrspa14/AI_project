using System.Windows;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;
using System.IO;
using MySql.Data.MySqlClient;
using System.Windows.Media.Imaging;

namespace AI_project_server
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private string bindIP = "10.10.20.113";
        private const int bindPort = 9195;
        private TcpListener server = null;
        private IPEndPoint localAddress;
        //private NetworkStream stream;
        //private TcpClient client;
        private NetworkStream rev_stream;
        private TcpClient client;

        //db용
        private MySqlConnection sql;

        private object lockObject = new object();
        FileStream read_file, send_to_python;
        string[] divide;//구분용
        string msg;//받아온 데이터 변환용
        //파이썬 스트림용
        string to_python, line_name;

        int fileLength;//파일 길이용
        byte[] buffer;//파일 데이터용

        int numbering = 0, num_test = 0;//테스트용

        //listview용 리스트
        List<string> pass_result = new List<string>();
        List<string> defect_result = new List<string>();

        //클라구분용
        Dictionary<string, NetworkStream> client_Distinguish = new Dictionary<string, NetworkStream>();
        public Dictionary<string, NetworkStream> Client_Distinguish { get => client_Distinguish; set => client_Distinguish = value; }

        public class FailData
        {
            public string Itemf { get; set; }
            public string Timef { get; set; }
            public string Linef { get; set; }
            public string Directorf { get; set; }
            public string Notef { get; set; }
        }
        public class PassData
        {
            public string Itemr { get; set; }
            public string Time { get; set; }
            public string Line { get; set; }
            public string Director { get; set; }
            public string Note { get; set; }
        }


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
                string connectstring = "UID=root; PWD=1234; Server=127.0.0.1; port=3306; Database=image_result";
                sql = new MySqlConnection(connectstring);
                sql.Open();//db 열고
                Handle_client();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류:{ex.Message}");
                server.Stop();
            }
        }
        private async void Handle_client()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    //server.Start();
                    await Task.Delay(1000);
                    client = server.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    IPEndPoint getIP = (IPEndPoint)client.Client.RemoteEndPoint;
                    string IP = getIP.Address.ToString();
                    Dispatcher.Invoke(() =>
                    {
                        IPstate.AppendText("입장 아이피 : " + IP + "\n");
                    });
                    Thread t1 = new Thread(new ParameterizedThreadStart(Connected_client));
                    t1.Start(stream);
                }
            });
        }

        private void Connected_client(object obj)
        {
            NetworkStream stream = (NetworkStream)obj;
            while (true)
            {
                int length;
                byte[] data = new byte[1024];

                while (true)//파일크기 받고 파일받기
                {
                    try
                    {
                        length = stream.Read(data, 0, data.Length);

                        //데이터 받고 구분자로 파이썬, WPF 구분하기
                        msg = Encoding.Default.GetString(data);
                        //MessageBox.Show(msg);
                        divide = msg.Split('/');
                        //stream.Write(data, 0, data.Length);
                        if (divide[0] == "1")
                        {
                            //WPF client 정보저장하기
                            Client_Distinguish.Add(divide[1], stream);
                            line_name = divide[1];

                            //파일수신 및 파이썬한테 전송하기
                            rev_stream = client.GetStream();
                        }
                        else if (divide[0] == "3")
                        {
                            byte[] size = new byte[4];
                            rev_stream.Read(size, 0, size.Length);
                            fileLength = BitConverter.ToInt32(size, 0);
                            //MessageBox.Show(fileLength.ToString() + "wpf클라 파일 수신확인용");
                            read_file = new FileStream("../../WPF_read_image/" + numbering + ".png", FileMode.Create, FileAccess.Write);
                            numbering++;
                            BinaryWriter write = new BinaryWriter(read_file);

                            buffer = new byte[fileLength];
                            rev_stream.Read(buffer, 0, buffer.Length);
                            write.Write(buffer, 0, buffer.Length);
                            read_file.Close();
                            Array.Clear(buffer, 0, buffer.Length);

                        }
                        else if (divide[0] == "6")//검사결과 수신//pass일 때
                        {
                            MessageBox.Show("6번 진입");
                            //디비 저장할 거
                            //번호/시간(초까지)/검사결과(P/F)/불량인이유 없으면 x
                            //db연동 테스트
                            string qry = "INSERT INTO result(" + "NO, Time, test_result" + ")" +
                                        "VALUES(" + "@NO, @Time, @test_result" + ");";
                            using (MySqlCommand cmd = sql.CreateCommand())
                            {
                                cmd.CommandText = qry;
                                cmd.Parameters.Add("@NO", MySqlDbType.Int32);
                                cmd.Parameters.Add("@Time", MySqlDbType.VarChar);
                                cmd.Parameters.Add("@test_result", MySqlDbType.VarChar);

                                cmd.Parameters["@NO"].Value = divide[1];
                                cmd.Parameters["@Time"].Value = divide[2];
                                cmd.Parameters["@test_result"].Value = divide[3];

                                cmd.ExecuteNonQuery();
                            }

                            
                            //리스트뷰에 띄우기
                            //ui 건드리는 스레드가 아닌 애가 건드리려고 해서 오류남
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                Result_Log.Items.Insert(numbering, "테스트");
                            }));
                            //검사결과 및 파일수신하기, DB저장하기

                            //결과 wpf 클라한테 보내주기
                            byte[] result = new byte[128];
                            result = Encoding.Default.GetBytes(divide[3]);//결과
                            rev_stream.Write(result, 0, result.Length);
                        }
                        else if (divide[0] == "7")//검사결과 불량일 때 사진도 받고 이미지에 사진 띄우기
                        {
                            MessageBox.Show("7번 진입");
                            Defect_receive();//이미지 수신용

                            //결과 wpf 클라한테 보내주기
                            byte[] result = new byte[1024];
                            result = Encoding.Default.GetBytes(divide[3]);//결과
                            rev_stream.Write(result, 0, result.Length);

                        }
                        else if (divide[0] == "2")
                        {
                            Client_Distinguish.Add(divide[1], stream);
                            to_python = divide[1];
                            Send_python();
                        }
                    }
                    catch (Exception s)
                    {
                        MessageBox.Show(s.ToString());
                    }
                }
            }
        }
        private async void Send_python()//저장된 파일 열어서 보내주기
        {
            NetworkStream stream_python = Client_Distinguish[to_python];
            try
            {
                while (true)
                {
                    await Task.Delay(10000);
                    // 파일 크기 전송
                    byte[] file_size = new byte[4];
                    send_to_python = new FileStream("../../WPF_read_image/" + num_test + ".png", FileMode.Open, FileAccess.Read);//
                    num_test++;
                    //int test_length = (int)send_to_python.Length;
                    int test_length = (int)send_to_python.Length;
                    //string str_length = test_length.ToString();
                    file_size = BitConverter.GetBytes(test_length);
                    //file_size = Encoding.Default.GetBytes(str_length);
                    stream_python.Write(file_size, 0, file_size.Length);
                    MessageBox.Show(test_length.ToString() + "파일크기용");

                    byte[] file_data = new byte[test_length];//outofmemory??

                    send_to_python.Read(file_data, 0, file_data.Length);//파일읽어서 배열에 넣고
                    stream_python.Write(file_data, 0, file_data.Length);//송신
                    MessageBox.Show("파일전송완료");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 전송 오류: " + ex.ToString());
            }
        }

        //private async void Pass_receive()//pass일때인데 흠
        //{
        //    await Task.Run(async () =>
        //    {
        //        await Task.Delay(1000);
        //        NetworkStream stream_python = client_Distinguish[to_python];
        //        byte[] result = new byte[1024];
        //        stream_python.Read(result, 0, result.Length);
        //        //여기서 받아서 리스트뷰 채우기
        //        //pass defect 구별하고

        //    });
        //}

        private async void Defect_receive()//얘는 이미지도
        {
            await Task.Run(async () =>
            {
                await Task.Delay(1000);
                //결과 db에 넣어주고
                string qry = "INSERT INTO result(" + "NO, Time, test_result, Cause" + ")" +
                                        "VALUES(" + "@NO, @Time, @test_result, @Cause" + ");";
                using (MySqlCommand cmd = sql.CreateCommand())
                {
                    cmd.CommandText = qry;
                    cmd.Parameters.Add("@NO", MySqlDbType.Int32);
                    cmd.Parameters.Add("@Time", MySqlDbType.VarChar);
                    cmd.Parameters.Add("@test_result", MySqlDbType.VarChar);
                    cmd.Parameters.Add("@Cause", MySqlDbType.VarChar);

                    cmd.Parameters["@NO"].Value = divide[1];
                    cmd.Parameters["@Time"].Value = divide[2];
                    cmd.Parameters["@test_result"].Value = divide[3];
                    cmd.Parameters["@Cause"].Value = divide[4];

                    cmd.ExecuteNonQuery();
                }

                NetworkStream stream_python = client_Distinguish[to_python];
                byte[] result = new byte[1024];
                stream_python.Read(result, 0, result.Length);//결과 먼저 수신받고 파일받기
                //결과 수신받고 리스트뷰 채우기
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    Result_Log.Items.Insert(numbering, "테스트");
                }));

                //파일 사이즈용
                byte[] image_size = new byte[4];
                stream_python.Read(image_size, 0, image_size.Length);
                int file_size = BitConverter.ToInt32(image_size, 0);

                //파일 만들고
                FileStream defect_image = new FileStream("../../defect_image/" + numbering + ".png", FileMode.Create, FileAccess.Write);
                //수신
                byte[] file_data = new byte[file_size];
                BinaryWriter write = new BinaryWriter(defect_image);
                stream_python.Read(file_data, 0, file_data.Length);
                write.Write(file_data, 0, file_data.Length);

                //이미지 수신하고 image에 사진 넣어주기
                Load_image();//이미지 띄우기용
            });
        }
        private async void Load_image()
        {
            await Task.Delay(100);
            try
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(@"C:/Users/aiot/Desktop/PycharmProjects/" + numbering + ".png");
                    bi.EndInit();
                    receive_image.Source = bi;
                }));
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}