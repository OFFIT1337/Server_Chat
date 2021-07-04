using MyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Сервер_ПП
{
    public partial class Form1 : Form
    {
        static ServerObject server; // сервер

        static Thread listenThread; // поток для прослушивания

        static Form1 form1;

        public Form1()
        {
            InitializeComponent();
            ClientObject.Form1 = this;
            ServerObject.Form1 = this;
            form1 = this;
            try
            {
                server = new ServerObject();
                listenThread = new Thread(new
               ThreadStart(server.Listen));
                listenThread.Start(); //старт потока
            }
            catch (Exception ex)
            {
                server.Disconnect();
                richTextBoxChat.Text += ex.Message + "\n";
            }
        }

        public static Dictionary<Image, int> photos = new Dictionary<Image, int>();
        public static void otrisovkaFoto()
        {
            foreach (KeyValuePair<Image, int> keys in photos)
            {
                Thread thread = new Thread(() => Clipboard.Clear());
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                
                thread = new Thread(() => Clipboard.SetImage(keys.Key));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                form1.richTextBoxChat.Invoke(new Action(() => form1.richTextBoxChat.Focus()));
                form1.richTextBoxChat.Invoke(new Action(() => form1.richTextBoxChat.SelectionStart = keys.Value));
                form1.richTextBoxChat.Invoke(new Action(() => form1.richTextBoxChat.ScrollToCaret()));
                form1.richTextBoxChat.Invoke(new Action(() => form1.richTextBoxChat.Paste()));
            }
        }
    }


        public class ClientObject
        {
            public static Form1 Form1;
            protected internal string Id { get; private set; }

            protected internal NetworkStream Stream
            {
                get;
                private set;
            }
            string userName;
            TcpClient client;
            ServerObject server; // объект сервера

            public ClientObject(TcpClient tcpClient, ServerObject serverObject)
            {
                Id = Guid.NewGuid().ToString();
                client = tcpClient;
                server = serverObject;
                serverObject.AddConnection(this);
            }

        
        
        public void Process()
        {
            //try
            //{
                Stream = client.GetStream();
                // получаем имя пользователя
                ComplexMessage Recivemessage = GetMessage();
                userName = (string)SerializeAndDeserialize.Deserialize(Recivemessage.First);
                string message = userName + " вошел в чат";
                // посылаем сообщение о входе в чат всем подключенным пользователям
                server.BroadcastMessage(message, this.Id);
                Form1.richTextBoxChat.Invoke(new Action(() => Form1.richTextBoxChat.Text += message + '\n'));
                // в бесконечном цикле получаем сообщения от клиента
                while (true)
                {
                    //try
                    //{
                        Recivemessage = GetMessage();
                        if (Recivemessage.NumberStatus == 1)
                        {
                            message = String.Format("{0}: {1}", userName, (string)SerializeAndDeserialize.Deserialize(Recivemessage.First));
                            Form1.richTextBoxChat.Invoke(new Action(() => Form1.richTextBoxChat.Text += message + '\n'));
                            Form1.otrisovkaFoto();
                            server.BroadcastMessage(message, this.Id);
                        }
                /*
                if(Recivemessage.NumberStatus == 2)
                {
                    message = String.Format("{0}: {1}", userName, " (" + DateTime.Now.ToShortTimeString() + ")");
                    Form1.richTextBoxChat.Invoke(new Action(() => Form1.richTextBoxChat.Text += message + '\n'));

                    photos.Add((MemoryStream)SerializeAndDeserialize.Deserialize(Recivemessage.First), Form1.richTextBoxChat.Text.Length - 8);
                otrisovkaFoto();
                */

                    if(Recivemessage.NumberStatus == 2) {
                    {
                        message = String.Format("{0}: {1}", userName, " (" + DateTime.Now.ToShortTimeString() + ")");
                        Form1.richTextBoxChat.Invoke(new Action(() => Form1.richTextBoxChat.Text += message + '\n'));

                        //вот это я
                        Image image = (Image)SerializeAndDeserialize.Deserialize(Recivemessage.First);
                        //System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
                        //image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);/*new Action((a) => a = Form1.richTextBoxChat.Text.Length)*/
                        int a = 0;
                        Action n = () => a = Form1.richTextBoxChat.Text.Length;
                        Form1.richTextBoxChat.Invoke(n);
                        Form1.photos.Add(image, a - 8);
                        Form1.otrisovkaFoto();
                    }




                    /*using (MemoryStream ms = new MemoryStream((byte[])SerializeAndDeserialize.Deserialize(Recivemessage.First)))
                    {
                        photos.Add(ms.ToArray(), Form1.richTextBoxChat.Text.Length - 8); 
                    }
                    */

                    //server.BroadcastMessage(message, this.Id);
                }
                    //}
                    /*catch
                    {
                        message = String.Format("{0}: покинул чат", userName);
                        Form1.richTextBoxChat.Invoke(new Action(() => Form1.richTextBoxChat.Text += message + '\n'));
                        server.BroadcastMessage(message, this.Id);
                        break;
                    }
                    */
                }
            //}
            //catch (Exception e)
            //{
             //   Form1.richTextBoxChat.Invoke(new Action(() => Form1.richTextBoxChat.Text += e.Message + '\n'));
            //}
            //finally
            //{
                //в случае выхода из цикла закрываем ресурсы
              //  server.RemoveConnection(this.Id);
              //  Close();
            //}
        }
            // чтение входящего сообщения и преобразование в строку
            private ComplexMessage GetMessage()
            {
            byte[] myReadBuffer = new byte[6297630];
                do
                {
                    Stream.Read(myReadBuffer, 0, myReadBuffer.Length);
                }

                while (Stream.DataAvailable);
                ComplexMessage complexMessage = new ComplexMessage();
                MyLib.Message message = new MyLib.Message();
                message.Data = myReadBuffer; //Запись принятого пакета данных с клиента в      свойство Data объекта message
                complexMessage = (ComplexMessage)SerializeAndDeserialize.Deserialize(message);
                return complexMessage;
            }
            // закрытие подключения
            protected internal void Close()
            {
                if (Stream != null)
                    Stream.Close();
                if (client != null)
                    client.Close();
            }
        }

        public class ServerObject
        {
            public static Form1 Form1;

            static TcpListener tcpListener; // сервер для прослушивания

            public static List<ClientObject> clients = new List<ClientObject>(); // все подключения
            ClientObject clientObject = null;


            protected internal void AddConnection(ClientObject clientObject)
            {
                clients.Add(clientObject);
            }
            protected internal void RemoveConnection(string id)
            {
                // получаем по id закрытое подключение
                ClientObject client = clients.FirstOrDefault(c =>  c.Id == id);
            // и удаляем его из списка подключений
            if (client != null)
            {
                clients.Remove(client);
            }
            }
            // прослушивание входящих подключений
            protected internal void Listen()
            {
                try
                {
                    tcpListener = new TcpListener(IPAddress.Any, 8888);
                    tcpListener.Start();

                    Form1.richTextBoxChat.Invoke(new Action(() => Form1.richTextBoxChat.Text += ("Сервер запущен. Ожидание подключений..." + "\n")));
                    while (true)
                    {
                        TcpClient tcpClient = tcpListener.AcceptTcpClient();
                        clientObject = new ClientObject(tcpClient, this);
                        Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                        clientThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    Form1.richTextBoxChat.Invoke(new Action(() => Form1.richTextBoxChat.Text += ex.Message + "\n"));
                    Disconnect();
                }
            }


        ComplexMessage cm = new ComplexMessage();
        MyLib.Message m;
            // трансляция сообщения подключенным клиентам
            protected internal void BroadcastMessage(string message, string id)
            {
                byte[] responseData; //Массив байтов для хранения ответа, fормируемого сервером на запрос клиента
                cm.NumberStatus = 1;
                m = SerializeAndDeserialize.Serialize(message);
                cm.First = m;
                m = SerializeAndDeserialize.Serialize(cm); //Сериализованное значение объекта cm    присваиваем переменной m
                responseData = m.Data;    
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].Id != id) // если id клиента не равно id отправляющего
                    {
                        clients[i].Stream.Write(responseData, 0, responseData.Length);
                        //передача данных
                    }
                }
            }
            // отключение всех клиентов
            protected internal void Disconnect()
            {
                tcpListener.Stop(); //остановка сервера
                for (int i = 0; i < clients.Count; i++)
                {
                    clients[i].Close(); //отключение клиента
                }
                Environment.Exit(0); //завершение процесса
            }
        }

    
}
