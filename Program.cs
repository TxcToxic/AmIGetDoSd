using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Net;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Runtime.CompilerServices;

// remove credits, but dont rip my code! ¯\_(ツ)_/¯

namespace AIGD
{
    internal class Program
    {
        static bool _pingProc = false;
        static bool _timeout = false;
        static Dictionary<string, bool> reportetList = new Dictionary<string, bool>();
        static string _wurl;
        static string _jpath1;
        static string _jpath2;

        public class AppConfig
        {
            public string wurl { get; set; }
            public string[] ips { get; set; }
            public string jp1 { get; set; }
            public string jp2 { get; set; }
        }

        static async Task SendWebhook(int offline, string serverIp, string time)
        {
            string jsonFilePath;
            if (offline == 1) {
                jsonFilePath = _jpath1;
            } else
            {
                jsonFilePath = _jpath2;
            }
            string jsonString = await File.ReadAllTextAsync(jsonFilePath);
            jsonString = jsonString.Replace("{server}", serverIp);
            jsonString = jsonString.Replace("{time}", time);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            using (var httpClient = new HttpClient())
            {
                await httpClient.PostAsync(_wurl, content);
            }
        }

        static void Ping()
        {
            using (var ping = new Ping())
            {
                while (_pingProc)
                {
                    foreach (string ip in reportetList.Keys)
                    {
                        try
                        {
                            var reply = ping.Send(IPAddress.Parse(ip));
                            if (reply.Status == IPStatus.Success)
                            {
                                Console.Write(ip.PadRight(16) + "> ");
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                Console.Write("Online");
                                Console.ResetColor();
                                if (!_timeout)
                                {
                                    if (reportetList[ip])
                                    {
                                        DateTime now = DateTime.Now.ToUniversalTime();
                                        string time = now.ToString("MM/dd/yyyy | hh:mm:ss tt (UTC)");
                                        reportetList[ip] = false;
                                        SendWebhook(2, ip, time);
                                        Console.WriteLine(" | reported");
                                    } else
                                    {
                                        Console.WriteLine();
                                    }
                                } else {
                                    Console.WriteLine(" | not reported (timeout active)");
                                }
                            }
                            else
                            {
                                if (!reportetList[ip])
                                {
                                    reportetList[ip] = true;
                                    Console.Write(ip.PadRight(16) + "> ");
                                    Console.ForegroundColor = ConsoleColor.DarkRed;
                                    Console.Write("Offline");
                                    Console.ResetColor();
                                    DateTime now = DateTime.Now.ToUniversalTime();
                                    string time = now.ToString("MM/dd/yyyy | hh:mm:ss tt (UTC)");
                                    _timeout = true;
                                    Thread.Sleep(30);
                                    var reply2 = ping.Send(IPAddress.Parse(ip));
                                    if (reply2.Status == IPStatus.Success)
                                    {
                                        Console.WriteLine(" | not reported (server came back)");
                                        reportetList[ip] = false;
                                        _timeout = false;
                                    }
                                    else
                                    {
                                        _timeout = false;
                                        SendWebhook(1, ip, time);
                                        Console.WriteLine(" | reported");
                                    }

                                }
                                else
                                {
                                    Console.Write(ip.PadRight(16) + "> ");
                                    Console.ForegroundColor = ConsoleColor.DarkRed;
                                    Console.Write("Offline");
                                    Console.ResetColor();
                                    Console.WriteLine(" | not reported (known)");
                                }
                            }
                        }
                        catch (PingException)
                        {
                            Console.Write(ip.PadRight(16) + "> ");
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine("ping failed");
                            Console.ResetColor();
                        }
                    }
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(Environment.NewLine + new string('-', 15) + " 5 Seconds Timeout " + new string('-', 15) + Environment.NewLine);
                    Console.ResetColor();
                    Thread.Sleep(5000);
                }
            }
        }

        static void Main(string[] args)
        {
            string configText = File.ReadAllText("config.json");
            AppConfig config = JsonSerializer.Deserialize<AppConfig>(configText);
            _wurl = config.wurl;
            _jpath1 = config.jp1;
            _jpath2 = config.jp2;
            _pingProc = true;
            Thread PingThread = new Thread(() => Ping());
            foreach (string ip in config.ips)
            {
                reportetList.Add(ip, false);
            }
            Console.Title = "AIGD | By -TOXIC-#1835 (CFT Development)";
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(new string('-', 16) + " AmIGettinDoS'd? " + new string('-', 16));
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(new string('-', 5) + " C, ESC or Backspace kills the program " + new string('-', 5) + Environment.NewLine);
            Console.ResetColor();
            PingThread.Start();
            ConsoleKeyInfo closeKey =  Console.ReadKey();
            if (closeKey.Key == ConsoleKey.Escape || closeKey.Key == ConsoleKey.C || closeKey.Key == ConsoleKey.Backspace)
            {
                Environment.Exit(0);
            }
        }
    }
}