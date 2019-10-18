using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Npgsql;
    using System.Data;

    public class NpgsqlHelper
    {
        public NpgsqlHelper(String connectionString)
        {
            _conn = connectionString;
        }

        //####  private methods ###
        private string _conn = null;

        private NpgsqlCommand GetCommand(string query,NpgsqlParameter[] parameters)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_conn);
            //conn.UseSslStream = false;
            conn.Open();

            //query = query.ToLower();
            
            NpgsqlCommand command = new NpgsqlCommand(query, conn);
           
            command.AllResultTypesAreUnknown = true;

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }
           
            return command;
        }

        public DataTable ExecuteDataTable(string query, NpgsqlParameter[] parameters)
        {
            using (NpgsqlCommand command = GetCommand(query, parameters))
            {
                try
                {
                    var reader = command.ExecuteReader();
                    DataTable resule = new DataTable();
                    resule.Load(reader);
                    
                    return resule;                   
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
                finally
                {
                    command.Connection?.Close();
                }
            }
        }

        public DataTable ExecuteDataTable(string query)
        {
            return ExecuteDataTable(query, null);
        }

        public int ExecuteNonQuery(string query, NpgsqlParameter[] parameters)
        {
            using (NpgsqlCommand command = GetCommand(query, parameters))
            {
                try
                {
                    int result = -1;
                    return result = command.ExecuteNonQuery();
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
                finally
                {
                     command.Connection?.Close();
                }
            }
        }

        //public DataTable ExecuteDataTable(string query)
        //{
        //    return ExecuteDataTable(query, null);
        //}

        public object ExecuteDataRow(string query, NpgsqlParameter[] parameters)
        {
            using (NpgsqlCommand command = GetCommand(query, parameters))
            {
                try
                {
                    NpgsqlDataReader reader = command.ExecuteReader();
                    reader.Read();
                    return reader;
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
                finally
                {
                    command.Connection?.Close();
                }
            }
        }

        public int ExecuteNonQuery(string query)
        {
            return ExecuteNonQuery(query, null);
        }

        public void Update(DataTable dt,string tableName)
        {
            var str = $"select * from {tableName}";
            using (NpgsqlCommand command = GetCommand(str, null))
            {
                try
                {
                    NpgsqlDataAdapter da=new NpgsqlDataAdapter(command);
                    
                    da.Update(dt);
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
                finally
                {
                    command.Connection?.Close();
                }
            }
        }

        public object ExecuteScalar(string query, NpgsqlParameter[] parameters)
        {
            using (NpgsqlCommand command = GetCommand(query, parameters))
            {
                object result;
                try
                {
                    result = command.ExecuteScalar();
                    return result;
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
                finally
                {
                    command.Connection?.Close();
                }
            }
        }

        public object ExecuteScalar(string query)
        {
            return ExecuteScalar(query, null);
        }

        public NpgsqlDataReader ExecuteReader(string query, NpgsqlParameter[] parameters)
        {
            using (NpgsqlCommand command = GetCommand(query, parameters))
            {
                NpgsqlDataReader result;
                try
                {
                    result = command.ExecuteReader();
                    return result;
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
                finally
                {
                    command.Connection?.Close();
                }
            }
        }

        public NpgsqlDataReader ExecuteReader(string query)
        {
            return ExecuteReader(query, null);
        }
        //public void Insert(DataTable dt)
        //{
        //    NpgsqlConnection conn = new NpgsqlConnection(_conn);
        //    //conn.UseSslStream = false;
        //    conn.Open();

        //    var commandFormat = string.Format(CultureInfo.InvariantCulture, "COPY {0} FROM STDIN BINARY", "building");
        //    NpgsqlBinaryImporter writer = conn.BeginBinaryImport(commandFormat);
        //    foreach (var dr in dt.Rows)
        //    {

        //        writer.WriteRow(dr);
        //    }
        //    conn.Close();
        //}

        #region MyRegion
        ////#### public methods ####
        //private NpgsqlCommand GetCommand(string query, NpgsqlParameter[] npgsqlParameters, CommandType commandType)
        //{
        //    NpgsqlConnection conn = new NpgsqlConnection(_conn);
        //    //conn.UseSslStream = false;
        //    conn.Open();

        //    query = query.ToLower();

        //    NpgsqlCommand command = new NpgsqlCommand(query, conn);
        //    command.CommandType = commandType;

        //    if (npgsqlParameters is NpgsqlParameter[])
        //    {
        //        command.Parameters.AddRange(npgsqlParameters);
        //    }

        //    return command;
        //}
        //public long ExecuteNonQuery(string query, NpgsqlParameter[] npgsqlParameters)
        //{
        //    return ExecuteNonQuery(CommandType.StoredProcedure, query, npgsqlParameters);
        //}

        //public long ExecuteNonQuery(CommandType commandType, string query, NpgsqlParameter[] npgsqlParameters)
        //{

        //    using (NpgsqlCommand command = GetCommand(query, npgsqlParameters, commandType))
        //    {
        //        Int32 rowsaffected;

        //        try
        //        {
        //            rowsaffected = command.ExecuteNonQuery();
        //            return rowsaffected;
        //        }
        //        catch (Exception Ex)
        //        {
        //            throw Ex;
        //        }

        //        finally
        //        {
        //            command.Connection.Close();
        //        }
        //    }

        //}

        //public long ExecuteNonQuery(CommandType commandType, string query)
        //{
        //    return ExecuteNonQuery(commandType, query, null);
        //}

        //public object ExecuteScalar(string query, NpgsqlParameter[] npgsqlParameters)
        //{
        //    return ExecuteScalar(CommandType.StoredProcedure, query, npgsqlParameters);
        //}

        //public object ExecuteScalar(CommandType commandType, string query, NpgsqlParameter[] npgsqlParameters)
        //{
        //    using (NpgsqlCommand command = GetCommand(query, npgsqlParameters, commandType))
        //    {
        //        object result;

        //        try
        //        {
        //            result = command.ExecuteScalar();
        //            return result;
        //        }
        //        catch (Exception Ex)
        //        {
        //            throw Ex;
        //        }
        //        finally
        //        {
        //            command.Connection.Close();
        //        }
        //    }
        //}

        //public object ExecuteScalar(CommandType commandType, string query)
        //{
        //    return ExecuteScalar(commandType, query, null);
        //}

        //public DataTable[] ExecuteDataset(string query, NpgsqlParameter[] npgsqlParameters)
        //{
        //    return ExecuteDataset(CommandType.StoredProcedure, query, npgsqlParameters);
        //}



        //public DataTable[] ExecuteDataset(CommandType commandType, string query, NpgsqlParameter[] npgsqlParameters)
        //{
        //    using (NpgsqlCommand command = GetCommand(query, npgsqlParameters, commandType))
        //    {
        //        try
        //        {
        //            DataSet myDS = new DataSet();

        //            NpgsqlTransaction t = command.Connection.BeginTransaction();

        //            NpgsqlDataAdapter da = new NpgsqlDataAdapter(command);
        //            da.Fill(myDS);

        //            t.Commit();

        //            DataTable[] tables = new DataTable[myDS.Tables.Count];

        //            myDS.Tables.CopyTo(tables, 0);

        //            return tables;

        //        }
        //        catch (Exception Ex)
        //        {
        //            throw Ex;
        //        }


        //        finally
        //        {
        //            command.Connection.Close();
        //        }
        //    }
        //}

        //public DataTable[] ExecuteDataset(CommandType commandType, string query)
        //{
        //    return ExecuteDataset(commandType, query, null);
        //}

        #endregion
    }
}
