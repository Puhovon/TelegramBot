using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegBot
{

    class Program
    {
        static TelegramBotClient bot;
        static string token = "5231816245:AAE06GBsraf_BUDeLeNelCUKXpRdBIJdzWE";
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
            var messageText = e.Message.Text;
            if (msg.Text == "/start")
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, $"Доброго времени суток, {msg.Chat.FirstName}! Это мой первый телеграмм бот написанный на C#");
                await bot.SendTextMessageAsync(msg.Chat.Id,
                    "Если вы хотите узнать как пользоваться ботом, нажмите /help"
                    );
            }
            else if (msg.Text == "/help")
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, $"И так, {msg.Chat.FirstName}, если ты хочешь узнать погоду, то напиши <Погода Название города>\n" +
                    $"Так же этот бот может сохранять файлы которые вы отправите\n" +
                    "/GetFiles");
            }


            if (msg.Text == "/GetFiles")
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, "/Download название файла, чтобы скачать его");
                FindFilesDir(msg.Chat.Id.ToString(), msg);

            }


            else
            {
                if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Document)
                {
                    Console.WriteLine(e.Message.Document.FileId);
                    Console.WriteLine(e.Message.Document.FileName);
                    Console.WriteLine(e.Message.Document.FileSize);

                    DownloadFile(e.Message.Document.FileId, e.Message.Document.FileName, msg);
                }
                else if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Photo)
                {
                    Console.WriteLine(e.Message.MessageId.ToString());
                    Console.WriteLine(e.Message.Photo);
                    DownloadFile(e.Message.Photo[e.Message.Photo.Length - 1].FileId.ToString(), $"{e.Message.MessageId.ToString()}.jpg", msg);
                }
                else if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Audio)
                {
                    Console.WriteLine(e.Message.MessageId.ToString());
                    Console.WriteLine(e.Message.Audio.FileName.ToString());
                    DownloadFile("id", $".mp3", msg);
                }
                else if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Video)
                {
                    Console.WriteLine(e.Message.Video.FileId);
                    Console.WriteLine(e.Message.Video.FileName);
                    Console.WriteLine(e.Message.Video.FileSize);

                    DownloadFile(e.Message.Video.FileId, e.Message.Video.FileName, msg);
                }
            }
            if (e.Message.Text == null)
            {
                return;
            }

            if (msg.Text.Contains("/Download"))
            {
                try
                {
                    string[] splitStr = msg.Text.Split(' ');
                    string FileName = splitStr[1];
                    foreach (var s in splitStr)
                    {
                        Console.WriteLine(s);
                    }
                    Console.WriteLine(FileName);
                    SendFileFromDir(FileName, msg);
                }
                catch (Exception ex)
                {
                    await bot.SendTextMessageAsync(msg.Chat.Id, "Неверное название файла");
                }
            }
            if (msg.Text.Contains("Погода"))
            {
                string[] s = messageText.Split(' ');
                CityName = s[1];
                FindCityWeather(CityName, msg);
                await bot.SendTextMessageAsync(msg.Chat.Id, $"Температура в {CityName}: {Math.Round(Temp)} градусов.");
            }
            string text = $"{DateTime.Now.ToLongTimeString()}: {e.Message.Chat.FirstName} {e.Message.Chat.Id} {e.Message.Text}";

            Console.WriteLine($"{text} TypeMessage: {e.Message.Type.ToString()}");

        }

        //private static async void GetFile(string fileName, Message msg)
        //{
        //    string path = $@"E:\TeleFiles\{msg.Chat.Id}\{fileName}";
        //    DirectoryInfo dir = new DirectoryInfo(path);
        //    var file = await bot.GetFileAsync(path);

        //    if (dir.Exists)
        //    {
        //        try
        //        {
        //            using (Stream stream = new FileStream(path, FileMode.Open))
        //            {
        //                await bot.SendPhotoAsync(msg.Chat.Id, stream,file);
        //            }
        //        }
        //        catch (Exception ex)
        //        { 
        //            await bot.SendTextMessageAsync(msg.Chat.Id, $"{ex.Message}");
        //        }
        //        //using(var stream = System.IO.File.Open(path, FileMode.Open))
        //        //{
        //        //    try
        //        //    {
        //        //        await bot.SendDocumentAsync(msg.Chat.Id, stream);
        //        //        await bot.SendTextMessageAsync(msg.Chat.Id, "успешно");
        //        //    }
        //        //    catch (Exception ex)
        //        //    {
        //        //        await bot.SendTextMessageAsync(msg.Chat.Id, $"Error: {ex.Message}");

        //        //    }
        //    }



        private static async void FindFilesDir(string ChatId, Message msg)
        {
            string path = $@"E:\TeleFiles\{ChatId}";
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, "Вы ещё ничего не отправляли");
            }
            else
            {
                FileInfo[] files = dir.GetFiles();
                //FillStringDir(path);
                string str = "";
                foreach (FileInfo file in files)
                {
                    str += $"\n{file.Name}";
                }
                await bot.SendTextMessageAsync(msg.Chat.Id, "Ваши файлы:" + str);

            }
        }
        private static async void SendFileFromDir(string FileName, Message msg)
        {
            string path = $@"E:\TeleFiles\{msg.Chat.Id}\";
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] fg = dir.GetFiles();
            try
            {
                string[] TypeFile = FileName.Split('.');
                using (Stream fs = new FileStream($@"{path}{FileName}", FileMode.Open))
                {
                    if (TypeFile[1] == "jpg")
                    {
                        await bot.SendPhotoAsync(msg.Chat.Id, fs, "");
                    }
                    else if (TypeFile[1] == "MP4")
                    {
                        await bot.SendVideoAsync(msg.Chat.Id, fs);
                    }
                    else
                    {
                        await bot.SendDocumentAsync(msg.Chat.Id, fs, "");
                    }
                }
            }
            catch (Exception ex)
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, $"Error: {ex.Message}");

            }
        }
        private static async void FindCityWeather(string CityName, Message msg)
        {

            try
            {
                string url = $"https://api.openweathermap.org/data/2.5/weather?q={CityName}&units=metric&appid=e2c3c78a4f3b7df11f8b0907abb036ad";
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest?.GetResponse();
                string response;

                using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    response = reader.ReadToEnd();
                }

                WeatherResponse weatherResponse = JsonConvert.DeserializeObject<WeatherResponse>(response);

                nameOfCity = weatherResponse.Name;
                Temp = weatherResponse.Main.Temp;
            }
            catch (WebException)
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, "Неверное название города");
                Console.WriteLine("Неверное название города");
                return;
            }
        }



        private static async void DownloadFile(string fileId, string path, Message msg)
        {

            try
            {
                DirectoryInfo dir = new DirectoryInfo(@"E:\TeleFiles\" + $"{msg.Chat.Id}");
                if (!dir.Exists)
                {
                    dir.Create();
                }
                var file = await bot.GetFileAsync(fileId);
                FileStream fs = new FileStream($@"E:\TeleFiles\{msg.Chat.Id}\" + $"_{path}", FileMode.Create);
                await bot.DownloadFileAsync(file.FilePath, fs);
                fs.Close();

                fs.Dispose();
                await bot.SendTextMessageAsync(msg.Chat.Id, "Установка успешно завершена");
            }
            catch (Exception ex)
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, "Ошибка\n" +
                   $"{ex.Message}");
                Console.WriteLine("Error downloading: " + ex.Message);

            }
        }
    }
}
