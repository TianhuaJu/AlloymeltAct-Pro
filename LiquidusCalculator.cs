namespace AlloyAct_Pro
{
    /// <summary>
    /// 液相线温度计算器
    /// 基于 Schroder-van Laar 方程，结合 Wagner/Pelton/Elliot 活度模型，
    /// 预测多组元合金熔体的液相线温度。
    /// </summary>
    class LiquidusCalculator
    {
        private const double R = constant.R; // 8.314 J/(mol·K)
        private const int MaxIterations = 100;
        private const double ConvergenceTol = 0.01; // K

        /// <summary>
        /// 液相线计算结果
        /// </summary>
        internal class LiquidusResult
        {
            public string MatrixElement { get; set; }
            public string Composition { get; set; }
            public double T_pure_melting { get; set; }
            public double DeltaHf { get; set; }
            public double T_liquidus_Wagner { get; set; }
            public double T_liquidus_Pelton { get; set; }
            public double T_liquidus_Elliot { get; set; }
            public double DeltaT_Wagner { get; set; }
            public double DeltaT_Pelton { get; set; }
            public double DeltaT_Elliot { get; set; }
            public double SolventActivity_Wagner { get; set; }
            public double SolventActivity_Pelton { get; set; }
            public double SolventActivity_Elliot { get; set; }
            public bool Converged { get; set; }
            public string ErrorMessage { get; set; }
        }

        /// <summary>
        /// 获取元素的熔化焓 (kJ/mol)
        /// 从 SGTE unary TDB 数据库计算（通过 Gibbs 函数数值微分）。
        /// ΔHf = H_liq(Tm) - H_solid(Tm) = Tm × (S_liq - S_solid)
        /// 若 TDB 数据不可用，返回 NaN，由调用方弹出对话框让用户输入。
        /// </summary>
        public static double GetFusionEnthalpy(string elementName, double Tm = 0)
        {
            try
            {
                var tdb = TdbParser.Instance;
                if (tdb.IsLoaded && tdb.HasLiquidData(elementName) && tdb.HasSolidData(elementName))
                {
                    double deltaHf_kJ = tdb.CalcFusionEnthalpy_kJ(elementName, Tm);
                    if (!double.IsNaN(deltaHf_kJ) && deltaHf_kJ > 0)
                        return deltaHf_kJ;
                }
            }
            catch
            {
                // TDB 解析失败
            }

            return double.NaN;
        }

        /// <summary>
        /// 解析用户输入的 ΔHf 表达式。
        /// 支持纯数字（如 "13.81"）和含温度的表达式（如 "-1500/T+2.5"，即 a/T+b 格式）。
        /// </summary>
        /// <param name="input">用户输入的字符串</param>
        /// <param name="T">用于求值的温度 (K)</param>
        /// <returns>求值结果(kJ/mol)，无效则返回 NaN</returns>
        public static double ParseDeltaHfExpression(string input, double T)
        {
            if (string.IsNullOrWhiteSpace(input))
                return double.NaN;

            input = input.Trim();

            // 先尝试纯数字
            if (double.TryParse(input, out double plainValue))
                return plainValue;

            // 尝试解析 a/T+b 或 a/T-b 格式
            // 匹配: 可选负号+数字 / T 可选(+或-数字)
            var re = new System.Text.RegularExpressions.Regex(
                @"^([-]?\d*\.?\d+)\s*/\s*T\s*(([\+\-])\s*(\d*\.?\d+))?\s*$");
            var match = re.Match(input);
            if (match.Success)
            {
                double a = double.Parse(match.Groups[1].Value);
                double b = 0;
                if (!string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    b = double.Parse(match.Groups[4].Value);
                    if (match.Groups[3].Value == "-")
                        b = -b;
                }
                if (T > 0)
                    return a / T + b;
            }

            // 尝试解析 a*T+b 或 a*T-b 格式
            var reMultT = new System.Text.RegularExpressions.Regex(
                @"^([-]?\d*\.?\d+)\s*\*\s*T\s*(([\+\-])\s*(\d*\.?\d+))?\s*$");
            var matchMultT = reMultT.Match(input);
            if (matchMultT.Success)
            {
                double a = double.Parse(matchMultT.Groups[1].Value);
                double b = 0;
                if (!string.IsNullOrEmpty(matchMultT.Groups[2].Value))
                {
                    b = double.Parse(matchMultT.Groups[4].Value);
                    if (matchMultT.Groups[3].Value == "-")
                        b = -b;
                }
                return a * T + b;
            }

            return double.NaN;
        }

        /// <summary>
        /// 参考态转换：将固相参考态的热力学量转换为液相参考态。
        /// 当溶质的数据库提供的是固态形成能（如 Miedema 模型基于固态参考），
        /// 而溶质实际溶于液态合金时，需要通过 Gibbs 熔化自由能进行修正。
        ///
        /// 线性近似: ΔG_fusion(T) ≈ ΔHf × (1 - T/Tm)
        /// 其中 ΔHf 为纯组元的熔化焓（J/mol），Tm 为纯组元的熔点（K）。
        ///
        /// 对于 ln(γ) 的修正: Δ(lnγ) = ΔG_fusion / (R·T) = ΔHf/(R·T) × (1 - T/Tm)
        /// </summary>
        /// <param name="deltaHf_J">纯组元的熔化焓 (J/mol)</param>
        /// <param name="Tm">纯组元的熔点 (K)</param>
        /// <param name="T">当前温度 (K)</param>
        /// <returns>ΔG_fusion(T) in J/mol（正值表示固态比液态更稳定，即 T < Tm）</returns>
        public static double ConvertReferenceState(double deltaHf_J, double Tm, double T)
        {
            if (Tm <= 0 || double.IsNaN(deltaHf_J) || double.IsNaN(T) || T <= 0)
                return 0;
            // ΔG_{L→S}(T) = -ΔHf × (1 - T/Tm)
            // 即 ΔG_fusion = ΔHf × (1 - T/Tm)（熔化方向，S→L）
            return deltaHf_J * (1.0 - T / Tm);
        }

        /// <summary>
        /// 计算多组元合金的液相线温度
        /// </summary>
        /// <param name="solvent">溶剂/基体元素符号</param>
        /// <param name="comp_dict">组成字典 {元素符号: 摩尔分数}</param>
        /// <param name="phaseState">相态 "liquid" 或 "solid"</param>
        /// <param name="geoModel">几何模型委托</param>
        /// <param name="geoModelName">几何模型名称</param>
        /// <param name="userDeltaHf">用户手动提供的 ΔHf (kJ/mol)，NaN 表示未提供</param>
        public LiquidusResult CalculateLiquidus(
            string solvent,
            Dictionary<string, double> comp_dict,
            string phaseState,
            Geo_Model geoModel,
            string geoModelName,
            double userDeltaHf = double.NaN)
        {
            var result = new LiquidusResult();
            result.MatrixElement = solvent;
            result.Converged = true;

            // 构建组成字符串
            var parts = new List<string>();
            foreach (var kvp in comp_dict)
            {
                if (kvp.Key != solvent)
                    parts.Add($"{kvp.Key}{Math.Round(kvp.Value, 4)}");
            }
            result.Composition = string.Join("", parts);

            // 获取溶剂元素的 Tm 和 ΔHf
            Element solventElement = new Element(solvent);
            if (!solventElement.isExist)
            {
                result.Converged = false;
                result.ErrorMessage = $"Element '{solvent}' not found in database";
                return result;
            }

            double Tm = solventElement.Tm;

            // 使用用户提供的 ΔHf 或从 TDB 计算
            double deltaHf = !double.IsNaN(userDeltaHf) ? userDeltaHf : GetFusionEnthalpy(solvent, Tm);

            if (double.IsNaN(deltaHf) || deltaHf <= 0)
            {
                result.Converged = false;
                result.ErrorMessage = $"NEED_DELTAHF:{solvent}:{Tm}";
                return result;
            }

            result.T_pure_melting = Tm;
            result.DeltaHf = deltaHf;

            // 纯金属情况：若只有溶剂组分
            double xSolvent = comp_dict.ContainsKey(solvent) ? comp_dict[solvent] : 1.0;
            if (xSolvent >= 0.9999)
            {
                result.T_liquidus_Wagner = Tm;
                result.T_liquidus_Pelton = Tm;
                result.T_liquidus_Elliot = Tm;
                result.DeltaT_Wagner = 0;
                result.DeltaT_Pelton = 0;
                result.DeltaT_Elliot = 0;
                result.SolventActivity_Wagner = 1.0;
                result.SolventActivity_Pelton = 1.0;
                result.SolventActivity_Elliot = 1.0;
                return result;
            }

            // 使用三种模型分别计算液相线温度
            try
            {
                var (T_w, lnA_w) = SolveLiquidusTemperature(
                    solvent, comp_dict, Tm, deltaHf, phaseState, geoModel, geoModelName,
                    ComputeSolventLnActivity_Wagner);
                result.T_liquidus_Wagner = T_w;
                result.SolventActivity_Wagner = Math.Exp(lnA_w);
                result.DeltaT_Wagner = Tm - T_w;
            }
            catch (Exception ex)
            {
                result.T_liquidus_Wagner = double.NaN;
                result.SolventActivity_Wagner = double.NaN;
                result.DeltaT_Wagner = double.NaN;
                result.Converged = false;
                result.ErrorMessage = $"Wagner: {ex.Message}";
            }

            try
            {
                var (T_p, lnA_p) = SolveLiquidusTemperature(
                    solvent, comp_dict, Tm, deltaHf, phaseState, geoModel, geoModelName,
                    ComputeSolventLnActivity_Pelton);
                result.T_liquidus_Pelton = T_p;
                result.SolventActivity_Pelton = Math.Exp(lnA_p);
                result.DeltaT_Pelton = Tm - T_p;
            }
            catch (Exception ex)
            {
                result.T_liquidus_Pelton = double.NaN;
                result.SolventActivity_Pelton = double.NaN;
                result.DeltaT_Pelton = double.NaN;
                result.Converged = false;
                result.ErrorMessage += $" Pelton: {ex.Message}";
            }

            try
            {
                var (T_e, lnA_e) = SolveLiquidusTemperature(
                    solvent, comp_dict, Tm, deltaHf, phaseState, geoModel, geoModelName,
                    ComputeSolventLnActivity_Elliot);
                result.T_liquidus_Elliot = T_e;
                result.SolventActivity_Elliot = Math.Exp(lnA_e);
                result.DeltaT_Elliot = Tm - T_e;
            }
            catch (Exception ex)
            {
                result.T_liquidus_Elliot = double.NaN;
                result.SolventActivity_Elliot = double.NaN;
                result.DeltaT_Elliot = double.NaN;
                result.Converged = false;
                result.ErrorMessage += $" Elliot: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 迭代求解液相线温度 (Newton-Raphson + bisection fallback)
        /// Schroder-van Laar: ln(a_solvent) = (ΔHf/R) * (1/Tm - 1/T)
        /// 求解 f(T) = ln(a_solvent(T)) - (ΔHf*1000/R) * (1/Tm - 1/T) = 0
        /// </summary>
        private (double T, double lnActivity) SolveLiquidusTemperature(
            string solvent,
            Dictionary<string, double> comp_dict,
            double Tm,
            double deltaHf_kJ,
            string phaseState,
            Geo_Model geoModel,
            string geoModelName,
            Func<string, Dictionary<string, double>, double, string, Geo_Model, string, double> activityFunc)
        {
            double deltaHf = deltaHf_kJ * 1000.0; // 转换为 J/mol

            // 在 Tm 处计算初始溶剂活度
            double lnA_atTm = activityFunc(solvent, comp_dict, Tm, phaseState, geoModel, geoModelName);

            // 初始估计: T0 = Tm / (1 - R*Tm*lnA / ΔHf)
            // 由 ln(a) = ΔHf/R * (1/Tm - 1/T) 反解 T
            double T_current;
            if (Math.Abs(lnA_atTm) < 1e-10)
            {
                T_current = Tm; // 纯金属
            }
            else
            {
                // T = 1 / (1/Tm - R*lnA/ΔHf)
                double inv_T = 1.0 / Tm - R * lnA_atTm / deltaHf;
                if (inv_T > 0)
                    T_current = 1.0 / inv_T;
                else
                    T_current = Tm - 100; // fallback
            }

            // 物理边界
            double T_lower = Math.Max(200, Tm - 1500);
            double T_upper = Tm + 50;
            T_current = Math.Max(T_lower, Math.Min(T_upper, T_current));

            // Newton-Raphson 迭代
            double dT_numerical = 1.0; // 数值微分步长 (K)
            bool converged = false;

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                double lnA = activityFunc(solvent, comp_dict, T_current, phaseState, geoModel, geoModelName);
                double f_T = lnA - (deltaHf / R) * (1.0 / Tm - 1.0 / T_current);

                if (Math.Abs(f_T) < 1e-6)
                {
                    converged = true;
                    return (Math.Round(T_current, 2), lnA);
                }

                // 数值求导 f'(T)
                double T_plus = T_current + dT_numerical;
                double lnA_plus = activityFunc(solvent, comp_dict, T_plus, phaseState, geoModel, geoModelName);
                double f_T_plus = lnA_plus - (deltaHf / R) * (1.0 / Tm - 1.0 / T_plus);
                double df_dT = (f_T_plus - f_T) / dT_numerical;

                if (Math.Abs(df_dT) < 1e-15)
                {
                    // 导数太小，切换到 bisection
                    break;
                }

                double T_next = T_current - f_T / df_dT;

                // 检查步长是否合理
                if (Math.Abs(T_next - T_current) > 500 || T_next < T_lower || T_next > T_upper)
                {
                    // Newton 发散，切换到 bisection
                    break;
                }

                if (Math.Abs(T_next - T_current) < ConvergenceTol)
                {
                    converged = true;
                    double lnA_final = activityFunc(solvent, comp_dict, T_next, phaseState, geoModel, geoModelName);
                    return (Math.Round(T_next, 2), lnA_final);
                }

                T_current = T_next;
            }

            if (!converged)
            {
                // Bisection fallback
                return BisectionSolve(solvent, comp_dict, Tm, deltaHf, phaseState,
                    geoModel, geoModelName, activityFunc, T_lower, T_upper);
            }

            double lnA_result = activityFunc(solvent, comp_dict, T_current, phaseState, geoModel, geoModelName);
            return (Math.Round(T_current, 2), lnA_result);
        }

        /// <summary>
        /// Bisection 求解作为 Newton-Raphson 的 fallback
        /// </summary>
        private (double T, double lnActivity) BisectionSolve(
            string solvent,
            Dictionary<string, double> comp_dict,
            double Tm,
            double deltaHf,
            string phaseState,
            Geo_Model geoModel,
            string geoModelName,
            Func<string, Dictionary<string, double>, double, string, Geo_Model, string, double> activityFunc,
            double T_low,
            double T_high)
        {
            Func<double, double> f = T =>
            {
                double lnA = activityFunc(solvent, comp_dict, T, phaseState, geoModel, geoModelName);
                return lnA - (deltaHf / R) * (1.0 / Tm - 1.0 / T);
            };

            double fLow = f(T_low);
            double fHigh = f(T_high);

            // 确保区间包含零点
            if (fLow * fHigh > 0)
            {
                // 扩展搜索区间
                T_low = Math.Max(200, Tm - 2000);
                T_high = Tm + 100;
                fLow = f(T_low);
                fHigh = f(T_high);
            }

            for (int iter = 0; iter < MaxIterations * 2; iter++)
            {
                double T_mid = (T_low + T_high) / 2.0;
                double fMid = f(T_mid);

                if (Math.Abs(fMid) < 1e-6 || (T_high - T_low) < ConvergenceTol)
                {
                    double lnA_final = activityFunc(solvent, comp_dict, T_mid, phaseState, geoModel, geoModelName);
                    return (Math.Round(T_mid, 2), lnA_final);
                }

                if (fLow * fMid < 0)
                    T_high = T_mid;
                else
                {
                    T_low = T_mid;
                    fLow = fMid;
                }
            }

            // 返回最终中值作为最佳近似
            double T_best = (T_low + T_high) / 2.0;
            double lnA_best = activityFunc(solvent, comp_dict, T_best, phaseState, geoModel, geoModelName);
            return (Math.Round(T_best, 2), lnA_best);
        }

        /// <summary>
        /// 计算溶质 i 在液态溶体中的参考态修正项。
        /// 当数据库提供的是固态参考数据，而溶质实际溶于液态合金时：
        /// Δ(lnγ_i) = ΔG_fusion,i / (R·T) = ΔHf_i/(R·T) × (1 - T/Tm_i)
        /// </summary>
        private double ReferenceStateCorrection(Element solute_i, double T)
        {
            double Tm_i = solute_i.Tm;
            if (Tm_i <= 0 || T <= 0) return 0;

            double deltaHf_kJ = GetFusionEnthalpy(solute_i.Name, Tm_i);
            if (double.IsNaN(deltaHf_kJ) || deltaHf_kJ <= 0) return 0;

            double deltaHf_J = deltaHf_kJ * 1000.0;
            // ΔG_fusion = ΔHf × (1 - T/Tm), in J/mol
            double dG = ConvertReferenceState(deltaHf_J, Tm_i, T);
            return dG / (R * T);
        }

        /// <summary>
        /// 基于 Wagner 稀溶液模型计算溶剂的 ln(activity)
        /// 通过 Gibbs-Duhem 关系:
        /// ln(a_k) = ln(x_k) - Σᵢ(xᵢ·lnγᵢ⁰) - ½·ΣᵢΣⱼ(εⁱⱼ·xᵢ·xⱼ)
        /// 其中 i,j 为溶质, k 为溶剂
        ///
        /// 液态参考态修正:
        /// 当 phaseState="liquid" 时，对每个溶质 i 加入修正:
        /// lnγᵢ⁰(liq) = lnγᵢ⁰ + ΔHf_i/(R·T)×(1 - T/Tm_i)
        /// </summary>
        private double ComputeSolventLnActivity_Wagner(
            string solvent,
            Dictionary<string, double> comp_dict,
            double T,
            string phaseState,
            Geo_Model geoModel,
            string geoModelName)
        {
            Element solv = new Element(solvent);
            double xSolvent = comp_dict.ContainsKey(solvent) ? comp_dict[solvent] : 1.0;

            Ternary_melts ternary = new Ternary_melts(T, phaseState);

            // 溶质列表
            var solutes = comp_dict.Where(kvp => kvp.Key != solvent).ToList();

            // 项1: ln(x_k)
            double lnXk = Math.Log(xSolvent);

            // 项2: -Σᵢ(xᵢ·lnγᵢ⁰_eff)
            // lnγᵢ⁰_eff = lnγᵢ⁰ + ΔG_fusion,i/(R·T) (液态修正)
            double sumLnY0 = 0;
            foreach (var (name, xi) in solutes)
            {
                Element soluteI = new Element(name);
                double lnY0_i = ternary.lnY0(solv, soluteI);
                if (!double.IsNaN(lnY0_i) && !double.IsInfinity(lnY0_i))
                {
                    // 参考态修正: 液态时加入 ΔG_fusion/(R·T)
                    if (phaseState == "liquid")
                        lnY0_i += ReferenceStateCorrection(soluteI, T);
                    sumLnY0 += xi * lnY0_i;
                }
            }

            // 项3: -½·ΣᵢΣⱼ(εⁱⱼ·xᵢ·xⱼ)
            double sumEpsilon = 0;
            foreach (var (nameI, xi) in solutes)
            {
                Element soluteI = new Element(nameI);
                foreach (var (nameJ, xj) in solutes)
                {
                    Element soluteJ = new Element(nameJ);
                    double epsilon_ij = ternary.Activity_Interact_Coefficient_1st(
                        solv, soluteI, soluteJ, geoModel, geoModelName);
                    if (!double.IsNaN(epsilon_ij) && !double.IsInfinity(epsilon_ij))
                        sumEpsilon += epsilon_ij * xi * xj;
                }
            }

            double lnA_solvent = lnXk - sumLnY0 - 0.5 * sumEpsilon;
            return lnA_solvent;
        }

        /// <summary>
        /// 基于 Pelton/Darken 模型计算溶剂的 ln(activity)
        /// Pelton 修正: ln(a_k) = ln(x_k) - Σᵢ(xᵢ·lnγᵢ⁰) - ½·ΣᵢΣⱼ(εⁱⱼ·xᵢ·xⱼ)
        ///                        + (1/2)·Σᵢ(xᵢ·ΣⱼΣₗ(εʲˡ·xⱼ·xₗ)/2)
        /// 近似处理: 使用 Pelton 的二次项修正
        /// </summary>
        private double ComputeSolventLnActivity_Pelton(
            string solvent,
            Dictionary<string, double> comp_dict,
            double T,
            string phaseState,
            Geo_Model geoModel,
            string geoModelName)
        {
            Element solv = new Element(solvent);
            double xSolvent = comp_dict.ContainsKey(solvent) ? comp_dict[solvent] : 1.0;

            Ternary_melts ternary = new Ternary_melts(T, phaseState);

            var solutes = comp_dict.Where(kvp => kvp.Key != solvent).ToList();

            // 项1: ln(x_k)
            double lnXk = Math.Log(xSolvent);

            // 项2: -Σᵢ(xᵢ·lnγᵢ⁰_eff)
            double sumLnY0 = 0;
            foreach (var (name, xi) in solutes)
            {
                Element soluteI = new Element(name);
                double lnY0_i = ternary.lnY0(solv, soluteI);
                if (!double.IsNaN(lnY0_i) && !double.IsInfinity(lnY0_i))
                {
                    if (phaseState == "liquid")
                        lnY0_i += ReferenceStateCorrection(soluteI, T);
                    sumLnY0 += xi * lnY0_i;
                }
            }

            // 项3: -½·ΣᵢΣⱼ(εⁱⱼ·xᵢ·xⱼ)
            double sumEpsilon = 0;
            foreach (var (nameI, xi) in solutes)
            {
                Element soluteI = new Element(nameI);
                foreach (var (nameJ, xj) in solutes)
                {
                    Element soluteJ = new Element(nameJ);
                    double epsilon_ij = ternary.Activity_Interact_Coefficient_1st(
                        solv, soluteI, soluteJ, geoModel, geoModelName);
                    if (!double.IsNaN(epsilon_ij) && !double.IsInfinity(epsilon_ij))
                        sumEpsilon += epsilon_ij * xi * xj;
                }
            }

            // Pelton 修正项: +(1/2)·ΣᵢΣⱼ(εⁱⱼ·xᵢ·xⱼ)·Σₗxₗ
            // 在稀溶液中该修正项为高阶小量，主要贡献来自交叉项
            double peltonCorrection = 0;
            double sumX_solutes = solutes.Sum(s => s.Value);
            peltonCorrection = 0.5 * sumEpsilon * sumX_solutes;

            double lnA_solvent = lnXk - sumLnY0 - 0.5 * sumEpsilon + peltonCorrection;
            return lnA_solvent;
        }

        /// <summary>
        /// 基于 Elliot 模型计算溶剂的 ln(activity)
        /// 在 Wagner 基础上增加二阶相互作用参数 ρ 的贡献
        /// </summary>
        private double ComputeSolventLnActivity_Elliot(
            string solvent,
            Dictionary<string, double> comp_dict,
            double T,
            string phaseState,
            Geo_Model geoModel,
            string geoModelName)
        {
            Element solv = new Element(solvent);
            double xSolvent = comp_dict.ContainsKey(solvent) ? comp_dict[solvent] : 1.0;

            Ternary_melts ternary = new Ternary_melts(T, phaseState);

            var solutes = comp_dict.Where(kvp => kvp.Key != solvent).ToList();

            // 项1: ln(x_k)
            double lnXk = Math.Log(xSolvent);

            // 项2: -Σᵢ(xᵢ·lnγᵢ⁰_eff)
            double sumLnY0 = 0;
            foreach (var (name, xi) in solutes)
            {
                Element soluteI = new Element(name);
                double lnY0_i = ternary.lnY0(solv, soluteI);
                if (!double.IsNaN(lnY0_i) && !double.IsInfinity(lnY0_i))
                {
                    if (phaseState == "liquid")
                        lnY0_i += ReferenceStateCorrection(soluteI, T);
                    sumLnY0 += xi * lnY0_i;
                }
            }

            // 项3: -½·ΣᵢΣⱼ(εⁱⱼ·xᵢ·xⱼ)
            double sumEpsilon = 0;
            foreach (var (nameI, xi) in solutes)
            {
                Element soluteI = new Element(nameI);
                foreach (var (nameJ, xj) in solutes)
                {
                    Element soluteJ = new Element(nameJ);
                    double epsilon_ij = ternary.Activity_Interact_Coefficient_1st(
                        solv, soluteI, soluteJ, geoModel, geoModelName);
                    if (!double.IsNaN(epsilon_ij) && !double.IsInfinity(epsilon_ij))
                        sumEpsilon += epsilon_ij * xi * xj;
                }
            }

            // 项4: 二阶修正 -⅙·ΣᵢΣⱼΣₗ(ρⁱⱼₗ·xᵢ·xⱼ·xₗ)
            // 在稀溶液中，三阶项的贡献通过 Gibbs-Duhem 积分得到
            double sumRho = 0;
            foreach (var (nameI, xi) in solutes)
            {
                Element soluteI = new Element(nameI);
                foreach (var (nameJ, xj) in solutes)
                {
                    Element soluteJ = new Element(nameJ);
                    double rho_ij = ternary.Roui_jj(solv, soluteI, soluteJ, geoModel, geoModelName);
                    if (!double.IsNaN(rho_ij) && !double.IsInfinity(rho_ij))
                        sumRho += rho_ij * xi * xj * xj;
                }
            }

            double lnA_solvent = lnXk - sumLnY0 - 0.5 * sumEpsilon - (1.0 / 6.0) * sumRho;
            return lnA_solvent;
        }
    }
}
