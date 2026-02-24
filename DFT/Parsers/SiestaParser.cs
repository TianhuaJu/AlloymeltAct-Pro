using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// SIESTA DFT 输出文件解析器
    /// 解析 SIESTA 的标准输出文件，提取能量、力、结构等信息
    /// </summary>
    public class SiestaParser : IDftParser
    {
        public string SoftwareName => "SIESTA";
        public string[] FilePatterns => new[] { "*.out", "*.siesta", "*.SIESTA" };
        public string[] SignatureStrings => new[] { "Siesta", "SIESTA", "siesta" };

        public bool CanParse(string filePath)
        {
            // 文件名检查
            var fileName = Path.GetFileName(filePath);
            foreach (var pattern in FilePatterns)
            {
                var ext = pattern.Replace("*", "");
                if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

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

                // 总能量: "siesta: E_KS(eV) =    -1234.5678"
                try
                {
                    if (line.Contains("siesta: E_KS(eV)") || line.Contains("siesta:         E_KS(eV)"))
                    {
                        var m = Regex.Match(line, @"E_KS\(eV\)\s*=\s*([-\d.Ee+]+)");
                        if (m.Success)
                            lastEnergy = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 费米能: "siesta: Fermi energy =   -3.1234 eV"
                try
                {
                    if (line.Contains("Fermi energy"))
                    {
                        var m = Regex.Match(line, @"Fermi energy\s*=\s*([-\d.Ee+]+)");
                        if (m.Success)
                            result.FermiEnergy_eV = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 原子数: "NumberOfAtoms         4"  或  "Number of atoms  =     4"
                try
                {
                    if (line.Contains("NumberOfAtoms") || line.Contains("Number of atoms"))
                    {
                        var m = Regex.Match(line, @"(?:NumberOfAtoms|Number of atoms)\s*[=:]?\s*(\d+)");
                        if (m.Success)
                            result.AtomCount = int.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 元素种类数: "NumberOfSpecies       2"
                // (informational, actual species parsed from block below)

                // 化学式种类块: "%block ChemicalSpeciesLabel"
                try
                {
                    if (line.TrimStart().StartsWith("%block ChemicalSpeciesLabel", StringComparison.OrdinalIgnoreCase))
                    {
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var specLine = lines[j].Trim();
                            if (specLine.StartsWith("%endblock", StringComparison.OrdinalIgnoreCase))
                                break;
                            // 格式: "1  26  Fe"
                            var m = Regex.Match(specLine, @"^\s*\d+\s+\d+\s+(\w+)");
                            if (m.Success)
                            {
                                var elem = m.Groups[1].Value;
                                if (!elementCounts.ContainsKey(elem))
                                    elementCounts[elem] = 0;
                            }
                        }
                    }
                }
                catch { }

                // 原子坐标块: "%block AtomicCoordinatesAndAtomicSpecies" 或 "outcoor: Final (Fraction)"
                try
                {
                    if (line.TrimStart().StartsWith("%block AtomicCoordinatesAndAtomicSpecies", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("outcoor: Final"))
                    {
                        var tempCounts = new Dictionary<string, int>();
                        var speciesList = elementCounts.Keys.ToList();

                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var coordLine = lines[j].Trim();
                            if (coordLine.StartsWith("%endblock", StringComparison.OrdinalIgnoreCase) ||
                                string.IsNullOrWhiteSpace(coordLine) ||
                                coordLine.StartsWith("siesta:"))
                                break;

                            var parts = coordLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            // 格式: "x  y  z  speciesIndex"  或  "x  y  z  speciesIndex  Element"
                            if (parts.Length >= 4)
                            {
                                string elem = "";
                                if (parts.Length >= 5 && Regex.IsMatch(parts[4], @"^[A-Z][a-z]?$"))
                                {
                                    elem = parts[4];
                                }
                                else if (int.TryParse(parts[3], out int specIdx) && specIdx >= 1 && specIdx <= speciesList.Count)
                                {
                                    elem = speciesList[specIdx - 1];
                                }

                                if (!string.IsNullOrEmpty(elem))
                                {
                                    if (!tempCounts.ContainsKey(elem))
                                        tempCounts[elem] = 0;
                                    tempCounts[elem]++;
                                }
                            }
                        }

                        if (tempCounts.Count > 0)
                            elementCounts = tempCounts;
                    }
                }
                catch { }

                // 晶格向量: "%block LatticeVectors" 或 "outcell: Unit cell vectors (Ang):"
                try
                {
                    if (line.TrimStart().StartsWith("%block LatticeVectors", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("Unit cell vectors"))
                    {
                        latticeVectors.Clear();
                        for (int j = i + 1; j < Math.Min(i + 4, lines.Length); j++)
                        {
                            var vecLine = lines[j].Trim();
                            if (vecLine.StartsWith("%endblock", StringComparison.OrdinalIgnoreCase) ||
                                string.IsNullOrWhiteSpace(vecLine))
                                break;
                            var parts = vecLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3)
                            {
                                double vx = double.Parse(parts[0], inv);
                                double vy = double.Parse(parts[1], inv);
                                double vz = double.Parse(parts[2], inv);
                                latticeVectors.Add(new[] { vx, vy, vz });
                            }
                        }
                    }
                }
                catch { }

                // 体积: "siesta: Cell volume =   123.456 Ang**3"
                try
                {
                    if (line.Contains("Cell volume"))
                    {
                        var m = Regex.Match(line, @"Cell volume\s*=\s*([\d.Ee+]+)");
                        if (m.Success)
                            result.Volume = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 压力: "siesta: Pressure (static):     1.234 GPa"
                try
                {
                    if (line.Contains("Pressure") && line.Contains("GPa"))
                    {
                        var m = Regex.Match(line, @"Pressure\s*\([^)]*\)\s*[:=]?\s*([-\d.Ee+]+)\s*GPa");
                        if (m.Success)
                            result.Pressure_GPa = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 力: "siesta: Atomic forces (eV/Ang):"
                try
                {
                    if (line.Contains("Atomic forces") && line.Contains("eV/Ang"))
                    {
                        foundForce = true;
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var forceLine = lines[j].Trim();
                            if (string.IsNullOrWhiteSpace(forceLine) || forceLine.StartsWith("siesta:") ||
                                forceLine.StartsWith("---") || forceLine.StartsWith("Tot"))
                                break;
                            var parts = forceLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            // 格式: "atomIndex  fx  fy  fz"
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

                // 泛函: "xc.functional    GGA" / "xc.authors       PBE"
                try
                {
                    if (line.Contains("xc.functional") || line.Contains("XC.functional"))
                    {
                        var m = Regex.Match(line, @"xc\.functional\s+(\S+)", RegexOptions.IgnoreCase);
                        if (m.Success && string.IsNullOrEmpty(result.Method))
                            result.Method = m.Groups[1].Value;
                    }
                    if (line.Contains("xc.authors") || line.Contains("XC.authors"))
                    {
                        var m = Regex.Match(line, @"xc\.authors\s+(\S+)", RegexOptions.IgnoreCase);
                        if (m.Success)
                            result.Method = m.Groups[1].Value;
                    }
                }
                catch { }

                // 截断能: "MeshCutoff" 或 "PAO.EnergyShift"
                try
                {
                    if (line.Contains("MeshCutoff"))
                    {
                        var m = Regex.Match(line, @"MeshCutoff\s*[=:]?\s*([\d.]+)\s*(Ry|eV)", RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            double val = double.Parse(m.Groups[1].Value, inv);
                            string unit = m.Groups[2].Value;
                            result.EnergyCutoff_eV = unit.Equals("Ry", StringComparison.OrdinalIgnoreCase)
                                ? val * DftResult.RY_TO_EV
                                : val;
                        }
                    }
                }
                catch { }

                // K 点: "siesta: k-grid:  4  4  4"
                try
                {
                    if (line.Contains("k-grid"))
                    {
                        var m = Regex.Match(line, @"k-grid:\s*(\d+)\s+(\d+)\s+(\d+)");
                        if (m.Success)
                            result.KPoints = $"{m.Groups[1].Value}x{m.Groups[2].Value}x{m.Groups[3].Value}";
                    }
                }
                catch { }

                // 自旋极化: "SpinPolarized    true" 或 "redata: Spin configuration  = spin"
                try
                {
                    if ((line.Contains("SpinPolarized") && line.Contains("true")) ||
                        (line.Contains("Spin configuration") && line.Contains("spin")))
                    {
                        result.SpinPolarized = true;
                    }
                }
                catch { }

                // 收敛: "SCF Convergence by" 或 "siesta: Final energy"
                try
                {
                    if (line.Contains("SCF Convergence") || line.Contains("End of run") ||
                        line.Contains("siesta: Final energy"))
                    {
                        result.IsConverged = true;
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
            if (latticeVectors.Count == 3)
            {
                var lp = new double[3];
                for (int k = 0; k < 3; k++)
                {
                    var v = latticeVectors[k];
                    lp[k] = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
                }
                result.LatticeParameters = lp;

                // 如果未获取体积，从向量叉积计算
                if (double.IsNaN(result.Volume))
                {
                    var a = latticeVectors[0];
                    var b = latticeVectors[1];
                    var c = latticeVectors[2];
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
