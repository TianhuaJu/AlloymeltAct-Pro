using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// Exciting DFT 输出文件解析器
    /// 解析 Exciting 的 INFO.OUT 文件，提取能量、体积、结构等信息
    /// 能量单位为 Hartree，需要转换为 eV
    /// 长度单位为 bohr，需要转换为 Angstrom
    /// </summary>
    public class ExcitingParser : IDftParser
    {
        public string SoftwareName => "Exciting";
        public string[] FilePatterns => new[] { "INFO.OUT", "info.out", "*.OUT" };
        public string[] SignatureStrings => new[] { "EXCITING", "exciting", "All-electron" };

        private const double BOHR_TO_ANG = 0.529177;
        private const double BOHR3_TO_ANG3 = 0.14818471;

        public bool CanParse(string filePath)
        {
            // 文件名检查
            var fileName = Path.GetFileName(filePath);
            if (fileName.Equals("INFO.OUT", StringComparison.OrdinalIgnoreCase))
            {
                var header = DftParserRegistry.ReadHeader(filePath, 50);
                if (header.Contains("EXCITING") || header.Contains("exciting") || header.Contains("All-electron"))
                    return true;
            }

            // 内容签名检查
            var headerContent = DftParserRegistry.ReadHeader(filePath, 50);
            if (headerContent.Contains("EXCITING") || headerContent.Contains("exciting"))
                return true;
            // "All-electron" 需要搭配其他特征
            if (headerContent.Contains("All-electron") && headerContent.Contains("full-potential"))
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

                // 总能量: "Total energy                :     -123.456789012" (Hartree)
                try
                {
                    if (line.Contains("Total energy") && !line.Contains("kinetic") &&
                        !line.Contains("change") && !line.Contains("previous"))
                    {
                        var m = Regex.Match(line, @"Total energy\s*:\s*([-\d.Ee+]+)");
                        if (m.Success)
                            lastEnergy_Ha = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 费米能: "Fermi energy                :     0.12345678" (Hartree)
                try
                {
                    if (line.Contains("Fermi energy") || line.Contains("Fermi Energy"))
                    {
                        var m = Regex.Match(line, @"Fermi [Ee]nergy\s*:\s*([-\d.Ee+]+)");
                        if (m.Success)
                        {
                            double fermi_Ha = double.Parse(m.Groups[1].Value, inv);
                            result.FermiEnergy_eV = fermi_Ha * DftResult.HARTREE_TO_EV;
                        }
                    }
                }
                catch { }

                // 晶格向量: "Lattice vectors :" 后跟3行向量（bohr）
                try
                {
                    if (line.Contains("Lattice vectors") && (line.Contains(":") || line.Contains("(")) )
                    {
                        latticeVectors.Clear();
                        for (int j = i + 1; j < Math.Min(i + 4, lines.Length); j++)
                        {
                            var vecLine = lines[j].Trim();
                            if (string.IsNullOrWhiteSpace(vecLine))
                                break;
                            var parts = vecLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3)
                            {
                                double vx = double.Parse(parts[0], inv) * BOHR_TO_ANG;
                                double vy = double.Parse(parts[1], inv) * BOHR_TO_ANG;
                                double vz = double.Parse(parts[2], inv) * BOHR_TO_ANG;
                                latticeVectors.Add(new[] { vx, vy, vz });
                            }
                        }
                    }
                }
                catch { }

                // 体积: "Unit cell volume           :    1234.5678" (bohr^3)
                try
                {
                    if (line.Contains("Unit cell volume"))
                    {
                        var m = Regex.Match(line, @"Unit cell volume\s*:\s*([\d.Ee+]+)");
                        if (m.Success)
                        {
                            double vol_bohr3 = double.Parse(m.Groups[1].Value, inv);
                            result.Volume = vol_bohr3 * BOHR3_TO_ANG3;
                        }
                    }
                }
                catch { }

                // 原子种类: "Species :    1 (Fe)" 后跟 "atoms in this species  :     2"
                try
                {
                    if (line.Contains("Species :") || line.Contains("Species:"))
                    {
                        var m = Regex.Match(line, @"Species\s*:\s*\d+\s*\((\w+)\)");
                        if (m.Success)
                        {
                            var rawName = m.Groups[1].Value;
                            var elem = Regex.Match(rawName, @"^[A-Z][a-z]?").Value;
                            if (!string.IsNullOrEmpty(elem) && !elementCounts.ContainsKey(elem))
                                elementCounts[elem] = 0;

                            // 查找后续的原子数
                            for (int j = i + 1; j < Math.Min(i + 10, lines.Length); j++)
                            {
                                if (lines[j].Contains("atoms") && lines[j].Contains("species"))
                                {
                                    var mCount = Regex.Match(lines[j], @":\s*(\d+)");
                                    if (mCount.Success)
                                        elementCounts[elem] = int.Parse(mCount.Groups[1].Value, inv);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }

                // 原子总数: "Total number of atoms per unit cell :    4"
                try
                {
                    if (line.Contains("Total number of atoms"))
                    {
                        var m = Regex.Match(line, @"Total number of atoms\s*(?:per unit cell)?\s*:\s*(\d+)");
                        if (m.Success)
                            result.AtomCount = int.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 力: "Total atomic forces including IBS (Hartree/Bohr):" 后跟原子力
                // 或 "atom     1   Fe :    0.001234    -0.002345    0.003456"
                try
                {
                    if (line.Contains("Total atomic forces") || line.Contains("atomic forces (cartesian)"))
                    {
                        foundForce = true;
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var forceLine = lines[j].Trim();
                            if (string.IsNullOrWhiteSpace(forceLine) || forceLine.StartsWith("---") ||
                                forceLine.StartsWith("Total") || forceLine.StartsWith("="))
                                break;

                            // "atom     1   Fe :    0.001234    -0.002345    0.003456"
                            var m = Regex.Match(forceLine, @"atom\s+\d+.*?:\s*([-\d.Ee+]+)\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)");
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
                            else
                            {
                                // 纯数字格式
                                var parts = forceLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 3)
                                {
                                    try
                                    {
                                        // 跳过前面的索引
                                        int startIdx = 0;
                                        while (startIdx < parts.Length && !parts[startIdx].Contains('.'))
                                            startIdx++;

                                        if (startIdx + 2 < parts.Length)
                                        {
                                            double fx = double.Parse(parts[startIdx], inv);
                                            double fy = double.Parse(parts[startIdx + 1], inv);
                                            double fz = double.Parse(parts[startIdx + 2], inv);
                                            double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz) *
                                                          DftResult.HARTREE_TO_EV / BOHR_TO_ANG;
                                            if (fmag > maxForce) maxForce = fmag;
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
                catch { }

                // 最大力: "Maximum force magnitude  :   0.001234"
                try
                {
                    if (line.Contains("Maximum force magnitude"))
                    {
                        var m = Regex.Match(line, @"Maximum force magnitude\s*(?:\(target\))?\s*:\s*([\d.Ee+-]+)");
                        if (m.Success)
                        {
                            double f = double.Parse(m.Groups[1].Value, inv);
                            // Hartree/bohr -> eV/Ang
                            double fConverted = f * DftResult.HARTREE_TO_EV / BOHR_TO_ANG;
                            if (fConverted > maxForce)
                            {
                                maxForce = fConverted;
                                foundForce = true;
                            }
                        }
                    }
                }
                catch { }

                // 泛函: "exchange-correlation type :   20"
                try
                {
                    if (line.Contains("exchange-correlation type") || line.Contains("xctype"))
                    {
                        var m = Regex.Match(line, @"(?:exchange-correlation type|xctype)\s*:\s*(\d+)");
                        if (m.Success)
                        {
                            int xcType = int.Parse(m.Groups[1].Value, inv);
                            result.Method = xcType switch
                            {
                                2 => "LDA-PZ",
                                3 => "LDA-PW",
                                20 => "PBE",
                                21 => "revPBE",
                                22 => "PBEsol",
                                26 => "WC",
                                _ => $"XC-{xcType}"
                            };
                        }
                    }
                }
                catch { }

                // K 点: "k-point grid :      4     4     4"
                try
                {
                    if (line.Contains("k-point grid"))
                    {
                        var m = Regex.Match(line, @"k-point grid\s*:\s*(\d+)\s+(\d+)\s+(\d+)");
                        if (m.Success)
                            result.KPoints = $"{m.Groups[1].Value}x{m.Groups[2].Value}x{m.Groups[3].Value}";
                    }
                    else if (line.Contains("Total number of k-points") && string.IsNullOrEmpty(result.KPoints))
                    {
                        var m = Regex.Match(line, @"Total number of k-points\s*:\s*(\d+)");
                        if (m.Success)
                            result.KPoints = $"{m.Groups[1].Value} k-points";
                    }
                }
                catch { }

                // 截断能: "rgkmax :     7.00" 或 "G-vector cutoff :   12.00"
                // exciting uses rgkmax, not a direct energy cutoff

                // 自旋极化: "spin-polarised  :  true"
                try
                {
                    if ((line.Contains("spin-polarised") || line.Contains("spin-polarized")) &&
                        line.Contains("true"))
                    {
                        result.SpinPolarized = true;
                    }
                }
                catch { }

                // 磁矩: "Total moment           :    1.23456"
                try
                {
                    if (line.Contains("Total moment"))
                    {
                        var m = Regex.Match(line, @"Total moment\s*:\s*([-\d.Ee+]+)");
                        if (m.Success)
                            result.TotalMagnetization = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 收敛: "Convergence targets achieved"
                try
                {
                    if (line.Contains("Convergence targets achieved") ||
                        line.Contains("SCF converged") ||
                        line.Contains("EXCITING stopped"))
                    {
                        result.IsConverged = true;
                    }
                }
                catch { }

                // 带隙: "Estimated fundamental gap :    1.234 Ha" 或 "Band gap  :  0.05432"
                try
                {
                    if (line.Contains("fundamental gap") || line.Contains("Band gap"))
                    {
                        var m = Regex.Match(line, @"(?:fundamental gap|Band gap)\s*:\s*([\d.Ee+]+)");
                        if (m.Success)
                        {
                            double gap = double.Parse(m.Groups[1].Value, inv);
                            // Exciting 通常在 Hartree 中
                            result.BandGap_eV = gap * DftResult.HARTREE_TO_EV;
                        }
                    }
                }
                catch { }

                // 压力: "Pressure  :   0.001234 (Ha/Bohr^3)"
                try
                {
                    if (line.Contains("Pressure"))
                    {
                        var m = Regex.Match(line, @"Pressure\s*:\s*([-\d.Ee+]+)");
                        if (m.Success)
                        {
                            double p_Ha_bohr3 = double.Parse(m.Groups[1].Value, inv);
                            // Ha/bohr^3 -> GPa: 1 Ha/bohr^3 = 29421.02 GPa
                            result.Pressure_GPa = p_Ha_bohr3 * 29421.02;
                        }
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

            // 从晶格向量计算参数和体积
            if (latticeVectors.Count >= 3)
            {
                var vecs = latticeVectors.Skip(latticeVectors.Count - 3).Take(3).ToList();
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
