using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// DFTB+ 输出文件解析器
    /// 解析 DFTB+（Density Functional Tight Binding）的输出文件
    /// 能量单位为 Hartree，需要转换为 eV
    /// </summary>
    public class DftbPlusParser : IDftParser
    {
        public string SoftwareName => "DFTB+";
        public string[] FilePatterns => new[] { "*.out", "detailed.out", "results.tag", "dftb_output" };
        public string[] SignatureStrings => new[] { "DFTB+", "dftb+", "Density Functional" };

        private const double BOHR_TO_ANG = 0.529177;

        public bool CanParse(string filePath)
        {
            // 文件名检查
            var fileName = Path.GetFileName(filePath);
            if (fileName.Equals("detailed.out", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("results.tag", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("dftb_output", StringComparison.OrdinalIgnoreCase))
            {
                var header = DftParserRegistry.ReadHeader(filePath, 50);
                if (header.Contains("DFTB+") || header.Contains("dftb+") ||
                    header.Contains("Density Functional Tight Binding"))
                    return true;
            }

            // 内容签名检查
            var headerContent = DftParserRegistry.ReadHeader(filePath, 50);
            if (headerContent.Contains("DFTB+") || headerContent.Contains("dftb+"))
                return true;
            // "Density Functional" 需要搭配 DFTB 特征
            if (headerContent.Contains("Density Functional") && headerContent.Contains("Tight Binding"))
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
            var speciesList = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 总能量: "Total Energy:                 -12.3456789012 H"
                // 或 "total_energy          -12.3456789012" (in results.tag)
                // 或 "Total Electronic energy:   -12.345 H"
                try
                {
                    if (line.Contains("Total Energy:") || line.Contains("Total Electronic energy:"))
                    {
                        var m = Regex.Match(line, @"Total\s+(?:Electronic\s+)?[Ee]nergy\s*:\s*([-\d.Ee+]+)");
                        if (m.Success)
                            lastEnergy_Ha = double.Parse(m.Groups[1].Value, inv);
                    }
                    else if (line.TrimStart().StartsWith("total_energy"))
                    {
                        var m = Regex.Match(line, @"total_energy\s+([-\d.Ee+]+)");
                        if (m.Success)
                            lastEnergy_Ha = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 备选能量格式: "Mermin free energy:   -12.345 H" 或 "Total Mermin free energy:"
                try
                {
                    if (line.Contains("Mermin free energy") && double.IsNaN(lastEnergy_Ha))
                    {
                        var m = Regex.Match(line, @"Mermin free energy\s*:\s*([-\d.Ee+]+)");
                        if (m.Success)
                            lastEnergy_Ha = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 费米能: "Fermi level:                  -0.1234 H" 或 "Fermi energy:  -0.1234 eV"
                try
                {
                    if (line.Contains("Fermi level") || line.Contains("Fermi energy"))
                    {
                        var m = Regex.Match(line, @"Fermi\s+(?:level|energy)\s*:\s*([-\d.Ee+]+)\s*(H|eV)?");
                        if (m.Success)
                        {
                            double val = double.Parse(m.Groups[1].Value, inv);
                            string unit = m.Groups[2].Value;
                            result.FermiEnergy_eV = unit == "eV" ? val : val * DftResult.HARTREE_TO_EV;
                        }
                    }
                }
                catch { }

                // 几何结构块: "Geometry = ..." 或 来自 detailed.out 中的坐标信息
                // "  Periodic geometry info:" 后跟原子坐标
                try
                {
                    if (line.Contains("Geometry =") || line.Contains("Periodic geometry"))
                    {
                        // 解析后续的原子信息
                    }
                }
                catch { }

                // 原子类型: "Type Coverage:" 或 "MaxAngularMomentum" 块可看到元素
                // DFTB+ gen 格式: 第一行 "4 S" 或 "4 F"，第二行 "Fe Al"
                try
                {
                    if (line.Contains("Type Coverage"))
                    {
                        // 后续行列出元素覆盖
                    }
                }
                catch { }

                // 种类列表: 在输入回显中 "  Fe" "  Al"
                // 或 "Atom types : Fe Al"
                try
                {
                    if (line.Contains("Atom types") || line.Contains("type names"))
                    {
                        var m = Regex.Match(line, @"(?:Atom types|type names)\s*[:=]\s*(.+)");
                        if (m.Success)
                        {
                            var typeStr = m.Groups[1].Value.Trim();
                            var types = typeStr.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            speciesList.Clear();
                            foreach (var t in types)
                            {
                                var elem = Regex.Match(t, @"^[A-Z][a-z]?").Value;
                                if (!string.IsNullOrEmpty(elem))
                                    speciesList.Add(elem);
                            }
                        }
                    }
                }
                catch { }

                // 原子数: "Number of atoms:       4"
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

                // 坐标块（gen 格式回显）:
                // 第一行: "4  F" (原子数和类型)
                // 第二行: "Fe  Al" (元素列表)
                // 后续: "1  1  0.000  0.000  0.000" (index typeIndex x y z)
                try
                {
                    if (line.Contains("Geometry") && i + 2 < lines.Length)
                    {
                        // 检查是否为 gen 格式的开头
                        var nextLine = lines[i + 1].Trim();
                        var genMatch = Regex.Match(nextLine, @"^(\d+)\s+([SFC])");
                        if (genMatch.Success)
                        {
                            int natoms = int.Parse(genMatch.Groups[1].Value, inv);
                            if (result.AtomCount == 0) result.AtomCount = natoms;

                            // 元素列表行
                            if (i + 2 < lines.Length)
                            {
                                var elemLine = lines[i + 2].Trim();
                                var elems = elemLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                speciesList.Clear();
                                foreach (var e in elems)
                                {
                                    var elem = Regex.Match(e, @"^[A-Z][a-z]?").Value;
                                    if (!string.IsNullOrEmpty(elem))
                                        speciesList.Add(elem);
                                }

                                // 原子坐标
                                elementCounts.Clear();
                                for (int j = i + 3; j < Math.Min(i + 3 + natoms, lines.Length); j++)
                                {
                                    var coordLine = lines[j].Trim();
                                    var parts = coordLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 5)
                                    {
                                        int typeIdx = int.Parse(parts[1], inv) - 1;
                                        if (typeIdx >= 0 && typeIdx < speciesList.Count)
                                        {
                                            var elem = speciesList[typeIdx];
                                            if (elementCounts.ContainsKey(elem))
                                                elementCounts[elem]++;
                                            else
                                                elementCounts[elem] = 1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                // 来自 detailed.out 中的元素统计
                // "Nr. of atoms (by species): 2 4"
                try
                {
                    if (line.Contains("Nr. of atoms") || line.Contains("atoms (by species)"))
                    {
                        var m = Regex.Match(line, @"(?:Nr\. of atoms|atoms)\s*\(by species\)\s*:\s*(.+)");
                        if (m.Success && speciesList.Count > 0)
                        {
                            var nums = Regex.Matches(m.Groups[1].Value, @"(\d+)");
                            elementCounts.Clear();
                            for (int j = 0; j < Math.Min(nums.Count, speciesList.Count); j++)
                            {
                                elementCounts[speciesList[j]] = int.Parse(nums[j].Groups[1].Value, inv);
                            }
                        }
                    }
                }
                catch { }

                // 晶格向量: "Lattice vectors (Angstrom):" 后跟3行
                try
                {
                    if (line.Contains("Lattice vectors"))
                    {
                        bool isBohr = line.Contains("Bohr") || line.Contains("bohr");
                        latticeVectors.Clear();
                        for (int j = i + 1; j < Math.Min(i + 4, lines.Length); j++)
                        {
                            var vecLine = lines[j].Trim();
                            if (string.IsNullOrWhiteSpace(vecLine))
                                break;
                            var parts = vecLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3)
                            {
                                double vx = double.Parse(parts[0], inv);
                                double vy = double.Parse(parts[1], inv);
                                double vz = double.Parse(parts[2], inv);
                                if (isBohr)
                                {
                                    vx *= BOHR_TO_ANG;
                                    vy *= BOHR_TO_ANG;
                                    vz *= BOHR_TO_ANG;
                                }
                                latticeVectors.Add(new[] { vx, vy, vz });
                            }
                        }
                    }
                }
                catch { }

                // 体积: "Volume:    123.456 Ang^3" 或 "Cell volume:  123.456"
                try
                {
                    if (line.Contains("Volume") || line.Contains("Cell volume"))
                    {
                        var m = Regex.Match(line, @"(?:Cell\s+)?[Vv]olume\s*[:=]\s*([\d.Ee+]+)");
                        if (m.Success)
                            result.Volume = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 力: "Total Forces (Hartree/Bohr):" 后跟力数据
                try
                {
                    if (line.Contains("Total Forces") || line.Contains("total forces"))
                    {
                        bool isHaBohr = line.Contains("Hartree") || line.Contains("Ha");
                        foundForce = true;
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var forceLine = lines[j].Trim();
                            if (string.IsNullOrWhiteSpace(forceLine) || forceLine.StartsWith("---") ||
                                forceLine.StartsWith("Max"))
                                break;

                            var parts = forceLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            // 跳过可能的原子索引和元素符号
                            int startIdx = 0;
                            while (startIdx < parts.Length)
                            {
                                if (double.TryParse(parts[startIdx], NumberStyles.Float, inv, out double testVal) &&
                                    parts[startIdx].Contains('.'))
                                    break;
                                startIdx++;
                            }

                            if (startIdx + 2 < parts.Length)
                            {
                                try
                                {
                                    double fx = double.Parse(parts[startIdx], inv);
                                    double fy = double.Parse(parts[startIdx + 1], inv);
                                    double fz = double.Parse(parts[startIdx + 2], inv);
                                    double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz);
                                    if (isHaBohr)
                                        fmag *= DftResult.HARTREE_TO_EV / BOHR_TO_ANG;
                                    if (fmag > maxForce) maxForce = fmag;
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }

                // 最大力: "Maximal force component:  0.001234"
                try
                {
                    if (line.Contains("Maximal force") || line.Contains("Max force"))
                    {
                        var m = Regex.Match(line, @"(?:Maximal force|Max force)\s*(?:component)?\s*[:=]\s*([\d.Ee+-]+)");
                        if (m.Success)
                        {
                            double f = double.Parse(m.Groups[1].Value, inv);
                            // 假设为 Hartree/Bohr
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

                // 泛函/方法: DFTB+ 使用紧束缚方法
                try
                {
                    if (line.Contains("Hamiltonian") && string.IsNullOrEmpty(result.Method))
                    {
                        if (line.Contains("DFTB"))
                            result.Method = "DFTB";
                        else if (line.Contains("xTB"))
                            result.Method = "xTB";
                    }
                    if (line.Contains("SCC") && line.Contains("Yes"))
                    {
                        result.Method = string.IsNullOrEmpty(result.Method) ? "SCC-DFTB" : result.Method;
                    }
                }
                catch { }

                // K 点: "KPointsAndWeights" 或 "K-points:  4  4  4"
                try
                {
                    if (line.Contains("K-points") || line.Contains("KPointsAndWeights") ||
                        line.Contains("SupercellFolding"))
                    {
                        var m = Regex.Match(line, @"(\d+)\s+(\d+)\s+(\d+)");
                        if (m.Success)
                            result.KPoints = $"{m.Groups[1].Value}x{m.Groups[2].Value}x{m.Groups[3].Value}";
                    }
                }
                catch { }

                // 自旋极化: "SpinPolarisation" 或 "Spin polarisation:  Yes"
                try
                {
                    if ((line.Contains("SpinPolarisation") || line.Contains("Spin polar")) &&
                        (line.Contains("Yes") || line.Contains("Colinear") || line.Contains("colinear")))
                    {
                        result.SpinPolarized = true;
                    }
                }
                catch { }

                // 压力: "Pressure (GPa):   1.234"
                try
                {
                    if (line.Contains("Pressure") && line.Contains("GPa"))
                    {
                        var m = Regex.Match(line, @"Pressure\s*(?:\(GPa\))?\s*[:=]\s*([-\d.Ee+]+)");
                        if (m.Success)
                            result.Pressure_GPa = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 收敛: "SCC converged" 或 "Geometry converged"
                try
                {
                    if (line.Contains("SCC converged") || line.Contains("Geometry converged") ||
                        line.Contains("Geometry is converged"))
                    {
                        result.IsConverged = true;
                    }
                }
                catch { }

                // 带隙: "Band gap:  1.234 eV" 或 "Gap:  1.234 H"
                try
                {
                    if (line.Contains("Band gap") || line.Contains("Gap:"))
                    {
                        var m = Regex.Match(line, @"(?:Band gap|Gap)\s*[:=]\s*([\d.Ee+]+)\s*(H|eV)?");
                        if (m.Success)
                        {
                            double gap = double.Parse(m.Groups[1].Value, inv);
                            string unit = m.Groups[2].Value;
                            result.BandGap_eV = (unit == "H" || unit == "") ? gap * DftResult.HARTREE_TO_EV : gap;
                        }
                    }
                }
                catch { }
            }

            // 设置总能量 (Hartree -> eV)
            if (!double.IsNaN(lastEnergy_Ha))
                result.TotalEnergy_eV = lastEnergy_Ha * DftResult.HARTREE_TO_EV;

            // 如果没找到 Method，设为默认
            if (string.IsNullOrEmpty(result.Method))
                result.Method = "DFTB";

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
