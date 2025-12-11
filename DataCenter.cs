using System.Data;
using System.Data.SQLite;

namespace AlloyAct_Pro
{
    /// <summary>
    /// 获取Miedema物性参数，MVIM相互作用参数，一些必要的实验值
    /// </summary>
    static class DataCenter
    {
        private static readonly string ConnectionString = "Data Source=data\\DataBase.db";
        private static string Miedemadata = "MiedemaParameter";

        public static void Database(string database)
        {
            Miedemadata = database;
        }

        /// <summary>
        /// 从数据库读取Miedema参数
        /// </summary>
        /// <param name="E1">元素对象</param>
        public static void get_MiedemaData(Element E1)
        {
            string cmdTXT = $"SELECT phi,nws,V,u,alpha_beta,hybirdvalue,isTrans,dHtrans,mass,Tm,name,Tb FROM {Miedemadata} WHERE Symbol = @symbol";

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(cmdTXT, conn))
                {
                    cmd.Parameters.AddWithValue("@symbol", E1.Name);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            E1.Phi = reader.GetDouble(0);
                            E1.N_WS = reader.GetDouble(1);
                            E1.V = reader.GetDouble(2);
                            E1.u = reader.GetDouble(3);
                            E1.hybird_factor = reader.GetString(4);
                            E1.hybird_Value = reader.GetDouble(5);
                            E1.isTrans_group = reader.GetBoolean(6);
                            E1.dH_Trans = reader.GetDouble(7);
                            E1.M = reader.GetDouble(8);
                            E1.Tm = reader.GetDouble(9);
                            E1.Tb = reader.GetDouble(11);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从SQLite中查询一阶活度相互作用系数
        /// 数据库中的数据全部以string形式存储，除数据格式很明确外
        /// </summary>
        public static void query_first_order_wagnerIntp(Melt melt)
        {
            string cmd1 = "SELECT eji,Rank,sji,T,reference FROM first_order WHERE solv = @solv and solui = @solui and soluj = @soluj";
            string cmd2 = "SELECT eji,Rank,sji,T,reference FROM first_order WHERE solv = @solv and solui = @soluj and soluj = @solui";

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                // 首先尝试查询 k-i-j
                using (var command1 = new SQLiteCommand(cmd1, conn))
                {
                    command1.Parameters.AddWithValue("@solv", melt.Based);
                    command1.Parameters.AddWithValue("@solui", melt.solui);
                    command1.Parameters.AddWithValue("@soluj", melt.soluj);

                    using (var rd = command1.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            melt.ji_flag = true;
                            ParseFirstOrderData(rd, melt, isJI: true);
                            return;
                        }
                    }
                }

                // 未查询到k-i-j，尝试查询k-j-i
                using (var command2 = new SQLiteCommand(cmd2, conn))
                {
                    command2.Parameters.AddWithValue("@solv", melt.Based);
                    command2.Parameters.AddWithValue("@solui", melt.solui);
                    command2.Parameters.AddWithValue("@soluj", melt.soluj);

                    using (var rd1 = command2.ExecuteReader())
                    {
                        if (rd1.Read())
                        {
                            melt.ij_flag = true;
                            ParseFirstOrderData(rd1, melt, isJI: false);
                        }
                        else
                        {
                            // 未查询到任何数据
                            melt.ij_flag = false;
                            melt.ji_flag = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 解析一阶相互作用系数数据
        /// </summary>
        private static void ParseFirstOrderData(SQLiteDataReader reader, Melt melt, bool isJI)
        {
            string eValue = reader.IsDBNull(0) ? null : reader.GetString(0);
            string rank = reader.GetString(1);
            string sValue = reader.IsDBNull(2) ? null : reader.GetString(2);
            string temperature = reader.GetString(3);
            string reference = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);

            if (isJI)
            {
                if (string.IsNullOrEmpty(eValue))
                {
                    melt.eji_str = null;
                    melt.sji_str = sValue;
                }
                else
                {
                    melt.eji_str = eValue;
                    melt.sji_str = null;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(eValue))
                {
                    melt.eij_str = null;
                    melt.sij_str = sValue;
                }
                else
                {
                    melt.eij_str = eValue;
                    melt.sij_str = null;
                }
            }

            melt.Rank_firstorder = rank;
            melt.str_T = temperature;
            melt.Ref = reference;
        }

        /// <summary>
        /// 查询无限稀活度系数的实验值Yi0
        /// </summary>
        /// <param name="melt">熔体对象</param>
        public static void query_lnYi0(Melt melt)
        {
            string cmdTXT = "SELECT lnYi0,Yi0,T FROM lnY0 WHERE solv = @solv and solui = @solui";

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var command = new SQLiteCommand(cmdTXT, conn))
                {
                    command.Parameters.AddWithValue("@solv", melt.Based);
                    command.Parameters.AddWithValue("@solui", melt.solui);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tempStr = reader.GetString(2);

                            // 检查是否匹配温度条件
                            if (tempStr == "T" || tempStr == melt.str_T)
                            {
                                // 解析 lnYi0
                                if (reader.IsDBNull(0))
                                {
                                    melt.str_lnYi0 = (string.Empty, tempStr);
                                }
                                else
                                {
                                    melt.str_lnYi0 = (reader.GetString(0), tempStr);
                                }

                                // 解析 Yi0
                                if (reader.IsDBNull(1))
                                {
                                    melt.str_Yi0 = (string.Empty, tempStr);
                                }
                                else
                                {
                                    melt.str_Yi0 = (reader.GetString(1), tempStr);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
