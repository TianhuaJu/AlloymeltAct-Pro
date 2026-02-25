using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// CASTEP .castep 文件解析器
    /// 支持 .castep, .out 格式
    /// 提取总能量（eV）、力、晶胞、收敛状态等
    /// </summary>
    public class CastepParser : IDftParser
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public string SoftwareName => "CASTEP";
        public string[] FilePatterns => new[] { "*.castep", "*.out" };
        public string[] SignatureStrings => new[] { "CASTEP", "Materials Studio" };

        public bool CanParse(string filePath)
        {
            // Check file extension first
            var ext = Path.GetExtension(filePath);
            if (string.Equals(ext, ".castep", StringComparison.OrdinalIgnoreCase))
                return true;

            // Content signature check
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
            bool geomConverged = false;
            bool scfConverged = false;

            // Atom tracking
            var elementCounts = new Dictionary<string, int>();
            int atomCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // --- Total Energy ---
                // "Final energy, E              =  -1234.567890123  eV"
                // "Final energy =  -1234.567890123  eV"
                if (line.Contains("Final energy"))
                {
                    var m = Regex.Match(line, @"Final energy[\s,E=]+=\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)\s*eV");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double e))
                    {
                        lastEnergy = e;
                    }
                }

                // Also match "Total energy corrected for finite basis set"
                if (line.Contains("Total energy corrected") && line.Contains("eV"))
                {
                    var m = Regex.Match(line, @"=\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)\s*eV");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double e))
                    {
                        lastEnergy = e;
                    }
                }

                // --- Fermi energy ---
                // "Fermi energy (in  eV) :     5.1234"
                if (line.Contains("Fermi energy"))
                {
                    var m = Regex.Match(line, @"Fermi energy\s*\(?.*?\)?\s*[=:]\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double ef))
                    {
                        result.FermiEnergy_eV = ef;
                    }
                }

                // --- Band gap ---
                // "Band gap     1.234 eV"
                if (line.Contains("Band gap"))
                {
                    var m = Regex.Match(line, @"Band gap\s+([-+]?\d+\.?\d*[Ee]?[+-]?\d*)\s*eV");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double gap))
                    {
                        result.BandGap_eV = gap;
                    }
                }

                // --- Forces ---
                // Look for forces block:
                // " *                         Cartesian Forces (eV/A)                        *"
                // " * -------------------------------------------------------------------- *"
                // " *  Element    Ion           Fx            Fy            Fz               *"
                // " * -------------------------------------------------------------------- *"
                // " *  Fe        1        0.01234      -0.05678       0.03456               *"
                if ((line.Contains("Cartesian Forces") || line.Contains("Forces (eV/A)") ||
                     line.Contains("Cartesian components of force")) &&
                    line.Contains("eV"))
                {
                    hasForces = true;
                    double blockMaxForce = 0;
                    // Skip header/separator lines
                    int j = i + 1;
                    while (j < lines.Length && (lines[j].Contains("---") || lines[j].Contains("Element") ||
                           lines[j].Contains("Ion") || string.IsNullOrWhiteSpace(lines[j].Replace("*", "").Trim())))
                    {
                        j++;
                    }

                    for (; j < lines.Length; j++)
                    {
                        var fLine = lines[j].Trim().Replace("*", "").Trim();
                        if (string.IsNullOrEmpty(fLine) || fLine.StartsWith("---") || fLine.StartsWith("*---"))
                            break;
                        // Parse: "Fe    1    0.01234   -0.05678    0.03456"
                        var parts = fLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5)
                        {
                            try
                            {
                                // Element  Ion  Fx  Fy  Fz
                                double fx = double.Parse(parts[2], Inv);
                                double fy = double.Parse(parts[3], Inv);
                                double fz = double.Parse(parts[4], Inv);
                                double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz);
                                if (fmag > blockMaxForce) blockMaxForce = fmag;
                            }
                            catch { }
                        }
                    }
                    if (blockMaxForce > 0) maxForce = blockMaxForce;
                }

                // --- Convergence ---
                if (line.Contains("Geometry optimization completed successfully"))
                {
                    geomConverged = true;
                }

                // SCF convergence
                if (line.Contains("Total energy has converged"))
                {
                    scfConverged = true;
                }

                // --- SCF cycles ---
                // "SCF loop      Energy"
                // "    1    -1234.56789"
                if (Regex.IsMatch(line, @"^\s+\d+\s+[-+]?\d+\.\d+\s"))
                {
                    var scfM = Regex.Match(line, @"^\s+(\d+)\s+[-+]?\d+\.\d+");
                    if (scfM.Success)
                    {
                        int step = int.Parse(scfM.Groups[1].Value, Inv);
                        if (step > electronSteps) electronSteps = step;
                    }
                }

                // --- Ion steps (BFGS) ---
                // "Starting BFGS iteration     2 ..."
                if (line.Contains("Starting BFGS iteration") || line.Contains("Starting CG iteration") ||
                    line.Contains("Starting LBFGS iteration"))
                {
                    ionSteps++;
                }

                // --- Unit Cell ---
                // "                           Unit Cell"
                // "                       a =    5.4300  alpha =   90.0000"
                // "                       b =    5.4300  beta  =   90.0000"
                // "                       c =    5.4300  gamma =   90.0000"
                if (line.Contains("Unit Cell") && !line.Contains("Contents"))
                {
                    var latticeParams = new double[3];
                    var latticeAngles = new double[3];
                    bool foundLattice = false;

                    for (int j = i + 1; j < Math.Min(i + 20, lines.Length); j++)
                    {
                        var lLine = lines[j];

                        // Match "a =  5.4300  alpha =  90.0000"
                        var aM = Regex.Match(lLine, @"\ba\s*=\s*([\d.]+)\s*alpha\s*=\s*([\d.]+)");
                        if (aM.Success)
                        {
                            double.TryParse(aM.Groups[1].Value, NumberStyles.Float, Inv, out latticeParams[0]);
                            double.TryParse(aM.Groups[2].Value, NumberStyles.Float, Inv, out latticeAngles[0]);
                            foundLattice = true;
                        }
                        var bM = Regex.Match(lLine, @"\bb\s*=\s*([\d.]+)\s*beta\s*=\s*([\d.]+)");
                        if (bM.Success)
                        {
                            double.TryParse(bM.Groups[1].Value, NumberStyles.Float, Inv, out latticeParams[1]);
                            double.TryParse(bM.Groups[2].Value, NumberStyles.Float, Inv, out latticeAngles[1]);
                            foundLattice = true;
                        }
                        var cM = Regex.Match(lLine, @"\bc\s*=\s*([\d.]+)\s*gamma\s*=\s*([\d.]+)");
                        if (cM.Success)
                        {
                            double.TryParse(cM.Groups[1].Value, NumberStyles.Float, Inv, out latticeParams[2]);
                            double.TryParse(cM.Groups[2].Value, NumberStyles.Float, Inv, out latticeAngles[2]);
                            foundLattice = true;
                        }

                        // Also match lattice vectors form:
                        // "    a1 =    5.4300    0.0000    0.0000"
                        var vecM = Regex.Match(lLine, @"a(\d)\s*=\s*([-+]?\d+\.?\d*)\s+([-+]?\d+\.?\d*)\s+([-+]?\d+\.?\d*)");
                        if (vecM.Success)
                        {
                            int idx = int.Parse(vecM.Groups[1].Value) - 1;
                            if (idx >= 0 && idx < 3)
                            {
                                double x = double.Parse(vecM.Groups[2].Value, Inv);
                                double y = double.Parse(vecM.Groups[3].Value, Inv);
                                double z = double.Parse(vecM.Groups[4].Value, Inv);
                                latticeParams[idx] = Math.Sqrt(x * x + y * y + z * z);
                                foundLattice = true;
                            }
                        }
                    }

                    if (foundLattice && latticeParams[0] > 0)
                    {
                        result.LatticeParameters = latticeParams;
                        if (latticeAngles[0] > 0)
                            result.LatticeAngles = latticeAngles;
                    }
                }

                // --- Real Lattice as printed by CASTEP ---
                // "            Real Lattice(A)                  Reciprocal Lattice(1/A)"
                // "  5.4300    0.0000    0.0000        1.1570    0.0000    0.0000"
                if (line.Contains("Real Lattice(A)"))
                {
                    var lp = new double[3];
                    bool valid = true;
                    for (int j = 1; j <= 3 && i + j < lines.Length; j++)
                    {
                        var parts = lines[i + j].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            try
                            {
                                double x = double.Parse(parts[0], Inv);
                                double y = double.Parse(parts[1], Inv);
                                double z = double.Parse(parts[2], Inv);
                                lp[j - 1] = Math.Sqrt(x * x + y * y + z * z);
                            }
                            catch { valid = false; }
                        }
                        else { valid = false; }
                    }
                    if (valid && lp[0] > 0) result.LatticeParameters = lp;
                }

                // --- Volume ---
                // "Current cell volume =   160.1234  A**3"
                // or "Cell volume =  160.1234  A**3"
                if (line.Contains("cell volume") || line.Contains("Cell volume"))
                {
                    var m = Regex.Match(line, @"volume\s*=\s*([\d.]+[Ee]?[+-]?\d*)\s*A", RegexOptions.IgnoreCase);
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double vol))
                    {
                        result.Volume = vol;
                    }
                }

                // --- Cell Contents → atoms ---
                // "                         Cell Contents"
                // "  x------x------x------x------x------x"
                // "  |  Element  Ion    x (frac)  y (frac)  z (frac) |"
                // "  |   Fe       1     0.0000    0.0000    0.0000   |"
                if (line.Contains("Cell Contents"))
                {
                    var tempCounts = new Dictionary<string, int>();
                    int tempAtomCount = 0;
                    bool inBlock = false;

                    for (int j = i + 1; j < Math.Min(i + 500, lines.Length); j++)
                    {
                        var aLine = lines[j].Trim();

                        // Start of atom listing
                        if (aLine.Contains("Element") && aLine.Contains("Ion"))
                        {
                            inBlock = true;
                            continue;
                        }

                        // Separator or end
                        if (inBlock && (aLine.StartsWith("x---") || aLine.StartsWith("x===") ||
                            aLine.StartsWith("----") || aLine.StartsWith("===") ||
                            string.IsNullOrEmpty(aLine)))
                        {
                            if (tempAtomCount > 0) break;
                            continue;
                        }

                        if (inBlock)
                        {
                            // Remove leading/trailing | and *
                            var cleaned = aLine.Replace("|", "").Replace("*", "").Trim();
                            var parts = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                string elem = Regex.Match(parts[0], @"^[A-Z][a-z]?").Value;
                                if (!string.IsNullOrEmpty(elem) && char.IsLetter(elem[0]))
                                {
                                    if (tempCounts.ContainsKey(elem))
                                        tempCounts[elem]++;
                                    else
                                        tempCounts[elem] = 1;
                                    tempAtomCount++;
                                }
                            }
                        }
                    }

                    if (tempAtomCount > 0)
                    {
                        elementCounts = tempCounts;
                        atomCount = tempAtomCount;
                    }
                }

                // --- Also match "Total number of ions in cell =" ---
                if (line.Contains("Total number of ions in cell"))
                {
                    var m = Regex.Match(line, @"=\s*(\d+)");
                    if (m.Success)
                    {
                        int n = int.Parse(m.Groups[1].Value, Inv);
                        if (atomCount == 0) atomCount = n;
                    }
                }

                // --- Species block for atom counting ---
                // "            x  Element    Atom        Fractional coordinates of atoms  x"
                if ((line.Contains("Fractional coordinates of atoms") ||
                     line.Contains("Fractional co-ordinates of atoms")) && atomCount == 0)
                {
                    var tempCounts = new Dictionary<string, int>();
                    int tempAtomCount = 0;

                    for (int j = i + 1; j < Math.Min(i + 500, lines.Length); j++)
                    {
                        var aLine = lines[j].Trim();
                        if (aLine.Contains("---") || string.IsNullOrWhiteSpace(aLine))
                        {
                            if (tempAtomCount > 0) break;
                            continue;
                        }

                        var cleaned = aLine.Replace("|", "").Replace("x", "").Replace("*", "").Trim();
                        var parts = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        // "Fe   1     0.000   0.000   0.000"
                        if (parts.Length >= 4)
                        {
                            string elem = Regex.Match(parts[0], @"^[A-Z][a-z]?").Value;
                            if (!string.IsNullOrEmpty(elem) && char.IsLetter(elem[0]))
                            {
                                if (tempCounts.ContainsKey(elem))
                                    tempCounts[elem]++;
                                else
                                    tempCounts[elem] = 1;
                                tempAtomCount++;
                            }
                        }
                    }

                    if (tempAtomCount > 0)
                    {
                        elementCounts = tempCounts;
                        atomCount = tempAtomCount;
                    }
                }

                // --- K-points ---
                // "MP grid size for SCF calculation is     4   4   4"
                if (line.Contains("MP grid size"))
                {
                    var m = Regex.Match(line, @"MP grid size\s*.*?is\s+(\d+)\s+(\d+)\s+(\d+)");
                    if (m.Success)
                    {
                        result.KPoints = $"{m.Groups[1].Value}x{m.Groups[2].Value}x{m.Groups[3].Value}";
                    }
                }

                // "Number of kpoints used =    12"
                if (line.Contains("Number of kpoints") || line.Contains("Number of k-points"))
                {
                    var m = Regex.Match(line, @"=\s*(\d+)");
                    if (m.Success && string.IsNullOrEmpty(result.KPoints))
                    {
                        result.KPoints = $"{m.Groups[1].Value} k-points";
                    }
                }

                // --- Cutoff energy ---
                // "plane wave basis set cut-off                   :   500.0000   eV"
                if (line.Contains("plane wave basis set cut-off") ||
                    line.Contains("basis set cut-off energy") ||
                    line.Contains("cut-off energy"))
                {
                    var m = Regex.Match(line, @"([\d.]+)\s*eV");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double ecut))
                    {
                        result.EnergyCutoff_eV = ecut;
                    }
                }

                // --- XC functional ---
                // "using functional                 : Perdew Burke Ernzerhof"
                // "type of calculation              : DFT+D"
                if (line.Contains("using functional"))
                {
                    var m = Regex.Match(line, @"using functional\s*:\s*(.+)");
                    if (m.Success)
                    {
                        string funcStr = m.Groups[1].Value.Trim();
                        result.Method = MapCastepFunctional(funcStr);
                    }
                }

                // --- Spin ---
                // "treating system as spin-polarized"
                if (line.Contains("spin-polarized") || line.Contains("spin polarized") ||
                    line.Contains("Spin polarised"))
                {
                    result.SpinPolarized = true;
                }

                // --- Magnetization ---
                // "Integrated Spin Density     =     2.0000"
                if (line.Contains("Integrated Spin Density") || line.Contains("Total Spin"))
                {
                    var m = Regex.Match(line, @"=\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double mag))
                    {
                        result.TotalMagnetization = mag;
                    }
                }

                // --- Pressure / Stress ---
                // "  *  Pressure:      1.2345  GPa          *"
                if (line.Contains("Pressure") && line.Contains("GPa"))
                {
                    var m = Regex.Match(line, @"Pressure\s*[:=]?\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)\s*GPa");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double p))
                    {
                        result.Pressure_GPa = p;
                    }
                }

                // Also parse stress tensor block
                // " *********** Stress Tensor ***********"
                // "  *          x            y            z  *"
                // "  * x    1.234        0.000        0.000  *"
                if (line.Contains("Stress Tensor") && double.IsNaN(result.Pressure_GPa))
                {
                    double traceSum = 0;
                    int traceCount = 0;
                    for (int j = i + 1; j < Math.Min(i + 10, lines.Length); j++)
                    {
                        var sLine = lines[j].Replace("*", "").Trim();
                        var sM = Regex.Match(sLine, @"^([xyz])\s+([-+]?\d+\.?\d*[Ee]?[+-]?\d*)\s+([-+]?\d+\.?\d*[Ee]?[+-]?\d*)\s+([-+]?\d+\.?\d*[Ee]?[+-]?\d*)");
                        if (sM.Success)
                        {
                            string comp = sM.Groups[1].Value;
                            double diagVal;
                            switch (comp)
                            {
                                case "x":
                                    double.TryParse(sM.Groups[2].Value, NumberStyles.Float, Inv, out diagVal);
                                    traceSum += diagVal;
                                    traceCount++;
                                    break;
                                case "y":
                                    double.TryParse(sM.Groups[3].Value, NumberStyles.Float, Inv, out diagVal);
                                    traceSum += diagVal;
                                    traceCount++;
                                    break;
                                case "z":
                                    double.TryParse(sM.Groups[4].Value, NumberStyles.Float, Inv, out diagVal);
                                    traceSum += diagVal;
                                    traceCount++;
                                    break;
                            }
                        }
                    }
                    if (traceCount == 3)
                    {
                        result.Pressure_GPa = -(traceSum / 3.0);
                    }
                }
            }

            // --- Post-processing ---

            result.TotalEnergy_eV = lastEnergy;
            result.MaxForce_eV_A = hasForces && maxForce > 0 ? maxForce : double.NaN;
            result.IonSteps = ionSteps > 0 ? ionSteps : 1;
            result.ElectronSteps = electronSteps;
            result.IsConverged = geomConverged || (scfConverged && ionSteps <= 1);

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

        /// <summary>
        /// Map CASTEP functional description to short name
        /// </summary>
        private static string MapCastepFunctional(string funcStr)
        {
            string upper = funcStr.ToUpperInvariant();

            if (upper.Contains("PBE") && upper.Contains("SOL"))
                return "PBEsol";
            if (upper.Contains("PERDEW") && upper.Contains("BURKE") && upper.Contains("ERNZERHOF"))
                return "PBE";
            if (upper.Contains("PBE0"))
                return "PBE0";
            if (upper.Contains("PBE"))
                return "PBE";
            if (upper.Contains("B3LYP"))
                return "B3LYP";
            if (upper.Contains("BLYP"))
                return "BLYP";
            if (upper.Contains("HSE") || upper.Contains("HEYD"))
                return "HSE06";
            if (upper.Contains("LDA") || upper.Contains("LOCAL DENSITY") ||
                upper.Contains("PERDEW") && upper.Contains("ZUNGER"))
                return "LDA";
            if (upper.Contains("WC") || upper.Contains("WU") && upper.Contains("COHEN"))
                return "WC";
            if (upper.Contains("RPBE") || upper.Contains("REVISED"))
                return "RPBE";

            return funcStr.Trim();
        }
    }
}
