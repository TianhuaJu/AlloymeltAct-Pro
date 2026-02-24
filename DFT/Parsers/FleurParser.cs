using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// FLEUR (FLAPW) DFT 输出文件解析器
    /// 解析 FLEUR 的输出文件，提取能量、结构等信息
    /// 能量单位为 Hartree，需要转换为 eV
    /// </summary>
    public class FleurParser : IDftParser
    {
        public string SoftwareName => "FLEUR";
        public string[] FilePatterns => new[] { "out", "out.*", "*.out", "fleur.out" };
        public string[] SignatureStrings => new[] { "FLEUR", "fleur", "FLAPW" };

        private const double BOHR_TO_ANG = 0.529177;
        private const double BOHR3_TO_ANG3 = 0.14818471;

        public bool CanParse(string filePath)
        {
            // 文件名检查
            var fileName = Path.GetFileName(filePath);
            if (fileName.Equals("out", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("out.", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("fleur.out", StringComparison.OrdinalIgnoreCase))
            {
                var header = DftParserRegistry.ReadHeader(filePath, 50);
                foreach (var sig in SignatureStrings)
                {
                    if (header.Contains(sig))
                        return true;
                }
            }

            // 内容签名检查
            var headerContent = DftParserRegistry.ReadHeader(filePath, 50);
            if (headerContent.Contains("FLEUR") || headerContent.Contains("FLAPW"))
                return true;

            return false;
        }

        public DftResult Parse(string filePath)
        {
            var result = new DftResult();
            var lines = File.ReadAllLines(filePath);
            var inv = CultureInfo.InvariantCulture;

            double lastEnergy_Ha = double.NaN;
            double maxForce = 0;
            bool foundForce = false;
            var elementCounts = new Dictionary<string, int>();
            var latticeVectors = new List<double[]>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 总能量: "total energy=     -123.456789012 htr" 或 "total energy=   -123.456"
                try
                {
                    if (line.Contains("total energy=") || line.Contains("total energy ="))
                    {
                        var m = Regex.Match(line, @"total energy\s*=\s*([-\d.Ee+]+)");
                        if (m.Success)
                            lastEnergy_Ha = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 费米能: "new fermi energy  :    0.12345 htr"
                try
                {
                    if (line.Contains("fermi energy") || line.Contains("Fermi energy"))
                    {
                        var m = Regex.Match(line, @"[Ff]ermi energy\s*[:=]\s*([-\d.Ee+]+)");
                        if (m.Success)
                        {
                            double fermi_Ha = double.Parse(m.Groups[1].Value, inv);
                            result.FermiEnergy_eV = fermi_Ha * DftResult.HARTREE_TO_EV;
                        }
                    }
                }
                catch { }

                // 原子类型块: "atom type  1: Fe  Z=26  ..." 或类似格式
                try
                {
                    if (line.Contains("atom type") || line.Contains("atom-type"))
                    {
                        var m = Regex.Match(line, @"atom[\s-]type\s+\d+\s*[:=]?\s*([A-Z][a-z]?)");
                        if (m.Success)
                        {
                            var elem = m.Groups[1].Value;
                            if (!elementCounts.ContainsKey(elem))
                                elementCounts[elem] = 0;

                            // 查找原子数
                            // "number of atoms  =   2" 或在同一行 "Fe  nAt= 2"
                            var mCount = Regex.Match(line, @"(?:nAt|atoms?)\s*=\s*(\d+)");
                            if (mCount.Success)
                            {
                                elementCounts[elem] = int.Parse(mCount.Groups[1].Value, inv);
                            }
                            else
                            {
                                // 搜索后续行
                                for (int j = i + 1; j < Math.Min(i + 5, lines.Length); j++)
                                {
                                    if (lines[j].Contains("atoms") || lines[j].Contains("number"))
                                    {
                                        mCount = Regex.Match(lines[j], @"(\d+)\s*atoms?|atoms?\s*[:=]\s*(\d+)");
                                        if (mCount.Success)
                                        {
                                            var val = mCount.Groups[1].Value;
                                            if (string.IsNullOrEmpty(val)) val = mCount.Groups[2].Value;
                                            elementCounts[elem] = int.Parse(val, inv);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                // 另一种原子识别: "ntype= 2" 后跟 "Fe  2" "Al  4"
                try
                {
                    if (Regex.IsMatch(line.Trim(), @"^([A-Z][a-z]?)\s+(\d+)\s*$") && elementCounts.Count == 0)
                    {
                        var m = Regex.Match(line.Trim(), @"^([A-Z][a-z]?)\s+(\d+)$");
                        if (m.Success)
                        {
                            elementCounts[m.Groups[1].Value] = int.Parse(m.Groups[2].Value, inv);
                        }
                    }
                }
                catch { }

                // 原子总数: "total number of atoms  =    4"
                try
                {
                    if (line.Contains("total number of atoms") || line.Contains("number of atoms"))
                    {
                        var m = Regex.Match(line, @"number of atoms\s*[:=]\s*(\d+)");
                        if (m.Success)
                            result.AtomCount = int.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 晶格向量: "a1 =   5.000  0.000  0.000" (bohr)
                try
                {
                    if (Regex.IsMatch(line, @"a[123]\s*="))
                    {
                        var m = Regex.Match(line, @"a[123]\s*=\s*([-\d.Ee+]+)\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)");
                        if (m.Success)
                        {
                            double vx = double.Parse(m.Groups[1].Value, inv) * BOHR_TO_ANG;
                            double vy = double.Parse(m.Groups[2].Value, inv) * BOHR_TO_ANG;
                            double vz = double.Parse(m.Groups[3].Value, inv) * BOHR_TO_ANG;
                            latticeVectors.Add(new[] { vx, vy, vz });
                        }
                    }
                }
                catch { }

                // 体积: "unit cell volume =   123.456" (bohr^3)
                try
                {
                    if (line.Contains("cell volume") || line.Contains("unit-cell volume"))
                    {
                        var m = Regex.Match(line, @"volume\s*=\s*([\d.Ee+]+)");
                        if (m.Success)
                        {
                            double vol_bohr3 = double.Parse(m.Groups[1].Value, inv);
                            result.Volume = vol_bohr3 * BOHR3_TO_ANG3;
                        }
                    }
                }
                catch { }

                // 力: "FX_TOT=  0.001234  FY_TOT= -0.002345  FZ_TOT=  0.003456"
                // 或 "force on atom  1 :  0.001234  -0.002345  0.003456  htr/bohr"
                try
                {
                    if (line.Contains("FX_TOT") || line.Contains("FY_TOT"))
                    {
                        foundForce = true;
                        var mx = Regex.Match(line, @"FX_TOT\s*=\s*([-\d.Ee+]+)");
                        var my = Regex.Match(line, @"FY_TOT\s*=\s*([-\d.Ee+]+)");
                        var mz = Regex.Match(line, @"FZ_TOT\s*=\s*([-\d.Ee+]+)");
                        if (mx.Success && my.Success && mz.Success)
                        {
                            double fx = double.Parse(mx.Groups[1].Value, inv);
                            double fy = double.Parse(my.Groups[1].Value, inv);
                            double fz = double.Parse(mz.Groups[1].Value, inv);
                            // Hartree/bohr -> eV/Ang
                            double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz) *
                                          DftResult.HARTREE_TO_EV / BOHR_TO_ANG;
                            if (fmag > maxForce) maxForce = fmag;
                        }
                    }
                    else if (line.Contains("force on atom") || line.Contains("force_on_atom"))
                    {
                        foundForce = true;
                        var m = Regex.Match(line, @":\s*([-\d.Ee+]+)\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)");
                        if (m.Success)
                        {
                            double fx = double.Parse(m.Groups[1].Value, inv);
                            double fy = double.Parse(m.Groups[2].Value, inv);
                            double fz = double.Parse(m.Groups[3].Value, inv);
                            // Hartree/bohr -> eV/Ang
                            double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz) *
                                          DftResult.HARTREE_TO_EV / BOHR_TO_ANG;
                            if (fmag > maxForce) maxForce = fmag;
                        }
                    }
                }
                catch { }

                // 泛函: "exchange-correlation: PBE" 或 "xc-potential: pbe"
                try
                {
                    if (line.Contains("exchange-correlation") || line.Contains("xc-potential") ||
                        line.Contains("xctyp"))
                    {
                        var m = Regex.Match(line, @"(?:exchange-correlation|xc-potential|xctyp)\s*[:=]\s*(\S+)",
                            RegexOptions.IgnoreCase);
                        if (m.Success)
                            result.Method = m.Groups[1].Value.ToUpper();
                    }
                }
                catch { }

                // K 点: "k-point mesh  4  4  4" 或 "number of k-points: 64"
                try
                {
                    if (line.Contains("k-point") && line.Contains("mesh"))
                    {
                        var m = Regex.Match(line, @"mesh\s+(\d+)\s+(\d+)\s+(\d+)");
                        if (m.Success)
                            result.KPoints = $"{m.Groups[1].Value}x{m.Groups[2].Value}x{m.Groups[3].Value}";
                    }
                    else if (line.Contains("number of k-points") && string.IsNullOrEmpty(result.KPoints))
                    {
                        var m = Regex.Match(line, @"number of k-points\s*[:=]\s*(\d+)");
                        if (m.Success)
                            result.KPoints = $"{m.Groups[1].Value} k-points";
                    }
                }
                catch { }

                // 截断能: "Kmax =   3.50" (1/bohr) - FLEUR 使用 Kmax 而非直接截断能
                // 或 "Gmax = 12.00"
                try
                {
                    if (line.Contains("Kmax") || line.Contains("kmax"))
                    {
                        // Kmax is not a direct energy cutoff in FLEUR
                    }
                }
                catch { }

                // 自旋极化: "jspins= 2" 或 "spin-polarized"
                try
                {
                    if (line.Contains("jspins") && line.Contains("2"))
                    {
                        result.SpinPolarized = true;
                    }
                    else if (line.Contains("spin-polarized") || line.Contains("spin polarized"))
                    {
                        result.SpinPolarized = true;
                    }
                }
                catch { }

                // 磁矩: "total magnetic moment  =  1.234"
                try
                {
                    if (line.Contains("magnetic moment") || line.Contains("mm   "))
                    {
                        var m = Regex.Match(line, @"(?:magnetic moment|mm)\s*[:=]\s*([-\d.Ee+]+)");
                        if (m.Success)
                            result.TotalMagnetization = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 收敛: "it= XX  is_conv=T" 或 "convergence reached"
                try
                {
                    if (line.Contains("is_conv=T") || line.Contains("convergence reached") ||
                        line.Contains("converged"))
                    {
                        result.IsConverged = true;
                    }
                }
                catch { }
            }

            // 设置总能量 (Hartree -> eV)
            if (!double.IsNaN(lastEnergy_Ha))
                result.TotalEnergy_eV = lastEnergy_Ha * DftResult.HARTREE_TO_EV;

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
                    if (kvp.Value > 0)
                    {
                        result.ElementCounts[kvp.Key] = kvp.Value;
                        formula += kvp.Key + (kvp.Value > 1 ? kvp.Value.ToString() : "");
                        total += kvp.Value;
                    }
                }
                result.Formula = formula;
                if (result.AtomCount == 0 && total > 0)
                    result.AtomCount = total;
            }

            // 从晶格向量计算参数
            if (latticeVectors.Count >= 3)
            {
                var vecs = latticeVectors.Take(3).ToList();
                var lp = new double[3];
                for (int k = 0; k < 3; k++)
                {
                    var v = vecs[k];
                    lp[k] = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
                }
                result.LatticeParameters = lp;

                if (double.IsNaN(result.Volume))
                {
                    var a = vecs[0];
                    var b = vecs[1];
                    var c = vecs[2];
                    double vol = Math.Abs(
                        a[0] * (b[1] * c[2] - b[2] * c[1]) -
                        a[1] * (b[0] * c[2] - b[2] * c[0]) +
                        a[2] * (b[0] * c[1] - b[1] * c[0]));
                    result.Volume = vol;
                }
            }

            return result;
        }
    }
}
