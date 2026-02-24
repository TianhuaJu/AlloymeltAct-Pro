using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// Quantum ESPRESSO pw.x 输出文件解析器
    /// 提取总能量、力、应力、收敛状态、晶格、原子位置等
    /// </summary>
    public class QuantumEspressoParser : IDftParser
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public string SoftwareName => "Quantum ESPRESSO";
        public string[] FilePatterns => new[] { "*.out", "*.log", "pw.out" };
        public string[] SignatureStrings => new[] { "Program PWSCF", "Quantum ESPRESSO" };

        public bool CanParse(string filePath)
        {
            var header = DftParserRegistry.ReadHeader(filePath, 50);
            return header.Contains("Program PWSCF") || header.Contains("Quantum ESPRESSO");
        }

        public DftResult Parse(string filePath)
        {
            var result = new DftResult();
            var lines = File.ReadAllLines(filePath);

            double lastEnergy_Ry = double.NaN;
            double maxForce = 0;
            int ionSteps = 0;
            int electronSteps = 0;
            bool converged = false;

            // 晶格向量（直角坐标，单位 Angstrom）
            double[]? latticeA = null, latticeB = null, latticeC = null;
            double alat_bohr = 0; // celldm(1) 以 Bohr 为单位
            const double BOHR_TO_ANGSTROM = 0.529177;

            // 元素和位置
            var elementTypes = new List<string>();
            var elementCounts = new Dictionary<string, int>();
            var positions = new List<AtomPosition>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // === 总能量 ===
                // "!    total energy              =     -123.45678901 Ry"
                if (line.Contains("!") && line.Contains("total energy") && line.Contains("Ry"))
                {
                    var m = Regex.Match(line, @"=\s*([-\d.]+)\s*Ry");
                    if (m.Success)
                    {
                        if (double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double e))
                            lastEnergy_Ry = e;
                    }
                    ionSteps++;
                }

                // === 费米能 ===
                // "the Fermi energy is     5.1234 ev"
                if (line.Contains("the Fermi energy is"))
                {
                    var m = Regex.Match(line, @"the Fermi energy is\s+([-\d.]+)\s*ev", RegexOptions.IgnoreCase);
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double ef))
                        result.FermiEnergy_eV = ef;
                }

                // === 带隙（绝缘体/半导体） ===
                // "highest occupied, lowest unoccupied level (ev):     5.1234    6.7890"
                if (line.Contains("highest occupied, lowest unoccupied level"))
                {
                    var m = Regex.Match(line, @":\s+([-\d.]+)\s+([-\d.]+)");
                    if (m.Success)
                    {
                        if (double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double vbm) &&
                            double.TryParse(m.Groups[2].Value, NumberStyles.Float, Inv, out double cbm))
                        {
                            result.BandGap_eV = cbm - vbm;
                        }
                    }
                }

                // === 收敛 ===
                if (line.Contains("convergence has been achieved"))
                {
                    converged = true;
                }

                // === 电子步数 ===
                // "convergence has been achieved in  12 iterations"
                if (line.Contains("convergence has been achieved in"))
                {
                    var m = Regex.Match(line, @"in\s+(\d+)\s+iterations");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int eSteps))
                        electronSteps = eSteps;
                }

                // === 晶格常数 celldm(1) ===
                // "celldm(1)=  10.26310  celldm(2)=   0.00000 ..."
                if (line.Contains("celldm(1)="))
                {
                    var m = Regex.Match(line, @"celldm\(1\)=\s*([\d.]+)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double cd1))
                        alat_bohr = cd1;
                }

                // === 晶格向量 ===
                // "a(1) = (   0.500000   0.500000   0.000000 )  "  (in units of alat)
                if (line.TrimStart().StartsWith("a(1) = ("))
                {
                    latticeA = ParseLatticeVector(line, alat_bohr, BOHR_TO_ANGSTROM);
                    if (i + 1 < lines.Length && lines[i + 1].TrimStart().StartsWith("a(2) = ("))
                        latticeB = ParseLatticeVector(lines[i + 1], alat_bohr, BOHR_TO_ANGSTROM);
                    if (i + 2 < lines.Length && lines[i + 2].TrimStart().StartsWith("a(3) = ("))
                        latticeC = ParseLatticeVector(lines[i + 2], alat_bohr, BOHR_TO_ANGSTROM);
                }

                // 也处理 "crystal axes: (cart. coord. in units of alat)" 后面的格式
                // "a(1) = (  -0.500000   0.000000   0.500000 )"
                if (line.Contains("crystal axes:"))
                {
                    if (i + 1 < lines.Length) latticeA = ParseLatticeVector(lines[i + 1], alat_bohr, BOHR_TO_ANGSTROM);
                    if (i + 2 < lines.Length) latticeB = ParseLatticeVector(lines[i + 2], alat_bohr, BOHR_TO_ANGSTROM);
                    if (i + 3 < lines.Length) latticeC = ParseLatticeVector(lines[i + 3], alat_bohr, BOHR_TO_ANGSTROM);
                }

                // === 原子位置 ===
                // "site n.     atom                  positions (alat units)"
                // "     1           Si  tau(   1) = (   0.0000000   0.0000000   0.0000000  )"
                if (line.Contains("site n.") && line.Contains("atom"))
                {
                    positions.Clear();
                    elementTypes.Clear();
                    elementCounts.Clear();

                    bool isAlat = line.Contains("alat units");
                    int j = i + 1;
                    while (j < lines.Length)
                    {
                        var atomLine = lines[j].Trim();
                        if (string.IsNullOrWhiteSpace(atomLine)) break;

                        // 匹配 "1  Si  tau(  1) = (  0.000  0.000  0.000  )"
                        var m = Regex.Match(atomLine, @"\d+\s+(\w+)\s+tau\(\s*\d+\)\s*=\s*\(\s*([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)\s*\)");
                        if (!m.Success) break;

                        var elem = m.Groups[1].Value.Trim();
                        if (double.TryParse(m.Groups[2].Value, NumberStyles.Float, Inv, out double x) &&
                            double.TryParse(m.Groups[3].Value, NumberStyles.Float, Inv, out double y) &&
                            double.TryParse(m.Groups[4].Value, NumberStyles.Float, Inv, out double z))
                        {
                            // 如果坐标以 alat 为单位，转换为 Angstrom
                            if (isAlat && alat_bohr > 0)
                            {
                                double scale = alat_bohr * BOHR_TO_ANGSTROM;
                                x *= scale;
                                y *= scale;
                                z *= scale;
                            }

                            positions.Add(new AtomPosition
                            {
                                Element = elem,
                                X = x,
                                Y = y,
                                Z = z,
                                IsFractional = false  // QE 通常输出笛卡尔坐标
                            });

                            if (!elementTypes.Contains(elem))
                                elementTypes.Add(elem);
                            elementCounts.TryGetValue(elem, out int cnt);
                            elementCounts[elem] = cnt + 1;
                        }
                        j++;
                    }
                }

                // === 原子位置（crystal 坐标格式）===
                // "ATOMIC_POSITIONS (crystal)"
                if (line.TrimStart().StartsWith("ATOMIC_POSITIONS"))
                {
                    bool isCrystal = line.Contains("crystal");
                    positions.Clear();
                    elementTypes.Clear();
                    elementCounts.Clear();

                    int j = i + 1;
                    while (j < lines.Length)
                    {
                        var atomLine = lines[j].Trim();
                        if (string.IsNullOrWhiteSpace(atomLine) || atomLine.StartsWith("End") || atomLine.StartsWith("CELL") || atomLine.StartsWith("ATOMIC"))
                            break;

                        // "Si   0.000000   0.000000   0.000000"
                        var m = Regex.Match(atomLine, @"^(\w+)\s+([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)");
                        if (!m.Success) break;

                        var elem = m.Groups[1].Value.Trim();
                        if (double.TryParse(m.Groups[2].Value, NumberStyles.Float, Inv, out double x) &&
                            double.TryParse(m.Groups[3].Value, NumberStyles.Float, Inv, out double y) &&
                            double.TryParse(m.Groups[4].Value, NumberStyles.Float, Inv, out double z))
                        {
                            positions.Add(new AtomPosition
                            {
                                Element = elem,
                                X = x,
                                Y = y,
                                Z = z,
                                IsFractional = isCrystal
                            });

                            if (!elementTypes.Contains(elem))
                                elementTypes.Add(elem);
                            elementCounts.TryGetValue(elem, out int cnt);
                            elementCounts[elem] = cnt + 1;
                        }
                        j++;
                    }
                }

                // === K 点 ===
                // "number of k points=    64  Gaussian smearing, width (Ry)=  0.0200"
                if (line.Contains("number of k points="))
                {
                    var m = Regex.Match(line, @"number of k points=\s*(\d+)");
                    if (m.Success)
                        result.KPoints = $"{m.Groups[1].Value} k-points";
                }

                // === 截断能 ===
                // "kinetic-energy cutoff     =      60.0000  Ry"
                if (line.Contains("kinetic-energy cutoff"))
                {
                    var m = Regex.Match(line, @"=\s*([\d.]+)\s*Ry");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double ecut_ry))
                        result.EnergyCutoff_eV = ecut_ry * DftResult.RY_TO_EV;
                }

                // === 交换关联泛函 ===
                // "Exchange-correlation= PBE" 或 "Exchange-correlation      = SLA  PW   PBE  PBE ( 1  4  3  4 0 0 0)"
                if (line.Contains("Exchange-correlation"))
                {
                    var m = Regex.Match(line, @"Exchange-correlation\s*[=:]\s*(.+)");
                    if (m.Success)
                    {
                        var xcRaw = m.Groups[1].Value.Trim();
                        // 清理括号中的内容
                        xcRaw = Regex.Replace(xcRaw, @"\(.*?\)", "").Trim();
                        result.Method = NormalizeXcFunctional(xcRaw);
                    }
                }

                // === 自旋极化 ===
                // "spin-polarized calculation" 或 "LSDA"
                if (line.Contains("spin-polarized") || line.Contains("LSDA"))
                {
                    result.SpinPolarized = true;
                }

                // === 总磁矩 ===
                // "total magnetization       =     2.00 Bohr mag/cell"
                if (line.Contains("total magnetization") && line.Contains("Bohr mag"))
                {
                    var m = Regex.Match(line, @"=\s*([-\d.]+)\s*Bohr");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double mag))
                        result.TotalMagnetization = mag;
                }

                // === 力 ===
                // "Forces acting on atoms (cartesian axes, Ry/au):"
                // "     atom    1 type  1   force =     0.00000000    0.00000000    0.00012345"
                if (line.Contains("Forces acting on atoms"))
                {
                    maxForce = 0;
                    int j = i + 1;
                    while (j < lines.Length)
                    {
                        var forceLine = lines[j].Trim();
                        if (string.IsNullOrWhiteSpace(forceLine)) break;

                        var m = Regex.Match(forceLine, @"force\s*=\s*([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)");
                        if (m.Success)
                        {
                            if (double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double fx) &&
                                double.TryParse(m.Groups[2].Value, NumberStyles.Float, Inv, out double fy) &&
                                double.TryParse(m.Groups[3].Value, NumberStyles.Float, Inv, out double fz))
                            {
                                // QE 输出力以 Ry/au 为单位，转换为 eV/Angstrom
                                // 1 Ry/au = 1 Ry/Bohr = (13.6057 eV) / (0.529177 A) ≈ 25.7112 eV/A
                                double ryPerBohrToEvPerA = DftResult.RY_TO_EV / BOHR_TO_ANGSTROM;
                                fx *= ryPerBohrToEvPerA;
                                fy *= ryPerBohrToEvPerA;
                                fz *= ryPerBohrToEvPerA;
                                double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz);
                                if (fmag > maxForce) maxForce = fmag;
                            }
                        }
                        j++;
                    }
                }

                // === 应力张量 ===
                // "total   stress  (Ry/bohr**3)                   (kbar)     P=    1.23"
                // "   0.00012   0.00000   0.00000        1.23    0.00    0.00"
                if (line.Contains("total   stress") && line.Contains("P="))
                {
                    // 提取压力
                    var pm = Regex.Match(line, @"P=\s*([-\d.]+)");
                    if (pm.Success && double.TryParse(pm.Groups[1].Value, NumberStyles.Float, Inv, out double p_kbar))
                        result.Pressure_GPa = p_kbar * 0.1; // kbar → GPa

                    // 解析 3x3 应力张量（kbar 列在右侧）
                    var tensor = new double[3, 3];
                    bool tensorOk = true;
                    for (int row = 0; row < 3; row++)
                    {
                        if (i + 1 + row >= lines.Length)
                        {
                            tensorOk = false;
                            break;
                        }
                        var stressLine = lines[i + 1 + row].Trim();
                        var parts = stressLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        // 格式：6 个数字，前 3 个是 Ry/bohr^3，后 3 个是 kbar
                        if (parts.Length >= 6)
                        {
                            for (int col = 0; col < 3; col++)
                            {
                                if (double.TryParse(parts[3 + col], NumberStyles.Float, Inv, out double val))
                                    tensor[row, col] = val * 0.1; // kbar → GPa
                                else
                                    tensorOk = false;
                            }
                        }
                        else
                        {
                            tensorOk = false;
                        }
                    }
                    if (tensorOk)
                        result.StressTensor_GPa = tensor;
                }

                // === 体积 ===
                // "unit-cell volume          =     270.0000 (a.u.)^3"
                if (line.Contains("unit-cell volume"))
                {
                    var m = Regex.Match(line, @"=\s*([\d.]+)\s*\(a\.u\.\)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double vol_bohr3))
                    {
                        // 1 bohr^3 = (0.529177)^3 A^3
                        double bohr3ToA3 = BOHR_TO_ANGSTROM * BOHR_TO_ANGSTROM * BOHR_TO_ANGSTROM;
                        result.Volume = vol_bohr3 * bohr3ToA3;
                    }
                }

                // === 原子数 ===
                // "number of atoms/cell      =            2"
                if (line.Contains("number of atoms/cell"))
                {
                    var m = Regex.Match(line, @"=\s*(\d+)");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int natom))
                        result.AtomCount = natom;
                }

                // === 元素种类数 ===
                // "number of atomic types    =            1"
                // (只作为参考，实际元素从原子位置提取)
            }

            // === 填充结果 ===

            // 总能量 Ry → eV
            if (!double.IsNaN(lastEnergy_Ry))
                result.TotalEnergy_eV = lastEnergy_Ry * DftResult.RY_TO_EV;

            result.MaxForce_eV_A = maxForce;
            result.IsConverged = converged;
            result.IonSteps = ionSteps;
            result.ElectronSteps = electronSteps;

            // 元素计数和化学式
            if (elementCounts.Count > 0)
            {
                result.ElementCounts = new Dictionary<string, int>(elementCounts);
                var formula = "";
                int total = 0;
                foreach (var (elem, count) in elementCounts)
                {
                    formula += elem + (count > 1 ? count.ToString() : "");
                    total += count;
                }
                result.Formula = formula;
                if (result.AtomCount == 0)
                    result.AtomCount = total;
            }

            // 原子位置
            if (positions.Count > 0)
            {
                result.Positions = new List<AtomPosition>(positions);
            }

            // 晶格参数
            if (latticeA != null && latticeB != null && latticeC != null)
            {
                double a = VectorLength(latticeA);
                double b = VectorLength(latticeB);
                double c = VectorLength(latticeC);
                result.LatticeParameters = new[] { a, b, c };

                double alpha = VectorAngle(latticeB, latticeC);
                double beta = VectorAngle(latticeA, latticeC);
                double gamma = VectorAngle(latticeA, latticeB);
                result.LatticeAngles = new[] { alpha, beta, gamma };

                // 如果体积还没从文件中读到，从向量计算
                if (double.IsNaN(result.Volume) || result.Volume == 0)
                {
                    double vol = Math.Abs(
                        latticeA[0] * (latticeB[1] * latticeC[2] - latticeB[2] * latticeC[1]) -
                        latticeA[1] * (latticeB[0] * latticeC[2] - latticeB[2] * latticeC[0]) +
                        latticeA[2] * (latticeB[0] * latticeC[1] - latticeB[1] * latticeC[0])
                    );
                    result.Volume = vol;
                }
            }

            return result;
        }

        // ===== 工具方法 =====

        /// <summary>
        /// 解析晶格向量行，如 "a(1) = (  -0.500000   0.000000   0.500000 )"
        /// 坐标以 alat 为单位，需乘以 alat * bohr_to_angstrom 转换为 Angstrom
        /// </summary>
        private static double[]? ParseLatticeVector(string line, double alat_bohr, double bohrToA)
        {
            var m = Regex.Match(line, @"\(\s*([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)\s*\)");
            if (!m.Success) return null;

            if (double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double x) &&
                double.TryParse(m.Groups[2].Value, NumberStyles.Float, Inv, out double y) &&
                double.TryParse(m.Groups[3].Value, NumberStyles.Float, Inv, out double z))
            {
                double scale = alat_bohr > 0 ? alat_bohr * bohrToA : 1.0;
                return new[] { x * scale, y * scale, z * scale };
            }
            return null;
        }

        /// <summary>
        /// 规范化交换关联泛函名称
        /// </summary>
        private static string NormalizeXcFunctional(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            var upper = raw.ToUpperInvariant();

            // 常见的 QE XC 泛函标识（注意顺序：更具体的在前）
            if (upper.Contains("PBESOL"))
                return "PBEsol";
            if (upper.Contains("B3LYP"))
                return "B3LYP";
            if (upper.Contains("BLYP"))
                return "BLYP";
            if (upper.Contains("HSE"))
                return "HSE06";
            if (upper.Contains("VDW-DF"))
                return "vdW-DF";
            if (upper.Contains("SCAN"))
                return "SCAN";
            if (upper.Contains("PBE"))
                return "PBE";
            if (upper.Contains("LDA") || upper.Contains("PZ") || upper.Contains("SLA  PZ"))
                return "LDA";

            // 返回清理后的原始值
            return raw.Trim();
        }

        /// <summary>计算向量长度</summary>
        private static double VectorLength(double[] v)
        {
            return Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
        }

        /// <summary>计算两个向量之间的夹角（度）</summary>
        private static double VectorAngle(double[] a, double[] b)
        {
            double dot = a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
            double lenA = VectorLength(a);
            double lenB = VectorLength(b);
            if (lenA < 1e-12 || lenB < 1e-12) return 0;
            double cosAngle = Math.Clamp(dot / (lenA * lenB), -1.0, 1.0);
            return Math.Acos(cosAngle) * 180.0 / Math.PI;
        }
    }
}
