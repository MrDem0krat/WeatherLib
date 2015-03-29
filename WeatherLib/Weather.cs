using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace WeatherLib
{
    public class Weather
    {
        #region Поля данных
        private static string FilePath = "content/xml/weather.xml";
        private DateTime _Date;
        private string _PartOfDay;
        private int _Temperature;
        private string _Condition;
        private string _Type;
        private string _TypeShort;
        private string _WindDirection;
        private string _WindSpeed;
        private int _Humidity;
        private int _Pressure;
        #endregion
        #region Свойства
        public DateTime Date
        {
            get { return _Date; }
            set { _Date = value; }
        }
        public string PartOfDay
        {
            get { return _PartOfDay; }
            set { _PartOfDay = value; }
        }
        public int Temperature 
        {
            get { return _Temperature; }
            set { _Temperature = value; } 
        }
        public string Condition 
        {
            get { return _Condition; }
            set { _Condition = value; } 
        }
        public string Type 
        {
            get { return _Type; }
            set { _Type = value; }
        }
        public string TypeShort 
        {
            get { return _TypeShort; }
            set { _TypeShort = value; } 
        }
        public string WindDirection 
        {
            get { return _WindDirection; }
            set { _WindDirection = value; } 
        }
        public string WindSpeed 
        {
            get { return _WindSpeed; }
            set { _WindSpeed = value; }
        }
        public int Humidity 
        {
            get { return _Humidity; }
            set { _Humidity = value; }
        }
        public int Pressure 
        {
            get { return _Pressure; }
            set { _Pressure = value; }
        }
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

         //Чтение погоды в _src день в _part время суток
        public static Weather ReadPart (XElement _src, XNamespace _ns, int _part) 
        {
            CultureInfo culture;
            DateTimeStyles style;
            DateTime date;
            Weather weather = new Weather();
            string str;

            culture = CultureInfo.CreateSpecificCulture("fr-FR");
            style = DateTimeStyles.None;
            
            DateTime.TryParse(_src.Attribute("date").Value, culture, style, out date);
            weather.Date = date;
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
            XDocument weather = XDocument.Load(FilePath);
            XNamespace ns = weather.Root.Name.Namespace;
            List<Weather> forecast = new List<Weather>();
            foreach(XElement element in weather.Root.Elements(ns + "day"))
            {
                for(int part = 0; part < 4; part++)
                {
                    forecast.Add(ReadPart(element, ns, part));
                }
            }
            return forecast;
        }

        //Чтение текущей погоды
        public static Weather Now () 
        {
            XDocument forecast = XDocument.Load(FilePath);
            XNamespace ns = forecast.Root.Name.Namespace;
            XElement fact = forecast.Root.Element(ns + "fact");
            Weather weather = new Weather();
            CultureInfo culture;
            DateTimeStyles style;
            DateTime date;
            string str;
            
            culture = CultureInfo.CreateSpecificCulture("fr-FR");
            style = DateTimeStyles.None;

            DateTime.TryParse(fact.Element(ns + "uptime").Value, culture, style, out date);
            weather.Date = date;
            weather.PartOfDay = fact.Element(ns + "daytime").Value;
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

        // Загрузка XML файла с погодой с сайта
        public static bool LoadWeather (string _cityID)
        {
            string weather_address = "http://export.yandex.ru/weather-ng/forecasts/" + _cityID + ".xml";
            XmlDocument result = new XmlDocument();
            result.Load(weather_address);
            isAllDirectoryExists();
            result.Save(FilePath);
            return true;
        }

        // Получение названия города
        public static string CityName ()
        {
            XDocument weather = XDocument.Load(FilePath);
            return weather.Root.Attribute("city").Value;
        }

        private static void isAllDirectoryExists() 
        {
            if (!File.Exists(FilePath))
            {
                if (!Directory.Exists("content"))
                    Directory.CreateDirectory("content");
                if (!Directory.Exists("content/xml"))
                    Directory.CreateDirectory("content/xml");
            }
            //if (!File.Exists("config/config.ini"))
            //{
            //    if (!Directory.Exists("config"))
            //        Directory.CreateDirectory("config");
            //    StreamWriter conf = File.CreateText("config/config.ini");
            //    conf.Close();
            //}
            //return true;
        }
    }
}
