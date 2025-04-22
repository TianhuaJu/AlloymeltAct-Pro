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

    }
}
