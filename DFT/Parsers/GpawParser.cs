using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// GPAW DFT 输出文件解析器
    /// 解析 GPAW（Grid-based Projector Augmented Wave）的输出文件
    /// 能量单位为 eV
    /// </summary>
    public class GpawParser : IDftParser
    {
        public string SoftwareName => "GPAW";
        public string[] FilePatterns => new[] { "*.txt", "*.gpaw", "*.out", "*.log" };
        public string[] SignatureStrings => new[] { "GPAW", "gpaw", "Grid-based PAW" };

        public bool CanParse(string filePath)
        {
            // 内容签名检查（GPAW 文件名不固定，主要靠内容识别）
            var header = DftParserRegistry.ReadHeader(filePath, 50);
            foreach (var sig in SignatureStrings)
            {
                if (header.Contains(sig))
                    return true;
            }

            return false;
        }

        public DftResult Parse(string filePath)
        {
            var result = new DftResult();
            var lines = File.ReadAllLines(filePath);
            var inv = CultureInfo.InvariantCulture;

            double lastEnergy = double.NaN;
            double maxForce = 0;
            bool foundForce = false;
            var elementCounts = new Dictionary<string, int>();
            var latticeVectors = new List<double[]>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 总能量: "Total:               -12.345678" 或 "Free energy:   -12.345678"
                // 或 "Extrapolated:        -12.345678"
                try
                {
                    if (line.TrimStart().StartsWith("Total:") || line.TrimStart().StartsWith("Free energy:"))
                    {
                        var m = Regex.Match(line, @"(?:Total|Free energy)\s*:\s*([-\d.Ee+]+)");
                        if (m.Success)
                            lastEnergy = double.Parse(m.Groups[1].Value, inv);
                    }
                    else if (line.TrimStart().StartsWith("Extrapolated:"))
                    {
                        var m = Regex.Match(line, @"Extrapolated\s*:\s*([-\d.Ee+]+)");
                        if (m.Success)
                            lastEnergy = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 费米能: "Fermi level:  5.1234" 或 "Fermi Level:  5.1234 eV"
                try
                {
                    if (line.Contains("Fermi") && (line.Contains("level") || line.Contains("Level")))
                    {
                        var m = Regex.Match(line, @"Fermi\s*[Ll]evel\s*:\s*([-\d.Ee+]+)");
                        if (m.Success)
                            result.FermiEnergy_eV = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 原子数和种类: "Number of atoms: 4"
                try
                {
                    if (line.Contains("Number of atoms"))
                    {
                        var m = Regex.Match(line, @"Number of atoms\s*[:=]\s*(\d+)");
                        if (m.Success)
                            result.AtomCount = int.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 元素种类列表:
                // "  Fe:                 2"  (在 "Positions:" 或 "Species:" 块中)
                // 或 "  1 Fe   2" 格式
                try
                {
                    // 模式1: GPAW 文本输出中的种类统计
                    // "Fe:  2" 或 "  Fe   2  ..."
                    if (Regex.IsMatch(line, @"^\s+([A-Z][a-z]?)\s*:\s+(\d+)\s*$"))
                    {
                        var m = Regex.Match(line, @"^\s+([A-Z][a-z]?)\s*:\s+(\d+)");
                        if (m.Success)
                        {
                            var elem = m.Groups[1].Value;
                            int count = int.Parse(m.Groups[2].Value, inv);
                            elementCounts[elem] = count;
                        }
                    }
                }
                catch { }

                // 模式2: 寻找 "Species" 或 "Chemical formula" 块
                try
                {
                    if (line.Contains("Chemical formula"))
                    {
                        // "Chemical formula: Fe2Al4"
                        var m = Regex.Match(line, @"Chemical formula\s*:\s*(.+)");
                        if (m.Success && string.IsNullOrEmpty(result.Formula))
                        {
                            result.Formula = m.Groups[1].Value.Trim();
                            // 解析化学式
                            var matches = Regex.Matches(result.Formula, @"([A-Z][a-z]?)(\d*)");
                            foreach (Match em in matches)
                            {
                                var elem = em.Groups[1].Value;
                                int count = string.IsNullOrEmpty(em.Groups[2].Value)
                                    ? 1
                                    : int.Parse(em.Groups[2].Value, inv);
                                elementCounts[elem] = count;
                            }
                        }
                    }
                }
                catch { }

                // 晶格向量: "Unit Cell:" 块
                try
                {
                    if (line.Contains("Unit Cell") || line.Contains("unit cell"))
                    {
                        latticeVectors.Clear();
                        for (int j = i + 1; j < Math.Min(i + 6, lines.Length); j++)
                        {
                            var vecLine = lines[j].Trim();
                            if (string.IsNullOrWhiteSpace(vecLine) || !vecLine.Contains("Ang"))
                                continue;
                            // 格式: "axis | 5.0000  0.0000  0.0000 | Ang"
                            var m = Regex.Match(vecLine, @"([-\d.Ee+]+)\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)");
                            if (m.Success)
                            {
                                double vx = double.Parse(m.Groups[1].Value, inv);
                                double vy = double.Parse(m.Groups[2].Value, inv);
                                double vz = double.Parse(m.Groups[3].Value, inv);
                                latticeVectors.Add(new[] { vx, vy, vz });
                            }
                            if (latticeVectors.Count >= 3) break;
                        }
                    }
                }
                catch { }

                // 体积: "Volume:  123.456 Ang^3"
                try
                {
                    if (line.Contains("Volume"))
                    {
                        var m = Regex.Match(line, @"Volume\s*[:=]\s*([\d.Ee+]+)");
                        if (m.Success)
                            result.Volume = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 力: GPAW 打印的力块
                // "Forces in eV/Ang:"
                // "  0 Fe    0.001234  -0.002345   0.003456"
                try
                {
                    if (line.Contains("Forces in eV/Ang") || line.Contains("Forces:"))
                    {
                        foundForce = true;
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var forceLine = lines[j].Trim();
                            if (string.IsNullOrWhiteSpace(forceLine))
                                break;
                            // "  0 Fe    0.001234  -0.002345   0.003456"
                            var m = Regex.Match(forceLine, @"\d+\s+[A-Z][a-z]?\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)");
                            if (m.Success)
                            {
                                double fx = double.Parse(m.Groups[1].Value, inv);
                                double fy = double.Parse(m.Groups[2].Value, inv);
                                double fz = double.Parse(m.Groups[3].Value, inv);
                                double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz);
                                if (fmag > maxForce) maxForce = fmag;
                            }
                            else
                            {
                                // 尝试纯数字格式: "0.001234  -0.002345   0.003456"
                                var parts = forceLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 3)
                                {
                                    bool allNumeric = true;
                                    foreach (var p in parts.Take(3))
                                    {
                                        if (!double.TryParse(p, NumberStyles.Float, inv, out _))
                                        { allNumeric = false; break; }
                                    }
                                    if (allNumeric)
                                    {
                                        double fx = double.Parse(parts[0], inv);
                                        double fy = double.Parse(parts[1], inv);
                                        double fz = double.Parse(parts[2], inv);
                                        double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz);
                                        if (fmag > maxForce) maxForce = fmag;
                                    }
                                    else break;
                                }
                                else break;
                            }
                        }
                    }
                }
                catch { }

                // 泛函: "XC Functional:  PBE" 或 "xc: PBE"
                try
                {
                    if (line.Contains("XC") && (line.Contains("Functional") || line.Contains("functional")))
                    {
                        var m = Regex.Match(line, @"(?:XC|xc)\s*[Ff]unctional\s*[:=]\s*(\S+)");
                        if (m.Success)
                            result.Method = m.Groups[1].Value;
                    }
                    else if (Regex.IsMatch(line, @"^\s*xc\s*[:=]", RegexOptions.IgnoreCase))
                    {
                        var m = Regex.Match(line, @"xc\s*[:=]\s*(\S+)", RegexOptions.IgnoreCase);
                        if (m.Success)
                            result.Method = m.Groups[1].Value;
                    }
                }
                catch { }

                // 截断能 (平面波模式): "Cutoff:  500.0 eV" 或 "ecut:  500" 或 "Grid spacing: 0.18 Ang"
                try
                {
                    if (line.Contains("Cutoff") || line.Contains("ecut"))
                    {
                        var m = Regex.Match(line, @"(?:Cutoff|ecut)\s*[:=]\s*([\d.]+)\s*(eV)?");
                        if (m.Success)
                            result.EnergyCutoff_eV = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // K 点: "Monkhorst-Pack:  4 x 4 x 4" 或 "k-points: 4x4x4"
                try
                {
                    if (line.Contains("Monkhorst-Pack") || line.Contains("k-points"))
                    {
                        var m = Regex.Match(line, @"(\d+)\s*[xX]\s*(\d+)\s*[xX]\s*(\d+)");
                        if (m.Success)
                            result.KPoints = $"{m.Groups[1].Value}x{m.Groups[2].Value}x{m.Groups[3].Value}";
                    }
                }
                catch { }

                // 自旋极化: "Spin-polarized calculation" 或 "spinpol: True"
                try
                {
                    if (line.Contains("Spin-polarized") || line.Contains("spin-polarized") ||
                        (line.Contains("spinpol") && line.Contains("True")))
                    {
                        result.SpinPolarized = true;
                    }
                }
                catch { }

                // 磁矩: "Magnetic moment:  1.234"
                try
                {
                    if (line.Contains("Magnetic moment"))
                    {
                        var m = Regex.Match(line, @"Magnetic moment\s*:\s*([-\d.Ee+]+)");
                        if (m.Success)
                            result.TotalMagnetization = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 收敛: "Converged after" 或 "SCF converged"
                try
                {
                    if (line.Contains("Converged after") || line.Contains("converged"))
                    {
                        result.IsConverged = true;
                    }
                }
                catch { }

                // 带隙: "Band gap: 1.234 eV"
                try
                {
                    if (line.Contains("Band gap") || line.Contains("band gap"))
                    {
                        var m = Regex.Match(line, @"[Bb]and gap\s*[:=]\s*([\d.Ee+]+)");
                        if (m.Success)
                            result.BandGap_eV = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 压力: "Pressure: 1.234 GPa"
                try
                {
                    if (line.Contains("Pressure") || line.Contains("pressure"))
                    {
                        var m = Regex.Match(line, @"[Pp]ressure\s*[:=]\s*([-\d.Ee+]+)\s*GPa");
                        if (m.Success)
                            result.Pressure_GPa = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }
            }

            // 设置总能量（GPAW 输出已经是 eV）
            result.TotalEnergy_eV = lastEnergy;

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
                if (string.IsNullOrEmpty(result.Formula))
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
