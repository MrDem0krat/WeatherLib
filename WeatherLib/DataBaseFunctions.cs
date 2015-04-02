using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace WeatherLib
{
    public partial class Weather
    {
        // Подключение к базе данных
        private static MySqlConnection BaseConnect()
        {
            MySqlConnection connection;
            string strConn = String.Format("server={0};uid={1};pwd={2};database={3};", _DataBaseServer, _DataBaseUser, _DataBasePassword, _DataBaseName);
            connection = new MySqlConnection();
            connection.ConnectionString = strConn;
            try
            {
                connection.Open();
                logger.Debug(String.Format("Установлено соединение с базой данных '{0}' на сервере '{1}'", _DataBaseName, _DataBaseServer));
            }
            catch(Exception e)
            {
                logger.ErrorException(String.Format("При подключении к базе данных '{0}', расположенной на сервере '{1}' произошла ошибка: ", _DataBaseName, _DataBaseServer), e);
                throw e;
            }
            return connection;
        }
        private static MySqlConnection BaseConnect(string _DBName)
        {
            MySqlConnection connection;
            string strConn = String.Format("server={0};uid={1};pwd={2};database={3};", _DataBaseServer, _DataBaseUser, _DataBasePassword, _DBName);
            connection = new MySqlConnection();
            connection.ConnectionString = strConn;
            try
            {
                connection.Open();
                logger.Debug(String.Format("Установлено соединение с базой данных '{0}' на сервере '{1}'", _DBName, _DataBaseServer));
            }
            catch (Exception e)
            {
                logger.ErrorException(String.Format("При подключении к базе данных '{0}' на сервере '{1}' произошла ошибка: ", _DBName, _DataBaseServer), e);
                throw e;
            }
            return connection;
        }
        private static void BaseDisconnect(MySqlConnection _connect)
        {
            Match match = Regex.Match(_connect.ConnectionString, @"database=\w*");
            string baseName = Regex.Replace(match.ToString(), @"\w*\=", String.Empty);
            _connect.Close();
            logger.Debug(String.Format("Соединение с базой данных '{0}' закрыто.", baseName));
        }

        // Проверка наличия базы на серевере и ее создание в случае отсутствия
        private static bool BaseCheckDatabase()
        {
            MySqlConnection connect = new MySqlConnection();
            MySqlCommand command;
            MySqlDataReader reader;
            List<string> result = new List<string>();
            string dbName = "information_schema";
            string commandCheck = "SHOW DATABASES";
            string commandCreate = "CREATE DATABASE " + _DataBaseName;
            try
            {
                connect = BaseConnect(dbName);
                command = new MySqlCommand(commandCheck, connect);
                reader = command.ExecuteReader();
                if(reader.HasRows)
                {
                    while (reader.Read())
                    {
                        result.Add(reader.GetString(0));
                    }
                    reader.Close();
                    if(result.IndexOf(_DataBaseName) < 0)
                    {
                        logger.Debug(String.Format("Необходимая база '{0}' отсутствует на сервере.", _DataBaseName));
                        command = new MySqlCommand(commandCreate, connect);
                        command.ExecuteNonQuery();
                        logger.Debug(String.Format("Новая база '{0}' успешно создана на сервере '{1}'.", _DataBaseName, _DataBaseServer));
                    }
                    else
                    {
                        logger.Debug(String.Format("База данных '{0}' уже имеется на сервере", _DataBaseName));
                    }
                }
                BaseDisconnect(connect);
            }
            catch (Exception e)
            {
                logger.Error(String.Format("При создании новой базы данных '{0}' произошла ошибка: {1}|{2}|{3}", _DataBaseName, e.Source, e.TargetSite, e.Message));
                BaseDisconnect(connect);
                return false;
            }
            return true;
        }

        // Проверка наличия необходимой таблицы в базе и ее создание в случае отсутствия
        private static bool BaseCheckTable()
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
                                _DataBaseName, _DataBaseTable);
            string comCheck = @"SHOW TABLES";
            try
            {
                connect = BaseConnect();
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
                if (resultList.IndexOf(_DataBaseTable) < 0)
                {
                    logger.Debug(String.Format("Необходимая таблица '{0}' отсутствует в базе '{1}'.", _DataBaseTable, _DataBaseName));
                    command = new MySqlCommand(comCreate, connect);
                    command.ExecuteReader();
                    logger.Trace(String.Format("Новая таблица '{0}' в базе данных '{1}' успешно создана.", _DataBaseTable, _DataBaseName));
                }
                else
                {
                    logger.Debug(String.Format("Таблица '{0}' уже имеется в базе '{1}'", _DataBaseTable, _DataBaseName));
                }
                BaseDisconnect(connect);
            }
            catch (Exception e)
            {
                logger.Error(String.Format("При создании новой таблицы '{0}' произошла ошибка: {1}|{2}|{3}", _DataBaseTable, e.Source, e.TargetSite, e.Message));
                BaseDisconnect(connect);
                return false;
            }
            return true;
        }
        
        //проверка наличия необходимых базы и таблицы
        public static bool BaseCheck()
        {
            if (BaseCheckDatabase() && BaseCheckTable())
            {
                logger.Debug(String.Format("Проверка наличия необходимых компонентов на сервере '{0}' прошла успешно.", _DataBaseServer));
                return true;
            }
            else
            {
                logger.Error(String.Format("В ходе проверки наличия необходимых компонентов на сервере '{0}' произошла ошибка.", _DataBaseServer));
                return false;
            }
        } 

        //Запись показателей в базу
        public void WriteToBase()
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
                                          )",_DataBaseTable);
            command.Parameters.Add("@ID", MySqlDbType.Int32);
            command.Parameters["@ID"].Value = null;
            command.Parameters.Add("@Date", MySqlDbType.Date);
            command.Parameters["@Date"].Value = this.Date.Date;
            command.Parameters.Add("@Time", MySqlDbType.Time);
            command.Parameters["@Time"].Value = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            command.Parameters.Add("@PartOfDay", MySqlDbType.VarChar);
            command.Parameters["@PartOfDay"].Value = this.PartOfDay;
            command.Parameters.Add("@Temperature", MySqlDbType.UInt32);
            command.Parameters["@Temperature"].Value = this.Temperature;
            command.Parameters.Add("@Condition", MySqlDbType.VarChar);
            command.Parameters["@Condition"].Value = this.Condition;
            command.Parameters.Add("@Type", MySqlDbType.VarChar);
            command.Parameters["@Type"].Value = this.Type;
            command.Parameters.Add("@TypeShort", MySqlDbType.VarChar);
            command.Parameters["@TypeShort"].Value = this.TypeShort;
            command.Parameters.Add("@WindDirection", MySqlDbType.VarChar);
            command.Parameters["@WindDirection"].Value = this.WindDirection;
            command.Parameters.Add("@WindSpeed", MySqlDbType.VarChar);
            command.Parameters["@WindSpeed"].Value = this.WindSpeed;
            command.Parameters.Add("@Humidity", MySqlDbType.Int32);
            command.Parameters["@Humidity"].Value = this.Humidity;
            command.Parameters.Add("@Pressure", MySqlDbType.Int32);
            command.Parameters["@Pressure"].Value = this.Pressure;
            try
            {
                command.Connection = BaseConnect();
                command.ExecuteNonQuery();
                logger.Debug(String.Format("Запись {0}:{1} успешно добавлена в базу", this.Date.ToShortDateString(),this.PartOfDay));
                BaseDisconnect(command.Connection);
            }
            catch (Exception e)
            {
                logger.Error(String.Format("При добавлении записи {0}:{1} произошла ошибка: {2}|{3}|{4}", this.Date.ToShortDateString(), this.PartOfDay, e.Source, e.TargetSite, e.Message));
                BaseDisconnect(command.Connection);
            }
        }
    }
}
