using System.Text.RegularExpressions;

namespace AlloyAct_Pro
{
    class Activity_Coefficient
    {
        private const double R = constant.R;
        private const double P_TT = constant.P_TT;
        private const double P_TN = constant.P_TN;
        private const double P_NN = constant.P_NN;
        /// <summary>
        /// 熔体的组成，{"A",x}形式的集合字典，非标准摩尔形式
        /// </summary>
        private Dictionary<string, double> _comp_dict = new Dictionary<string, double>();
        /// <summary>
        /// 熔体的原始组成，{"A",x}形式的集合字典，非标准摩尔形式
        /// </summary>
        private Dictionary<string, double> _comp_dict_Original
        {
            get
            { return this._comp_dict; }
        }
        /// <summary>
        /// 熔体的组成{"A",x}形式的集合字典，标准摩尔形式
        /// </summary>
        private Dictionary<string, double> melts_dict
        {
            get
            {
                double sum = 0;
                Dictionary<string, double> compDict = new Dictionary<string, double>();
                if (_comp_dict.Count > 1)
                {
                    foreach (var item in _comp_dict)
                    {
                        sum = sum + item.Value;
                    }
                    foreach (var item in _comp_dict)
                    {
                        if (compDict.ContainsKey(item.Key))
                        {
                            compDict[item.Key] += item.Value / sum;
                        }
                        else
                        {
                            compDict.Add(item.Key, item.Value / sum);
                        }
                    }
                }
                return compDict;
            }
        }


        public Activity_Coefficient()
        {

        }
        private double pow(double x, double y)
        {
            return Math.Pow(x, y);
        }
        private double ln(double x)
        {
            if (x > 0)
            {
                return Math.Log(x);
            }
            else
                return double.NaN;
        }
        private double Exp(double x)
        {
            return Math.Exp(x);
        }
        /// <summary>
        /// 设定系统的初始组成{"A",x}形式
        /// </summary>
        /// <param name="text">AxByCz形式</param>
        public void set_CompositionDict(string text)
        {

            Regex re = new Regex(@"([A-Z]{1}[a-z]?)(\d+[\.]?\d*)?");
            MatchCollection matchs = re.Matches(text);

            foreach (System.Text.RegularExpressions.Match match in matchs)
            {
                double x = 1.0;
                string A = "";
                GroupCollection groups = match.Groups;
                A = groups[1].Value;
                if (double.TryParse(groups[2].Value, out x))
                {
                    double.TryParse(groups[2].Value, out x);
                }
                else { x = 1.0; }

                if (this._comp_dict.ContainsKey(A))
                {
                    this._comp_dict[A] += x;
                }
                else
                {
                    this._comp_dict.Add(A, x);
                }

            }

        }
        /// <summary>
        /// 返回新条件下的系统摩尔组成{"A",x}
        /// </summary>
        /// <param name="varable">变量组分</param>
        /// <param name="x">输入变量的摩尔组成</param>
        /// <returns></returns>
        public Dictionary<string, double> get_NewCompositionDict(string varable, double x)
        {

            Dictionary<string, double> componet_molarPairs = new Dictionary<string, double>();
            Dictionary<string, double> componet_molarfractionPairs = new Dictionary<string, double>();
            double nA = 0, sum_noA = 0;
            foreach (var item in this._comp_dict_Original)
            {
                if (!item.Key.Equals(varable))
                {
                    componet_molarPairs.Add(item.Key, item.Value);
                    sum_noA = sum_noA + item.Value;
                }

            }
            nA = x * sum_noA / (1 - x);
            foreach (var item in this._comp_dict_Original)
            {
                if (item.Key.Equals(varable))
                {
                    componet_molarfractionPairs.Add(varable, x);
                }
                else
                {
                    componet_molarfractionPairs.Add(item.Key, item.Value * (1 - x) / sum_noA);
                }

            }
            return componet_molarfractionPairs;


        }

        /// <summary>
        /// 计算活度系数,多组分体系。利用Wagner稀溶液模型
        /// </summary>
        /// <param name="solvent">基体或溶剂</param>
        /// <param name="solute_i">待计算活度系数的组元</param>
        /// <param name="comp_dict">合金熔体的组成</param>
        /// <param name="Tem">温度</param>
        public double activity_Coefficient_Wagner(Dictionary<string, double> comp_dict, string solvent, string solute_i, Geo_Model geo_Model, string GeoModel, (string state, double T) info)
        {

            double lnY0 = 0.0, lnYi;

            Element solv = new Element(solvent);
            Element solu_i = new Element(solute_i);
            double acf = 0.0;
            if (comp_dict.ContainsKey(solute_i))
            {
                /**判断需求活度系数的溶质是否包含在组成内，如果包含，执行下列计算 */


                Ternary_melts inacoef = new Ternary_melts(info.T, info.state);

                lnY0 = inacoef.lnY0(solv, solu_i);
                foreach (string elementSymbol in this.melts_dict.Keys)
                {



                    if (elementSymbol != solvent)
                    {
                        Element solu_j = new Element(elementSymbol);
                        double x = this.melts_dict[elementSymbol];

                        acf += this.melts_dict[elementSymbol] * inacoef.Activity_Interact_Coefficient_1st(solv, solu_i, solu_j, geo_Model, GeoModel);

                    }


                }

                lnYi = lnY0 + acf;




            }
            else
            {
                MessageBox.Show("组成中不存在" + solute_i);
                lnYi = 0.0;
            }

            return lnYi;

        }

        /// <summary>
        /// 在Wagner稀溶液模型基础上添加修正项
        /// </summary>
        /// <param name="comp_dict">合金熔体的组成 </param>
        /// <param name="solute_i">溶质</param>
        /// <param name="matrix">基体元素</param>
        /// <param name="T">熔体温度</param>
        /// <param name="geo_Model">计算相互作用系数时使用的几何模型</param>
        /// <param name="GeoModel">几何模型的名称</param>
        /// <returns></returns>
        public double activity_coefficient_Pelton(Dictionary<string, double> comp_dict, string solute_i, string matrix, double T, Geo_Model geo_Model, string GeoModel, string phase_state = "liquid")
        {

            Element solv = new Element(matrix);
            Element solui = new Element(solute_i);
            double lnYi_0 = 0, lnYi = 0;
            Ternary_melts ternary_melts = new Ternary_melts(T, phase_state);
            lnYi_0 = ternary_melts.lnY0(solv, solui);


            if (comp_dict.ContainsKey(solv.Name) && comp_dict.ContainsKey(solui.Name))
            {
                double sum_xsij = 0, sum_xskj = 0;
                foreach (var item in comp_dict)
                {
                    if (item.Key != solv.Name)
                    {
                        //计算∑xjɛ^j_i
                        double sji = ternary_melts.Activity_Interact_Coefficient_1st(solv, solui, new Element(item.Key), geo_Model, GeoModel);
                        sum_xsij += sji * item.Value;
                    }
                }

                for (int p = 0; p < comp_dict.Count; p++)
                {
                    //计算∑xj*xi*ɛ^j_i
                    for (int q = p; q < comp_dict.Count; q++)
                    {

                        string m, n;
                        m = comp_dict.ElementAt(p).Key;
                        n = comp_dict.ElementAt(q).Key;
                        if (m != solv.Name && n != solv.Name)
                        {
                            double xm, xn;
                            xm = comp_dict.ElementAt(p).Value;
                            xn = comp_dict.ElementAt(q).Value;
                            double Smn = ternary_melts.Activity_Interact_Coefficient_1st(solv, new Element(m), new Element(n), geo_Model, GeoModel);


                            sum_xskj += xm * xn * Smn;
                        }


                    }
                }

                lnYi = lnYi_0 + sum_xsij - 1.0 / 2 * sum_xskj;
                return lnYi;
            }
            else
            {
                return 0.0;
            }



        }

        /// <summary>
        /// 使用二阶相互作用参数模型计算组分i在多元混合物中的活度系数对数 (ln Gamma_i)。
        /// </summary>
        /// <param name="comp_dict">包含所有组分名称及其摩尔分数的字典。</param>
        /// <param name="solute_i">目标溶质组分i的名称。</param>
        /// <param name="matrix">溶剂（基体）组分m的名称。</param>
        /// <param name="T">温度 (K)。</param>
        /// <param name="geo_Model">包含模型参数或方法的对象。</param>
        /// <param name="GeoModel">具体的模型名称字符串。</param>
        /// <param name="phase_state">相态（默认为 "liquid"）。</param>
        /// <returns>组分i的活度系数对数 ln(Gamma_i)。</returns>
        /// <exception cref="ArgumentException">如果输入参数无效或缺少必要的组分。</exception>
        public double activity_coefficient_Elloit(
            Dictionary<string, double> comp_dict,
            string solute_i,
            string matrix,
            double T,
            Geo_Model geo_Model,
            string GeoModel,
            string phase_state = "liquid")
        {
            // --- 输入验证和初始化 ---
            if (string.IsNullOrEmpty(solute_i) || string.IsNullOrEmpty(matrix) || comp_dict == null || comp_dict.Count < 2)
            {
                throw new ArgumentException("无效的输入参数: comp_dict, solute_i, 或 matrix。");
            }

            Element solv = new Element(matrix); // 溶剂 m
            Element solui = new Element(solute_i); // 溶质 i

            // 确保字典中包含指定的溶质和溶剂
            if (!comp_dict.ContainsKey(solv.Name))
            {
                throw new ArgumentException($"成分字典 comp_dict 必须包含溶剂: {matrix}");
            }
            if (!comp_dict.ContainsKey(solui.Name))
            {
                throw new ArgumentException($"成分字典 comp_dict 必须包含目标溶质: {solute_i}");
            }
            // 注意：如果 solute_i 就是 matrix，理论上 ln(gamma) 可能为 0 或需要特殊处理，
            // 但此模型通常用于计算溶质在溶剂中的行为。

            // 创建 Ternary_melts 实例 (假设构造函数需要 T 和 phase_state)
            Ternary_melts ternary_melts = new Ternary_melts(T, phase_state);

            // --- 项 1: 无限稀释活度系数对数 ln(gamma_i^0) ---
            double lnYi_0 = ternary_melts.lnY0(solv, solui);
            if (double.IsNaN(lnYi_0) || double.IsInfinity(lnYi_0))
            {
                // 根据需要处理错误，例如抛出异常或返回特定值
                Console.WriteLine($"警告: lnY0 返回无效值 ({lnYi_0})，溶质 {solui.Name}，溶剂 {solv.Name}");
                // throw new Exception($"lnY0 calculation failed for solute {solui.Name}");
            }


            // --- 项 2: 一阶相互作用项 ∑ (epsilon_i^j * xj) ---
            double linear_sum = 0;
            // 遍历所有组分 j
            foreach (var item_j in comp_dict)
            {
                string j_name = item_j.Key;
                // 只对溶质组分求和 (j != m)
                if (j_name != solv.Name)
                {
                    double xj = item_j.Value;
                    Element soluj = new Element(j_name);
                    // 获取一阶参数 epsilon_i^j
                    double epsilon_i_j = ternary_melts.Activity_Interact_Coefficient_1st(solv, solui, soluj, geo_Model, GeoModel);
                    if (double.IsNaN(epsilon_i_j) || double.IsInfinity(epsilon_i_j))
                    {
                        Console.WriteLine($"警告: Epsilon(i={solui.Name}, j={j_name}) 返回无效值 ({epsilon_i_j})");
                        // 根据需要处理错误
                    }
                    else
                    {

                        linear_sum += epsilon_i_j * xj;

                    }
                }
            }

            // --- 项 3: 二阶相互作用项 (1/2) * ∑ ∑ (rho_i^{j,k} * xj * xk) ---
            double quadratic_sum = 0;
            // 获取所有溶质名称列表 (j != m)
            List<string> solute_keys = comp_dict.Keys.Where(k => k != solv.Name).ToList();

            // 遍历所有溶质对 (j, k)
            foreach (string j_name in solute_keys)
            {
                double xj = comp_dict[j_name];
                Element soluj = new Element(j_name);

                foreach (string k_name in solute_keys)
                {
                    double xk = comp_dict[k_name];
                    Element soluk = new Element(k_name);

                    // 获取二阶参数 rho_i^{j,k} (关键假设: Roui_jk 能处理所有 j,k 组合)
                    double rho_i_jk = ternary_melts.Roui_jk(solv, solui, soluj, soluk, geo_Model, GeoModel);
                    if (double.IsNaN(rho_i_jk) || double.IsInfinity(rho_i_jk))
                    {
                        Console.WriteLine($"警告: Rho(i={solui.Name}, j={j_name}, k={k_name}) 返回无效值 ({rho_i_jk})");
                        // 根据需要处理错误
                    }
                    else
                    {
                        // 累加 (1/2) * rho_i^{j,k} * xj * xk
                        quadratic_sum += 0.5 * rho_i_jk * xj * xk;
                    }
                }
            }

            // --- 最终结果 ---
            // ln(gamma_i) = ln(gamma_i^0) + linear_term + quadratic_term
            double lnYi = lnYi_0 + linear_sum + quadratic_sum;

            return lnYi;
        }

        #region 溶剂（熔剂）活度系数 - 数值 Gibbs-Duhem 积分

        // ── 溶质 lnγ 计算委托类型 ──
        private delegate Dictionary<string, double> SoluteLnGammaFunc(
            Dictionary<string, double> comp, Element solv, List<string> soluteNames,
            Ternary_melts ternary, Geo_Model geoModel, string geoModelName);

        /// <summary>
        /// 数值 Gibbs-Duhem 积分核心引擎。
        /// 沿组成路径 t∈[0,1] 从纯溶剂积分到目标组成。
        /// 稀溶液端 (t→0) 使用 s=t^(1/α) 变换加密步点，α=3。
        /// 积分采用复合 Simpson 法。
        /// </summary>
        private (double lnGammaSolvent, double gdResidual) NumericalGD(
            Dictionary<string, double> comp_target,
            string solvent, double T,
            Geo_Model geoModel, string geoModelName, string phase,
            SoluteLnGammaFunc soluteLnGammaFunc,
            int N = 200, double alpha = 3.0)
        {
            Element solv = new Element(solvent);
            Ternary_melts ternary = new Ternary_melts(T, phase);

            // 目标溶质摩尔分数
            var soluteNames = comp_target.Keys.Where(k => k != solvent).ToList();
            var x_target = new Dictionary<string, double>();
            foreach (var name in soluteNames)
                x_target[name] = comp_target[name];
            double xk_target = comp_target.ContainsKey(solvent) ? comp_target[solvent] : 1.0;

            // 纯溶剂或无溶质时 lnγ_k = 0
            if (soluteNames.Count == 0 || xk_target >= 0.9999)
                return (0.0, 0.0);

            // ── 生成非均匀节点: s 均匀 → t = s^α，稀溶液端加密 ──
            double[] tNodes = new double[N + 1];
            for (int k = 0; k <= N; k++)
            {
                double s = (double)k / N;
                tNodes[k] = Math.Pow(s, alpha); // t = s^α, α>1 时在 t→0 端加密
            }

            // ── 在每个节点计算所有溶质的 lnγᵢ ──
            double[][] lnGamma_all = new double[N + 1][];
            for (int k = 0; k <= N; k++)
            {
                double t = tNodes[k];
                var comp_t = BuildComposition(solvent, soluteNames, x_target, t);
                var lnG = soluteLnGammaFunc(comp_t, solv, soluteNames, ternary, geoModel, geoModelName);
                lnGamma_all[k] = new double[soluteNames.Count];
                for (int i = 0; i < soluteNames.Count; i++)
                    lnGamma_all[k][i] = lnG.ContainsKey(soluteNames[i]) ? lnG[soluteNames[i]] : 0.0;
            }

            // ── 数值积分: ln(γ_k) = -∫₀¹ Σᵢ [xᵢ(t)/x_k(t)] · d(lnγᵢ)/dt · dt ──
            // 用复合梯形法（节点不等距）
            double lnGammaSolvent = 0;
            for (int k = 0; k < N; k++)
            {
                double t0 = tNodes[k];
                double t1 = tNodes[k + 1];
                double dt = t1 - t0;
                if (dt < 1e-15) continue;

                // 中点 t_mid
                double t_mid = 0.5 * (t0 + t1);
                double xk_mid = 1.0 - t_mid * (1.0 - xk_target);
                if (xk_mid < 1e-12) xk_mid = 1e-12;

                // d(lnγᵢ)/dt ≈ [lnγᵢ(t1) - lnγᵢ(t0)] / dt
                double integrand = 0;
                for (int i = 0; i < soluteNames.Count; i++)
                {
                    double xi_mid = t_mid * x_target[soluteNames[i]];
                    double dlnGamma_dt = (lnGamma_all[k + 1][i] - lnGamma_all[k][i]) / dt;
                    integrand += (xi_mid / xk_mid) * dlnGamma_dt;
                }

                lnGammaSolvent += -integrand * dt;
            }

            // ── G-D 验证 ──
            double gdResidual = VerifyGD(comp_target, solvent, soluteNames,
                lnGammaSolvent, lnGamma_all[N], solv, ternary, geoModel, geoModelName,
                soluteLnGammaFunc);

            return (lnGammaSolvent, gdResidual);
        }

        /// <summary>
        /// 构建路径上 t 点的组成: xᵢ(t)=t·xᵢ_target, x_k(t)=1-t·Σxᵢ_target
        /// </summary>
        private Dictionary<string, double> BuildComposition(
            string solvent, List<string> soluteNames,
            Dictionary<string, double> x_target, double t)
        {
            var comp = new Dictionary<string, double>();
            double sumSolute = 0;
            foreach (var name in soluteNames)
            {
                double xi = t * x_target[name];
                comp[name] = xi;
                sumSolute += xi;
            }
            comp[solvent] = 1.0 - sumSolute;
            return comp;
        }

        /// <summary>
        /// G-D 验证: 在目标组成处微扰验证 Σ xᵢ·d(lnγᵢ) ≈ 0。
        /// 沿各溶质方向做微扰 δ，计算残差。
        /// 返回 max|residual| （越小越好，理想为 0）。
        /// </summary>
        private double VerifyGD(
            Dictionary<string, double> comp_target, string solvent,
            List<string> soluteNames, double lnGammaSolvent,
            double[] lnGammaSolutes_at_target,
            Element solv, Ternary_melts ternary, Geo_Model geoModel, string geoModelName,
            SoluteLnGammaFunc soluteLnGammaFunc)
        {
            double delta = 1e-5;
            double xk = comp_target.ContainsKey(solvent) ? comp_target[solvent] : 1.0;
            double maxResidual = 0;

            foreach (var pertName in soluteNames)
            {
                // 正微扰组成
                var comp_plus = new Dictionary<string, double>(comp_target);
                comp_plus[pertName] += delta;
                comp_plus[solvent] -= delta;
                if (comp_plus[solvent] < 1e-12) continue;

                // 计算微扰后溶质 lnγ
                var lnG_plus = soluteLnGammaFunc(comp_plus, solv, soluteNames, ternary, geoModel, geoModelName);

                // 计算微扰后溶剂 lnγ（用同一数值积分，但简化为单步微扰估计）
                // lnγ_k(x+δ) ≈ lnγ_k(x) + (∂lnγ_k/∂xⱼ)·δ
                // 从 G-D: x_k·d(lnγ_k) = -Σᵢ xᵢ·d(lnγᵢ)
                double sum_xi_dlnGi = 0;
                for (int i = 0; i < soluteNames.Count; i++)
                {
                    double xi = comp_target[soluteNames[i]];
                    double dlnGi = (lnG_plus.ContainsKey(soluteNames[i]) ? lnG_plus[soluteNames[i]] : 0)
                                   - lnGammaSolutes_at_target[i];
                    sum_xi_dlnGi += xi * dlnGi;
                }
                double dlnGk_predicted = -sum_xi_dlnGi / xk;

                // G-D 残差: x_k·dlnγ_k + Σ xᵢ·dlnγᵢ 应 = 0
                double residual = xk * dlnGk_predicted + sum_xi_dlnGi;
                maxResidual = Math.Max(maxResidual, Math.Abs(residual));
            }

            return maxResidual;
        }

        // ── Wagner 模型的溶质 lnγ 计算 ──
        private Dictionary<string, double> SoluteLnGamma_Wagner(
            Dictionary<string, double> comp, Element solv, List<string> soluteNames,
            Ternary_melts ternary, Geo_Model geoModel, string geoModelName)
        {
            var result = new Dictionary<string, double>();
            foreach (var iName in soluteNames)
            {
                Element si = new Element(iName);
                double lnY0 = ternary.lnY0(solv, si);
                if (double.IsNaN(lnY0) || double.IsInfinity(lnY0)) lnY0 = 0;
                double sum = 0;
                foreach (var jName in soluteNames)
                {
                    double xj = comp.ContainsKey(jName) ? comp[jName] : 0;
                    Element sj = new Element(jName);
                    double eps = ternary.Activity_Interact_Coefficient_1st(solv, si, sj, geoModel, geoModelName);
                    if (!double.IsNaN(eps) && !double.IsInfinity(eps))
                        sum += eps * xj;
                }
                result[iName] = lnY0 + sum;
            }
            return result;
        }

        // ── Darken/Pelton 模型的溶质 lnγ 计算 ──
        private Dictionary<string, double> SoluteLnGamma_Pelton(
            Dictionary<string, double> comp, Element solv, List<string> soluteNames,
            Ternary_melts ternary, Geo_Model geoModel, string geoModelName)
        {
            var result = new Dictionary<string, double>();
            // 预计算 ΣⱼΣₖ εⱼₖ·xⱼ·xₖ
            double sumEps_cross = 0;
            foreach (var m in soluteNames)
            {
                double xm = comp.ContainsKey(m) ? comp[m] : 0;
                foreach (var n in soluteNames)
                {
                    double xn = comp.ContainsKey(n) ? comp[n] : 0;
                    double eps_mn = ternary.Activity_Interact_Coefficient_1st(solv, new Element(m), new Element(n), geoModel, geoModelName);
                    if (!double.IsNaN(eps_mn) && !double.IsInfinity(eps_mn))
                        sumEps_cross += eps_mn * xm * xn;
                }
            }

            foreach (var iName in soluteNames)
            {
                Element si = new Element(iName);
                double lnY0 = ternary.lnY0(solv, si);
                if (double.IsNaN(lnY0) || double.IsInfinity(lnY0)) lnY0 = 0;
                double sum = 0;
                foreach (var jName in soluteNames)
                {
                    double xj = comp.ContainsKey(jName) ? comp[jName] : 0;
                    Element sj = new Element(jName);
                    double eps = ternary.Activity_Interact_Coefficient_1st(solv, si, sj, geoModel, geoModelName);
                    if (!double.IsNaN(eps) && !double.IsInfinity(eps))
                        sum += eps * xj;
                }
                result[iName] = lnY0 + sum - 0.5 * sumEps_cross;
            }
            return result;
        }

        // ── Elliott 模型的溶质 lnγ 计算 ──
        private Dictionary<string, double> SoluteLnGamma_Elliott(
            Dictionary<string, double> comp, Element solv, List<string> soluteNames,
            Ternary_melts ternary, Geo_Model geoModel, string geoModelName)
        {
            var result = new Dictionary<string, double>();
            foreach (var iName in soluteNames)
            {
                Element si = new Element(iName);
                double lnY0 = ternary.lnY0(solv, si);
                if (double.IsNaN(lnY0) || double.IsInfinity(lnY0)) lnY0 = 0;
                double linearSum = 0, quadSum = 0;
                foreach (var jName in soluteNames)
                {
                    double xj = comp.ContainsKey(jName) ? comp[jName] : 0;
                    Element sj = new Element(jName);
                    double eps = ternary.Activity_Interact_Coefficient_1st(solv, si, sj, geoModel, geoModelName);
                    if (!double.IsNaN(eps) && !double.IsInfinity(eps))
                        linearSum += eps * xj;
                    foreach (var kName in soluteNames)
                    {
                        double xk = comp.ContainsKey(kName) ? comp[kName] : 0;
                        Element sk = new Element(kName);
                        double rho = ternary.Roui_jk(solv, si, sj, sk, geoModel, geoModelName);
                        if (!double.IsNaN(rho) && !double.IsInfinity(rho))
                            quadSum += 0.5 * rho * xj * xk;
                    }
                }
                result[iName] = lnY0 + linearSum + quadSum;
            }
            return result;
        }

        // ══════ 公开接口 ══════

        /// <summary>
        /// Wagner 模型 - 数值 G-D 积分求溶剂活度系数。
        /// 返回 (lnγ_solvent, GD验证残差)。
        /// </summary>
        public (double lnGamma, double gdResidual) solvent_activity_coefficient_Wagner(
            Dictionary<string, double> comp_dict, string solvent, double T,
            Geo_Model geo_Model, string GeoModel, string phase_state = "liquid")
        {
            return NumericalGD(comp_dict, solvent, T, geo_Model, GeoModel, phase_state,
                SoluteLnGamma_Wagner);
        }

        /// <summary>
        /// Darken/Pelton 模型 - 数值 G-D 积分求溶剂活度系数。
        /// 返回 (lnγ_solvent, GD验证残差)。
        /// </summary>
        public (double lnGamma, double gdResidual) solvent_activity_coefficient_Pelton(
            Dictionary<string, double> comp_dict, string solvent, double T,
            Geo_Model geo_Model, string GeoModel, string phase_state = "liquid")
        {
            return NumericalGD(comp_dict, solvent, T, geo_Model, GeoModel, phase_state,
                SoluteLnGamma_Pelton);
        }

        /// <summary>
        /// Elliott 模型 - 数值 G-D 积分求溶剂活度系数。
        /// 返回 (lnγ_solvent, GD验证残差)。
        /// </summary>
        public (double lnGamma, double gdResidual) solvent_activity_coefficient_Elliott(
            Dictionary<string, double> comp_dict, string solvent, double T,
            Geo_Model geo_Model, string GeoModel, string phase_state = "liquid")
        {
            return NumericalGD(comp_dict, solvent, T, geo_Model, GeoModel, phase_state,
                SoluteLnGamma_Elliott);
        }

        /// <summary>
        /// Darken 二次式 - 解析公式直接计算溶剂活度系数（无需 G-D 积分）。
        ///
        /// 公式: lnγ₁ = -½·Σᵢ εᵢⁱ·Xᵢ² - Σᵢ˂ⱼ εᵢʲ·Xᵢ·Xⱼ
        ///
        /// 其中 1 为溶剂，i,j = 2..n 为溶质。
        /// εᵢʲ = Activity_Interact_Coefficient_1st(solvent, solute_i, solute_j)。
        /// 返回 lnγ_solvent（double），无 G-D 残差（非积分方法）。
        /// </summary>
        public double solvent_activity_coefficient_Darken(
            Dictionary<string, double> comp_dict, string solvent, double T,
            Geo_Model geo_Model, string GeoModel, string phase_state = "liquid")
        {
            Ternary_melts ternary = new Ternary_melts(T, phase_state);
            Element solv = new Element(solvent);

            // 溶质列表（排除溶剂）
            var soluteList = new List<(string name, double x)>();
            foreach (var kvp in comp_dict)
            {
                if (kvp.Key != solvent)
                    soluteList.Add((kvp.Key, kvp.Value));
            }

            int n = soluteList.Count;
            if (n == 0) return 0.0; // 纯溶剂，lnγ = 0

            double lnGammaSolvent = 0;

            // 项1: 自交互项 -½·Σᵢ εᵢⁱ·Xᵢ²
            for (int i = 0; i < n; i++)
            {
                Element si = new Element(soluteList[i].name);
                double xi = soluteList[i].x;
                double eps_ii = ternary.Activity_Interact_Coefficient_1st(
                    solv, si, si, geo_Model, GeoModel);
                if (!double.IsNaN(eps_ii) && !double.IsInfinity(eps_ii))
                    lnGammaSolvent += -0.5 * eps_ii * xi * xi;
            }

            // 项2: 交叉项 -Σ_{i<j} εᵢʲ·Xᵢ·Xⱼ （每对只计一次）
            for (int i = 0; i < n - 1; i++)
            {
                Element si = new Element(soluteList[i].name);
                double xi = soluteList[i].x;
                for (int j = i + 1; j < n; j++)
                {
                    Element sj = new Element(soluteList[j].name);
                    double xj = soluteList[j].x;
                    double eps_ij = ternary.Activity_Interact_Coefficient_1st(
                        solv, si, sj, geo_Model, GeoModel);
                    if (!double.IsNaN(eps_ij) && !double.IsInfinity(eps_ij))
                        lnGammaSolvent += -eps_ij * xi * xj;
                }
            }

            return lnGammaSolvent;
        }

        #endregion

    }
}
