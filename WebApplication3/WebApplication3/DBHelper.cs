using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace WebApplication3
{
    public static class DBHelper
    {
        private const string NullOrEmptyConnectionStringErrorMsg = "连接字符串为空！";
        private const string CallEndCommandBeforeCallBeginCommandErrorMsg = "使用EndCommand前未调用BeginCommand！";
        private const string ParametersErrorMsg = "传递的参数与存储过程参数不匹配。\n 存储过程名称: {0}";

        private static List<string> mCommandList = new List<string>();
        private static bool mBatchCommandMode = false;

        public static string ConnectionString { get; set; } = string.Empty;
        public static string InputCommands { get; private set; } = string.Empty;

        public static bool TryConnect()
        {
            bool result = false;
            if (string.IsNullOrEmpty(ConnectionString))
                return result;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    conn.Close();
                    result = true;
                }
                catch (Exception)
                {
                    result = false;
                }
                finally
                {
                    conn.Close();
                }
            }
            return result;
        }

        public static string DBNow()
        {
            string dateTime = ExecuteScalar("SELECT GETDATE()").ToString();
            return dateTime;
        }

        public static int FillTable(string strCmd, DataTable result, bool append = false)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception(NullOrEmptyConnectionStringErrorMsg);

            if (!append)
            {
                result.Rows.Clear();
                result.Columns.Clear();
            }

            int retVal = 0;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter(strCmd, conn);
                    retVal = dataAdapter.Fill(result);
                    conn.Close();
                }
                catch (Exception)
                {
                    InputCommands = strCmd;
                    throw;
                }
                finally
                {
                    conn.Close();
                }
            }
            return retVal;
        }

        public static void FillSchema(string strCmd, DataTable result)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception(NullOrEmptyConnectionStringErrorMsg);

            result.Rows.Clear();
            result.Columns.Clear();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter(strCmd, conn);
                    dataAdapter.FillSchema(result, SchemaType.Source);
                    conn.Close();
                }
                catch (Exception)
                {
                    InputCommands = strCmd;
                    throw;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        public static void BeginCommand()
        {
            mBatchCommandMode = true;
            mCommandList.Clear();
        }

        public static int ExecuteCommand(string strCmd)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception(NullOrEmptyConnectionStringErrorMsg);

            int retVal = 0;
            if (mBatchCommandMode)
            {
                mCommandList.Add(strCmd);
                return retVal;
            }

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(strCmd, conn);
                    retVal = cmd.ExecuteNonQuery();
                    conn.Close();
                }
                catch (Exception)
                {
                    InputCommands = strCmd;
                    throw;
                }
                finally
                {
                    conn.Close();
                }
            }

            return retVal;
        }

        public static int EndCommand()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception(NullOrEmptyConnectionStringErrorMsg);
            if (!mBatchCommandMode)
                throw new Exception(CallEndCommandBeforeCallBeginCommandErrorMsg);

            int retVal = 0;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                try
                {
                    conn.Open();
                    cmd.Transaction = conn.BeginTransaction();//开启事务  
                    foreach (string strCmd in mCommandList)
                    {
                        cmd.CommandText = strCmd;
                        retVal += cmd.ExecuteNonQuery();
                    }
                    cmd.Transaction.Commit();//提交事务  
                }
                catch (Exception)
                {
                    if (cmd.Transaction != null)
                        cmd.Transaction.Rollback();//回滚事务  
                    string input = string.Empty;
                    foreach (string strCmd in mCommandList)
                    {
                        if (!string.IsNullOrEmpty(input))
                            input += Environment.NewLine;
                        input += strCmd;
                    }
                    InputCommands = input;
                    throw;
                }
                finally
                {
                    if (cmd.Transaction != null)
                        cmd.Transaction = null;//清空事务  
                    conn.Close();

                    mCommandList.Clear();
                    mBatchCommandMode = false;
                }
            }
            return retVal;
        }

        public static object ExecuteScalar(string strCmd)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception(NullOrEmptyConnectionStringErrorMsg);

            SqlCommand cmd;
            SqlConnection conn = new SqlConnection(ConnectionString);
            object retval = null;
            try
            {
                conn.Open();
                cmd = new SqlCommand(strCmd, conn);
                retval = cmd.ExecuteScalar();
                conn.Close();
            }
            catch (Exception)
            {
                InputCommands = strCmd;
                throw;
            }
            finally
            {
                conn.Close();
            }
            return retval;
        }

        public static int ExecuteProcedure(string procedureName, object[] paraValues, DataTable outputTable = null, Hashtable outputValues = null)
        {
            int returnValue = 0;

            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception(NullOrEmptyConnectionStringErrorMsg);

            if (outputTable != null)
            {
                outputTable.Rows.Clear();
                outputTable.Columns.Clear();
            }

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(procedureName, conn);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                try
                {
                    // 获取参数
                    sqlCommand.Connection.Open();
                    SqlCommandBuilder.DeriveParameters(sqlCommand);
                    sqlCommand.Connection.Close();
                    // 用指定的参数值列表为存储过程参数赋值
                    if (paraValues != null)
                    {
                        if ((sqlCommand.Parameters.Count - 1) != paraValues.Length)
                            throw new Exception(string.Format(ParametersErrorMsg, procedureName));

                        for (int i = 0; i < paraValues.Length; i++)
                            sqlCommand.Parameters[i + 1].Value = (paraValues[i] == null) ? DBNull.Value : ConvertToSqlType(sqlCommand.Parameters[i + 1].SqlDbType, paraValues[i].ToString());
                    }

                    conn.Open();
                    if (outputTable == null)
                        sqlCommand.ExecuteNonQuery();
                    else
                    {
                        SqlDataAdapter dataAdapter = new SqlDataAdapter(sqlCommand);
                        dataAdapter.Fill(outputTable);
                    }
                    foreach (SqlParameter para in sqlCommand.Parameters)
                    {
                        if (para.Direction == ParameterDirection.ReturnValue)
                            int.TryParse(Convert.ToString(para.Value), out returnValue);
                        else if (para.Direction == ParameterDirection.Output
                            || para.Direction == ParameterDirection.InputOutput)
                        {
                            if (outputValues != null)
                                outputValues.Add(para.ParameterName, para.Value);
                        }
                    }
                    conn.Close();
                }
                catch (Exception)
                {
                    InputCommands = GetInputInfo(procedureName, paraValues, sqlCommand);
                    throw;
                }
                finally
                {
                    conn.Close();
                }
            }

            return returnValue;
        }

        public static int ExecuteProcedureFillTable(DataTable table, string procedureName, params object[] paraValues)
        {
            int returnValue = 0;

            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception(NullOrEmptyConnectionStringErrorMsg);

            table.Rows.Clear();
            table.Columns.Clear();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(procedureName, conn);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                try
                {
                    // 获取参数
                    sqlCommand.Connection.Open();
                    SqlCommandBuilder.DeriveParameters(sqlCommand);
                    sqlCommand.Connection.Close();
                    // 用指定的参数值列表为存储过程参数赋值
                    if (paraValues != null)
                    {
                        if ((sqlCommand.Parameters.Count - 1) != paraValues.Length)
                            throw new Exception(string.Format(ParametersErrorMsg, procedureName));

                        for (int i = 0; i < paraValues.Length; i++)
                            sqlCommand.Parameters[i + 1].Value = (paraValues[i] == null) ? DBNull.Value : ConvertToSqlType(sqlCommand.Parameters[i + 1].SqlDbType, paraValues[i].ToString());
                    }

                    conn.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter(sqlCommand);
                    dataAdapter.Fill(table);
                    foreach (SqlParameter para in sqlCommand.Parameters)
                    {
                        if (para.Direction == ParameterDirection.ReturnValue)
                        {
                            int.TryParse(Convert.ToString(para.Value), out returnValue);
                            break;
                        }
                    }
                    conn.Close();
                }
                catch (Exception)
                {
                    InputCommands = GetInputInfo(procedureName, paraValues, sqlCommand);
                    throw;
                }
                finally
                {
                    conn.Close();
                }
            }

            return returnValue;
        }

        public static Hashtable ExecuteProcedureReturnMsg(string procedureName, params object[] paraValues)
        {
            Hashtable outputValues = new Hashtable();

            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception(NullOrEmptyConnectionStringErrorMsg);

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(procedureName, conn);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                try
                {
                    // 获取参数
                    sqlCommand.Connection.Open();
                    SqlCommandBuilder.DeriveParameters(sqlCommand);
                    sqlCommand.Connection.Close();
                    // 用指定的参数值列表为存储过程参数赋值
                    if (paraValues != null)
                    {
                        if ((sqlCommand.Parameters.Count - 1) != paraValues.Length)
                            throw new Exception(string.Format(ParametersErrorMsg, procedureName));

                        for (int i = 0; i < paraValues.Length; i++)
                            sqlCommand.Parameters[i + 1].Value = (paraValues[i] == null) ? DBNull.Value : ConvertToSqlType(sqlCommand.Parameters[i + 1].SqlDbType, paraValues[i].ToString());
                    }

                    conn.Open();
                    sqlCommand.ExecuteNonQuery();
                    foreach (SqlParameter para in sqlCommand.Parameters)
                    {
                        if (para.Direction == ParameterDirection.Output
                            || para.Direction == ParameterDirection.InputOutput)
                            outputValues.Add(para.ParameterName, para.Value);
                    }
                    conn.Close();
                }
                catch (Exception)
                {
                    InputCommands = GetInputInfo(procedureName, paraValues, sqlCommand);
                    throw;
                }
                finally
                {
                    conn.Close();
                }
                return outputValues;
            }
        }

        private static object ConvertToSqlType(SqlDbType sqlType, string value)
        {
            switch (sqlType)
            {
                case SqlDbType.Bit:
                    {
                        bool retval = false;
                        if (!Boolean.TryParse(value, out retval))
                        {
                            if (!string.IsNullOrEmpty(value))
                            {
                                if (value == "1")
                                    return true;
                                else if (value == "0")
                                    return false;
                            }
                            return null;
                        }
                        return retval;
                    }
                case SqlDbType.DateTime:
                case SqlDbType.SmallDateTime:
                    {
                        DateTime retval = DateTime.MinValue;
                        if (!DateTime.TryParse(value, out retval))
                            return null;
                        return retval;
                    }
                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                    {
                        Decimal retval = 0;
                        if (!Decimal.TryParse(value, out retval))
                            return null;
                        return retval;
                    }
                case SqlDbType.Real:
                    {
                        Single retval = 0;
                        if (!Single.TryParse(value, out retval))
                            return null;
                        return retval;
                    }
                case SqlDbType.Float:
                    {
                        Double retval = 0.0F;
                        if (!Double.TryParse(value, out retval))
                            return null;
                        return retval;
                    }
                case SqlDbType.TinyInt:
                    {
                        Byte retval = 0;
                        if (!Byte.TryParse(value, out retval))
                            return null;
                        return retval;
                    }
                case SqlDbType.SmallInt:
                    {
                        Int16 retval = 0;
                        if (!Int16.TryParse(value, out retval))
                            return null;
                        return retval;
                    }
                case SqlDbType.Int:
                    {
                        Int32 retval = 0;
                        if (!Int32.TryParse(value, out retval))
                            return null;
                        return retval;
                    }
                case SqlDbType.BigInt:
                    {
                        Int64 retval = 0;
                        if (!Int64.TryParse(value, out retval))
                            return null;
                        return retval;
                    }
                case SqlDbType.Char:
                case SqlDbType.Text:
                case SqlDbType.NChar:
                case SqlDbType.NText:
                case SqlDbType.VarChar:
                case SqlDbType.NVarChar:
                case SqlDbType.Xml:
                    return value;
                case SqlDbType.Binary:
                case SqlDbType.Image:
                case SqlDbType.Timestamp:
                case SqlDbType.Udt:
                case SqlDbType.UniqueIdentifier:
                case SqlDbType.VarBinary:
                case SqlDbType.Variant:
                    return value;
                default:
                    return null;
            }
        }

        private static string GetInputInfo(string procedureName, object[] paraValues, SqlCommand sqlCommand)
        {
            string input = procedureName;
            input += Environment.NewLine;
            if (paraValues != null)
            {
                input += "paraValues(";
                for (int i = 0; i < paraValues.Length; ++i)
                {
                    if (i > 0)
                        input += ", ";
                    input += ("[" + i + "]" + (paraValues[i] == null ? "null" : paraValues[i].ToString()));
                }

                input += ")";
            }
            input += Environment.NewLine;
            input += "Parameters(";
            if (sqlCommand.Parameters.Count > 1)
            {
                for (int i = 1; i < sqlCommand.Parameters.Count; i++)
                {
                    if (i > 1)
                        input += ", ";
                    input += ("[" + (i - 1) + "]" + sqlCommand.Parameters[i].ParameterName);
                }
            }
            input += ")";
            return input;
        }
    }
}
