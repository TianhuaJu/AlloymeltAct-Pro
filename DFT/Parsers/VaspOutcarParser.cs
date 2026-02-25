using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// VASP OUTCAR 文件解析器
    /// 提取总能量、力、应力、收敛状态等
    /// </summary>
    public class VaspOutcarParser : IDftParser
    {
        public string SoftwareName => "VASP";
        public string[] FilePatterns => new[] { "OUTCAR", "OUTCAR.*" };
        public string[] SignatureStrings => new[] { "vasp.", "VASP", "INCAR:", "POTCAR:" };

        public bool CanParse(string filePath)
        {
            // 文件名检查
            var fileName = Path.GetFileName(filePath);
            if (fileName.StartsWith("OUTCAR", StringComparison.OrdinalIgnoreCase))
                return true;

            // 内容签名检查
            var header = DftParserRegistry.ReadHeader(filePath, 30);
            return header.Contains("vasp.") || header.Contains("INCAR:") ||
                   (header.Contains("POTCAR:") && header.Contains("POSCAR:"));
        }

        public DftResult Parse(string filePath)
        {
            var result = new DftResult();
            var lines = File.ReadAllLines(filePath);

            double lastEnergy = double.NaN;
            double maxForce = 0;
            bool reachedForces = false;
            int ionSteps = 0;
            int eSteps = 0;
            var elements = new List<string>();
            var ionCounts = new List<int>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 总能量: "free  energy   TOTEN  =   -123.456789 eV"
                if (line.Contains("free  energy   TOTEN"))
                {
                    var m = Regex.Match(line, @"TOTEN\s*=\s*([-\d.]+)");
                    if (m.Success) lastEnergy = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                    ionSteps++;
                }

                // 元素种类: "POTCAR:    PAW_PBE Fe 06Sep2000"
                if (line.Contains("POTCAR:") && line.Contains("PAW"))
                {
                    var m = Regex.Match(line, @"POTCAR:\s+\S+\s+(\w+)");
                    if (m.Success)
                    {
                        var elem = m.Groups[1].Value;
                        // 只取元素名（去掉后面可能的_pv等后缀对应的原始元素）
                        elem = Regex.Match(elem, @"^[A-Z][a-z]?").Value;
                        if (!string.IsNullOrEmpty(elem) && !elements.Contains(elem))
                            elements.Add(elem);
                    }
                }

                // 离子数: "   ions per type =     2     4"
                if (line.Contains("ions per type"))
                {
                    var nums = Regex.Matches(line, @"\d+");
                    ionCounts.Clear();
                    foreach (Match m in nums)
                        ionCounts.Add(int.Parse(m.Value));
                }

                // 费米能: "E-fermi :  5.1234"
                if (line.Contains("E-fermi"))
                {
                    var m = Regex.Match(line, @"E-fermi\s*:\s*([-\d.]+)");
                    if (m.Success) result.FermiEnergy_eV = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                }

                // 截断能: "ENCUT  =  520.00 eV"
                if (line.Contains("ENCUT") && line.Contains("eV"))
                {
                    var m = Regex.Match(line, @"ENCUT\s*=\s*([\d.]+)");
                    if (m.Success) result.EnergyCutoff_eV = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                }

                // 泛函: "GGA     =    PE"
                if (line.TrimStart().StartsWith("GGA") && line.Contains("="))
                {
                    var m = Regex.Match(line, @"GGA\s*=\s*(\S+)");
                    if (m.Success)
                    {
                        result.Method = m.Groups[1].Value switch
                        {
                            "PE" => "PBE",
                            "PS" => "PBEsol",
                            "RP" => "revPBE",
                            "CA" => "LDA",
                            _ => m.Groups[1].Value
                        };
                    }
                }

                // K 点: "k-points           NKPTS ="
                if (line.Contains("NKPTS"))
                {
                    var m = Regex.Match(line, @"NKPTS\s*=\s*(\d+)");
                    if (m.Success) result.KPoints = $"{m.Groups[1].Value} k-points";
                }

                // 自旋极化: "ISPIN  =      2"
                if (line.Contains("ISPIN") && line.Contains("2"))
                {
                    result.SpinPolarized = true;
                }

                // 磁矩: "number of electron  xxx magnetization  yyy"
                if (line.Contains("magnetization"))
                {
                    var m = Regex.Match(line, @"magnetization\s+([-\d.]+)");
                    if (m.Success) result.TotalMagnetization = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                }

                // 力: "TOTAL-FORCE (eV/Angst)"
                if (line.Contains("TOTAL-FORCE"))
                {
                    reachedForces = true;
                    i++; // 跳过分隔线
                    continue;
                }

                if (reachedForces && line.Contains("------"))
                {
                    reachedForces = false;
                    continue;
                }

                if (reachedForces)
                {
                    var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 6)
                    {
                        try
                        {
                            double fx = Math.Abs(double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
                            double fy = Math.Abs(double.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture));
                            double fz = Math.Abs(double.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture));
                            double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz);
                            if (fmag > maxForce) maxForce = fmag;
                        }
                        catch { }
                    }
                }

                // 压力: "external pressure =    1.23 kB"
                if (line.Contains("external pressure"))
                {
                    var m = Regex.Match(line, @"external pressure\s*=\s*([-\d.]+)\s*kB");
                    if (m.Success) result.Pressure_GPa = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) * 0.1; // kBar → GPa
                }

                // 体积: "volume of cell :   123.456"
                if (line.Contains("volume of cell"))
                {
                    var m = Regex.Match(line, @"volume of cell\s*:\s*([\d.]+)");
                    if (m.Success) result.Volume = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                }

                // 晶格向量
                if (line.Contains("direct lattice vectors"))
                {
                    var lp = new List<double>();
                    for (int j = 1; j <= 3 && i + j < lines.Length; j++)
                    {
                        var parts = lines[i + j].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            double x = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                            double y = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                            double z = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                            lp.Add(Math.Sqrt(x * x + y * y + z * z));
                        }
                    }
                    if (lp.Count == 3) result.LatticeParameters = lp.ToArray();
                }

                // 电子步数
                if (line.Contains("Iteration"))
                {
                    var m = Regex.Match(line, @"\(\s*(\d+)\)");
                    if (m.Success) eSteps = int.Parse(m.Groups[1].Value);
                }

                // 收敛判定: "reached required accuracy"
                if (line.Contains("reached required accuracy"))
                {
                    result.IsConverged = true;
                }
            }

            result.TotalEnergy_eV = lastEnergy;
            result.MaxForce_eV_A = maxForce;
            result.IonSteps = ionSteps;
            result.ElectronSteps = eSteps;

            // 构建元素计数和化学式
            if (elements.Count > 0 && ionCounts.Count == elements.Count)
            {
                int total = 0;
                var formula = "";
                for (int i = 0; i < elements.Count; i++)
                {
                    result.ElementCounts[elements[i]] = ionCounts[i];
                    formula += elements[i] + (ionCounts[i] > 1 ? ionCounts[i].ToString() : "");
                    total += ionCounts[i];
                }
                result.Formula = formula;
                result.AtomCount = total;
            }

            return result;
        }
    }
}
