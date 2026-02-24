using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// Elk (INFO.OUT) DFT 输出文件解析器
    /// 解析 Elk 的 INFO.OUT 文件，提取能量、体积、结构等信息
    /// 能量单位为 Hartree，需要转换为 eV
    /// 体积单位为 bohr³，需要转换为 Ang³
    /// </summary>
    public class ElkParser : IDftParser
    {
        public string SoftwareName => "Elk";
        public string[] FilePatterns => new[] { "INFO.OUT", "info.out", "*.OUT" };
        public string[] SignatureStrings => new[] { "Elk code", "Elk version", "+---" };

        private const double BOHR_TO_ANG = 0.529177;
        private const double BOHR3_TO_ANG3 = 0.14818471; // BOHR_TO_ANG^3

        public bool CanParse(string filePath)
        {
            // 文件名检查
            var fileName = Path.GetFileName(filePath);
            if (fileName.Equals("INFO.OUT", StringComparison.OrdinalIgnoreCase))
            {
                var header = DftParserRegistry.ReadHeader(filePath, 50);
                if (header.Contains("Elk") || header.Contains("+---"))
                    return true;
            }

            // 内容签名检查
            var headerContent = DftParserRegistry.ReadHeader(filePath, 50);
            if (headerContent.Contains("Elk code") || headerContent.Contains("Elk version"))
                return true;

            return false;
        }

        public DftResult Parse(string filePath)
        {
            var result = new DftResult();
            var lines = File.ReadAllLines(filePath);
            var inv = CultureInfo.InvariantCulture;

            double lastEnergy_Ha = double.NaN;
            var elementCounts = new Dictionary<string, int>();
            var latticeVectors = new List<double[]>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 总能量: "Total energy                :     -123.456789012" (Hartree)
                try
                {
                    if (line.Contains("Total energy") && !line.Contains("kinetic") && !line.Contains("change"))
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
                    if (line.Contains("Fermi energy") || line.Contains("Fermi"))
                    {
                        var m = Regex.Match(line, @"Fermi\s*(?:energy)?\s*:\s*([-\d.Ee+]+)");
                        if (m.Success)
                        {
                            double fermi_Ha = double.Parse(m.Groups[1].Value, inv);
                            result.FermiEnergy_eV = fermi_Ha * DftResult.HARTREE_TO_EV;
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

                // 晶格向量: 以 "Lattice vectors :" 开头的块
                try
                {
                    if (line.Contains("Lattice vectors"))
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

                // 原子种类: "Species :    1 (Fe)" 或 "Species : 1 (Fe)"
                try
                {
                    if (line.Contains("Species :") || line.Contains("Species:"))
                    {
                        var m = Regex.Match(line, @"Species\s*:\s*\d+\s*\((\w+)\)");
                        if (m.Success)
                        {
                            var elem = Regex.Match(m.Groups[1].Value, @"^[A-Z][a-z]?").Value;
                            if (!string.IsNullOrEmpty(elem) && !elementCounts.ContainsKey(elem))
                                elementCounts[elem] = 0;

                            // 查找后续的原子数: "atoms in this species  :     2"
                            for (int j = i + 1; j < Math.Min(i + 10, lines.Length); j++)
                            {
                                if (lines[j].Contains("atoms") && (lines[j].Contains("species") || lines[j].Contains("this")))
                                {
                                    var mCount = Regex.Match(lines[j], @":\s*(\d+)");
                                    if (mCount.Success)
                                    {
                                        elementCounts[elem] = int.Parse(mCount.Groups[1].Value, inv);
                                    }
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

                // K 点: "k-point grid :      4     4     4" 或 "Total number of k-points :   64"
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

                // 泛函: "Type of exchange-correlation functional :    20" (20=PBE, 3=LDA etc.)
                try
                {
                    if (line.Contains("exchange-correlation functional"))
                    {
                        var m = Regex.Match(line, @"exchange-correlation functional\s*:\s*(\d+)");
                        if (m.Success)
                        {
                            int xcType = int.Parse(m.Groups[1].Value, inv);
                            result.Method = xcType switch
                            {
                                2 => "LDA-PZ",
                                3 => "LDA-PW",
                                4 => "LDA-LSDA",
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

                // 截断能: "R_MT * |G+K|_max (rgkmax)  :     7.00000"
                // Elk uses rgkmax, not direct cutoff energy
                try
                {
                    if (line.Contains("rgkmax") || line.Contains("R_MT"))
                    {
                        var m = Regex.Match(line, @":\s*([\d.]+)");
                        if (m.Success)
                        {
                            // Not a direct energy cutoff, but store as reference
                        }
                    }
                }
                catch { }

                // 最大力: "Maximum force magnitude (target)  :   0.001234 (0.000500)"
                try
                {
                    if (line.Contains("Maximum force magnitude"))
                    {
                        var m = Regex.Match(line, @"Maximum force magnitude\s*(?:\(target\))?\s*:\s*([\d.Ee+-]+)");
                        if (m.Success)
                        {
                            // Elk 力单位为 Hartree/bohr，转换为 eV/Ang
                            double f_Ha_bohr = double.Parse(m.Groups[1].Value, inv);
                            result.MaxForce_eV_A = f_Ha_bohr * DftResult.HARTREE_TO_EV / BOHR_TO_ANG;
                        }
                    }
                }
                catch { }

                // 自旋极化: "Spin treatment : spin-polarised" 或 "spinpol"
                try
                {
                    if (line.Contains("spin-polarised") || line.Contains("spin-polarized") ||
                        (line.Contains("spinpol") && line.Contains("true")))
                    {
                        result.SpinPolarized = true;
                    }
                }
                catch { }

                // 磁矩: "Total moment            :    1.23456"
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

                // 带隙: "Estimated fundamental gap :    1.234 eV" 或 in Hartree
                try
                {
                    if (line.Contains("fundamental gap") || line.Contains("band gap"))
                    {
                        var m = Regex.Match(line, @"gap\s*:\s*([\d.Ee+]+)\s*(eV|Ha)?");
                        if (m.Success)
                        {
                            double gap = double.Parse(m.Groups[1].Value, inv);
                            string unit = m.Groups[2].Value;
                            result.BandGap_eV = unit == "Ha" ? gap * DftResult.HARTREE_TO_EV : gap;
                        }
                    }
                }
                catch { }
            }

            // 设置总能量 (Hartree -> eV)
            if (!double.IsNaN(lastEnergy_Ha))
                result.TotalEnergy_eV = lastEnergy_Ha * DftResult.HARTREE_TO_EV;

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
                var vecs = latticeVectors.Skip(latticeVectors.Count - 3).Take(3).ToList();
                var lp = new double[3];
                for (int k = 0; k < 3; k++)
                {
                    var v = vecs[k];
                    lp[k] = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
                }
                result.LatticeParameters = lp;

                // 如果未获取体积，从向量叉积计算
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
