namespace AlloyAct_Pro
{
    /// <summary>
    /// 液相线温度计算器
    /// 基于 Schroder-van Laar 方程，预测多组元合金熔体的液相线温度。
    ///
    /// 默认策略：
    ///   溶剂活度系数 → Darken 二次式（解析公式，瞬间完成）
    ///   溶质模型 → Pelton（一阶交互系数）
    ///   只输出一个模型结果
    ///
    /// 用户要求时：
    ///   溶剂活度系数 → G-D 数值积分（精确但较慢）
    ///   溶质模型 → 用户指定的 Wagner / Pelton / Elliot
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
            /// <summary>实际使用的溶剂活度计算方法</summary>
            public string SolventMethod { get; set; }
            /// <summary>实际计算的模型列表</summary>
            public string ModelsComputed { get; set; }

            // ═══ 热力学一致性检验 ═══
            /// <summary>固相UEM模型估算的溶剂活度（用液相组成近似）</summary>
            public double SolidPhaseActivity { get; set; } = double.NaN;
            /// <summary>液相溶剂活度（对应首个计算模型）</summary>
            public double LiquidPhaseActivity { get; set; } = double.NaN;
            /// <summary>溶剂摩尔分数</summary>
            public double SolventMoleFraction { get; set; } = double.NaN;
            /// <summary>一致性评估等级: "good" / "warning" / "caution"</summary>
            public string ConsistencyLevel { get; set; } = "";
            /// <summary>一致性评估说明</summary>
            public string ConsistencyNote { get; set; } = "";
        }

        /// <summary>
        /// 获取元素的熔化焓 (kJ/mol)
        /// 从 SGTE unary TDB 数据库计算（通过 Gibbs 函数数值微分）。
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
        public static double ParseDeltaHfExpression(string input, double T)
        {
            if (string.IsNullOrWhiteSpace(input))
                return double.NaN;

            input = input.Trim();

            // 先尝试纯数字
            if (double.TryParse(input, out double plainValue))
                return plainValue;

            // 尝试解析 a/T+b 或 a/T-b 格式
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
        /// 计算多组元合金的液相线温度
        /// </summary>
        /// <param name="solvent">溶剂/基体元素符号</param>
        /// <param name="comp_dict">组成字典 {元素符号: 摩尔分数}</param>
        /// <param name="phaseState">相态 "liquid" 或 "solid"</param>
        /// <param name="geoModel">几何模型委托</param>
        /// <param name="geoModelName">几何模型名称</param>
        /// <param name="userDeltaHf">用户手动提供的 ΔHf (kJ/mol)，NaN 表示未提供</param>
        /// <param name="model">溶质模型: "Pelton"(默认) / "Wagner" / "Elliot" / "all"(三个都算)</param>
        /// <param name="useGD">true=G-D积分计算溶剂活度, false=Darken二次式(默认)</param>
        public LiquidusResult CalculateLiquidus(
            string solvent,
            Dictionary<string, double> comp_dict,
            string phaseState,
            Geo_Model geoModel,
            string geoModelName,
            double userDeltaHf = double.NaN,
            string model = "Pelton",
            bool useGD = false)
        {
            var result = new LiquidusResult();
            result.MatrixElement = solvent;
            result.Converged = true;
            result.SolventMethod = useGD ? "G-D积分" : "Darken二次式";
            result.ModelsComputed = model;

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

            // 纯金属情况
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

            // 选择溶剂活度函数
            // 默认用 Darken 二次式（解析公式，瞬间完成）
            // 用户要求时用 G-D 积分（精确但慢）
            bool doWagner = model == "all" || model == "Wagner";
            bool doPelton = model == "all" || model == "Pelton";
            bool doElliot = model == "all" || model == "Elliot";

            // Wagner
            if (doWagner)
            {
                try
                {
                    var actFunc = useGD
                        ? (Func<string, Dictionary<string, double>, double, string, Geo_Model, string, double>)ComputeSolventLnActivity_Wagner
                        : ComputeSolventLnActivity_Darken;
                    var (T_w, lnA_w) = SolveLiquidusTemperature(
                        solvent, comp_dict, Tm, deltaHf, phaseState, geoModel, geoModelName, actFunc);
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
            }

            // Pelton（默认模型）
            if (doPelton)
            {
                try
                {
                    var actFunc = useGD
                        ? (Func<string, Dictionary<string, double>, double, string, Geo_Model, string, double>)ComputeSolventLnActivity_Pelton
                        : ComputeSolventLnActivity_Darken;
                    var (T_p, lnA_p) = SolveLiquidusTemperature(
                        solvent, comp_dict, Tm, deltaHf, phaseState, geoModel, geoModelName, actFunc);
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
            }

            // Elliot
            if (doElliot)
            {
                try
                {
                    var actFunc = useGD
                        ? (Func<string, Dictionary<string, double>, double, string, Geo_Model, string, double>)ComputeSolventLnActivity_Elliot
                        : ComputeSolventLnActivity_Darken;
                    var (T_e, lnA_e) = SolveLiquidusTemperature(
                        solvent, comp_dict, Tm, deltaHf, phaseState, geoModel, geoModelName, actFunc);
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
            }

            // ═══ 热力学一致性检验：固相 UEM 模型估算 ═══
            // 在 T_L 处，用固相 Darken 二次式估算溶剂在固溶体中的活度
            // 若 a_solid ≈ 1 → 纯固体假设合理
            // 若 a_solid << 1 → 存在显著固溶体，T_L 被低估
            try
            {
                // 取首个成功计算的液相线温度和活度
                double T_L = double.NaN;
                double a_liquid = double.NaN;
                if (doPelton && !double.IsNaN(result.T_liquidus_Pelton))
                {
                    T_L = result.T_liquidus_Pelton;
                    a_liquid = result.SolventActivity_Pelton;
                }
                else if (doWagner && !double.IsNaN(result.T_liquidus_Wagner))
                {
                    T_L = result.T_liquidus_Wagner;
                    a_liquid = result.SolventActivity_Wagner;
                }
                else if (doElliot && !double.IsNaN(result.T_liquidus_Elliot))
                {
                    T_L = result.T_liquidus_Elliot;
                    a_liquid = result.SolventActivity_Elliot;
                }

                if (!double.IsNaN(T_L) && T_L > 200)
                {
                    result.SolventMoleFraction = xSolvent;
                    result.LiquidPhaseActivity = a_liquid;

                    // 用固相 UEM 参数 + Darken 二次式计算溶剂在固溶体中的活度
                    double lnA_solid = ComputeSolventLnActivity_Darken(
                        solvent, comp_dict, T_L, "solid", geoModel, geoModelName);
                    double a_solid = Math.Exp(lnA_solid);
                    result.SolidPhaseActivity = a_solid;

                    // 一致性判断
                    if (a_solid > 0.95)
                    {
                        result.ConsistencyLevel = "good";
                        result.ConsistencyNote =
                            $"固相溶剂活度 a(solid)={a_solid:F4} ≈ 1，纯固体假设合理";
                    }
                    else if (a_solid > 0.85)
                    {
                        result.ConsistencyLevel = "warning";
                        result.ConsistencyNote =
                            $"固相溶剂活度 a(solid)={a_solid:F4}，可能存在一定程度固溶，液相线温度可能略被低估";
                    }
                    else
                    {
                        result.ConsistencyLevel = "caution";
                        result.ConsistencyNote =
                            $"固相溶剂活度 a(solid)={a_solid:F4} 明显偏离1，固溶效应显著或已超出稀溶液范围，建议谨慎使用";
                    }
                }
            }
            catch
            {
                // 一致性检验失败不影响主结果
                result.ConsistencyNote = "一致性检验计算异常";
            }

            return result;
        }

        /// <summary>
        /// 迭代求解液相线温度 (Newton-Raphson + bisection fallback)
        /// Schroder-van Laar: ln(a_solvent) = (ΔHf/R) * (1/Tm - 1/T)
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

            // 初始估计
            double T_current;
            if (Math.Abs(lnA_atTm) < 1e-10)
            {
                T_current = Tm;
            }
            else
            {
                double inv_T = 1.0 / Tm - R * lnA_atTm / deltaHf;
                if (inv_T > 0)
                    T_current = 1.0 / inv_T;
                else
                    T_current = Tm - 100;
            }

            // 物理边界
            double T_lower = Math.Max(200, Tm - 1500);
            double T_upper = Tm + 50;
            T_current = Math.Max(T_lower, Math.Min(T_upper, T_current));

            // Newton-Raphson 迭代
            double dT_numerical = 1.0;
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
                    break;

                double T_next = T_current - f_T / df_dT;

                if (Math.Abs(T_next - T_current) > 500 || T_next < T_lower || T_next > T_upper)
                    break;

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

            if (fLow * fHigh > 0)
            {
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

            double T_best = (T_low + T_high) / 2.0;
            double lnA_best = activityFunc(solvent, comp_dict, T_best, phaseState, geoModel, geoModelName);
            return (Math.Round(T_best, 2), lnA_best);
        }

        // ═══════ 溶剂活度计算方法 ═══════

        private static string CompDictToString(Dictionary<string, double> comp)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in comp)
                sb.Append($"{kvp.Key}{kvp.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            return sb.ToString();
        }

        /// <summary>
        /// Darken 二次式 — 解析公式，无需 G-D 积分（默认方法）
        /// </summary>
        private double ComputeSolventLnActivity_Darken(
            string solvent,
            Dictionary<string, double> comp_dict,
            double T,
            string phaseState,
            Geo_Model geoModel,
            string geoModelName)
        {
            double xSolvent = comp_dict.ContainsKey(solvent) ? comp_dict[solvent] : 1.0;
            var ac = new Activity_Coefficient();
            ac.set_CompositionDict(CompDictToString(comp_dict));
            double lnGamma = ac.solvent_activity_coefficient_Darken(
                comp_dict, solvent, T, geoModel, geoModelName, phaseState);
            return Math.Log(xSolvent) + lnGamma;
        }

        /// <summary>
        /// Wagner 模型 — G-D 积分（用户要求时使用）
        /// </summary>
        private double ComputeSolventLnActivity_Wagner(
            string solvent,
            Dictionary<string, double> comp_dict,
            double T,
            string phaseState,
            Geo_Model geoModel,
            string geoModelName)
        {
            double xSolvent = comp_dict.ContainsKey(solvent) ? comp_dict[solvent] : 1.0;
            var ac = new Activity_Coefficient();
            ac.set_CompositionDict(CompDictToString(comp_dict));
            var (lnGamma, _) = ac.solvent_activity_coefficient_Wagner(
                comp_dict, solvent, T, geoModel, geoModelName, phaseState);
            return Math.Log(xSolvent) + lnGamma;
        }

        /// <summary>
        /// Pelton 模型 — G-D 积分（用户要求时使用）
        /// </summary>
        private double ComputeSolventLnActivity_Pelton(
            string solvent,
            Dictionary<string, double> comp_dict,
            double T,
            string phaseState,
            Geo_Model geoModel,
            string geoModelName)
        {
            double xSolvent = comp_dict.ContainsKey(solvent) ? comp_dict[solvent] : 1.0;
            var ac = new Activity_Coefficient();
            ac.set_CompositionDict(CompDictToString(comp_dict));
            var (lnGamma, _) = ac.solvent_activity_coefficient_Pelton(
                comp_dict, solvent, T, geoModel, geoModelName, phaseState);
            return Math.Log(xSolvent) + lnGamma;
        }

        /// <summary>
        /// Elliott 模型 — G-D 积分（用户要求时使用）
        /// </summary>
        private double ComputeSolventLnActivity_Elliot(
            string solvent,
            Dictionary<string, double> comp_dict,
            double T,
            string phaseState,
            Geo_Model geoModel,
            string geoModelName)
        {
            double xSolvent = comp_dict.ContainsKey(solvent) ? comp_dict[solvent] : 1.0;
            var ac = new Activity_Coefficient();
            ac.set_CompositionDict(CompDictToString(comp_dict));
            var (lnGamma, _) = ac.solvent_activity_coefficient_Elliott(
                comp_dict, solvent, T, geoModel, geoModelName, phaseState);
            return Math.Log(xSolvent) + lnGamma;
        }
    }
}
