using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Linq;

namespace TwitchChatBot
{
    public class TwitchInit
    {
        public const string Host = "irc.twitch.tv";
        public const int port = 6667;
    }

    public class TwitchIRCClient
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private RichTextBox _output;
        private string passToken;
        private string botNick;
        private string channelName;

        private Dictionary<string, Action<string, TwitchIRCClient>> answers = new Dictionary<string, Action<string, TwitchIRCClient>>{};

        public TwitchIRCClient(RichTextBox outputTextBox, string channelName, string botNick, string authToken, string victimName)
        {
            _output = outputTextBox;

            client = new TcpClient(TwitchInit.Host, TwitchInit.port);
            reader = new StreamReader(client.GetStream());
            writer = new StreamWriter(client.GetStream());
            writer.AutoFlush = true;
            Random rnd = new Random();
            var ans = "";

            try
            {
                var Lines = File.ReadLines(@"C:\\paste.txt");
                var LineCount = Lines.Count();

                ans = Lines.ElementAt(rnd.Next(0, Lines.Count()));

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            //finally
            //{
                //line = "Кароч ты заебал пиздеть глохни рыба";
                //sr.Close();
            //}

            answers.Add(victimName, delegate (string msg, TwitchIRCClient client){client.SendMessage("@" + victimName + " " + ans);});

            this.channelName = channelName;
            this.botNick = botNick;

            passToken = authToken;
            if (passToken == string.Empty)
            {
                passToken = File.ReadAllText("auth.ps");
            }

        }

        public void Connect()
        {
            SendCommand("PASS", passToken);
            SendCommand("USER", string.Format("{0} 0 * {0}", botNick));
            SendCommand("NICK", botNick);
            SendCommand("JOIN", "#" + channelName);
        }

        public void CheckCommand(string msg)
        {
            foreach (var pair in answers)
            {
                if (msg.Contains(pair.Key))
                {
                    pair.Value.Invoke(msg, this);
                }
            }
        }

        public async void Chat(CancellationToken cancellationToken)
        {
            try
            {
                string message;

                while ((message = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                {
                    if (message != null)
                    {
                        _output.Invoke((MethodInvoker)delegate { _output.Text += message + '\n'; });
                        CheckCommand(message);
                        if (message == "PING :tmi.twitch.tv")
                        {
                            SendCommand("PONG", ":tmi.twitch.tv");
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }


        public void SendMessage(string message)
        {
            SendCommand("PRIVMSG", string.Format("#{0} :{1}", channelName, message));
        }

        public void SendCommand(string cmd, string param)
        {
            writer.WriteLine(cmd + " " + param);
        }

    }
}
