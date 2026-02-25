using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// Wien2k (case.scf) 文件解析器
    /// 解析 Wien2k 的 SCF 输出文件，提取能量、体积、力等信息
    /// 能量单位为 Rydberg，需要转换为 eV
    /// </summary>
    public class Wien2kParser : IDftParser
    {
        public string SoftwareName => "Wien2k";
        public string[] FilePatterns => new[] { "*.scf", "*.scf2" };
        public string[] SignatureStrings => new[] { ":ENE", ":VOL", "WIEN2k" };

        public bool CanParse(string filePath)
        {
            // 文件名检查
            var fileName = Path.GetFileName(filePath);
            if (fileName.EndsWith(".scf", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".scf2", StringComparison.OrdinalIgnoreCase))
            {
                // 还需要验证内容
                var header = DftParserRegistry.ReadHeader(filePath, 50);
                foreach (var sig in SignatureStrings)
                {
                    if (header.Contains(sig))
                        return true;
                }
            }

            // 内容签名检查（对于任意文件名）
            var headerContent = DftParserRegistry.ReadHeader(filePath, 50);
            if (headerContent.Contains("WIEN2k"))
                return true;

            // 检查是否含有 Wien2k 特征标签组合
            if (headerContent.Contains(":ENE") && headerContent.Contains(":VOL"))
                return true;

            return false;
        }

        public DftResult Parse(string filePath)
        {
            var result = new DftResult();
            var lines = File.ReadAllLines(filePath);
            var inv = CultureInfo.InvariantCulture;

            double lastEnergy_Ry = double.NaN;
            double maxForce = 0;
            bool foundForce = false;
            var elementCounts = new Dictionary<string, int>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 总能量: ":ENE  : ********** TOTAL ENERGY IN Ry =     -1234.56789012"
                try
                {
                    if (line.StartsWith(":ENE"))
                    {
                        var m = Regex.Match(line, @"TOTAL ENERGY IN Ry\s*=\s*([-\d.Ee+]+)");
                        if (m.Success)
                            lastEnergy_Ry = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 体积: ":VOL  : UNIT CELL VOLUME =     123.456789"
                try
                {
                    if (line.StartsWith(":VOL"))
                    {
                        var m = Regex.Match(line, @"UNIT CELL VOLUME\s*=\s*([\d.Ee+]+)");
                        if (m.Success)
                        {
                            // Wien2k 体积单位为 bohr^3，需转换为 Ang^3
                            double vol_bohr3 = double.Parse(m.Groups[1].Value, inv);
                            result.Volume = vol_bohr3 * 0.14818471; // 1 bohr^3 = 0.14818471 Ang^3
                        }
                    }
                }
                catch { }

                // 费米能: ":FER  : F E R M I - Loss E N E R G Y(Ry) =  0.12345"
                try
                {
                    if (line.StartsWith(":FER"))
                    {
                        var m = Regex.Match(line, @"=\s*([-\d.Ee+]+)");
                        if (m.Success)
                        {
                            double fermi_Ry = double.Parse(m.Groups[1].Value, inv);
                            result.FermiEnergy_eV = fermi_Ry * DftResult.RY_TO_EV;
                        }
                    }
                }
                catch { }

                // 力: ":FGL001: 1.ATOM                              0.00123   0.00456   0.00789    PARTIAL FORCES"
                try
                {
                    if (line.StartsWith(":FGL"))
                    {
                        foundForce = true;
                        // 格式: ":FGLnnn: n.ATOM  fx  fy  fz"
                        var m = Regex.Match(line, @":FGL\d+:\s*\d+\.ATOM\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)");
                        if (m.Success)
                        {
                            double fx = double.Parse(m.Groups[1].Value, inv);
                            double fy = double.Parse(m.Groups[2].Value, inv);
                            double fz = double.Parse(m.Groups[3].Value, inv);
                            // Wien2k 力单位为 mRy/bohr，转换为 eV/Ang
                            // 1 mRy/bohr = 0.013606 * 1.8897 eV/Ang ≈ 0.025711 eV/Ang
                            double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz) * 0.025711;
                            if (fmag > maxForce) maxForce = fmag;
                        }
                    }
                }
                catch { }

                // 晶格参数: ":LAT  : LATTICE PARAMETERS=   5.123   5.123   5.123  90.00  90.00  90.00"
                try
                {
                    if (line.StartsWith(":LAT"))
                    {
                        var m = Regex.Match(line, @"LATTICE PARAMETERS\s*=\s*([\d.]+)\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)");
                        if (m.Success)
                        {
                            // Wien2k 晶格参数单位为 bohr，转换为 Ang
                            double a = double.Parse(m.Groups[1].Value, inv) * 0.529177; // bohr -> Ang
                            double b = double.Parse(m.Groups[2].Value, inv) * 0.529177;
                            double c = double.Parse(m.Groups[3].Value, inv) * 0.529177;
                            result.LatticeParameters = new[] { a, b, c };

                            double alpha = double.Parse(m.Groups[4].Value, inv);
                            double beta = double.Parse(m.Groups[5].Value, inv);
                            double gamma = double.Parse(m.Groups[6].Value, inv);
                            result.LatticeAngles = new[] { alpha, beta, gamma };
                        }
                    }
                }
                catch { }

                // 原子信息: "ATOM   1: X=0.0000 Y=0.0000 Z=0.0000" 或 ":POS001: ATOM   1  Fe"
                try
                {
                    if (line.StartsWith(":POS"))
                    {
                        var m = Regex.Match(line, @":POS\d+:\s*ATOM\s*[-\d]+\s+([A-Z][a-z]?)");
                        if (m.Success)
                        {
                            var elem = m.Groups[1].Value;
                            if (elementCounts.ContainsKey(elem))
                                elementCounts[elem]++;
                            else
                                elementCounts[elem] = 1;
                        }
                    }
                }
                catch { }

                // 原子数从NOE标签: ":NOE  : NUMBER OF ELECTRONS =  26.000"
                // 不直接给原子数，但可参考

                // 交换关联泛函: ":POT  : POTENTIAL OPTION   5 ... PBE"
                try
                {
                    if (line.StartsWith(":POT"))
                    {
                        if (line.Contains("PBE"))
                            result.Method = "PBE";
                        else if (line.Contains("LDA") || line.Contains("LSDA"))
                            result.Method = "LDA";
                        else if (line.Contains("WC"))
                            result.Method = "WC-GGA";
                        else if (line.Contains("PBEsol"))
                            result.Method = "PBEsol";
                    }
                }
                catch { }

                // K 点: ":KPT  : NUMBER OF K-POINTS:  1000"
                try
                {
                    if (line.StartsWith(":KPT") || line.Contains("NUMBER OF K-POINTS"))
                    {
                        var m = Regex.Match(line, @"NUMBER OF K-POINTS\s*[:=]\s*(\d+)");
                        if (m.Success)
                            result.KPoints = $"{m.Groups[1].Value} k-points";
                    }
                }
                catch { }

                // 截断能: "RKMAX =   7.00" (不是直接截断能，但是Wien2k的重要参数)
                try
                {
                    if (line.Contains("RKMAX"))
                    {
                        var m = Regex.Match(line, @"RKMAX\s*=\s*([\d.]+)");
                        if (m.Success)
                        {
                            // RKmax 不是截断能，但可记录
                            // Wien2k 使用 APW+lo 方法，不直接指定截断能
                        }
                    }
                }
                catch { }

                // 自旋极化: 如果文件包含 "SPIN-POLARIZED" 或 ":SP "
                try
                {
                    if (line.Contains("SPIN-POLARIZED") || line.Contains("spin-polarized"))
                    {
                        result.SpinPolarized = true;
                    }
                }
                catch { }

                // 磁矩: ":MMT001: SPIN MOMENT =  1.2345"
                try
                {
                    if (line.StartsWith(":MMI") && line.Contains("MAGNETIC MOMENT"))
                    {
                        var m = Regex.Match(line, @"MAGNETIC MOMENT\s*=\s*([-\d.Ee+]+)");
                        if (m.Success)
                            result.TotalMagnetization = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 收敛判定: ":DIS  :  CHARGE DISTANCE       0.0000012"
                try
                {
                    if (line.StartsWith(":DIS"))
                    {
                        var m = Regex.Match(line, @"CHARGE DISTANCE\s+([\d.Ee+-]+)");
                        if (m.Success)
                        {
                            double dist = double.Parse(m.Groups[1].Value, inv);
                            if (dist < 0.0001)
                                result.IsConverged = true;
                        }
                    }
                }
                catch { }
            }

            // 设置总能量 (Ry -> eV)
            if (!double.IsNaN(lastEnergy_Ry))
                result.TotalEnergy_eV = lastEnergy_Ry * DftResult.RY_TO_EV;

            // 设置力
            if (foundForce)
                result.MaxForce_eV_A = maxForce;

            // 构建元素计数和化学式
            if (elementCounts.Count > 0)
            {
                int total = 0;
                var formula = "";
                foreach (var kvp in elementCounts)
                {
                    result.ElementCounts[kvp.Key] = kvp.Value;
                    formula += kvp.Key + (kvp.Value > 1 ? kvp.Value.ToString() : "");
                    total += kvp.Value;
                }
                result.Formula = formula;
                if (result.AtomCount == 0)
                    result.AtomCount = total;
            }

            return result;
        }
    }
}
