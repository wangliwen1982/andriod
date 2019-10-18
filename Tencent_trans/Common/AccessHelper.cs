using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace Tencent_trans.Common
{
    public class AccessHelper
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionStr { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public AccessHelper(string connection)
        {
            ConnectionStr = connection;
        }

        /// <summary>
        /// 获取table
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public async Task<DataTable> ExecuteRowAsync(string sql)
        {
            using (var conn = new OleDbConnection(ConnectionStr))
            {
                await conn.OpenAsync();
                var datatable = new DataTable();
                using (var cmd = new OleDbCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = sql;
                    var dr = await cmd.ExecuteReaderAsync();
                    datatable.Load(dr);
                    return datatable;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DataTable ExecuteRow(string sql)
        {
            using (var conn = new OleDbConnection(ConnectionStr))
            {
                conn.Open();
                var datatable = new DataTable();
                using (var cmd = new OleDbCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = sql;
                    var dr = cmd.ExecuteReader();
                    if (dr != null) datatable.Load(dr);
                    return datatable;
                }
            }
        }

        /// <summary>
        /// 获取首行首列
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(string sql)
        {
            using (var conn = new OleDbConnection(ConnectionStr))
            {
                await conn.OpenAsync();
                using (var cmd = new OleDbCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = sql;
                    return await cmd.ExecuteScalarAsync();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public object ExecuteScalar(string sql)
        {
            using (var conn = new OleDbConnection(ConnectionStr))
            {
                conn.Open();
                using (var cmd = new OleDbCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = sql;
                    return cmd.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// 是否可以打开连接
        /// </summary>
        /// <returns></returns>
        public bool CanConnected()
        {
            var canConnected = false;
            OleDbConnection conn = null;
            try
            {
                conn = new OleDbConnection(ConnectionStr);
                conn.Open();
                canConnected = true;
            }
            catch
            {
                canConnected = false;
            }
            finally
            {
                conn?.Close();
            }
            return canConnected;
        }

        public DataTable ExecuteSchemaTable()
        {
            var conn = new OleDbConnection(ConnectionStr);
            DataTable dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
            return dt;
        }


    }
}
