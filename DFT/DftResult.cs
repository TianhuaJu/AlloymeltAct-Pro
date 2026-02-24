namespace AlloyAct_Pro.DFT
{
    /// <summary>
    /// DFT 计算结果统一数据模型
    /// 不同 DFT 软件的输出最终都解析为此格式
    /// </summary>
    public class DftResult
    {
        // ===== 来源信息 =====

        /// <summary>DFT 软件名称（如 VASP, Quantum ESPRESSO, ABINIT 等）</summary>
        public string SourceSoftware { get; set; } = "";

        /// <summary>源文件路径</summary>
        public string SourceFile { get; set; } = "";

        /// <summary>导入时间</summary>
        public DateTime ImportTime { get; set; } = DateTime.Now;

        // ===== 结构信息 =====

        /// <summary>化学式（如 Fe2Al, Si）</summary>
        public string Formula { get; set; } = "";

        /// <summary>体系中原子总数</summary>
        public int AtomCount { get; set; }

        /// <summary>晶格参数 [a, b, c]（Å）</summary>
        public double[] LatticeParameters { get; set; } = Array.Empty<double>();

        /// <summary>晶格角度 [alpha, beta, gamma]（度）</summary>
        public double[] LatticeAngles { get; set; } = Array.Empty<double>();

        /// <summary>晶胞体积（Å³）</summary>
        public double Volume { get; set; } = double.NaN;

        /// <summary>原子位置列表</summary>
        public List<AtomPosition> Positions { get; set; } = new();

        /// <summary>各元素及其原子数</summary>
        public Dictionary<string, int> ElementCounts { get; set; } = new();

        // ===== 能量信息（核心热力学数据） =====

        /// <summary>总能量（eV）</summary>
        public double TotalEnergy_eV { get; set; } = double.NaN;

        /// <summary>总能量（kJ/mol）- 自动换算</summary>
        public double TotalEnergy_kJ_mol { get; set; } = double.NaN;

        /// <summary>每原子能量（eV/atom）</summary>
        public double EnergyPerAtom_eV { get; set; } = double.NaN;

        /// <summary>费米能（eV）</summary>
        public double FermiEnergy_eV { get; set; } = double.NaN;

        /// <summary>带隙（eV），0 表示金属</summary>
        public double BandGap_eV { get; set; } = double.NaN;

        // ===== 力和应力 =====

        /// <summary>最大原子力（eV/Å）</summary>
        public double MaxForce_eV_A { get; set; } = double.NaN;

        /// <summary>应力张量（GPa），3×3 矩阵</summary>
        public double[,]? StressTensor_GPa { get; set; }

        /// <summary>压力（GPa）</summary>
        public double Pressure_GPa { get; set; } = double.NaN;

        // ===== 收敛信息 =====

        /// <summary>计算是否收敛</summary>
        public bool IsConverged { get; set; }

        /// <summary>离子步数</summary>
        public int IonSteps { get; set; }

        /// <summary>电子步数（最后一个离子步）</summary>
        public int ElectronSteps { get; set; }

        // ===== 计算参数 =====

        /// <summary>交换关联泛函（如 PBE, LDA, PBEsol, HSE06）</summary>
        public string Method { get; set; } = "";

        /// <summary>截断能（eV）</summary>
        public double EnergyCutoff_eV { get; set; } = double.NaN;

        /// <summary>K 点信息</summary>
        public string KPoints { get; set; } = "";

        /// <summary>自旋极化</summary>
        public bool SpinPolarized { get; set; }

        /// <summary>总磁矩（μB）</summary>
        public double TotalMagnetization { get; set; } = double.NaN;

        // ===== 声子数据（如果可用） =====

        /// <summary>零点振动能（eV）</summary>
        public double ZeroPointEnergy_eV { get; set; } = double.NaN;

        // ===== 派生热力学量（由 DftParserRegistry 计算） =====

        /// <summary>形成能（eV/atom）- 需要参考能量</summary>
        public double? FormationEnergy_eV_atom { get; set; }

        /// <summary>混合焓（kJ/mol）- 需要参考能量</summary>
        public double? MixingEnthalpy_kJ_mol { get; set; }

        // ===== 单位转换常数 =====

        /// <summary>1 eV = 96.4853 kJ/mol</summary>
        public const double EV_TO_KJ_PER_MOL = 96.4853;

        /// <summary>1 Hartree = 27.2114 eV</summary>
        public const double HARTREE_TO_EV = 27.2114;

        /// <summary>1 Ry = 13.6057 eV</summary>
        public const double RY_TO_EV = 13.6057;

        /// <summary>
        /// 计算每原子能量并转换单位
        /// </summary>
        public void ComputeDerivedUnits()
        {
            if (!double.IsNaN(TotalEnergy_eV) && AtomCount > 0)
            {
                EnergyPerAtom_eV = TotalEnergy_eV / AtomCount;
                TotalEnergy_kJ_mol = TotalEnergy_eV * EV_TO_KJ_PER_MOL;
            }
        }
    }

    /// <summary>
    /// 原子位置
    /// </summary>
    public class AtomPosition
    {
        /// <summary>元素符号</summary>
        public string Element { get; set; } = "";

        /// <summary>分数坐标或笛卡尔坐标 X</summary>
        public double X { get; set; }

        /// <summary>分数坐标或笛卡尔坐标 Y</summary>
        public double Y { get; set; }

        /// <summary>分数坐标或笛卡尔坐标 Z</summary>
        public double Z { get; set; }

        /// <summary>是否为分数坐标（true）或笛卡尔坐标（false）</summary>
        public bool IsFractional { get; set; } = true;
    }
}
