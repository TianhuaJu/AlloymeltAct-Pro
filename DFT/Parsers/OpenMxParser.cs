using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// OpenMX DFT 输出文件解析器
    /// 解析 OpenMX 的标准输出文件，提取能量、结构等信息
    /// 能量单位为 Hartree，需要转换为 eV
    /// </summary>
    public class OpenMxParser : IDftParser
    {
        public string SoftwareName => "OpenMX";
        public string[] FilePatterns => new[] { "*.out", "*.std", "*.result" };
        public string[] SignatureStrings => new[] { "OpenMX", "openmx", "T. Ozaki" };

        private const double BOHR_TO_ANG = 0.529177;

        public bool CanParse(string filePath)
        {
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

            double lastEnergy_Ha = double.NaN;
            double maxForce = 0;
            bool foundForce = false;
            var elementCounts = new Dictionary<string, int>();
            var latticeVectors = new List<double[]>();
            var speciesMap = new Dictionary<string, string>(); // label -> element

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 总能量: "Utot.        -123.456789012345  (Hartree)" 或 "Utot  = -123.456"
                try
                {
                    if (line.Contains("Utot.") || line.Contains("Utot "))
                    {
                        var m = Regex.Match(line, @"Utot[.\s]+\s*([-\d.Ee+]+)");
                        if (m.Success)
                            lastEnergy_Ha = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 备选总能量: "Total Energy (Hartree)   -123.456789"
                try
                {
                    if (line.Contains("Total Energy") && line.Contains("Hartree"))
                    {
                        var m = Regex.Match(line, @"Total Energy\s*\(Hartree\)\s*([-\d.Ee+]+)");
                        if (m.Success && double.IsNaN(lastEnergy_Ha))
                            lastEnergy_Ha = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 费米能: "Chemical Potential (Hartree)  0.12345"
                try
                {
                    if (line.Contains("Chemical Potential") && line.Contains("Hartree"))
                    {
                        var m = Regex.Match(line, @"Chemical Potential\s*\(Hartree\)\s*([-\d.Ee+]+)");
                        if (m.Success)
                            result.FermiEnergy_eV = double.Parse(m.Groups[1].Value, inv) * DftResult.HARTREE_TO_EV;
                    }
                }
                catch { }

                // 原子数: "Atoms.Number      4" 或 "<Atoms.Number   4"
                try
                {
                    if (line.Contains("Atoms.Number"))
                    {
                        var m = Regex.Match(line, @"Atoms\.Number\s+(\d+)");
                        if (m.Success)
                            result.AtomCount = int.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 种类数: "Species.Number    2"
                // 种类定义块: "<Definition.of.Atomic.Species"
                try
                {
                    if (line.Contains("Definition.of.Atomic.Species") && line.Contains("<"))
                    {
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var specLine = lines[j].Trim();
                            if (specLine.Contains("Definition.of.Atomic.Species>"))
                                break;
                            // 格式: "Fe  Fe6.0-s2p2d1  Fe_PBE19"
                            var parts = specLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var label = parts[0];
                                // 从标签提取元素符号
                                var mElem = Regex.Match(label, @"^([A-Z][a-z]?)");
                                if (mElem.Success)
                                    speciesMap[label] = mElem.Groups[1].Value;
                            }
                        }
                    }
                }
                catch { }

                // 原子坐标块: "<Atoms.SpeciesAndCoordinates"
                try
                {
                    if (line.Contains("Atoms.SpeciesAndCoordinates") && line.Contains("<") &&
                        !line.Contains("Atoms.SpeciesAndCoordinates>"))
                    {
                        elementCounts.Clear();
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var coordLine = lines[j].Trim();
                            if (coordLine.Contains("Atoms.SpeciesAndCoordinates>"))
                                break;
                            if (string.IsNullOrWhiteSpace(coordLine))
                                continue;
                            // 格式: "1  Fe  0.000  0.000  0.000  4.0  4.0"
                            var parts = coordLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 5)
                            {
                                var label = parts[1];
                                // 从种类映射或直接从标签提取元素
                                string elem;
                                if (speciesMap.ContainsKey(label))
                                    elem = speciesMap[label];
                                else
                                    elem = Regex.Match(label, @"^([A-Z][a-z]?)").Value;

                                if (!string.IsNullOrEmpty(elem))
                                {
                                    if (elementCounts.ContainsKey(elem))
                                        elementCounts[elem]++;
                                    else
                                        elementCounts[elem] = 1;
                                }
                            }
                        }
                    }
                }
                catch { }

                // 晶格向量: "<Atoms.UnitVectors" 块（Ang 或 bohr）
                try
                {
                    if (line.Contains("Atoms.UnitVectors") && line.Contains("<") &&
                        !line.Contains("Atoms.UnitVectors>"))
                    {
                        latticeVectors.Clear();
                        // 检查是否是 bohr 单位
                        bool isBohr = line.Contains("Bohr") || line.Contains("AU");

                        for (int j = i + 1; j < Math.Min(i + 4, lines.Length); j++)
                        {
                            var vecLine = lines[j].Trim();
                            if (vecLine.Contains("Atoms.UnitVectors>"))
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

                // 体积: "Cell_Volume=   123.456789 (Ang^3)"
                try
                {
                    if (line.Contains("Cell_Volume") || line.Contains("cell volume"))
                    {
                        var m = Regex.Match(line, @"(?:Cell_Volume|cell volume)\s*=?\s*([\d.Ee+]+)");
                        if (m.Success)
                            result.Volume = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 力: "   atom=  1  Fe  Fx=  0.001234  Fy= -0.002345  Fz=  0.003456 (Hartree/Bohr)"
                try
                {
                    if (line.Contains("Fx=") || line.Contains("Fy=") || line.Contains("Fz="))
                    {
                        foundForce = true;
                        var mx = Regex.Match(line, @"Fx\s*=\s*([-\d.Ee+]+)");
                        var my = Regex.Match(line, @"Fy\s*=\s*([-\d.Ee+]+)");
                        var mz = Regex.Match(line, @"Fz\s*=\s*([-\d.Ee+]+)");
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
                }
                catch { }

                // 备选力格式: 力表格块
                try
                {
                    if (line.Contains("Atomic forces") && line.Contains("Hartree/Bohr"))
                    {
                        foundForce = true;
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var forceLine = lines[j].Trim();
                            if (string.IsNullOrWhiteSpace(forceLine) || forceLine.StartsWith("*"))
                                break;
                            var parts = forceLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 4)
                            {
                                try
                                {
                                    double fx = double.Parse(parts[parts.Length - 3], inv);
                                    double fy = double.Parse(parts[parts.Length - 2], inv);
                                    double fz = double.Parse(parts[parts.Length - 1], inv);
                                    double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz) *
                                                  DftResult.HARTREE_TO_EV / BOHR_TO_ANG;
                                    if (fmag > maxForce) maxForce = fmag;
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }

                // 泛函: "scf.XcType       GGA-PBE"
                try
                {
                    if (line.Contains("scf.XcType") || line.Contains("XcType"))
                    {
                        var m = Regex.Match(line, @"(?:scf\.)?XcType\s+(\S+)", RegexOptions.IgnoreCase);
                        if (m.Success)
                            result.Method = m.Groups[1].Value;
                    }
                }
                catch { }

                // K 点: "scf.Kgrid    4 4 4"
                try
                {
                    if (line.Contains("scf.Kgrid") || line.Contains("Kgrid"))
                    {
                        var m = Regex.Match(line, @"(?:scf\.)?Kgrid\s+(\d+)\s+(\d+)\s+(\d+)", RegexOptions.IgnoreCase);
                        if (m.Success)
                            result.KPoints = $"{m.Groups[1].Value}x{m.Groups[2].Value}x{m.Groups[3].Value}";
                    }
                }
                catch { }

                // 截断能: "scf.energycutoff    150.0 (Ry)"
                try
                {
                    if (line.Contains("scf.energycutoff") || line.Contains("Energy.Cutoff"))
                    {
                        var m = Regex.Match(line, @"(?:scf\.energycutoff|Energy\.Cutoff)\s+([\d.]+)", RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            double val = double.Parse(m.Groups[1].Value, inv);
                            // OpenMX 截断能单位通常为 Ry
                            result.EnergyCutoff_eV = val * DftResult.RY_TO_EV;
                        }
                    }
                }
                catch { }

                // 自旋极化: "scf.SpinPolarization   on"
                try
                {
                    if (line.Contains("SpinPolarization") && (line.Contains("on") || line.Contains("ON") ||
                        line.Contains("NC") || line.Contains("nc")))
                    {
                        result.SpinPolarized = true;
                    }
                }
                catch { }

                // 磁矩: "Total Magnetic Moment (muB)  1.234"
                try
                {
                    if (line.Contains("Magnetic Moment") || line.Contains("magnetic moment"))
                    {
                        var m = Regex.Match(line, @"[Mm]agnetic [Mm]oment\s*(?:\([^)]*\))?\s*([-\d.Ee+]+)");
                        if (m.Success)
                            result.TotalMagnetization = double.Parse(m.Groups[1].Value, inv);
                    }
                }
                catch { }

                // 收敛: "SCF calculation was achieved" 或 "The SCF iteration was converged"
                try
                {
                    if (line.Contains("SCF calculation was achieved") ||
                        line.Contains("converged") ||
                        line.Contains("Geometry optimization was achieved"))
                    {
                        result.IsConverged = true;
                    }
                }
                catch { }

                // 压力: "Stress tensor (GPa):" 后读取对角线
                try
                {
                    if (line.Contains("Stress tensor") && line.Contains("GPa"))
                    {
                        double sumDiag = 0;
                        int diagCount = 0;
                        for (int j = i + 1; j < Math.Min(i + 4, lines.Length); j++)
                        {
                            var parts = lines[j].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3 && diagCount < 3)
                            {
                                try
                                {
                                    sumDiag += double.Parse(parts[diagCount], inv);
                                    diagCount++;
                                }
                                catch { break; }
                            }
                        }
                        if (diagCount == 3)
                            result.Pressure_GPa = -sumDiag / 3.0;
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
