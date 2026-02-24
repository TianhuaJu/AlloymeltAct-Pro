using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// CP2K 输出文件解析器
    /// 支持 .out, .log, cp2k.out 格式
    /// 提取总能量（Hartree→eV）、力、晶胞、收敛状态等
    /// </summary>
    public class Cp2kParser : IDftParser
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // Bohr to Angstrom conversion
        private const double BOHR_TO_ANG = 0.529177249;
        // Hartree/Bohr to eV/Angstrom conversion
        private const double HA_BOHR_TO_EV_ANG = DftResult.HARTREE_TO_EV / BOHR_TO_ANG;

        public string SoftwareName => "CP2K";
        public string[] FilePatterns => new[] { "*.out", "*.log", "cp2k.out" };
        public string[] SignatureStrings => new[] { "CP2K|", "PROGRAM STARTED AT", "CP2K version" };

        public bool CanParse(string filePath)
        {
            var header = DftParserRegistry.ReadHeader(filePath, 50);
            foreach (var sig in SignatureStrings)
            {
                if (header.Contains(sig, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public DftResult Parse(string filePath)
        {
            var result = new DftResult();
            var lines = File.ReadAllLines(filePath);

            double lastEnergy = double.NaN;
            double maxForce = 0;
            bool hasForces = false;
            int ionSteps = 0;
            int electronSteps = 0;
            bool scfConverged = false;

            // Cell vectors in Angstrom
            double[][] cellVectors = new double[3][];
            bool hasCell = false;

            // Atom tracking
            var elementCounts = new Dictionary<string, int>();
            int atomCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // --- Total Energy ---
                // "ENERGY| Total FORCE_EVAL ( QS ) energy [a.u.]:     -123.456789012345"
                if (line.Contains("ENERGY| Total FORCE_EVAL"))
                {
                    var m = Regex.Match(line, @"energy\s*\[a\.u\.\]\s*:\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)", RegexOptions.IgnoreCase);
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double eHa))
                    {
                        lastEnergy = eHa * DftResult.HARTREE_TO_EV;
                    }
                }

                // --- SCF convergence ---
                if (line.Contains("SCF run converged"))
                {
                    scfConverged = true;
                }

                // "SCF run NOT converged"
                if (line.Contains("SCF run NOT converged"))
                {
                    scfConverged = false;
                }

                // --- SCF steps ---
                // Count SCF iterations: lines like "  1 OT DIIS     0.80E-01    0.3     0.12345678    -123.456789"
                // or "     1 P_Mix/DIAG. 0.40E+00    0.3     2.12345678  -123.456789"
                if (Regex.IsMatch(line, @"^\s+\d+\s+\w+"))
                {
                    var scfM = Regex.Match(line, @"^\s+(\d+)\s+(?:OT|P_Mix|DIAG|CG)");
                    if (scfM.Success)
                    {
                        int step = int.Parse(scfM.Groups[1].Value, Inv);
                        if (step > electronSteps) electronSteps = step;
                    }
                }

                // --- Geometry optimization step ---
                // "OPTIMIZATION STEP:     1"
                if (line.Contains("OPTIMIZATION STEP:"))
                {
                    ionSteps++;
                }

                // --- Geometry optimization converged ---
                if (line.Contains("GEOMETRY OPTIMIZATION COMPLETED") ||
                    line.Contains("OPTIMIZATION COMPLETED"))
                {
                    scfConverged = true;
                }

                // --- Forces ---
                // "ATOMIC FORCES in [a.u.]"
                if (line.Contains("ATOMIC FORCES in [a.u.]"))
                {
                    hasForces = true;
                    maxForce = 0; // reset for latest force block
                    // Skip header lines: "# Atom   Kind   Element          X              Y              Z"
                    int j = i + 1;
                    // Skip comment/header lines starting with # or empty
                    while (j < lines.Length && (lines[j].TrimStart().StartsWith("#") || string.IsNullOrWhiteSpace(lines[j])))
                        j++;

                    for (; j < lines.Length; j++)
                    {
                        var fLine = lines[j].Trim();
                        if (string.IsNullOrEmpty(fLine) || fLine.StartsWith("SUM") || fLine.StartsWith("---"))
                            break;
                        // "   1    1   Fe     0.00123    -0.00456     0.00789"
                        var parts = fLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 6)
                        {
                            try
                            {
                                // Forces are in Hartree/Bohr, convert to eV/Angstrom
                                double fx = double.Parse(parts[3], Inv) * HA_BOHR_TO_EV_ANG;
                                double fy = double.Parse(parts[4], Inv) * HA_BOHR_TO_EV_ANG;
                                double fz = double.Parse(parts[5], Inv) * HA_BOHR_TO_EV_ANG;
                                double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz);
                                if (fmag > maxForce) maxForce = fmag;
                            }
                            catch { }
                        }
                    }
                }

                // --- Cell vectors ---
                // "CELL| Vector a [angstrom]:      5.430     0.000     0.000   |a| =   5.430"
                var cellM = Regex.Match(line, @"CELL\|\s*Vector\s+([abc])\s+\[angstrom\]\s*:\s*([-+]?\d+\.?\d*)\s+([-+]?\d+\.?\d*)\s+([-+]?\d+\.?\d*)");
                if (cellM.Success)
                {
                    int idx = cellM.Groups[1].Value[0] - 'a';
                    if (idx >= 0 && idx < 3)
                    {
                        double x = double.Parse(cellM.Groups[2].Value, Inv);
                        double y = double.Parse(cellM.Groups[3].Value, Inv);
                        double z = double.Parse(cellM.Groups[4].Value, Inv);
                        cellVectors[idx] = new[] { x, y, z };
                        hasCell = true;
                    }
                }

                // --- Volume ---
                // "CELL| Volume [angstrom^3]:       123.456"
                if (line.Contains("CELL| Volume"))
                {
                    var m = Regex.Match(line, @"Volume\s*\[angstrom\^3\]\s*:\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double vol))
                    {
                        result.Volume = vol;
                    }
                }

                // --- Atoms from coordinate block ---
                // "MODULE QUICKSTEP:  ATOMIC COORDINATES IN angstrom"
                // or "ATOMIC COORDINATES IN ANGSTROM"
                if (line.Contains("ATOMIC COORDINATES") && line.Contains("angstrom", StringComparison.OrdinalIgnoreCase))
                {
                    elementCounts.Clear();
                    atomCount = 0;
                    // Skip header lines
                    int j = i + 1;
                    while (j < lines.Length && (string.IsNullOrWhiteSpace(lines[j]) || lines[j].TrimStart().StartsWith("Atom")))
                        j++;

                    for (; j < lines.Length; j++)
                    {
                        var aLine = lines[j].Trim();
                        if (string.IsNullOrEmpty(aLine) || aLine.StartsWith("---") || aLine.StartsWith("MODULE"))
                            break;
                        // "   1    Fe   1     26    5.123   2.456   7.890         0.000"
                        var parts = aLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5)
                        {
                            // The element is typically the second column
                            string elem = parts[1];
                            // Clean element name: take only alphabetic chars
                            elem = Regex.Match(elem, @"^[A-Z][a-z]?").Value;
                            if (!string.IsNullOrEmpty(elem))
                            {
                                if (elementCounts.ContainsKey(elem))
                                    elementCounts[elem]++;
                                else
                                    elementCounts[elem] = 1;
                                atomCount++;
                            }
                        }
                    }
                }

                // --- Also try to get atoms from "- Atoms:" block ---
                // "- Atoms:    12"
                if (Regex.IsMatch(line, @"-\s*Atoms\s*:\s*(\d+)"))
                {
                    var m = Regex.Match(line, @"-\s*Atoms\s*:\s*(\d+)");
                    if (m.Success)
                    {
                        int n = int.Parse(m.Groups[1].Value, Inv);
                        if (n > 0 && atomCount == 0) atomCount = n;
                    }
                }

                // --- Functional ---
                // "FUNCTIONAL| ROUTINE=NEW"
                // "FUNCTIONAL| PBE:"
                // " XC_FUNCTIONAL  PBE" or similar
                if (line.Contains("FUNCTIONAL"))
                {
                    if (line.Contains("BLYP", StringComparison.OrdinalIgnoreCase))
                        result.Method = "BLYP";
                    else if (line.Contains("PBE0", StringComparison.OrdinalIgnoreCase))
                        result.Method = "PBE0";
                    else if (line.Contains("PBESOL", StringComparison.OrdinalIgnoreCase) ||
                             line.Contains("PBE_SOL", StringComparison.OrdinalIgnoreCase))
                        result.Method = "PBEsol";
                    else if (line.Contains("PBE", StringComparison.OrdinalIgnoreCase) &&
                             string.IsNullOrEmpty(result.Method))
                        result.Method = "PBE";
                    else if (line.Contains("B3LYP", StringComparison.OrdinalIgnoreCase))
                        result.Method = "B3LYP";
                    else if (line.Contains("LDA", StringComparison.OrdinalIgnoreCase) ||
                             line.Contains("PADE", StringComparison.OrdinalIgnoreCase))
                        result.Method = "LDA";
                }

                // --- Cutoff energy ---
                // "GLOBAL| Cutoff [a.u.]   300.0"
                // or "PW_CUTOFF [Ry]     300.0"
                if (line.Contains("Cutoff") && line.Contains("[a.u.]"))
                {
                    var m = Regex.Match(line, @"Cutoff\s*\[a\.u\.\]\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double cutRy))
                    {
                        result.EnergyCutoff_eV = cutRy * DftResult.RY_TO_EV;
                    }
                }

                // "GLOBAL| Cutoff [Ry]"
                if (line.Contains("Cutoff") && line.Contains("[Ry]"))
                {
                    var m = Regex.Match(line, @"Cutoff\s*\[Ry\]\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double cutRy))
                    {
                        result.EnergyCutoff_eV = cutRy * DftResult.RY_TO_EV;
                    }
                }

                // --- K-points ---
                // "Number of k-points:    12"
                if (line.Contains("Number of k-points") || line.Contains("K_POINTS"))
                {
                    var m = Regex.Match(line, @"(\d+)\s*$");
                    if (m.Success) result.KPoints = $"{m.Groups[1].Value} k-points";
                }

                // --- Spin ---
                // "SPIN| Spin unrestricted (UKS) calculation"
                if (line.Contains("Spin unrestricted") || line.Contains("UKS") ||
                    (line.Contains("SPIN") && line.Contains("POLARIZED")))
                {
                    result.SpinPolarized = true;
                }

                // --- Magnetization ---
                // "Magnetization (total):     1.2345"
                if (line.Contains("Magnetization") && line.Contains("total"))
                {
                    var m = Regex.Match(line, @"Magnetization\s*\(total\)\s*:\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)", RegexOptions.IgnoreCase);
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double mag))
                    {
                        result.TotalMagnetization = mag;
                    }
                }

                // --- Fermi energy ---
                // "Fermi energy:    -0.12345 a.u."
                if (line.Contains("Fermi energy"))
                {
                    var m = Regex.Match(line, @"Fermi energy\s*:\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double ef))
                    {
                        // CP2K reports Fermi in Hartree
                        result.FermiEnergy_eV = ef * DftResult.HARTREE_TO_EV;
                    }
                }

                // --- Pressure / Stress ---
                // "STRESS| Analytical stress tensor [GPa]"
                if (line.Contains("STRESS|") && line.Contains("[GPa]"))
                {
                    double traceSum = 0;
                    int traceCount = 0;
                    for (int j = i + 1; j < Math.Min(i + 10, lines.Length); j++)
                    {
                        // "STRESS|                        x             y             z"
                        // "STRESS|          x     1.234     0.000     0.000"
                        var sM = Regex.Match(lines[j], @"STRESS\|\s+([xyz])\s+([-+]?\d+\.?\d*[Ee]?[+-]?\d*)\s+([-+]?\d+\.?\d*[Ee]?[+-]?\d*)\s+([-+]?\d+\.?\d*[Ee]?[+-]?\d*)");
                        if (sM.Success)
                        {
                            string comp = sM.Groups[1].Value;
                            int colIdx = comp == "x" ? 0 : (comp == "y" ? 1 : 2);
                            // Diagonal element
                            double diagVal = 0;
                            switch (colIdx)
                            {
                                case 0:
                                    double.TryParse(sM.Groups[2].Value, NumberStyles.Float, Inv, out diagVal);
                                    break;
                                case 1:
                                    double.TryParse(sM.Groups[3].Value, NumberStyles.Float, Inv, out diagVal);
                                    break;
                                case 2:
                                    double.TryParse(sM.Groups[4].Value, NumberStyles.Float, Inv, out diagVal);
                                    break;
                            }
                            traceSum += diagVal;
                            traceCount++;
                        }
                    }
                    if (traceCount == 3)
                    {
                        // Pressure = -1/3 * Tr(stress)
                        result.Pressure_GPa = -(traceSum / 3.0);
                    }
                }

                // --- Band gap ---
                // "HOMO - LUMO gap [eV] :    1.234"
                if (line.Contains("HOMO - LUMO gap") || line.Contains("HOMO-LUMO gap"))
                {
                    var m = Regex.Match(line, @"gap\s*\[eV\]\s*:?\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double gap))
                    {
                        result.BandGap_eV = gap;
                    }
                }
            }

            // --- Post-processing ---

            result.TotalEnergy_eV = lastEnergy;
            result.MaxForce_eV_A = hasForces && maxForce > 0 ? maxForce : double.NaN;
            result.IonSteps = ionSteps > 0 ? ionSteps : 1;
            result.ElectronSteps = electronSteps;
            result.IsConverged = scfConverged;

            // Compute lattice parameters from cell vectors
            if (hasCell)
            {
                var lp = new double[3];
                for (int v = 0; v < 3; v++)
                {
                    if (cellVectors[v] != null)
                    {
                        double x = cellVectors[v][0];
                        double y = cellVectors[v][1];
                        double z = cellVectors[v][2];
                        lp[v] = Math.Sqrt(x * x + y * y + z * z);
                    }
                }
                result.LatticeParameters = lp;

                // Compute volume from cell vectors if not already set
                if (double.IsNaN(result.Volume) && cellVectors[0] != null &&
                    cellVectors[1] != null && cellVectors[2] != null)
                {
                    result.Volume = Math.Abs(
                        cellVectors[0][0] * (cellVectors[1][1] * cellVectors[2][2] - cellVectors[1][2] * cellVectors[2][1]) -
                        cellVectors[0][1] * (cellVectors[1][0] * cellVectors[2][2] - cellVectors[1][2] * cellVectors[2][0]) +
                        cellVectors[0][2] * (cellVectors[1][0] * cellVectors[2][1] - cellVectors[1][1] * cellVectors[2][0])
                    );
                }
            }

            // Set element counts and formula
            if (elementCounts.Count > 0)
            {
                result.ElementCounts = elementCounts;
                result.AtomCount = atomCount;

                var formula = "";
                foreach (var kvp in elementCounts)
                {
                    formula += kvp.Key + (kvp.Value > 1 ? kvp.Value.ToString() : "");
                }
                result.Formula = formula;
            }
            else if (atomCount > 0)
            {
                result.AtomCount = atomCount;
            }

            return result;
        }
    }
}
