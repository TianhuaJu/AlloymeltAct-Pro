namespace AlloyAct_Pro
{
    delegate double Geo_Model(string k, string A, string B, string Geomodel);
    public delegate double Interaction_Func_b(string A, string B, double x, double y);
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.

            release_Resource();
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }

        private static void release_Resource()
        {

            byte[] dataBase = global::AlloyAct_Pro.Properties.Resources.DataBase;
            byte[] NPOI = global::AlloyAct_Pro.Properties.Resources.NPOI;

          
            string strPath_dataBase = Application.StartupPath + @"\data\DataBase.db";
            
            string NPOI_path = Application.StartupPath + @"\NPOI.dll";
            


            create_file_path(dataBase, strPath_dataBase);
           
            create_file_path(NPOI, NPOI_path);

            void create_file_path(byte[] file, string filepath)
            {

                if (!File.Exists(filepath))
                {
                    //判断文件是否存在，不存在执行以下操作
                    if (Directory.Exists(Path.GetDirectoryName(filepath)))
                    {//文件夹存在

                        using (FileStream fs = new FileStream(filepath, FileMode.CreateNew))
                        {

                            fs.Write(file, 0, file.Length);
                            fs.Flush();
                            fs.Close();
                        }
                    }
                    else
                    {
                        //文件夹不存在
                        Directory.CreateDirectory(Path.GetDirectoryName(filepath));


                        using (FileStream fs = new FileStream(filepath, FileMode.CreateNew))
                        {
                            fs.Write(file, 0, file.Length);
                            fs.Flush();
                            fs.Close();
                        }
                    }
                }

            }


        }
    }
}