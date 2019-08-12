using System;
using MySql.Data.MySqlClient;
using LinuxTcpServerDotnetCore.Statics;

namespace Sql
{
    public static class SqlWorker
    {
        public static bool MySqlInit()
        {
            MySqlConnection conn;
            conn = new MySqlConnection(StaticObjects.SqlUrl);

            var sqlre = true;
            try
            {
                conn.Open();
                Debuger.PrintStr("Connected!", EPRINT_TYPE.NORMAL, false);
            }
            catch (MySqlException ex)
            {
                Debuger.PrintStr(ex.Message, EPRINT_TYPE.ERROR, false);
                sqlre = false;
            }
            finally
            {
                conn.Close();

            }
            return sqlre;
        }

        public static int MySqlInsert(string dataBase, string tableName, string[] colNames, string[] values)
        {
            string command = "INSERT INTO " + dataBase + "." + tableName + " (";
            for (int i = 0; i < colNames.Length; i++)
            {
                command = command + colNames[i];
                if (i != colNames.Length - 1)
                {
                    command = command + ",";
                }
            }
            command = command + ") value (";
            for (int i = 0; i < values.Length; i++)
            {
                command = command + "\"" + values[i] + "\"";
                if (i != values.Length - 1)
                {
                    command = command + ",";
                }
            }
            command = command + ");";
            using (var conn = new MySqlConnection(StaticObjects.SqlUrl))
            {
                conn.Open();
                var comm = new MySqlCommand(command, conn);
                int re = comm.ExecuteNonQuery();
                return re;
            }
        }

        public static MySqlDataReader MySqlQuery(string dataBase, string tableName, string[] colNames, string where, string whereValue, out MySqlConnection conn, out string reStr)
        {
            MySqlDataReader reader = null;
            conn = null;
            try
            {
                string command = "select ";
                for (int i = 0; i < colNames.Length; i++)
                {
                    command = command + colNames[i];
                    if (i != colNames.Length - 1)
                    {
                        command = command + ",";
                    }
                    else
                    {
                        command = command + " ";
                    }
                }
                command = command + " from " + dataBase + "." + tableName;
                if (where != null)
                    command = command + " where " + where + "=\"" + whereValue + "\"";
                conn = new MySqlConnection(StaticObjects.SqlUrl);
                conn.Open();
                MySqlCommand CMD = new MySqlCommand(command, conn);

                reader = CMD.ExecuteReader();
                reStr = "OK";
                return reader;
            }
            catch (Exception ex)
            {
                if (conn != null)
                {
                    conn.Close();
                }
                reStr = ex.Message;
                reader = null;
                return reader;
            }
        }

        public static MySqlDataReader MySqlLocateQuery(string dataBase, string tableName, string[] colNames, string[] locateColName, string[] locateString, out MySqlConnection conn)
        {
            MySqlDataReader reader = null;
            conn = null;
            try
            {
                string command = "select ";
                for (int i = 0; i < colNames.Length; i++)
                {
                    command = command + colNames[i];
                    if (i != colNames.Length - 1)
                    {
                        command = command + ",";
                    }
                    else
                    {
                        command = command + " ";
                    }
                }
                command = command + " from " + dataBase + "." + tableName;
                if (locateColName != null)
                    if (locateColName.Length > 0)
                    {
                        command = command + " where ";
                        for (int i = 0; i < locateColName.Length; i++)
                        {
                            command = command + "locate(" + "\'" + locateString[i] + '\'' + "," + locateColName[i] + ") ";
                            if(i == locateColName.Length -1)
                            {
                                command = command + "or ";
                            }
                            
                        }

                    }
                command = command + ";";
                conn = new MySqlConnection(StaticObjects.SqlUrl);
                conn.Open();
                MySqlCommand CMD = new MySqlCommand(command, conn);

                reader = CMD.ExecuteReader();
                return reader;
            }
            catch
            {
                if (conn != null)
                {
                    conn.Close();
                }
                reader = null;
                return reader;
            }
        }

        public static bool MySqlIsExist(string dataBase, string tableName, string colNames, string specificValue)
        {
            string command = "select " + colNames;

            command = command + " from " + dataBase + "." + tableName + " where " + colNames + "=\"" + specificValue + "\"";
            using (var conn = new MySqlConnection(StaticObjects.SqlUrl))
            {
                conn.Open();
                MySqlCommand CMD = new MySqlCommand(command, conn);
                MySqlDataReader reader = null;
                reader = CMD.ExecuteReader();
                bool isExist = false;
                while (reader.Read())
                {
                    isExist = true;
                }
                return isExist;
            }
        }

        public static bool MySqlEdit(string dataBase, string tableName, string primary_key_name, string primary_key_value, string[] keys, string[] values)
        {
            if (keys.Length != values.Length)
            {
                return false;
            }
            string command = "UPDATE " + dataBase + "." + tableName + " SET ";
            int index = 0;
            string tmp = "";
            foreach (var i in keys)
            {
                tmp = tmp + i + " = " + "\"" + values[index] + "\"";
                if (index != values.Length - 1)
                {
                    tmp = tmp + ",";
                }
                index++;
            }
            command = command + tmp + " WHERE " + primary_key_name + " = " + "\"" + primary_key_value + "\"";
            using (var conn = new MySqlConnection(StaticObjects.SqlUrl))
            {
                conn.Open();
                MySqlCommand CMD = new MySqlCommand(command, conn);
                CMD.ExecuteNonQuery();
            }
            return true;
        }

        public static bool MySqlDelete(string dataBase, string tableName, string[] id)
        {
            string command = "DELETE FROM " + dataBase + "." + tableName + " WHERE id in(";
            int index = 0;
            foreach (var i in id)
            {
                command = command + i;
                if (index != id.Length - 1)
                {
                    command = command + ",";
                }
                index++;
            }
            command = command + ")";
            using (var conn = new MySqlConnection(StaticObjects.SqlUrl))
            {
                conn.Open();
                MySqlCommand CMD = new MySqlCommand(command, conn);
                CMD.ExecuteNonQuery();
            }
            return true;
        }

        public static void MySqlFlush()
        {
            string command = "flush privileges;";
            using (var conn = new MySqlConnection(StaticObjects.SqlUrl))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(command, conn);
                cmd.ExecuteNonQuery();
                Debuger.PrintStr("flush privileges;", EPRINT_TYPE.NORMAL);
            }
        }

        public static void MySqlPureCommand(string command)
        {
            using (var conn = new MySqlConnection(StaticObjects.SqlUrl))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(command, conn);
                cmd.ExecuteNonQuery();
                Debuger.PrintStr(command, EPRINT_TYPE.NORMAL);
            }
        }

    }
}
