using System.Globalization;
using System.Text.RegularExpressions;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// ABINIT 输出文件解析器
    /// 支持 .out, .abo, .log 格式
    /// 提取总能量（Hartree→eV）、力、应力、收敛状态等
    /// </summary>
    public class AbinitParser : IDftParser
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // Bohr to Angstrom conversion
        private const double BOHR_TO_ANG = 0.529177249;
        // Hartree/Bohr to eV/Angstrom conversion
        private const double HA_BOHR_TO_EV_ANG = DftResult.HARTREE_TO_EV / BOHR_TO_ANG;
        // Atomic unit of pressure (Hartree/Bohr^3) to GPa
        private const double HA_BOHR3_TO_GPA = DftResult.HARTREE_TO_EV / (BOHR_TO_ANG * BOHR_TO_ANG * BOHR_TO_ANG) * 160.21766208;

        public string SoftwareName => "ABINIT";
        public string[] FilePatterns => new[] { "*.out", "*.abo", "*.log" };
        public string[] SignatureStrings => new[] { ".Version", "ABINIT", "--- !DIFFCODE" };

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
            int ionSteps = 0;
            int electronSteps = 0;
            bool scfConverged = false;

            // Atom info
            int natom = 0;
            int[] typat = Array.Empty<int>();
            double[] znucl = Array.Empty<double>();

            // Lattice vectors for volume/lattice parameter calculation
            double[][] rprim = new double[3][];
            bool hasRprim = false;
            double[] acell = new double[] { 1.0, 1.0, 1.0 }; // default in Bohr

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // --- Total Energy ---
                // "Etotal=  -1.23456789E+01"  or  "etotal    -1.23456789E+01"
                if (line.TrimStart().StartsWith("Etotal", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Total energy (Hartree)"))
                {
                    var m = Regex.Match(line, @"[-+]?\d+\.\d+[Ee]?[+-]?\d*");
                    if (m.Success && double.TryParse(m.Value, NumberStyles.Float, Inv, out double eHa))
                    {
                        lastEnergy = eHa * DftResult.HARTREE_TO_EV;
                    }
                }

                // Also match "etotal" in YAML output blocks
                if (Regex.IsMatch(line, @"^\s*etotal\s+", RegexOptions.IgnoreCase))
                {
                    var m = Regex.Match(line, @"etotal\s+([-+]?\d+\.\d+[Ee]?[+-]?\d*)", RegexOptions.IgnoreCase);
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double eHa))
                    {
                        lastEnergy = eHa * DftResult.HARTREE_TO_EV;
                    }
                }

                // --- Forces (eV/Angstrom) ---
                if (line.Contains("cartesian forces (eV/Angstrom)"))
                {
                    // Lines after this contain: atom_index  fx  fy  fz
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        var fLine = lines[j].Trim();
                        if (string.IsNullOrEmpty(fLine) || !char.IsDigit(fLine[0]))
                            break;
                        var parts = fLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4)
                        {
                            try
                            {
                                double fx = double.Parse(parts[1], Inv);
                                double fy = double.Parse(parts[2], Inv);
                                double fz = double.Parse(parts[3], Inv);
                                double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz);
                                if (fmag > maxForce) maxForce = fmag;
                            }
                            catch { }
                        }
                    }
                }

                // --- Forces (Hartree/Bohr) ---
                if (line.Contains("Cartesian forces (hartree/bohr)") ||
                    line.Contains("cartesian forces (hartree/bohr)"))
                {
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        var fLine = lines[j].Trim();
                        if (string.IsNullOrEmpty(fLine) || !char.IsDigit(fLine[0]))
                            break;
                        var parts = fLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4)
                        {
                            try
                            {
                                double fx = double.Parse(parts[1], Inv) * HA_BOHR_TO_EV_ANG;
                                double fy = double.Parse(parts[2], Inv) * HA_BOHR_TO_EV_ANG;
                                double fz = double.Parse(parts[3], Inv) * HA_BOHR_TO_EV_ANG;
                                double fmag = Math.Sqrt(fx * fx + fy * fy + fz * fz);
                                if (fmag > maxForce) maxForce = fmag;
                            }
                            catch { }
                        }
                    }
                }

                // --- Convergence ---
                if (line.Contains("At Broyd/Pulay step"))
                {
                    ionSteps++;
                }

                if (line.Contains("SCF cycle converged") || line.Contains("scf_history") ||
                    line.Contains("Calculation completed"))
                {
                    scfConverged = true;
                }

                // "nstep" reached → check if converged
                if (line.Contains("converged after") || line.Contains("is_converged"))
                {
                    scfConverged = true;
                }

                // --- Number of atoms ---
                // "natom      4" or "natom  4"
                if (Regex.IsMatch(line, @"^\s*natom\s+\d+"))
                {
                    var m = Regex.Match(line, @"natom\s+(\d+)");
                    if (m.Success) natom = int.Parse(m.Groups[1].Value, Inv);
                }

                // --- typat (atom type indices) ---
                // "typat  1 1 2 2"
                if (Regex.IsMatch(line, @"^\s*typat\s"))
                {
                    var nums = new List<int>();
                    // typat can span multiple lines
                    var tLine = line;
                    while (true)
                    {
                        var matches = Regex.Matches(tLine, @"\d+");
                        bool first = tLine.TrimStart().StartsWith("typat");
                        foreach (Match mm in matches)
                        {
                            if (first && mm.Index == Regex.Match(tLine, @"\d+").Index &&
                                tLine.Substring(0, mm.Index).Contains("typat"))
                            {
                                // This is the first number after "typat"
                            }
                            nums.Add(int.Parse(mm.Value));
                        }
                        // Remove the "typat" keyword count if accidentally included
                        // Actually, just parse all integers on the line after "typat"
                        break;
                    }
                    // Re-parse more carefully
                    nums.Clear();
                    var typatStr = Regex.Replace(line, @"^\s*typat\s+", "");
                    // Continue reading if typat spans multiple lines
                    int nextLine = i + 1;
                    while (nextLine < lines.Length && !string.IsNullOrWhiteSpace(lines[nextLine]) &&
                           !Regex.IsMatch(lines[nextLine], @"^\s*[a-zA-Z]"))
                    {
                        typatStr += " " + lines[nextLine].Trim();
                        nextLine++;
                    }
                    foreach (Match mm in Regex.Matches(typatStr, @"\d+"))
                    {
                        nums.Add(int.Parse(mm.Value));
                    }
                    typat = nums.ToArray();
                }

                // --- znucl (nuclear charges = atomic numbers) ---
                // "znucl  26.00  13.00"
                if (Regex.IsMatch(line, @"^\s*znucl\s"))
                {
                    var zList = new List<double>();
                    var znuclStr = Regex.Replace(line, @"^\s*znucl\s+", "");
                    foreach (Match mm in Regex.Matches(znuclStr, @"[\d.]+"))
                    {
                        if (double.TryParse(mm.Value, NumberStyles.Float, Inv, out double z))
                            zList.Add(z);
                    }
                    znucl = zList.ToArray();
                }

                // --- K-points ---
                // "nkpt     12"
                if (Regex.IsMatch(line, @"^\s*nkpt\s+\d+"))
                {
                    var m = Regex.Match(line, @"nkpt\s+(\d+)");
                    if (m.Success) result.KPoints = $"{m.Groups[1].Value} k-points";
                }

                // --- Cutoff energy ---
                // "ecut   30.0000" (in Hartree)
                if (Regex.IsMatch(line, @"^\s*ecut\s"))
                {
                    var m = Regex.Match(line, @"ecut\s+([\d.]+[Ee]?[+-]?\d*)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double ecutHa))
                    {
                        result.EnergyCutoff_eV = ecutHa * DftResult.HARTREE_TO_EV;
                    }
                }

                // --- XC functional ---
                // "ixc    11" or "ixc   -101130"
                if (Regex.IsMatch(line, @"^\s*ixc\s"))
                {
                    var m = Regex.Match(line, @"ixc\s+([-\d]+)");
                    if (m.Success)
                    {
                        int ixc = int.Parse(m.Groups[1].Value, Inv);
                        result.Method = MapIxcToFunctional(ixc);
                    }
                }

                // --- Spin polarization ---
                // "nsppol  2"
                if (Regex.IsMatch(line, @"^\s*nsppol\s+2"))
                {
                    result.SpinPolarized = true;
                }

                // --- Total magnetization ---
                if (line.Contains("total magnetization"))
                {
                    var m = Regex.Match(line, @"total magnetization\s*[=:]\s*([-+]?\d+\.?\d*)", RegexOptions.IgnoreCase);
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double mag))
                    {
                        result.TotalMagnetization = mag;
                    }
                }

                // --- Fermi energy ---
                // "Fermi energy  :     0.12345 Ha" or "Fermi (or HOMO) energy (eV) =   3.456"
                if (line.Contains("Fermi") && (line.Contains("energy") || line.Contains("HOMO")))
                {
                    // Try eV first
                    var mEv = Regex.Match(line, @"\(eV\)\s*=\s*([-+]?\d+\.?\d+[Ee]?[+-]?\d*)");
                    if (mEv.Success && double.TryParse(mEv.Groups[1].Value, NumberStyles.Float, Inv, out double ef))
                    {
                        result.FermiEnergy_eV = ef;
                    }
                    else
                    {
                        // Try Hartree
                        var mHa = Regex.Match(line, @"[-+]?\d+\.\d+[Ee]?[+-]?\d*");
                        if (mHa.Success && line.Contains("Ha") &&
                            double.TryParse(mHa.Value, NumberStyles.Float, Inv, out double efHa))
                        {
                            result.FermiEnergy_eV = efHa * DftResult.HARTREE_TO_EV;
                        }
                    }
                }

                // --- Band gap ---
                if (line.Contains("Band gap") || line.Contains("bandgap"))
                {
                    var m = Regex.Match(line, @"(\d+\.?\d*[Ee]?[+-]?\d*)\s*(?:eV|Ha)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double gap))
                    {
                        if (line.Contains("Ha"))
                            result.BandGap_eV = gap * DftResult.HARTREE_TO_EV;
                        else
                            result.BandGap_eV = gap;
                    }
                }

                // --- Lattice vectors (acell) ---
                // "acell    1.0 1.0 1.0 Bohr" or Angstrom
                if (Regex.IsMatch(line, @"^\s*acell\s"))
                {
                    var matches = Regex.Matches(line, @"[-+]?\d+\.?\d*[Ee]?[+-]?\d*");
                    if (matches.Count >= 3)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            if (double.TryParse(matches[k].Value, NumberStyles.Float, Inv, out double v))
                                acell[k] = v;
                        }
                        // If in Angstrom, convert to Bohr for consistency with rprim
                        if (line.Contains("Angstr", StringComparison.OrdinalIgnoreCase))
                        {
                            for (int k = 0; k < 3; k++)
                                acell[k] /= BOHR_TO_ANG;
                        }
                    }
                }

                // --- rprim (primitive vectors in reduced coordinates) ---
                // "rprim   1.0 0.0 0.0 ..."
                if (Regex.IsMatch(line, @"^\s*rprim\s"))
                {
                    var allNums = new List<double>();
                    var rprimStr = Regex.Replace(line, @"^\s*rprim\s+", "");
                    int nextL = i + 1;
                    while (allNums.Count < 9 && nextL < lines.Length)
                    {
                        foreach (Match mm in Regex.Matches(rprimStr, @"[-+]?\d+\.?\d*[Ee]?[+-]?\d*"))
                        {
                            if (double.TryParse(mm.Value, NumberStyles.Float, Inv, out double v))
                                allNums.Add(v);
                        }
                        if (allNums.Count < 9)
                        {
                            rprimStr = lines[nextL].Trim();
                            nextL++;
                        }
                    }
                    if (allNums.Count >= 9)
                    {
                        rprim[0] = new[] { allNums[0], allNums[1], allNums[2] };
                        rprim[1] = new[] { allNums[3], allNums[4], allNums[5] };
                        rprim[2] = new[] { allNums[6], allNums[7], allNums[8] };
                        hasRprim = true;
                    }
                }

                // --- Real-space lattice vectors printed in output ---
                // "Real(R)+Coverage lattice vectors" followed by vectors
                if (line.Contains("R(1)=") || line.Contains("R(2)=") || line.Contains("R(3)="))
                {
                    var m = Regex.Match(line, @"R\((\d)\)=\s*([-+]?\d+\.?\d*)\s+([-+]?\d+\.?\d*)\s+([-+]?\d+\.?\d*)");
                    if (m.Success)
                    {
                        int idx = int.Parse(m.Groups[1].Value) - 1;
                        if (idx >= 0 && idx < 3)
                        {
                            double x = double.Parse(m.Groups[2].Value, Inv) * BOHR_TO_ANG;
                            double y = double.Parse(m.Groups[3].Value, Inv) * BOHR_TO_ANG;
                            double z = double.Parse(m.Groups[4].Value, Inv) * BOHR_TO_ANG;
                            double len = Math.Sqrt(x * x + y * y + z * z);
                            if (result.LatticeParameters.Length < 3)
                                result.LatticeParameters = new double[3];
                            result.LatticeParameters[idx] = len;
                        }
                    }
                }

                // --- Stress tensor (Hartree/Bohr^3) → pressure ---
                if (line.Contains("Cartesian components of stress tensor") ||
                    line.Contains("cartesian components of stress tensor"))
                {
                    // Next lines: "- sigma(1 1)=  ..."
                    double sigmaSum = 0;
                    int sigmaCount = 0;
                    for (int j = i + 1; j < Math.Min(i + 10, lines.Length); j++)
                    {
                        // "- sigma(1 1)=  1.23456789E-04"
                        var sm = Regex.Match(lines[j], @"sigma\((\d)\s+\1\)\s*=\s*([-+]?\d+\.\d+[Ee]?[+-]?\d*)");
                        if (sm.Success && double.TryParse(sm.Groups[2].Value, NumberStyles.Float, Inv, out double sig))
                        {
                            sigmaSum += sig;
                            sigmaCount++;
                        }
                    }
                    if (sigmaCount == 3)
                    {
                        // Pressure = -1/3 * Tr(sigma), convert from Ha/Bohr^3 to GPa
                        result.Pressure_GPa = -(sigmaSum / 3.0) * HA_BOHR3_TO_GPA;
                    }
                }

                // --- Electron steps ---
                // "ETOT  5  -1.234567890E+02" lines count SCF iterations
                if (Regex.IsMatch(line, @"^\s*ETOT\s+\d+"))
                {
                    var m = Regex.Match(line, @"ETOT\s+(\d+)");
                    if (m.Success)
                    {
                        int step = int.Parse(m.Groups[1].Value, Inv);
                        if (step > electronSteps) electronSteps = step;
                    }
                }

                // --- Volume from output ---
                // "Unit cell volume ucvol=  1.23456E+03 bohr^3"
                if (line.Contains("ucvol"))
                {
                    var m = Regex.Match(line, @"ucvol\s*=?\s*([-+]?\d+\.?\d*[Ee]?[+-]?\d*)");
                    if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, Inv, out double vol))
                    {
                        // Convert Bohr^3 to Angstrom^3
                        result.Volume = vol * BOHR_TO_ANG * BOHR_TO_ANG * BOHR_TO_ANG;
                    }
                }
            }

            // --- Post-processing ---

            result.TotalEnergy_eV = lastEnergy;
            result.MaxForce_eV_A = maxForce > 0 ? maxForce : double.NaN;
            result.IonSteps = ionSteps > 0 ? ionSteps : 1;
            result.ElectronSteps = electronSteps;
            result.IsConverged = scfConverged;

            // Compute lattice parameters from acell + rprim if not already set
            if (result.LatticeParameters.Length == 0 && hasRprim)
            {
                var lp = new double[3];
                for (int v = 0; v < 3; v++)
                {
                    double x = acell[v] * rprim[v][0] * BOHR_TO_ANG;
                    double y = acell[v] * rprim[v][1] * BOHR_TO_ANG;
                    double z = acell[v] * rprim[v][2] * BOHR_TO_ANG;
                    lp[v] = Math.Sqrt(x * x + y * y + z * z);
                }
                result.LatticeParameters = lp;
            }

            // Build element counts and formula from znucl + typat + natom
            if (znucl.Length > 0 && typat.Length > 0)
            {
                var elementCounts = new Dictionary<string, int>();
                foreach (int t in typat)
                {
                    if (t >= 1 && t <= znucl.Length)
                    {
                        string elem = ZToSymbol((int)Math.Round(znucl[t - 1]));
                        if (elementCounts.ContainsKey(elem))
                            elementCounts[elem]++;
                        else
                            elementCounts[elem] = 1;
                    }
                }
                result.ElementCounts = elementCounts;
                result.AtomCount = natom > 0 ? natom : typat.Length;

                // Build formula
                var formula = "";
                foreach (var kvp in elementCounts)
                {
                    formula += kvp.Key + (kvp.Value > 1 ? kvp.Value.ToString() : "");
                }
                result.Formula = formula;
            }
            else if (natom > 0)
            {
                result.AtomCount = natom;
            }

            return result;
        }

        /// <summary>
        /// Map ABINIT ixc parameter to functional name
        /// </summary>
        private static string MapIxcToFunctional(int ixc)
        {
            return ixc switch
            {
                1 => "LDA (Teter93)",
                2 => "LDA (PZ)",
                7 => "LDA (PW92)",
                11 => "PBE",
                14 => "revPBE",
                15 => "RPBE",
                -101130 or -116133 => "PBE (LibXC)",
                -106131 => "PBEsol (LibXC)",
                _ => $"ixc={ixc}"
            };
        }

        /// <summary>
        /// Convert atomic number Z to element symbol
        /// </summary>
        private static string ZToSymbol(int z)
        {
            var symbols = new[]
            {
                "", "H", "He", "Li", "Be", "B", "C", "N", "O", "F", "Ne",
                "Na", "Mg", "Al", "Si", "P", "S", "Cl", "Ar",
                "K", "Ca", "Sc", "Ti", "V", "Cr", "Mn", "Fe", "Co", "Ni", "Cu", "Zn",
                "Ga", "Ge", "As", "Se", "Br", "Kr",
                "Rb", "Sr", "Y", "Zr", "Nb", "Mo", "Tc", "Ru", "Rh", "Pd", "Ag", "Cd",
                "In", "Sn", "Sb", "Te", "I", "Xe",
                "Cs", "Ba", "La", "Ce", "Pr", "Nd", "Pm", "Sm", "Eu", "Gd", "Tb", "Dy",
                "Ho", "Er", "Tm", "Yb", "Lu",
                "Hf", "Ta", "W", "Re", "Os", "Ir", "Pt", "Au", "Hg",
                "Tl", "Pb", "Bi", "Po", "At", "Rn",
                "Fr", "Ra", "Ac", "Th", "Pa", "U", "Np", "Pu", "Am", "Cm", "Bk", "Cf",
                "Es", "Fm", "Md", "No", "Lr",
                "Rf", "Db", "Sg", "Bh", "Hs", "Mt", "Ds", "Rg", "Cn",
                "Nh", "Fl", "Mc", "Lv", "Ts", "Og"
            };
            if (z >= 1 && z < symbols.Length) return symbols[z];
            return $"Z{z}";
        }
    }
}
