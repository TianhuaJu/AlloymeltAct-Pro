using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// FHI-aims DFT 输出文件解析器
    /// 解析 FHI-aims 的标准输出文件，提取能量、力、结构等信息
    /// </summary>
    public class FhiAimsParser : IDftParser
    {
        public string SoftwareName => "FHI-aims";
        public string[] FilePatterns => new[] { "*.out", "aims.out", "FHI-aims.out" };
        public string[] SignatureStrings => new[] { "FHI-aims", "Fritz Haber Institute", "Invoking FHI-aims" };

        public bool CanParse(string filePath)
        {
            // 文件名检查
            var fileName = Path.GetFileName(filePath);
            if (fileName.Equals("aims.out", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("FHI-aims"))
                return true;

            // 内容签名检查
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

                // 总能量: "| Total energy corrected        :    -1234.56789012 eV"
                try
                {
                    if (line.Contains("Total energy corrected"))
                    {
                        var m = Regex.Match(line, @"Total energy corrected\s*:\s*([-\d.Ee+]+)\s*eV");
                        if (m.Success)
                            lastEnergy = double.Parse(m.Groups[1].Value, inv);
                    }
                    // 备选: "| Total energy                  :    -1234.56789012 eV"
                    else if (line.Contains("| Total energy") && line.Contains("eV") && double.IsNaN(lastEnergy))
                    {
                        var m = Regex.Match(line, @"Total energy\s*:\s*([-\d.Ee+]+)\s*eV");
                        if (m.Success)
                            lastEnergy = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 费米能: "| Chemical potential (Fermi level):    -3.12345678 eV"
                try
                {
                    if (line.Contains("Chemical potential") || line.Contains("Fermi level"))
                    {
                        var m = Regex.Match(line, @"(?:Chemical potential|Fermi level)\s*[):]?\s*:\s*([-\d.Ee+]+)\s*eV");
                        if (m.Success)
                            result.FermiEnergy_eV = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 原子坐标: "| atom    1:  Fe  ...  x y z"
                // 或 "atom  0.0000  0.0000  0.0000  Fe" (geometry.in format echoed)
                try
                {
                    if (line.TrimStart().StartsWith("atom ") || line.TrimStart().StartsWith("atom\t"))
                    {
                        var m = Regex.Match(line, @"atom\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)\s+([A-Z][a-z]?)");
                        if (m.Success)
                        {
                            var elem = m.Groups[4].Value;
                            if (elementCounts.ContainsKey(elem))
                                elementCounts[elem]++;
                            else
                                elementCounts[elem] = 1;
                        }
                    }
                }
                catch { }

                // 原子数: "| Number of atoms                   :        4"
                try
                {
                    if (line.Contains("Number of atoms"))
                    {
                        var m = Regex.Match(line, @"Number of atoms\s*:\s*(\d+)");
                        if (m.Success)
                            result.AtomCount = int.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 晶格向量: "lattice_vector  5.000  0.000  0.000"
                try
                {
                    if (line.TrimStart().StartsWith("lattice_vector"))
                    {
                        var m = Regex.Match(line, @"lattice_vector\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)\s+([-\d.Ee+]+)");
                        if (m.Success)
                        {
                            double vx = double.Parse(m.Groups[1].Value, inv);
                            double vy = double.Parse(m.Groups[2].Value, inv);
                            double vz = double.Parse(m.Groups[3].Value, inv);
                            latticeVectors.Add(new[] { vx, vy, vz });
                        }
                    }
                }
                catch { }

                // 体积: "| Unit cell volume                 :    123.456789 A^3"
                try
                {
                    if (line.Contains("Unit cell volume"))
                    {
                        var m = Regex.Match(line, @"Unit cell volume\s*:\s*([\d.Ee+]+)");
                        if (m.Success)
                            result.Volume = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 力: "Total atomic forces (unitary forces cleaned) [eV/Ang]:"
                // 后续行: "    1    0.001234   -0.002345    0.003456"
                try
                {
                    if (line.Contains("Total atomic forces") && line.Contains("eV/Ang"))
                    {
                        foundForce = true;
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var forceLine = lines[j].Trim();
                            if (string.IsNullOrWhiteSpace(forceLine) || !char.IsDigit(forceLine[0]))
                                break;
                            var parts = forceLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 4)
                            {
                                double fx = double.Parse(parts[1], inv);
                                double fy = double.Parse(parts[2], inv);
                                double fz = double.Parse(parts[3], inv);
                                double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz);
                                if (fmag > maxForce) maxForce = fmag;
                            }
                        }
                    }
                }
                catch { }

                // 最大力直接: "Maximum force component"
                try
                {
                    if (line.Contains("Maximum force component"))
                    {
                        var m = Regex.Match(line, @"Maximum force component\s*[:=]\s*([\d.Ee+-]+)");
                        if (m.Success)
                        {
                            double f = double.Parse(m.Groups[1].Value, inv);
                            if (f > maxForce) maxForce = f;
                            foundForce = true;
                        }
                    }
                }
                catch { }

                // 压力: "| Analytical pressure      :    1.234 eV/A^3" 或含 GPa
                try
                {
                    if (line.Contains("pressure") || line.Contains("Pressure"))
                    {
                        // 优先查找 GPa 单位
                        var m = Regex.Match(line, @"[Pp]ressure\s*[:=]\s*([-\d.Ee+]+)\s*GPa");
                        if (m.Success)
                        {
                            result.Pressure_GPa = double.Parse(m.Groups[1].Value, inv);
                        }
                        else
                        {
                            // eV/A^3 转换为 GPa
                            m = Regex.Match(line, @"[Pp]ressure\s*[:=]\s*([-\d.Ee+]+)\s*eV/A");
                            if (m.Success)
                            {
                                double p_eV_A3 = double.Parse(m.Groups[1].Value, inv);
                                result.Pressure_GPa = p_eV_A3 * 160.21766; // eV/Ang^3 -> GPa
                            }
                        }
                    }
                }
                catch { }

                // 泛函: "| XC functional  :  PBE" 或 "xc    pw-lda"
                try
                {
                    if (line.Contains("XC functional") || line.Contains("xc "))
                    {
                        var m = Regex.Match(line, @"(?:XC functional|xc)\s*[:=]?\s*(\S+)");
                        if (m.Success)
                            result.Method = m.Groups[1].Value;
                    }
                }
                catch { }

                // K 点: "| k-grid:  4  4  4" 或 "| Number of k-points  :  64"
                try
                {
                    if (line.Contains("k-grid") || line.Contains("k_grid"))
                    {
                        var m = Regex.Match(line, @"k[-_]grid\s*[:=]?\s*(\d+)\s+(\d+)\s+(\d+)");
                        if (m.Success)
                            result.KPoints = $"{m.Groups[1].Value}x{m.Groups[2].Value}x{m.Groups[3].Value}";
                    }
                    else if (line.Contains("Number of k-points"))
                    {
                        var m = Regex.Match(line, @"Number of k-points\s*:\s*(\d+)");
                        if (m.Success && string.IsNullOrEmpty(result.KPoints))
                            result.KPoints = $"{m.Groups[1].Value} k-points";
                    }
                }
                catch { }

                // 自旋极化: "| spin collinear" 或 "spin   collinear"
                try
                {
                    if (line.Contains("spin") && (line.Contains("collinear") || line.Contains("non_collinear")))
                    {
                        result.SpinPolarized = true;
                    }
                }
                catch { }

                // 收敛: "| Self-consistency cycle converged."
                try
                {
                    if (line.Contains("Self-consistency cycle converged") ||
                        line.Contains("Geometry optimization converged") ||
                        line.Contains("Have a nice day"))
                    {
                        result.IsConverged = true;
                    }
                }
                catch { }

                // 带隙: "| Overall HOMO-LUMO gap:     1.234 eV"
                try
                {
                    if (line.Contains("HOMO-LUMO gap"))
                    {
                        var m = Regex.Match(line, @"HOMO-LUMO gap\s*:\s*([\d.Ee+]+)\s*eV");
                        if (m.Success)
                            result.BandGap_eV = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }
            }

            // 设置总能量
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
                result.Formula = formula;
                if (result.AtomCount == 0 && total > 0)
                    result.AtomCount = total;
            }

            // 从晶格向量计算参数
            if (latticeVectors.Count >= 3)
            {
                // 取最后3个（如果有多组的话，取最后的结构）
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
