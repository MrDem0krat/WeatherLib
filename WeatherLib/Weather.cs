using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using MySql.Data.MySqlClient;
using NLog;
using System.Net;
using System.Security;

namespace WeatherLib
{
    public partial class Weather
    {
        #region Поля данных
        private static Logger logger = LogManager.GetCurrentClassLogger(); 
        #endregion
        #region Свойства
        public DateTime Date { get; set; }
        public string PartOfDay { get; set; }
        public int Temperature { get; set; }
        public string Condition { get; set; }
        public string Type { get; set; }
        public string TypeShort { get; set; }
        public string WindDirection { get; set; }
        public string WindSpeed { get; set; }
        public int Humidity { get; set; }
        public int Pressure { get; set; }

        public static string CityName 
        {
            get { return WeatherLib.Properties.Settings.Default.CityName; }
            set { WeatherLib.Properties.Settings.Default.CityName = value; } 
        }
        public static string CityNameEng 
        {
            get{ return WeatherLib.Properties.Settings.Default.CityNameEng; }
            set{ WeatherLib.Properties.Settings.Default.CityNameEng = value; }
        }
        public static string CityID 
        {
            get { return WeatherLib.Properties.Settings.Default.CityID; }
            set { WeatherLib.Properties.Settings.Default.CityID = value; } 
        }

        public struct DayPart
        {
            public static string Morning
            {
                get { return "morning"; }
            }
            public static string Day
            {
                get { return "day"; }
            }
            public static string Evening
            {
                get { return "evening"; }
            }
            public static string Night
            {
                get { return "night"; }
            }
            
            public static int IndexOf(string obj)
            {
                switch (obj)
                {
                    default:
                        return -1;
                    case "morning":
                        return 0;
                    case "day":
                        return 1;
                    case "evening":
                        return 2;
                    case "night":
                        return 3;
                }
            }
            public static string ValueOf(int index)
            {
                switch (index)
                {
                    default:
                        return null;
                    case 0:
                        return "morning";
                    case 1:
                        return "day";
                    case 2:
                        return "evening";
                    case 3:
                        return "night";
                }
            }
        };
        public struct DayPartRus
        {
            public static string Morning
            {
                get { return "Утро"; }
            }
            public static string Day
            {
                get { return "День"; }
            }
            public static string Evening
            {
                get { return "Вечер"; }
            }
            public static string Night
            {
                get { return "Ночь"; }
            }
            public static string Convert(string part)
            {
                switch (part)
	            {
		            default:
                        return null;
                    case "morning":
                        return "Утро";
                    case "day":
                        return "День";
                    case "evening":
                        return "Вечер";
                    case "night":
                        return "Ночь";
	            }
            }
        };
        #endregion
        
        public Weather()
        {
            Date = new DateTime();
            PartOfDay = "";
            Temperature = 0;
            Condition = "";
            Type = "";
            TypeShort = "";
            WindDirection = "";
            WindSpeed = "";
            Humidity = 0;
            Pressure = 0;
        }

        public static string ReadCityNameEng()
        {
            XDocument forecast = XDocument.Load(WeatherLib.Properties.Settings.Default.FilePath);
            if (forecast.Root.Attribute("slug").Value != null)
                return forecast.Root.Attribute("slug").Value;
            else
                return "";
        }
        public async static Task<string> ReadCityNameEngAsync()
        {
            XDocument forecast = await Task<XDocument>.Factory.StartNew(() => 
            {
                return XDocument.Load(WeatherLib.Properties.Settings.Default.FilePath);
            });
            if(forecast.Root.Attribute("slug").Value != null)
                return forecast.Root.Attribute("slug").Value;
            else
                return "";
        }
        //Чтение из файла погоды в _src день в _part время суток
        public static Weather ReadPart (XElement _src, XNamespace _ns, int _part) 
        {
            CultureInfo culture;
            DateTimeStyles style;
            Weather weather = new Weather();
            string str;

            culture = CultureInfo.CreateSpecificCulture("fr-FR");
            style = DateTimeStyles.None;
            
            weather.Date = DateTime.Parse(_src.Attribute("date").Value, culture, style);
            weather.PartOfDay = _src.Elements(_ns + "day_part").ElementAt(_part).Attribute("type").Value;
            if (_src.Elements(_ns + "day_part").ElementAt(_part).Element(_ns + "temperature") != null)
                weather.Temperature = int.Parse(_src.Elements(_ns + "day_part").ElementAt(_part).Element(_ns + "temperature").Value);
            else
            {
                int temp = int.Parse(_src.Elements(_ns + "day_part").ElementAt(_part).Element(_ns + "temperature_to").Value) +
                           int.Parse(_src.Elements(_ns + "day_part").ElementAt(_part).Element(_ns + "temperature_from").Value);
                weather.Temperature = temp / 2;
            }
            weather.Condition = _src.Elements(_ns + "day_part").ElementAt(_part).Element(_ns + "image-v3").Value;
            str = _src.Elements(_ns + "day_part").ElementAt(_part).Element(_ns + "weather_type").Value;
            weather.Type = str[0].ToString().ToUpper() + str.Substring(1);
            str = _src.Elements(_ns + "day_part").ElementAt(_part).Element(_ns + "weather_type_short").Value;
            weather.TypeShort = str[0].ToString().ToUpper() + str.Substring(1);
            weather.WindDirection = _src.Elements(_ns + "day_part").ElementAt(_part).Element(_ns + "wind_direction").Value;
            weather.WindSpeed = _src.Elements(_ns + "day_part").ElementAt(_part).Element(_ns + "wind_speed").Value;
            weather.Humidity = int.Parse(_src.Elements(_ns + "day_part").ElementAt(_part).Element(_ns + "humidity").Value);
            weather.Pressure = int.Parse(_src.Elements(_ns + "day_part").ElementAt(_part).Element(_ns + "pressure").Value);
            return weather;
        }

         //Чтение всей погоды из файла
        public static List<Weather> ReadAll ()
        {
            XDocument weather = XDocument.Load(WeatherLib.Properties.Settings.Default.FilePath);
            XNamespace ns = weather.Root.Name.Namespace;
            List<Weather> forecast = new List<Weather>();
            CityNameEng = ReadCityNameEng();
            if (weather.Root.Attribute("slug").Value != WeatherLib.Properties.Settings.Default.CityNameEng)
            {
                WeatherLib.Properties.Settings.Default.CityNameEng = weather.Root.Attribute("slug").Value;
            }
            foreach(XElement element in weather.Root.Elements(ns + "day"))
            {
                for(int part = 0; part < 4; part++)
                {
                    forecast.Add(ReadPart(element, ns, part));
                }
            }
            return forecast;
        }
        public async static Task<List<Weather>> ReadAllAsync()
        {
            XDocument forecast = await Task<XDocument>.Factory.StartNew(() => { return XDocument.Load(WeatherLib.Properties.Settings.Default.FilePath); });
            XNamespace ns = forecast.Root.Name.Namespace;
            List<Weather> weather = new List<Weather>();
            CityNameEng = await ReadCityNameEngAsync();
            await Task.Factory.StartNew(() =>
                {
                    foreach (XElement element in forecast.Root.Elements(ns + "day"))
                    {
                        for (int part = 0; part < 4; part++)
                            weather.Add(ReadPart(element, ns, part));
                    }
                });
            return weather;
        }

        //Чтение текущей погоды
        public static Weather Now () 
        {
            XDocument forecast = XDocument.Load(WeatherLib.Properties.Settings.Default.FilePath);
            XNamespace ns = forecast.Root.Name.Namespace;
            XElement fact = forecast.Root.Element(ns + "fact");
            Weather weather = new Weather();
            CultureInfo culture;
            DateTimeStyles style;
            string str;
            
            culture = CultureInfo.CreateSpecificCulture("fr-FR");
            style = DateTimeStyles.None;

            weather.Date = DateTime.Parse(fact.Element(ns + "uptime").Value, culture, style);
            if (DateTime.Now.Hour >= 6)
                if (DateTime.Now.Hour >= 12)
                    if (DateTime.Now.Hour >= 18)
                        weather.PartOfDay = Weather.DayPart.Evening;
                    else
                        weather.PartOfDay = Weather.DayPart.Day;
                else
                    weather.PartOfDay = Weather.DayPart.Morning;
            else
                weather.PartOfDay = Weather.DayPart.Night;

            if (fact.Elements(ns + "temperature") != null)
                weather.Temperature = int.Parse(fact.Element(ns + "temperature").Value);
            else
            {
                int temp = int.Parse(fact.Element(ns + "temperature_to").Value) +
                    int.Parse(fact.Element(ns + "temperature_from").Value);
                weather.Temperature = temp / 2;
            }
            weather.Condition = fact.Element(ns + "image-v3").Value;
            str = fact.Element(ns + "weather_type").Value;
            weather.Type = str[0].ToString().ToUpper() + str.Substring(1);
            str = fact.Element(ns + "weather_type_short").Value;
            weather.TypeShort = str[0].ToString().ToUpper() + str.Substring(1);
            weather.WindDirection = fact.Element(ns + "wind_direction").Value;
            weather.WindSpeed = fact.Element(ns + "wind_speed").Value;
            weather.Humidity = int.Parse(fact.Element(ns + "humidity").Value);
            weather.Pressure = int.Parse(fact.Element(ns + "pressure").Value);

            return weather;
        }
        public async static Task<Weather> NowAsync()
        {
            XDocument forecast = await Task<XDocument>.Factory.StartNew(() => 
            { 
                return XDocument.Load(WeatherLib.Properties.Settings.Default.FilePath); 
            });
            XNamespace ns = forecast.Root.Name.Namespace;
            XElement now = forecast.Root.Element(ns + "fact");
            Weather weather = new Weather();
            CultureInfo culture;
            DateTimeStyles style;
            string str;
            culture = CultureInfo.CreateSpecificCulture("fr-FR");
            style = DateTimeStyles.None;
            await Task.Factory.StartNew(() =>
                {
                    weather.Date = DateTime.Parse(now.Element(ns + "uptime").Value, culture, style);
                    if (DateTime.Now.Hour >= 6)
                        if (DateTime.Now.Hour >= 12)
                            if (DateTime.Now.Hour >= 18)
                                weather.PartOfDay = Weather.DayPart.Evening;
                            else
                                weather.PartOfDay = Weather.DayPart.Day;
                        else
                            weather.PartOfDay = Weather.DayPart.Morning;
                    else
                        weather.PartOfDay = Weather.DayPart.Night;

                    if (now.Elements(ns + "temperature") != null)
                        weather.Temperature = int.Parse(now.Element(ns + "temperature").Value);
                    else
                    {
                        int temp = int.Parse(now.Element(ns + "temperature_to").Value) +
                            int.Parse(now.Element(ns + "temperature_from").Value);
                        weather.Temperature = temp / 2;
                    }
                    weather.Condition = now.Element(ns + "image-v3").Value;
                    str = now.Element(ns + "weather_type").Value;
                    weather.Type = str[0].ToString().ToUpper() + str.Substring(1);
                    str = now.Element(ns + "weather_type_short").Value;
                    weather.TypeShort = str[0].ToString().ToUpper() + str.Substring(1);
                    weather.WindDirection = now.Element(ns + "wind_direction").Value;
                    weather.WindSpeed = now.Element(ns + "wind_speed").Value;
                    weather.Humidity = int.Parse(now.Element(ns + "humidity").Value);
                    weather.Pressure = int.Parse(now.Element(ns + "pressure").Value);
                });
            return weather;
        }

        //Функция проверки подключения к интернету
        public static bool CheckInternet()
        {
            WebClient client = new WebClient();
            string response;
            try
            {
                response = client.DownloadString("http://www.ya.ru");
                logger.Debug("Проверка доступа в интернет прошла успешно");
                return true;
            }
            catch(WebException ex)
            {
                logger.Info(String.Format("Отсутствует подключение к сети Интернет. {0}", ex.Message));
                return false;
            }
        }

        // Загрузка XML файла с погодой с сайта
        public static bool Load ()
        {
            string weather_address = WeatherLib.Properties.Settings.Default.WebPath + CityID + ".xml";
            XmlDocument result = new XmlDocument();
            if (Weather.CheckInternet())
            {
                result.Load(weather_address);
                isAllDirectoryExists(); 
                result.Save(WeatherLib.Properties.Settings.Default.FilePath);
                logger.Trace(String.Format("Файл прогноза погоды успешно загружен."));
                return true;
            }
            else
            {
                logger.Trace("Прогноз погоды не загружен.");
                return false;
            }
        }
        public async static Task<bool> LoadAsync()
        {
            string WeatherAddress = WeatherLib.Properties.Settings.Default.WebPath + CityID + ".xml";
            XmlDocument result = new XmlDocument();
            bool IsInternetOk = await Task<bool>.Factory.StartNew(() => Weather.CheckInternet());
            if (IsInternetOk == true)
            {
                await Task.Factory.StartNew(() =>
                    {
                        result.Load(WeatherAddress);
                        isAllDirectoryExists();
                        result.Save(WeatherLib.Properties.Settings.Default.FilePath);
                    });
                logger.Trace("Прогноз погоды успешно загружен");
                return true;
            }
            else
            {
                logger.Trace("При загрузке прогноза погоды произошла ошибка. Прогноз не загружен.");
                return false;
            }
        }
      
        // Локализация названия дня недели
        public static string DayOfWeekRus(DateTime date)
        {
            switch(date.DayOfWeek)
            {
                default:
                    return "";
                case DayOfWeek.Monday:
                    return "Понедельник";
                case DayOfWeek.Tuesday:
                    return "Вторник";
                case DayOfWeek.Wednesday:
                    return "Среда";
                case DayOfWeek.Thursday:
                    return "Четверг";
                case DayOfWeek.Friday:
                    return "Пятница";
                case DayOfWeek.Saturday:
                    return "Суббота";
                case DayOfWeek.Sunday:
                    return "Воскресенье";
            }
        }
        // Локализация вывода направления ветра
        public static string WindDirectionRus(string src)
        {
            switch (src)
            {
                case "n":
                    return "↓С";
                case "ne":
                    return "↙СВ";
                case "e":
                    return "←В";
                case "se":
                    return "↖ЮВ";
                case "s":
                    return "↑Ю";
                case "sw":
                    return "↗ЮЗ";
                case "w":
                    return "→З";
                case "nw":
                    return "↘СЗ";
                default:
                    return "";
            }
        }
        private static void isAllDirectoryExists() 
        {
            if (!File.Exists(WeatherLib.Properties.Settings.Default.FilePath))
            {
                if (!Directory.Exists("content"))
                    Directory.CreateDirectory("content");
                if (!Directory.Exists("content/xml"))
                    Directory.CreateDirectory("content/xml");
            }
        }
        // Сохранение настроек
        public static void SaveAuthData()
        {
            WeatherLib.Properties.Settings.Default.Save();
            logger.Debug("Настройки успешно сохранены");
        }

    }
}
