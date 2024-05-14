using System.Data;

namespace AlloyAct_Pro
{
    public delegate double fx(double x);
    public delegate double fy(double y, double step_length);
    /// <summary>
    /// 自定义常用函数集
    /// </summary>
    static class myFunctions
    {
        private static double pow(double x, double y)
        {
            return Math.Pow(x, y);
        }
        /// <summary>
        /// write the data into local excel file
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="filePath">写入（保存）文件地址</param>
        private static void Write2Excel(DataTable dt, string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && null != dt && dt.Rows.Count > 0)
            {
                NPOI.HSSF.UserModel.HSSFWorkbook book = new NPOI.HSSF.UserModel.HSSFWorkbook();
                NPOI.SS.UserModel.ISheet sheet = book.CreateSheet(dt.TableName);

                NPOI.SS.UserModel.IRow row = sheet.CreateRow(0);
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    row.CreateCell(i).SetCellValue(dt.Columns[i].ColumnName);
                }
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    NPOI.SS.UserModel.IRow row2 = sheet.CreateRow(i + 1);
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        row2.CreateCell(j).SetCellValue(Convert.ToString(dt.Rows[i][j]));
                    }
                }
                // 写入到客户端  
                using (MemoryStream ms = new MemoryStream())
                {
                    book.Write(ms);
                    if (File.Exists(filePath))
                    {
                        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            byte[] data = ms.ToArray();
                            fs.Write(data, 0, data.Length);

                            fs.Flush();
                        }
                    }
                    else
                    {
                        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            byte[] data = ms.ToArray();
                            fs.Write(data, 0, data.Length);
                            fs.Flush();
                        }
                    }
                    book = null;
                }
            }
        }
        /// <summary>
        /// 乔芝郁非对称组元判断规则 ,according to the relative size of the heating of each binary
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static double asymtermJudge(double a, double b, double c)
        {
            double t;

            if ((a > 0 && b > 0 && c > 0) || (a < 0 && b < 0 && c < 0))
            {
                if (a * b * c > 0)
                {

                    t = a > b ? b : a;
                    return (t > c) ? c : t;
                }
                else
                {


                    t = a > b ? a : b;
                    return (t > c) ? t : c;
                }

            }
            else
            {


                return (a * b > 0) ? c : (a * c > 0 ? b : a);
            }
        }
        public static void WriteLog(string logFile, string content)
        {
            if (Path.GetDirectoryName(logFile) != "")
            {
                if (!Directory.Exists(Path.GetDirectoryName(logFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logFile));
                }
                string strNewsPath = logFile;
                string smb = content;
                StreamWriter sw = new StreamWriter(strNewsPath, true);
                sw.WriteLine(smb);
                sw.Close();

            }
            else
            {
                string strNewsPath = logFile;
                string smb = content;
                StreamWriter sw = new StreamWriter(strNewsPath, true);
                sw.WriteLine(smb);
                sw.Close();

            }



        }

        /// <summary>
        /// 将datagridView表格里的数据转换成DataTable格式
        /// </summary>
        /// <param name="dg"></param>
        /// <returns></returns>
        private static DataTable dgViewToDt(DataGridView dg)
        {
            DataTable DT = new DataTable("SaveResult");
            List<string> lst1 = new List<string>();
            for (int k = 0; k < dg.ColumnCount; k++)
            {

                DT.Columns.Add(dg.Columns[k].HeaderText);

            }
            foreach (DataGridViewRow dgRow in dg.Rows)
            {
                if (dgRow.IsNewRow)
                {
                    continue;
                }
                DataRow dtrow = DT.NewRow();
                for (int i = 0; i < dg.Columns.Count; i++)
                {
                    dtrow[i] = (dgRow.Cells[i].Value == null) ? "None" : dgRow.Cells[i].Value;
                }
                DT.Rows.Add(dtrow);

            }

            return DT;

        }
        public static void saveToExcel(DataGridView dg)
        {
            DataTable dt1 = dgViewToDt(dg);
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Execl files (*.xls)|*.xls|(*.xlsx)|*.xlsx";
            dlg.FilterIndex = 0;
            dlg.RestoreDirectory = true;
            dlg.CreatePrompt = true;

            dlg.Title = "Saved as Excel";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string fileName = Path.GetFileName(dlg.FileName);

                Write2Excel(dt1, Path.GetDirectoryName(dlg.FileName) + @"\" + fileName);
            }

        }
        /// <summary>
        /// 一阶相互作用系数单位转换,m to w
        /// </summary>
        /// <param name="sji">摩尔分数表示的相互作用系数,j on i</param>
        /// <param name="Ej">溶质j</param>
        /// <param name="matrix">基体</param>
        /// <returns></returns>
        public static double first_order_mTow(double sji, Element Ej, Element matrix)
        {
            double w;

            w = (sji - 1 + Ej.M / matrix.M) * matrix.M / (230 * Ej.M);

            return w;
        }
        /// <summary>
        /// 一阶相互作用系数转换，w to M
        /// </summary>
        /// <param name="eji">质量分数表示的相互作用系数，j on i</param>
        /// <param name="Ej">溶质j</param>
        /// <param name="matrix">基体</param>
        /// <returns></returns>
        public static double first_order_w2m(double eji, Element Ej, Element matrix)
        {
            double sij = 0.0;
            sij = 230 * eji * Ej.M / matrix.M + (1 - Ej.M / matrix.M);

            return Math.Round(sij, 2);

        }

        /// <summary>
        /// 二阶相互作用系数转换，返回以摩尔分数表示的二阶相互作用系数ρji/εjj_i
        /// </summary>
        /// <param name="solv">溶剂/基体</param>
        /// <param name="soluj">溶质j</param>
        /// <param name="rij">以质量分数表示的二阶相互作用系数，j on i</param>
        /// <param name="eij">以质量分数表示的一阶相互作用系数，j on i</param>
        /// <returns>以摩尔分数表示的二阶相互作用系数，j on i</returns>
        public static double second_order_w2m(Element solv, Element soluj, double rij, double eij)
        {

            double pij;
            pij = 230 / pow(solv.M, 2.0) * (100 * pow(soluj.M, 2.0) * rij + soluj.M * (solv.M - soluj.M) * eij) + 1.0 / 2
                * pow((solv.M - soluj.M) / solv.M, 2.0);
            return pij;
        }
        /// <summary>
        /// 相互作用系数交互项的单位转换，w2m
        /// </summary>
        /// <param name="solv">溶剂/基体</param>
        /// <param name="soluj">溶质j</param>
        /// <param name="soluk">溶质k</param>
        /// <param name="rjki">质量分数表示的交互作用参数，j ，k on i</param>
        /// <param name="eji">质量分数表示的一阶相互作用参数，j on i</param>
        /// <param name="eki">质量分数表示的一阶相互作用参数，k on i</param>
        /// <returns>摩尔分数表示的交互作用参数，j，k on i</returns>
        public static double cross_term_w2m(Element solv, Element soluj, Element soluk, double rjki, double eji, double eki)
        {
            double pjki;
            pjki = 230 / pow(solv.M, 2.0) * (100 * soluj.M * soluk.M * rjki + soluj.M * (solv.M - soluk.M) * eji + soluk.M * (solv.M - soluj.M) * eki)
                + (solv.M - soluk.M) * (solv.M - soluj.M) / pow(solv.M, 2.0);
            return pjki;
        }

        public static class deferientialate
        {

            /// <summary>
            /// 数值求导
            /// </summary>
            /// <param name="func"></param>
            /// <param name="x"></param>
            /// <returns></returns>
            public static double dydx(fx func, double x)
            {
                double h = Math.Pow(10, -8);
                double olddiff, diff;
                olddiff = (func(x + 2 * h) - func(x - 2 * h)) / (4.0 * h);


                diff = olddiff;
                return diff;


            }


            public static double ddx(fx func, double x)
            {
                double h = Math.Pow(10, -5);
                double dxx1 = (func(x - h) - 2.0 * func(x) + func(x + h)) / (h * h);

                return dxx1;

            }
        }












    }
}
