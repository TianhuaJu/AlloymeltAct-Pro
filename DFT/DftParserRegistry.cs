using AlloyAct_Pro.DFT.Parsers;

namespace AlloyAct_Pro.DFT
{
    /// <summary>
    /// DFT 解析器注册中心 - 自动检测文件格式并分派给正确的解析器
    /// 遵循 TdbParser 的门面模式
    /// </summary>
    public static class DftParserRegistry
    {
        private static readonly List<IDftParser> _parsers = new()
        {
            new VaspOutcarParser(),
            new VaspXmlParser(),
            new QuantumEspressoParser(),
            new AbinitParser(),
            new Cp2kParser(),
            new CastepParser(),
            new SiestaParser(),
            new Wien2kParser(),
            new FhiAimsParser(),
            new ElkParser(),
            new GpawParser(),
            new FleurParser(),
            new OpenMxParser(),
            new ExcitingParser(),
            new DftbPlusParser()
        };

        /// <summary>
        /// 自动检测文件类型并解析
        /// 遍历所有注册的解析器，找到第一个能处理的
        /// </summary>
        public static DftResult? AutoParse(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在: {filePath}");

            foreach (var parser in _parsers)
            {
                try
                {
                    if (parser.CanParse(filePath))
                    {
                        var result = parser.Parse(filePath);
                        result.SourceFile = filePath;
                        result.SourceSoftware = parser.SoftwareName;
                        result.ComputeDerivedUnits();
                        return result;
                    }
                }
                catch
                {
                    // 该解析器无法处理，尝试下一个
                    continue;
                }
            }

            return null;  // 无法识别
        }

        /// <summary>
        /// 获取所有支持软件的名称列表
        /// </summary>
        public static List<string> GetSupportedSoftware()
        {
            return _parsers.Select(p => p.SoftwareName).ToList();
        }

        /// <summary>
        /// 获取文件对话框过滤器字符串
        /// </summary>
        public static string GetFileFilter()
        {
            var filters = new List<string>
            {
                "所有支持的DFT文件|*.*"
            };

            foreach (var parser in _parsers)
            {
                var patterns = string.Join(";", parser.FilePatterns);
                filters.Add($"{parser.SoftwareName} ({patterns})|{patterns}");
            }

            return string.Join("|", filters);
        }

        /// <summary>
        /// 计算形成能（需要纯元素参考能量）
        /// ΔE_f = E_compound - Σ(x_i * E_i^ref)
        /// </summary>
        /// <param name="result">DFT 计算结果</param>
        /// <param name="referenceEnergies">纯元素参考能量（eV/atom），键为元素符号</param>
        public static void ComputeFormationEnergy(DftResult result, Dictionary<string, double> referenceEnergies)
        {
            if (double.IsNaN(result.EnergyPerAtom_eV) || result.ElementCounts.Count == 0)
                return;

            double refSum = 0;
            int totalAtoms = result.AtomCount;

            foreach (var (element, count) in result.ElementCounts)
            {
                if (!referenceEnergies.TryGetValue(element, out double refEnergy))
                    return;  // 缺少参考能量，无法计算

                refSum += (double)count / totalAtoms * refEnergy;
            }

            result.FormationEnergy_eV_atom = result.EnergyPerAtom_eV - refSum;
            result.MixingEnthalpy_kJ_mol = result.FormationEnergy_eV_atom * DftResult.EV_TO_KJ_PER_MOL;
        }

        /// <summary>
        /// 从文件前 N 行读取内容（用于快速识别）
        /// </summary>
        internal static string ReadHeader(string filePath, int lineCount = 50)
        {
            try
            {
                var lines = new List<string>();
                using var reader = new StreamReader(filePath);
                for (int i = 0; i < lineCount && !reader.EndOfStream; i++)
                {
                    lines.Add(reader.ReadLine() ?? "");
                }
                return string.Join("\n", lines);
            }
            catch
            {
                return "";
            }
        }
    }
}
