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

namespace AI_project_server
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private string bindIP = "10.10.20.113";
        private const int bindPort = 9197;
        private TcpListener server = null;
        private IPEndPoint localAddress;
        private TcpClient client;
        private NetworkStream stream;

        FileStream read_file;
        string[] divide;//구분용
        string msg;//받아온 데이터 변환용
        //FileStream filestr;

        int fileLength;//파일 길이용
        byte[] buffer;//파일 데이터용

        int numbering = 0;//테스트용

        //listview용 리스트
        string date;//listview 날짜용
        List<string> pass_result = new List<string>();
        List<string> defect_result = new List<string>();

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
                //Thread t1 = new Thread(new ThreadStart(Connect));
                //t1.Start();
                //Connect();
                Handle_client();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                server.Stop();
            }
        }
        private async void Handle_client()
        {
            await Task.Run(async () =>
            {
                while(true)
                {
                    await Task.Delay(1000);
                    client = server.AcceptTcpClient();
                    stream = client.GetStream();
                    IPEndPoint getIP = (IPEndPoint)client.Client.RemoteEndPoint;
                    string IP = getIP.Address.ToString();
                    Dispatcher.Invoke(() =>
                    {
                        IPstate.AppendText("입장 아이피 : " + IP + "\n");
                    });
                    Thread t1 = new Thread(new ThreadStart(Connected_client));
                    t1.Start();//왜 쓰레드 안 타냐
                }
                
            });
            
        }
        private void Connected_client()
        {
            while (true)
            {
                int length;
                byte[] data = new byte[128];
                while ((length = stream.Read(data, 0, data.Length)) != 0)//파일크기 받고 파일받기
                {
                    try
                    {
                        //데이터 받고 구분자로 파이썬, WPF 구분하기
                        msg = Encoding.Default.GetString(data);
                        MessageBox.Show(msg);
                        divide = msg.Split('/');
                        //stream.Write(data, 0, data.Length);
                        if (divide[0] == "1")
                        {
                            //WPF client 정보저장하기
                            client_Distinguish.Add(divide[1], stream);
                            //파일수신 및 파이썬한테 전송하기
                        }
                        else if (divide[0] == "3")
                        {
                            //파일수신 테스트
                            byte[] size = new byte[4];
                            stream.Read(size, 0, size.Length);
                            fileLength = BitConverter.ToInt32(size, 0);
                            MessageBox.Show(fileLength.ToString() + "파일크기계속 바뀌나");//잘 바뀜
                            read_file = new FileStream("../../WPF_read_image/" + numbering + ".png", FileMode.Create, FileAccess.Write);
                            numbering++;
                            //int byteread;//리드한 파일 크기 담을거
                            //¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿
                            BinaryWriter write = new BinaryWriter(read_file);

                            buffer = new byte[fileLength];
                            //MessageBox.Show("여기");
                            stream.Read(buffer, 0, buffer.Length);
                            write.Write(buffer, 0, buffer.Length);
                            read_file.Close();
                            //Send_python(buffer);
                        }
                        else if (divide[0] == "5")
                        {
                            //python 정보저장하기
                            client_Distinguish.Add(divide[1], stream);

                        }
                        else if (divide[0] == "6")//검사결과 수신//pass일 때
                        {
                            MessageBox.Show("6번 진입");
                            //리스트뷰에 띄우기
                            //ui 건드리는 스레드가 아닌 애가 건드리려고 해서 오류남
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            {
                                //Result_Log.Items.Insert(numbering, test);
                            }));
                            numbering++;
                            //검사결과 및 파일수신하기, DB저장하기
                            
                            //결과 wpf 클라한테 보내주기
                            byte[] result = new byte[128];
                            result = Encoding.Default.GetBytes(divide[1]);//결과
                            stream.Write(result, 0, result.Length);
                        }
                        else if (divide[0] == "7")//검사결과 불량일 때 사진도 받고 이미지에 사진 띄우기
                        {
                            MessageBox.Show("7번 진입");
                            //Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                            //{
                            //    Result_Log.Items.Insert(numbering, test);
                            //}));
                            numbering++;
                            //결과 wpf 클라한테 보내주기
                            byte[] result = new byte[128];
                            result = Encoding.Default.GetBytes(divide[1]);//결과
                            stream.Write(result, 0, result.Length);

                        }
                        else if (divide[0] == "2")
                        {
                            MessageBox.Show("파이썬 중복 확인 50번 진입");
                            client_Distinguish.Add(divide[1], stream);
                            //Send_python();//error
                            //MessageBox.Show("함수호출성공");
                            //if (buffer != null && buffer.Length > 0)
                            //{
                            //    // 파일 크기 전송
                            //    byte[] fileSizeBytes = BitConverter.GetBytes(fileLength);
                            //    stream.Write(fileSizeBytes, 0, fileSizeBytes.Length);

                            //    // 파일 데이터 전송
                            //    stream.Write(buffer, 0, buffer.Length);

                            //    MessageBox.Show("파일이 성공적으로 전송되었습니다.");
                            //}
                            //else
                            //{
                            //    MessageBox.Show("전송할 파일 데이터가 없습니다.");
                            //}
                        }
                    }
                    catch (Exception s)
                    {
                        MessageBox.Show(s.ToString());
                    }
                }
                client.Close();
                stream.Close();
            }
        }


        //private void Send_python()
        //{
        //    try
        //    {
        //        //파일크기 보내주기
        //        //error//?¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿¿"¿ 너는 왜
        //        byte[] test = new byte[4];
        //        test = Encoding.Default.GetBytes(fileLength.ToString());
        //        MessageBox.Show(test.ToString() + " 파일크기 확인용");
        //        stream.Write(test, 0, 4);
        //        //파일데이터 보내주기
        //        //MessageBox.Show(file_buf.ToString() + " 파일데이터 확인용");
        //        stream.Write(file_buf, 0, file_buf.Length);//error//null이라서//
        //        //MessageBox.Show(file_buf.ToString() + " 파일데이터 확인용123");
        //        //Array.Clear(file_length, 0, file_length.Length);
        //        //Array.Clear(file_data, 0, file_data.Length);
        //    }
        //    catch(Exception s)
        //    {
        //        MessageBox.Show(s.ToString());
        //    }
        //}

        private async void Send_python(byte[] buffer)
        {
            try
            {
                await Task.Delay(1000);
                if (buffer != null && buffer.Length > 0)
                {
                    // 파일 크기 전송
                    //byte[] fileSizeBytes = BitConverter.GetBytes(buffer.Length);
                    byte[] fileSizeBytes = new byte[buffer.Length];
                    stream.Write(fileSizeBytes, 0, fileSizeBytes.Length);
                    string test = fileSizeBytes.ToString();
                    MessageBox.Show(test + "파일사이즈");

                    // 파일 데이터 전송
                    stream.Write(buffer, 0, buffer.Length);

                    MessageBox.Show("파일이 성공적으로 전송되었습니다.");
                }
                else
                {
                    MessageBox.Show("전송할 파일 데이터가 없습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 전송 중 오류가 발생했습니다: " + ex.Message);
            }
        }

    }
}

