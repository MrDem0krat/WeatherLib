using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using NLog;
using System.Security.Cryptography;

namespace WeatherLib
{
    public static class WeatherDatabase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static string BaseName = "weather";

        private static byte[] additionalEntropy
        {
            get { return Convert.FromBase64String(WeatherLib.Properties.Settings.Default.AdditionalEntropy); }
        }

        public static string Server 
        {
            get { return WeatherLib.Properties.Settings.Default.Server; }
            set { WeatherLib.Properties.Settings.Default.Server = value; } 
        }
        public static uint Port 
        {
            get { return WeatherLib.Properties.Settings.Default.Port; }
            set { WeatherLib.Properties.Settings.Default.Port = value; }
        }
        public static string User 
        {
            get { return WeatherLib.Properties.Settings.Default.UserID; }
            set { WeatherLib.Properties.Settings.Default.UserID = value; }  
        }
        public static string Password 
        { 
            private get
            {
                return UnprotectPassword(WeatherLib.Properties.Settings.Default.PassWord);
            } 
            set
            {
                WeatherLib.Properties.Settings.Default.PassWord = ProtectPassword(value);
            } 
        }

        // Шифрование пароля
        private static string ProtectPassword(string src)
        {
            try
            {
                return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(src), additionalEntropy, DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException e)
            {
                logger.Debug(String.Format("Не удалось зашифровать данные: {0}", e.ToString()));
                return null;
            }
        }
        private static string UnprotectPassword(string src)
        {
            try
            {
                 return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(src),additionalEntropy,DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException e)
            {
                logger.Trace("Ошибка при подключении: Повторите ввод пароля MySQL в настрйоках приложения");
                logger.Debug(String.Format("Не удалось расшифровать данные: {0}",e.ToString()));
                return null;
            }
        }

        // Подключение к базе данных
        private static MySqlConnection Connect(string _DBName)
        {
            MySqlConnectionStringBuilder connectionBuilder = new MySqlConnectionStringBuilder();
            connectionBuilder.Password = Password;
            connectionBuilder.UserID = User;
            connectionBuilder.Server = Server;
            connectionBuilder.Port = Port;
            connectionBuilder.Database = _DBName;

            MySqlConnection connection;
            connection = new MySqlConnection();
            connection.ConnectionString = connectionBuilder.ConnectionString;
            try
            {
                connection.Open();
                logger.Debug(String.Format("Установлено соединение с базой данных '{0}' на сервере '{1}'", connection.Database, Server));
            }
            catch (Exception e)
            {
                logger.ErrorException(String.Format("При подключении к базе данных '{0}' на сервере '{1}' произошла ошибка: ", connection.Database, Server), e);
                throw e; 
            }
            return connection;
        }
        private static void Disconnect(MySqlConnection _connect)
        {
            string baseName = Regex.Match(_connect.ConnectionString, @"(?<=database=).*?(?=\;|$)").Value;
            _connect.Close();
            logger.Debug(String.Format("Соединение с базой данных '{0}' закрыто.", baseName));
        }

        // Проверка наличия базы на серевере и ее создание в случае отсутствия
        private static bool CheckDatabase()
        {
            MySqlConnection connect = new MySqlConnection();
            MySqlCommand command;
            MySqlDataReader reader;
            List<string> result = new List<string>();
            string dbName = "information_schema";
            string commandCheck = "SHOW DATABASES";
            string commandCreate = "CREATE DATABASE " + BaseName;
            try
            {
                connect = Connect(dbName);
                command = new MySqlCommand(commandCheck, connect);
                reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        result.Add(reader.GetString(0));
                    }
                    reader.Close();
                    if (result.IndexOf(BaseName) < 0)
                    {
                        logger.Debug(String.Format("Необходимая база '{0}' отсутствует на сервере.", BaseName));
                        command = new MySqlCommand(commandCreate, connect);
                        command.ExecuteNonQuery();
                        logger.Debug(String.Format("Новая база '{0}' успешно создана на сервере '{1}'.", BaseName, Server));
                    }
                    else
                    {
                        logger.Debug(String.Format("База данных '{0}' уже имеется на сервере", BaseName));
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(String.Format("При создании новой базы данных '{0}' произошла ошибка: {1}|{2}|{3}", BaseName, e.Source, e.TargetSite, e.Message));
                return false;
            }
            finally
            {
                Disconnect(connect);
            }
            return true;
        }
        private async static Task<bool> CheckDatabaseAsync()
        {
            MySqlConnection connection = new MySqlConnection();
            MySqlCommand command;
            MySqlDataReader reader;
            List<string> result = new List<string>();
            string dbName = "information_schema";
            string commandCheck = "SHOW DATABASES";
            string commandCreate = "CREATE DATABASE " + BaseName;
            try 
            {
                connection = Connect(dbName);
                command = new MySqlCommand(commandCheck, connection);
                reader = await Task<MySqlDataReader>.Factory.StartNew(() => { return command.ExecuteReader(); });
                if (reader.HasRows)
                {
                    await Task.Factory.StartNew(() =>
                    {
                        while (reader.Read())
                        {
                            result.Add(reader.GetString(0));
                        }
                        reader.Close();
                    });
                    if (result.IndexOf(BaseName) < 0)
                    {
                        logger.Debug(String.Format("Необходимая база '{0}' отсутствует на сервере.", BaseName));
                        command = new MySqlCommand(commandCreate, connection);
                        await command.ExecuteNonQueryAsync();
                        logger.Debug(String.Format("Новая база '{0}' успешно создана на сервере '{1}'.", BaseName, Server));
                    }
                    else
                    {
                        logger.Debug(String.Format("База данных '{0}' уже имеется на сервере", BaseName));
                    }
                }
            }
            catch (MySqlException e)
            {
                logger.Error(String.Format("При создании новой базы данных '{0}' произошла ошибка: {1}|{2}|{3}", BaseName, e.Source, e.TargetSite, e.Message));
                return false;
            }
            finally
            {
                Disconnect(connection);
            }
            return true;
        }

        // Проверка наличия необходимой таблицы в базе и ее создание в случае отсутствия
        private static bool CheckTable()
        {
            MySqlConnection connect = new MySqlConnection();
            MySqlCommand command;
            MySqlDataReader reader;
            string comCreate = String.Format(@"CREATE TABLE {0}.{1} (
                                    ID int(10) UNSIGNED NOT NULL AUTO_INCREMENT,
                                    Date date NOT NULL,
                                    Time time NOT NULL,
                                    PartOfDay varchar(255) NOT NULL,
                                    Temperature int(11) NOT NULL,
                                    `Condition` varchar(255) NOT NULL,
                                    Type varchar(255) NOT NULL,
                                    TypeShort varchar(255) NOT NULL,
                                    WindDirection varchar(255) NOT NULL,
                                    WindSpeed varchar(255) NOT NULL,
                                    Humidity int(11) NOT NULL,
                                    Pressure int(11) NOT NULL,
                                    PRIMARY KEY (ID)
                                )
                                ENGINE = INNODB
                                AUTO_INCREMENT = 1
                                CHARACTER SET cp1251
                                COLLATE cp1251_general_ci;",
                                BaseName, Properties.Settings.Default.CityNameEng);
            string comCheck = @"SHOW TABLES";
            try
            {
                connect = Connect(BaseName);
                command = new MySqlCommand(comCheck, connect);
                reader = command.ExecuteReader();
                List<string> resultList = new List<string>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        resultList.Add(reader.GetString(0));
                    }
                }
                reader.Close();
                if (resultList.IndexOf(Properties.Settings.Default.CityNameEng) < 0)
                {
                    logger.Debug(String.Format("Необходимая таблица '{0}' отсутствует в базе '{1}'.", Properties.Settings.Default.CityNameEng, BaseName));
                    command = new MySqlCommand(comCreate, connect);
                    command.ExecuteReader();
                    logger.Trace(String.Format("Новая таблица '{0}' в базе данных '{1}' успешно создана.", Properties.Settings.Default.CityNameEng, BaseName));
                }
                else
                {
                    logger.Debug(String.Format("Таблица '{0}' уже имеется в базе '{1}'", Properties.Settings.Default.CityNameEng, BaseName));
                }
            }
            catch (Exception e)
            {
                logger.Error(String.Format("При создании новой таблицы '{0}' произошла ошибка: {1}|{2}|{3}", Properties.Settings.Default.CityNameEng, e.Source, e.TargetSite, e.Message));
                return false;
            }
            finally
            {
                Disconnect(connect);
            }
            return true;
        }
        private async static Task<bool> CheckTableAsync()
        {
            MySqlConnection connection = new MySqlConnection();
            MySqlCommand command;
            MySqlDataReader reader;
            string comCreate = String.Format(@"CREATE TABLE {0}.{1} (
                                    ID int(10) UNSIGNED NOT NULL AUTO_INCREMENT,
                                    Date date NOT NULL,
                                    Time time NOT NULL,
                                    PartOfDay varchar(255) NOT NULL,
                                    Temperature int(11) NOT NULL,
                                    `Condition` varchar(255) NOT NULL,
                                    Type varchar(255) NOT NULL,
                                    TypeShort varchar(255) NOT NULL,
                                    WindDirection varchar(255) NOT NULL,
                                    WindSpeed varchar(255) NOT NULL,
                                    Humidity int(11) NOT NULL,
                                    Pressure int(11) NOT NULL,
                                    PRIMARY KEY (ID)
                                )
                                ENGINE = INNODB
                                AUTO_INCREMENT = 1
                                CHARACTER SET cp1251
                                COLLATE cp1251_general_ci;",
                                BaseName, Properties.Settings.Default.CityNameEng);
            string comCheck = @"SHOW TABLES";
            try
            {
                connection = Connect(BaseName);
                command = new MySqlCommand(comCheck, connection);
                reader = await Task<MySqlDataReader>.Factory.StartNew(() => { return command.ExecuteReader(); }); 
                List<string> resultList = new List<string>();
                if (reader.HasRows)
                {
                    await Task.Factory.StartNew(() =>
                        {
                            while (reader.Read())
                            {
                                resultList.Add(reader.GetString(0));
                            }
                        });
                }
                reader.Close();
                if (resultList.IndexOf(Properties.Settings.Default.CityNameEng) < 0)
                {
                    logger.Debug(String.Format("Необходимая таблица '{0}' отсутствует в базе '{1}'.", Properties.Settings.Default.CityNameEng, BaseName));
                    command = new MySqlCommand(comCreate, connection);
                    await command.ExecuteReaderAsync();
                    logger.Trace(String.Format("Новая таблица '{0}' в базе данных '{1}' успешно создана.", Properties.Settings.Default.CityNameEng, BaseName));
                }
                else
                {
                    logger.Debug(String.Format("Таблица '{0}' уже имеется в базе '{1}'", Properties.Settings.Default.CityNameEng, BaseName));
                }
            }
            catch (MySqlException e)
            {
                logger.Error(String.Format("При создании новой таблицы '{0}' произошла ошибка: {1}|{2}|{3}", Properties.Settings.Default.CityNameEng, e.Source, e.TargetSite, e.Message));
                return false;
            }
            finally
            {
                Disconnect(connection);
            }
            return true;
        }
       
        /// <summary>
        /// Проверяет наличие на MySQL-сервере необходимых для хранения истории базы данных
        /// и таблиц. Если необходимые компоненты отсутствуют - создает новые.
        /// </summary>
        /// <returns>
        /// Возвращает true если все компоненты уже имеются или вновь созданы.
        /// В случае ошибки возвращает false.
        /// </returns>
        // Проверка наличия необходимых компонентов на сервере
        public static bool Check()
        {
            if (CheckDatabase() && CheckTable())
            {
                logger.Debug(String.Format("Проверка наличия необходимых компонентов на сервере '{0}' прошла успешно.", Server));
                return true;
            }
            else
            {
                logger.Error(String.Format("В ходе проверки наличия необходимых компонентов на сервере '{0}' произошла ошибка.", Server));
                return false;
                // добавить выброс исключения
            }
        }
        public async static Task<bool> CheckAsync()
        {
            if(await CheckDatabaseAsync() && await CheckTableAsync())
            {
                logger.Debug(String.Format("Проверка наличия необходимых компонентов на сервере '{0}' прошла успешно", Server));
                return true;
            }
            else
            {
                logger.Error(String.Format("В ходе проверки наличия необходимых компонентов на сервере '{0}' произошла ошибка", Server));
                return false;
            }
        }

        //Запись показаний в базу
        public static void Write(Weather data)
        {
            MySqlCommand command = new MySqlCommand();
            command.CommandText = String.Format(@"INSERT INTO {0}(ID,Date,Time,PartOfDay,Temperature,`Condition`,Type,TypeShort,WindDirection,WindSpeed,Humidity,Pressure)
                                    VALUES(@ID,
                                           @Date,
                                           @Time,
                                           @PartOfDay,
                                           @Temperature,
                                           @Condition,
                                           @Type,
                                           @TypeShort,
                                           @WindDirection,
                                           @WindSpeed,
                                           @Humidity,
                                           @Pressure
                                          )", Properties.Settings.Default.CityNameEng);
            command.Parameters.Add("@ID", MySqlDbType.Int32);
            command.Parameters["@ID"].Value = null;
            command.Parameters.Add("@Date", MySqlDbType.Date);
            command.Parameters["@Date"].Value = data.Date.Date;
            command.Parameters.Add("@Time", MySqlDbType.Time);
            command.Parameters["@Time"].Value = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            command.Parameters.Add("@PartOfDay", MySqlDbType.VarChar);
            command.Parameters["@PartOfDay"].Value = data.PartOfDay;
            command.Parameters.Add("@Temperature", MySqlDbType.UInt32);
            command.Parameters["@Temperature"].Value = data.Temperature;
            command.Parameters.Add("@Condition", MySqlDbType.VarChar);
            command.Parameters["@Condition"].Value = data.Condition;
            command.Parameters.Add("@Type", MySqlDbType.VarChar);
            command.Parameters["@Type"].Value = data.Type;
            command.Parameters.Add("@TypeShort", MySqlDbType.VarChar);
            command.Parameters["@TypeShort"].Value = data.TypeShort;
            command.Parameters.Add("@WindDirection", MySqlDbType.VarChar);
            command.Parameters["@WindDirection"].Value = data.WindDirection;
            command.Parameters.Add("@WindSpeed", MySqlDbType.VarChar);
            command.Parameters["@WindSpeed"].Value = data.WindSpeed;
            command.Parameters.Add("@Humidity", MySqlDbType.Int32);
            command.Parameters["@Humidity"].Value = data.Humidity;
            command.Parameters.Add("@Pressure", MySqlDbType.Int32);
            command.Parameters["@Pressure"].Value = data.Pressure;
            try
            {
                command.Connection = Connect(BaseName);
                command.ExecuteNonQuery();
                logger.Debug(String.Format("Запись {0}:{1} успешно добавлена в базу", data.Date.ToShortDateString(), data.PartOfDay));
            }
            catch (Exception e)
            {
                logger.Error(String.Format("При добавлении записи {0}:{1} произошла ошибка: {2}|{3}|{4}", data.Date.ToShortDateString(), data.PartOfDay, e.Source, e.TargetSite, e.Message));
            }
            finally
            {
                Disconnect(command.Connection);
            }
        }
        public async static Task WriteAsync(Weather data)
        {
            MySqlCommand command = new MySqlCommand();
            command.CommandText = String.Format(@"INSERT INTO {0}(ID,Date,Time,PartOfDay,Temperature,`Condition`,Type,TypeShort,WindDirection,WindSpeed,Humidity,Pressure)
                                    VALUES(@ID,
                                           @Date,
                                           @Time,
                                           @PartOfDay,
                                           @Temperature,
                                           @Condition,
                                           @Type,
                                           @TypeShort,
                                           @WindDirection,
                                           @WindSpeed,
                                           @Humidity,
                                           @Pressure
                                          )", Properties.Settings.Default.CityNameEng);
            command.Parameters.Add("@ID", MySqlDbType.Int32);
            command.Parameters["@ID"].Value = null;
            command.Parameters.Add("@Date", MySqlDbType.Date);
            command.Parameters["@Date"].Value = data.Date.Date;
            command.Parameters.Add("@Time", MySqlDbType.Time);
            command.Parameters["@Time"].Value = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            command.Parameters.Add("@PartOfDay", MySqlDbType.VarChar);
            command.Parameters["@PartOfDay"].Value = data.PartOfDay;
            command.Parameters.Add("@Temperature", MySqlDbType.UInt32);
            command.Parameters["@Temperature"].Value = data.Temperature;
            command.Parameters.Add("@Condition", MySqlDbType.VarChar);
            command.Parameters["@Condition"].Value = data.Condition;
            command.Parameters.Add("@Type", MySqlDbType.VarChar);
            command.Parameters["@Type"].Value = data.Type;
            command.Parameters.Add("@TypeShort", MySqlDbType.VarChar);
            command.Parameters["@TypeShort"].Value = data.TypeShort;
            command.Parameters.Add("@WindDirection", MySqlDbType.VarChar);
            command.Parameters["@WindDirection"].Value = data.WindDirection;
            command.Parameters.Add("@WindSpeed", MySqlDbType.VarChar);
            command.Parameters["@WindSpeed"].Value = data.WindSpeed;
            command.Parameters.Add("@Humidity", MySqlDbType.Int32);
            command.Parameters["@Humidity"].Value = data.Humidity;
            command.Parameters.Add("@Pressure", MySqlDbType.Int32);
            command.Parameters["@Pressure"].Value = data.Pressure;
            try
            {
                command.Connection = Connect(BaseName);
                await command.ExecuteNonQueryAsync();
                logger.Debug(String.Format("Запись {0}:{1} успешно добавлена в базу", data.Date.ToShortDateString(), data.PartOfDay));
            }
            catch (MySqlException e)
            {
                logger.Error(String.Format("При добавлении записи {0}:{1} произошла ошибка: {2}|{3}|{4}", data.Date.ToShortDateString(), data.PartOfDay, e.Source, e.TargetSite, e.Message));
            }
            finally
            {
                Disconnect(command.Connection);
            }
        }

    }
}
