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
            release_Resource();
            ApplicationConfiguration.Initialize();

            // 全局异常处理：捕获未处理异常并记录详细堆栈
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                var logPath = Path.Combine(Application.StartupPath, "crash.log");
                var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ThreadException:\n{e.Exception}\n\n";
                File.AppendAllText(logPath, msg);
                MessageBox.Show(
                    $"发生异常（已记录到 crash.log）:\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                    "AlloyAct Pro 错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var logPath = Path.Combine(Application.StartupPath, "crash.log");
                var ex = e.ExceptionObject as Exception;
                var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UnhandledException:\n{ex}\n\n";
                File.AppendAllText(logPath, msg);
            };

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
                    if (Directory.Exists(Path.GetDirectoryName(filepath)))
                    {
                        using (FileStream fs = new FileStream(filepath, FileMode.CreateNew))
                        {
                            fs.Write(file, 0, file.Length);
                            fs.Flush();
                            fs.Close();
                        }
                    }
                    else
                    {
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
