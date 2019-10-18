using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Tencent_trans
{
    class DataHelper
    {
        NpgsqlConnection _conn;
        public DataHelper(NpgsqlConnection conn)
        {
            _conn = conn;
        }
    }
}
