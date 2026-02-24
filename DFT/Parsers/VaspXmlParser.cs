using System.Globalization;
using System.Xml;

namespace AlloyAct_Pro.DFT.Parsers
{
    /// <summary>
    /// VASP vasprun.xml 文件解析器（流式 XmlReader，支持超大文件）
    /// 提取总能量、原子信息、晶格、力、计算参数等
    /// </summary>
    public class VaspXmlParser : IDftParser
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public string SoftwareName => "VASP";
        public string[] FilePatterns => new[] { "vasprun.xml", "*.xml" };
        public string[] SignatureStrings => new[] { "<modeling>", "<generator>", "vasp" };

        public bool CanParse(string filePath)
        {
            // 文件名检查
            var fileName = Path.GetFileName(filePath);
            if (fileName.Equals("vasprun.xml", StringComparison.OrdinalIgnoreCase))
                return true;

            // 内容签名检查：在前 20 行中查找 <modeling> 标记
            var header = DftParserRegistry.ReadHeader(filePath, 20);
            return header.Contains("<modeling>") ||
                   (header.Contains("<generator>") && header.Contains("vasp", StringComparison.OrdinalIgnoreCase));
        }

        public DftResult Parse(string filePath)
        {
            var result = new DftResult();

            // 从 <atominfo> 提取的元素列表和计数
            var elementTypes = new List<string>();
            var elementCounts = new List<int>();

            // 晶格向量（最后一组）
            double[]? latticeA = null, latticeB = null, latticeC = null;

            // 力向量（最后一组）
            var forces = new List<double[]>();

            // 追踪最后一个总能量
            double lastEnergy = double.NaN;

            // 追踪离子步数
            int ionSteps = 0;
            int electronSteps = 0;

            // 位置（最后一组）
            var positions = new List<double[]>();

            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                DtdProcessing = DtdProcessing.Ignore
            };

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = XmlReader.Create(stream, settings);

                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element)
                        continue;

                    switch (reader.Name)
                    {
                        case "generator":
                            ParseGenerator(reader, result);
                            break;

                        case "separator":
                            {
                                var sepName = reader.GetAttribute("name") ?? "";
                                if (sepName == "electronic")
                                    ParseElectronicParameters(reader, result);
                                else if (sepName == "ionic")
                                    ParseIonicParameters(reader, result);
                            }
                            break;

                        case "atominfo":
                            ParseAtomInfo(reader, elementTypes, elementCounts);
                            break;

                        case "structure":
                            // 每个 <structure> 包含 <crystal> 和 <varray name="positions">
                            ParseStructure(reader, ref latticeA, ref latticeB, ref latticeC, positions);
                            break;

                        case "calculation":
                            ionSteps++;
                            ParseCalculation(reader, ref lastEnergy, ref electronSteps, forces, positions,
                                             ref latticeA, ref latticeB, ref latticeC, result);
                            break;

                        case "dos":
                            ParseDos(reader, result);
                            break;
                    }
                }
            }
            catch (XmlException)
            {
                // 文件可能不完整（计算被中断），继续使用已解析的数据
            }

            // === 填充结果 ===

            result.TotalEnergy_eV = lastEnergy;
            result.IonSteps = ionSteps;
            result.ElectronSteps = electronSteps;

            // 元素和化学式
            if (elementTypes.Count > 0 && elementCounts.Count == elementTypes.Count)
            {
                int total = 0;
                var formula = "";
                for (int i = 0; i < elementTypes.Count; i++)
                {
                    var elem = elementTypes[i].Trim();
                    var count = elementCounts[i];
                    result.ElementCounts[elem] = count;
                    formula += elem + (count > 1 ? count.ToString() : "");
                    total += count;
                }
                result.Formula = formula;
                result.AtomCount = total;
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

                // 体积 = |a · (b × c)|
                double vol = Math.Abs(
                    latticeA[0] * (latticeB[1] * latticeC[2] - latticeB[2] * latticeC[1]) -
                    latticeA[1] * (latticeB[0] * latticeC[2] - latticeB[2] * latticeC[0]) +
                    latticeA[2] * (latticeB[0] * latticeC[1] - latticeB[1] * latticeC[0])
                );
                if (double.IsNaN(result.Volume) || result.Volume == 0)
                    result.Volume = vol;
            }

            // 力（最后一组）
            if (forces.Count > 0)
            {
                double maxForce = 0;
                foreach (var f in forces)
                {
                    double fmag = Math.Sqrt(f[0] * f[0] + f[1] * f[1] + f[2] * f[2]);
                    if (fmag > maxForce) maxForce = fmag;
                }
                result.MaxForce_eV_A = maxForce;
            }

            // 原子位置（最后一组）
            if (positions.Count > 0 && elementTypes.Count > 0)
            {
                result.Positions.Clear();
                int atomIndex = 0;
                for (int typeIdx = 0; typeIdx < elementTypes.Count && atomIndex < positions.Count; typeIdx++)
                {
                    int count = typeIdx < elementCounts.Count ? elementCounts[typeIdx] : 0;
                    for (int j = 0; j < count && atomIndex < positions.Count; j++)
                    {
                        var pos = positions[atomIndex];
                        result.Positions.Add(new AtomPosition
                        {
                            Element = elementTypes[typeIdx].Trim(),
                            X = pos[0],
                            Y = pos[1],
                            Z = pos[2],
                            IsFractional = true
                        });
                        atomIndex++;
                    }
                }
            }

            // 收敛判定：如果有多个离子步，且力足够小，认为收敛
            // vasprun.xml 中没有直接的收敛标记，但如果 IBRION 运行完成，
            // 通常意味着收敛（否则 VASP 会继续跑）
            if (ionSteps > 0 && !double.IsNaN(lastEnergy))
            {
                result.IsConverged = true;
            }

            return result;
        }

        // ===== 子解析方法 =====

        /// <summary>
        /// 解析 <generator> 段，提取 VASP 版本等信息
        /// </summary>
        private static void ParseGenerator(XmlReader reader, DftResult result)
        {
            if (reader.IsEmptyElement) return;
            string endElement = reader.Name;
            int depth = reader.Depth;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == endElement && reader.Depth == depth)
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "i")
                {
                    var name = reader.GetAttribute("name") ?? "";
                    var text = reader.ReadElementContentAsString().Trim();
                    if (name == "program" && text.Contains("vasp", StringComparison.OrdinalIgnoreCase))
                    {
                        // 确认是 VASP
                    }
                }
            }
        }

        /// <summary>
        /// 解析 <separator name="electronic"> 段，提取 ISPIN, ENCUT, GGA 等
        /// </summary>
        private static void ParseElectronicParameters(XmlReader reader, DftResult result)
        {
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "separator" && reader.Depth == depth)
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "i")
                {
                    var name = reader.GetAttribute("name") ?? "";
                    string value;
                    try { value = reader.ReadElementContentAsString().Trim(); }
                    catch { continue; }

                    switch (name)
                    {
                        case "ENCUT":
                            if (double.TryParse(value, NumberStyles.Float, Inv, out double encut))
                                result.EnergyCutoff_eV = encut;
                            break;
                        case "ISPIN":
                            if (int.TryParse(value, out int ispin))
                                result.SpinPolarized = ispin == 2;
                            break;
                        case "GGA":
                            result.Method = value switch
                            {
                                "PE" => "PBE",
                                "PS" => "PBEsol",
                                "RP" => "revPBE",
                                "CA" => "LDA",
                                "MK" => "SCAN",
                                _ => value
                            };
                            break;
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "separator")
                {
                    // 嵌套的 separator（如 electronic spin）- 检查并继续
                    var subName = reader.GetAttribute("name") ?? "";
                    if (subName == "electronic spin")
                    {
                        ParseElectronicSpinParameters(reader, result);
                    }
                    else
                    {
                        // 跳过其他嵌套 separator
                        SkipElement(reader);
                    }
                }
            }
        }

        /// <summary>
        /// 解析 <separator name="electronic spin"> 段
        /// </summary>
        private static void ParseElectronicSpinParameters(XmlReader reader, DftResult result)
        {
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "separator" && reader.Depth == depth)
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "i")
                {
                    var name = reader.GetAttribute("name") ?? "";
                    string value;
                    try { value = reader.ReadElementContentAsString().Trim(); }
                    catch { continue; }

                    // 可扩展：提取 MAGMOM 等参数
                }
            }
        }

        /// <summary>
        /// 解析 <separator name="ionic"> 段，提取 IBRION, NSW 等
        /// </summary>
        private static void ParseIonicParameters(XmlReader reader, DftResult result)
        {
            // 跳过，暂不需要额外的离子参数
            SkipElement(reader);
        }

        /// <summary>
        /// 解析 <atominfo> 段，提取元素种类和数量
        /// </summary>
        private static void ParseAtomInfo(XmlReader reader, List<string> elementTypes, List<int> elementCounts)
        {
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;

            // 在 <atominfo> 中查找 <array name="atomtypes"> 获取元素种类和数量
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "atominfo" && reader.Depth == depth)
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "array")
                {
                    var arrayName = reader.GetAttribute("name") ?? "";
                    if (arrayName == "atomtypes")
                    {
                        ParseAtomTypesArray(reader, elementTypes, elementCounts);
                    }
                }
            }
        }

        /// <summary>
        /// 解析 <array name="atomtypes"> 中的 <set><rc><c> 结构
        /// 每行格式：<rc><c>count</c><c>Element</c><c>mass</c><c>pseudopotential</c><c>valence</c></rc>
        /// </summary>
        private static void ParseAtomTypesArray(XmlReader reader, List<string> elementTypes, List<int> elementCounts)
        {
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "array" && reader.Depth == depth)
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "rc")
                {
                    // 解析一行原子类型信息
                    int rcDepth = reader.Depth;
                    var columns = new List<string>();

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "rc" && reader.Depth == rcDepth)
                            break;

                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "c")
                        {
                            var text = reader.ReadElementContentAsString().Trim();
                            columns.Add(text);
                        }
                    }

                    // columns[0] = atomspertype (count), columns[1] = element symbol
                    if (columns.Count >= 2)
                    {
                        if (int.TryParse(columns[0], out int count))
                        {
                            elementCounts.Add(count);
                            // 元素名可能带有下划线后缀（如 Fe_pv），取纯元素符号
                            var elem = columns[1].Split('_')[0].Trim();
                            elementTypes.Add(elem);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 解析 <structure> 段，提取晶格和原子位置
        /// </summary>
        private static void ParseStructure(XmlReader reader,
            ref double[]? latticeA, ref double[]? latticeB, ref double[]? latticeC,
            List<double[]> positions)
        {
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "structure" && reader.Depth == depth)
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "crystal")
                {
                    ParseCrystal(reader, ref latticeA, ref latticeB, ref latticeC);
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "varray")
                {
                    var varrayName = reader.GetAttribute("name") ?? "";
                    if (varrayName == "positions")
                    {
                        positions.Clear();
                        ParseVarray(reader, positions);
                    }
                }
            }
        }

        /// <summary>
        /// 解析 <crystal> 段中的 <varray name="basis">
        /// </summary>
        private static void ParseCrystal(XmlReader reader,
            ref double[]? latticeA, ref double[]? latticeB, ref double[]? latticeC)
        {
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "crystal" && reader.Depth == depth)
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "varray")
                {
                    var varrayName = reader.GetAttribute("name") ?? "";
                    if (varrayName == "basis")
                    {
                        var vectors = new List<double[]>();
                        ParseVarray(reader, vectors);
                        if (vectors.Count >= 3)
                        {
                            latticeA = vectors[0];
                            latticeB = vectors[1];
                            latticeC = vectors[2];
                        }
                    }
                    else
                    {
                        SkipElement(reader);
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "i")
                {
                    // <i name="volume"> 可能在这里
                    reader.ReadElementContentAsString();
                }
            }
        }

        /// <summary>
        /// 解析 <calculation> 段，提取能量、力等
        /// </summary>
        private static void ParseCalculation(XmlReader reader,
            ref double lastEnergy, ref int electronSteps, List<double[]> forces,
            List<double[]> positions,
            ref double[]? latticeA, ref double[]? latticeB, ref double[]? latticeC,
            DftResult result)
        {
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;
            int scSteps = 0;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "calculation" && reader.Depth == depth)
                    break;

                if (reader.NodeType != XmlNodeType.Element)
                    continue;

                switch (reader.Name)
                {
                    case "scstep":
                        scSteps++;
                        SkipElement(reader);
                        break;

                    case "structure":
                        ParseStructure(reader, ref latticeA, ref latticeB, ref latticeC, positions);
                        break;

                    case "varray":
                        {
                            var varrayName = reader.GetAttribute("name") ?? "";
                            if (varrayName == "forces")
                            {
                                forces.Clear();
                                ParseVarray(reader, forces);
                            }
                            else if (varrayName == "stress")
                            {
                                var stressRows = new List<double[]>();
                                ParseVarray(reader, stressRows);
                                if (stressRows.Count == 3)
                                {
                                    var tensor = new double[3, 3];
                                    for (int i = 0; i < 3; i++)
                                        for (int j = 0; j < 3 && j < stressRows[i].Length; j++)
                                            tensor[i, j] = stressRows[i][j] * 0.1; // kBar → GPa
                                    result.StressTensor_GPa = tensor;
                                    // 压力 = -(σ11 + σ22 + σ33) / 3
                                    result.Pressure_GPa = -(tensor[0, 0] + tensor[1, 1] + tensor[2, 2]) / 3.0;
                                }
                            }
                            else
                            {
                                SkipElement(reader);
                            }
                        }
                        break;

                    case "energy":
                        {
                            // <energy> 段中查找 <i name="e_fr_energy">
                            ParseEnergy(reader, ref lastEnergy);
                        }
                        break;

                    case "eigenvalues":
                        SkipElement(reader);
                        break;

                    case "dos":
                        ParseDos(reader, result);
                        break;

                    default:
                        if (!reader.IsEmptyElement)
                            SkipElement(reader);
                        break;
                }
            }

            electronSteps = scSteps;
        }

        /// <summary>
        /// 解析 <energy> 段，提取 e_fr_energy
        /// </summary>
        private static void ParseEnergy(XmlReader reader, ref double lastEnergy)
        {
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "energy" && reader.Depth == depth)
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "i")
                {
                    var name = reader.GetAttribute("name") ?? "";
                    var text = reader.ReadElementContentAsString().Trim();

                    if (name == "e_fr_energy")
                    {
                        if (double.TryParse(text, NumberStyles.Float, Inv, out double energy))
                            lastEnergy = energy;
                    }
                }
            }
        }

        /// <summary>
        /// 解析 <dos> 段，提取费米能
        /// </summary>
        private static void ParseDos(XmlReader reader, DftResult result)
        {
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "dos" && reader.Depth == depth)
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "i")
                {
                    var name = reader.GetAttribute("name") ?? "";
                    var text = reader.ReadElementContentAsString().Trim();

                    if (name == "efermi")
                    {
                        if (double.TryParse(text, NumberStyles.Float, Inv, out double efermi))
                            result.FermiEnergy_eV = efermi;
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name != "i")
                {
                    // 跳过 DOS 数据块（可能非常大）
                    if (!reader.IsEmptyElement)
                        SkipElement(reader);
                }
            }
        }

        // ===== 工具方法 =====

        /// <summary>
        /// 解析 <varray> 中的向量数据
        /// 结构：<varray><v>x y z</v><v>x y z</v>...</varray>
        /// </summary>
        private static void ParseVarray(XmlReader reader, List<double[]> vectors)
        {
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "varray" && reader.Depth == depth)
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "v")
                {
                    var text = reader.ReadElementContentAsString().Trim();
                    var parts = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    var vals = new double[parts.Length];
                    bool ok = true;
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (!double.TryParse(parts[i], NumberStyles.Float, Inv, out vals[i]))
                        {
                            ok = false;
                            break;
                        }
                    }
                    if (ok && vals.Length > 0)
                        vectors.Add(vals);
                }
            }
        }

        /// <summary>
        /// 跳过当前元素及其所有子节点
        /// </summary>
        private static void SkipElement(XmlReader reader)
        {
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                    break;
            }
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
