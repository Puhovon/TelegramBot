using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegBot
{
    class Program
    {
        static TelegramBotClient bot;
        static string token = "";
        
        static string CityName;
        static float Temp;
        static string nameOfCity;

        

        static void Main(string[] args)
        {


            #region exc

            //// https://hidemyna.me/ru/proxy-list/?maxtime=250#list

            // Содержит параметры HTTP-прокси для System.Net.WebRequest класса.
            var proxy = new WebProxy()
            {
                Address = new Uri($"http://77.87.240.74:3128"),
                UseDefaultCredentials = false,
                //Credentials = new NetworkCredential(userName: "login", password: "password")
            };

            // Создает экземпляр класса System.Net.Http.HttpClientHandler.
            var httpClientHandler = new HttpClientHandler() { Proxy = proxy };

            // Предоставляет базовый класс для отправки HTTP-запросов и получения HTTP-ответов 
            // от ресурса с заданным URI.
            HttpClient hc = new HttpClient(httpClientHandler);

            bot = new TelegramBotClient(token);

            #endregion

            //bot = new TelegramBotClient(token);
            bot.OnMessage += MessageListener;
            bot.StartReceiving();
            Console.ReadKey();
        }

        private static async void MessageListener(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var msg = e.Message;

            string text = $"{DateTime.Now.ToLongTimeString()}: {e.Message.Chat.FirstName} {e.Message.Chat.Id} {e.Message.Text}";

            Console.WriteLine($"{text} TypeMessage: {e.Message.Type.ToString()}");

            await bot.SendTextMessageAsync(msg.Chat.Id, msg.Text, replyMarkup: GetButtons());

            if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Document)
            {
                Console.WriteLine(e.Message.Document.FileId);
                Console.WriteLine(e.Message.Document.FileName);
                Console.WriteLine(e.Message.Document.FileSize);

                DownloadFile(e.Message.Document.FileId, e.Message.Document.FileName);
            }
            else if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Photo)
            {
                Console.WriteLine(e.Message.MessageId.ToString());
                Console.WriteLine(e.Message.Photo);
                DownloadFile(e.Message.Photo[e.Message.Photo.Length - 1].FileId.ToString(), $"{e.Message.MessageId.ToString()}.jpg");
            }
            else if(e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Audio)
            {
                Console.WriteLine(e.Message.MessageId.ToString());
                Console.WriteLine(e.Message.Audio.FileName.ToString());
                DownloadFile("id", $".mp3");
            }
            if (e.Message.Text == null) return;

            var messageText = e.Message.Text;
            switch (e.Message.Text)
            {
                case "Погода":
                    await bot.SendTextMessageAsync(msg.Chat.Id,
                        "Напишите название города");
                    if (e.Message.Text == null) return;
                    else
                    {
                        nameOfCity = e.Message.Text;
                        FindCityWeather(sender, e);
                    }
                    await bot.SendTextMessageAsync(msg.Chat.Id, $"Температура в {CityName}: {Math.Round(Temp)} градусов.");
                    break;
                case "Скинуть файл":
                    await bot.SendTextMessageAsync(msg.Chat.Id,
                        "Просто отправьте файл");
                break;
                case "Скачать файл":
                    await bot.SendTextMessageAsync(msg.Chat.Id,
                        "Пока что не доступно");
                    break;
            }

        }

        private static async void FindCityWeather(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            CityName = e.Message.Text;
            try
            {
                string url = "https://api.openweathermap.org/data/2.5/weather?id=" + CityName + "&appid={e2c3c78a4f3b7df11f8b0907abb036ad}";
                HttpWebRequest httpRequest= (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest?.GetResponse();
                string response;

                using(StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    response = reader.ReadToEnd();
                }

                WeatherResponse weatherResponse = JsonConvert.DeserializeObject<WeatherResponse>(response);

                nameOfCity = weatherResponse.Name;
                Temp = weatherResponse.Main.Temp - 273;
            }
            catch (System.Net.WebException)
            {
                Console.WriteLine("Неверное название города");
                return;
            }
        }

        private static IReplyMarkup GetButtons()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>{ new KeyboardButton { Text = "Погода" }, new KeyboardButton { Text = "Скинуть файл" } },
                    new List<KeyboardButton>{ new KeyboardButton { Text ="Скачать файл"} }
                }
            };
        }

        private static async void DownloadFile(string fileId, string path)
        {
            try
            {
                var file = await bot.GetFileAsync(fileId);
                FileStream fs = new FileStream("_" + path, FileMode.Create);
                await bot.DownloadFileAsync(file.FilePath, fs);
                fs.Close();

                fs.Dispose();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error downloading: " + ex.Message);
            }
            
        }
    }
}
