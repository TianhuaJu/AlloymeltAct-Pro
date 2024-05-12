

using System.Data.SQLite;
using System.Data;


namespace AlloyAct_Pro
{

    /// <summary>
    /// 获取Miedema物性参数，MVIM相互作用参数，一些必要的实验值
    /// </summary>
    static class DataCenter
    {


        
        static string Miedemadata = "MiedemaParameter";
        
        public static void Database(string database)
        {
            Miedemadata = database;
        }
        /// <summary>
        /// 从数据库读取Miedema参数
        /// </summary>
        /// <param name="E1"></param>
        public static  void get_MiedemaData(Element E1)
        {

            string dbpath = "Data Source =" + "data\\DataBase.db";
            string cmdTXT = "SELECT phi,nws,V,u,alpha_beta,hybirdvalue,isTrans,dHtrans,mass,Tm,name,Tb FROM " + Miedemadata + " WHERE Symbol ='" + E1.Name + "'";
            SQLiteConnection conn = new SQLiteConnection(dbpath);
            conn.Open();
            SQLiteCommand cmd = new SQLiteCommand(cmdTXT, conn);
            SQLiteDataReader reader = cmd.ExecuteReader();
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
                //E1.Name = reader.GetString(10);
                E1.Tb = reader.GetDouble(11);
                
            }
             
            
            if (!reader.IsClosed )
            {
                reader.Close();
            }
            cmd.Dispose();

            if (conn.State == ConnectionState.Open)
            {
                 
                conn.Close();
            }
           


        }
        
     
        /// <summary>
        /// 从SQLite中查询一阶活度相互作用系数
        /// 数据库中的数据全部以string形式存储，除数据格式很明确外
        /// </summary>
        public static void query_first_order_wagnerIntp(Melt melt)
        {

            SQLiteConnection liteConnection = null;
            string dbpath = "Data Source =" + "data\\DataBase.db";//数据库中存储的eji或sji,默认是包含温度的字符串，y = a/T+b
            string cmd1 = "SELECT eji,Rank,sji,T,reference FROM first_order WHERE solv = '" + melt.Based + "' and solui = '" + melt.solui + "' and soluj='" + melt.soluj + "'";
            string cmd2 = "SELECT eji,Rank,sji,T,reference FROM first_order WHERE solv = '" + melt.Based + "' and solui = '" + melt.soluj + "' and soluj='" + melt.solui + "'";
            liteConnection = new SQLiteConnection(dbpath);
            liteConnection.Open();
            SQLiteCommand command1 = new SQLiteCommand(cmd1, liteConnection);
            SQLiteDataReader rd = command1.ExecuteReader();

            if (rd.Read())
            {
                melt.ji_flag = true;
                //查询到k-i-j,执行eji、sji赋值
                if (string.IsNullOrEmpty(rd.GetString(0)))
                {
                    melt.eji_str = null;
                    melt.Rank_firstorder = rd.GetString(1);
                    melt.sji_str = rd.GetString(2);
                    melt.str_T = rd.GetString(3);
                    if (!rd.IsDBNull(4))
                    {
                        melt.Ref = rd.GetString(4);
                    }
                    else
                    {
                        melt.Ref = String.Empty;
                    }
                }
                else
                {
                    melt.eji_str = rd.GetString(0);
                    melt.Rank_firstorder = rd.GetString(1);
                    melt.sji_str = null;
                    melt.str_T = rd.GetString(3);
                    if (!rd.IsDBNull(4))
                    {
                        melt.Ref = rd.GetString(4);
                    }
                    else
                    {
                        melt.Ref = String.Empty;
                    }
                }
                rd.Close();
            }
            else
            {

                //未查询到k-i-j,执行k-j-i查询
                SQLiteCommand commd2 = new SQLiteCommand(cmd2, liteConnection);
                SQLiteDataReader rd1 = commd2.ExecuteReader();
                if (rd1.Read())
                {
                    //查询到k-j-i
                    melt.ij_flag = true;
                    
                    if (string.IsNullOrEmpty(rd1.GetString(0)))
                    {
                        melt.eij_str = null;
                        melt.Rank_firstorder = rd1.GetString(1);
                        melt.sij_str = rd1.GetString(2);                        
                        melt.str_T = rd1.GetString(3);
                        if (!rd1.IsDBNull(4))
                        {
                            melt.Ref = rd1.GetString(4);
                        }
                        else
                        {
                            melt.Ref = String.Empty;
                        }

                    }
                    else
                    {
                       
                        melt.eij_str = rd1.GetString(0);
                        melt.sij_str = null;
                        melt.Rank_firstorder = rd1.GetString(1);
                        melt.str_T = rd1.GetString(3);
                        if (!rd1.IsDBNull(4))
                        {
                            melt.Ref = rd1.GetString(4);
                        }
                        else
                        {
                            melt.Ref = String.Empty;
                        }


                    }




                }
                else
                {
                    //未查询到k-j-i 
                    melt.ij_flag = false;
                    melt.ji_flag = false;
                }
                rd1.Close();
            }
            
            if (liteConnection.State == ConnectionState.Open)
            {
                liteConnection.Close();
            }


        }
        /// <summary>
        /// 查询无限稀活度系数的实验值Yi0
        /// </summary>
        /// <param name="melt"></param>
        public static void query_lnYi0(Melt melt)
        {
            SQLiteConnection liteConnection = null;
            string dbpath = "Data Source =" + "data\\DataBase.db";
            string cmdTXT = "SELECT lnYi0,Yi0,T FROM lnY0 WHERE solv ='"+melt.Based+"'and solui ='"+melt.solui+"'";

            liteConnection = new SQLiteConnection(dbpath);
            if (liteConnection.State != ConnectionState.Open)
            {
                liteConnection.Open();
                SQLiteCommand command = new SQLiteCommand(cmdTXT, liteConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetString(2) == "T")
                    {//是一个表达式
                        if (reader.IsDBNull(0))
                        {
                            melt.str_lnYi0 = (string.Empty, reader.GetString(2));
                        }
                        else
                        {
                            melt.str_lnYi0 = (reader.GetString(0), reader.GetString(2));
                        }

                        if (reader.IsDBNull(1))
                        {
                            melt.str_Yi0 = (string.Empty, reader.GetString(2));
                        }
                        else
                        {
                            melt.str_Yi0 = (reader.GetString(1), reader.GetString(2));

                        }

                    }
                    else if (reader.GetString(2) == melt.str_T)
                    {//可能存在多个温度下的值
                        if (reader.IsDBNull(0))
                        {
                            melt.str_lnYi0 = (string.Empty, reader.GetString(2));
                        }
                        else
                        {
                            melt.str_lnYi0 = (reader.GetString(0), reader.GetString(2));
                        }

                        if (reader.IsDBNull(1))
                        {
                            melt.str_Yi0 = (string.Empty, reader.GetString(2));
                        }
                        else
                        {
                            melt.str_Yi0 = (reader.GetString(1), reader.GetString(2));

                        }


                    }

                }
                if (liteConnection.State == ConnectionState.Open)
                {
                    liteConnection.Close();
                }

            }
            else
            {
                SQLiteCommand command = new SQLiteCommand(cmdTXT, liteConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    melt.str_lnYi0 = (reader.GetString(0), reader.GetString(1));
                }
                if (liteConnection.State == ConnectionState.Open)
                {
                    liteConnection.Close();
                }

            }


        }
        
        

      


    }
}
