using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherLib
{
    public partial class Weather
    {
        // Подключение к базе данных
        private static MySqlConnection BaseConnect()
        {
            MySqlConnection connection;
            string strConn = String.Format("server={0};uid={1};pwd={2};database={3};", _DataBaseServer, _DataBaseUser, _DataBasePassword, _DataBaseName);
            connection = new MySqlConnection(strConn);
            connection.Open();
            //logger.Trace(String.Format("Установлено соединение с базой данных '{0}', server = '{0}'", _DataBaseName, _DataBaseServer));
            return connection;
        }
        private static MySqlConnection BaseConnect(string _DBName)
        {
            MySqlConnection connection;
            string strConn = String.Format("server={0};uid={1};pwd={2};database={3};", _DataBaseServer, _DataBaseUser, _DataBasePassword, _DBName);
            connection = new MySqlConnection(strConn);
            connection.Open();
            //logger.Trace(String.Format("Установлено соединение с базой данных '{0}', server = '{0}'", _DBName, _DataBaseServer));
            return connection;
        }

        // Проверка наличия базы на серевере и ее создание в случае отсутствия
        private static bool BaseCheckDatabase()
        {
            MySqlConnection connect = new MySqlConnection();
            MySqlCommand command;
            string dbName = "information_schema";
            string commandCreate = "CREATE DATABASE IF NOT EXISTS " + _DataBaseName;
            try
            {
                connect = BaseConnect(dbName);
                command = new MySqlCommand(commandCreate, connect);
                command.ExecuteNonQuery();
                //logger.Trace(String.Format("Новая база данных '{0}' успешно создана.", _DataBaseName));
                connect.Close();
            }
            catch (Exception e)
            {
                connect.Close();
                //logger.ErrorException(String.Format("При создании новой базы данных '{0}' произошла ошибка: ", _DataBaseName), e);
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
                while (reader.Read())
                {
                    resultList.Add(reader.GetString(0));
                }
                reader.Close();
                if (resultList.IndexOf(_DataBaseTable) < 0)
                {
                    command = new MySqlCommand(comCreate, connect);
                    command.ExecuteReader();
                    //logger.Trace(String.Format("Новая таблица '{0}' в базе данных '{1}' успешно создана.", _DataBaseTable, _DataBaseName));
                }
                connect.Close();
            }
            catch (Exception e)
            {
                //logger.ErrorException(String.Format("При создании новой таблицы '{0}' произошла ошибка: ", _DataBaseTable), e);
                connect.Close();
                return false;
            }
            return true;
        }
        
        //проверка наличия необходимых базы и таблицы
        public static bool BaseCheck()
        {
            return BaseCheckDatabase() && BaseCheckTable(); 
        } 

        //Запись показателей в базу
        public void WriteToBase()
        {
            MySqlConnection connect = new MySqlConnection();
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
            command.Parameters["@Time"].Value = DateTime.Now.TimeOfDay;
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
                //logger.Trace(String.Format("Запись {0}:{1} успешно добавлена в базу", this.Date.ToShortDateString(),this.PartOfDay));
                connect.Close();
            }
            catch (Exception e)
            {
                //logger.ErrorException(String.Format("При добавлении записи {0}:{1} произошла ошибка: ", this.Date.ToShortDateString(), this.PartOfDay), e);
                connect.Close();
            }
        }
    }
}
