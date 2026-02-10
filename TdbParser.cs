using System.Text.RegularExpressions;

namespace AlloyAct_Pro
{
    /// <summary>
    /// SGTE Unary TDB 数据库解析器
    /// 解析 unary50.tdb 格式的热力学数据，提取纯物质的 Gibbs 自由能函数，
    /// 用于计算熔化焓 ΔHf、熔点 Tm 等热力学参数。
    ///
    /// TDB格式说明:
    /// - ELEMENT 行: 元素定义（含分子量、H298、S298）
    /// - FUNCTION GHSERXX: 标准参考态（通常为稳定固相）Gibbs 自由能
    /// - FUNCTION GLIQXX: 液相 Gibbs 自由能
    /// - 每个 FUNCTION 由分段多项式组成，以温度范围分隔
    /// - 表达式格式: a + b*T + c*T*LN(T) + d*T**2 + e*T**3 + f*T**(-1) + g*T**n
    /// </summary>
    class TdbParser
    {
        /// <summary>
        /// 单个温度区间的 Gibbs 自由能多项式
        /// G(T) = a + b*T + c*T*ln(T) + d*T² + e*T³ + f*T⁻¹ + g*T^n (特殊幂次项)
        /// </summary>
        internal class GibbsSegment
        {
            public double T_upper { get; set; }     // 该段的上限温度
            public double A { get; set; }            // 常数项
            public double B { get; set; }            // T 的系数
            public double C { get; set; }            // T*LN(T) 的系数
            public double D { get; set; }            // T**2 的系数
            public double E { get; set; }            // T**3 的系数
            public double F { get; set; }            // T**(-1) 的系数
            public double SpecialExp { get; set; }   // 特殊幂次 (如 T**7, T**(-9))
            public double SpecialCoeff { get; set; } // 特殊幂次的系数
            public string RefFunction { get; set; }  // 引用的其他函数名 (如 GHSERAL)

            /// <summary>
            /// 计算该段在温度T处的 G 值 (J/mol)
            /// </summary>
            public double Evaluate(double T, Func<string, double, double> resolveRef)
            {
                double G = A + B * T + C * T * Math.Log(T) + D * T * T + E * T * T * T;
                if (Math.Abs(T) > 1e-10)
                    G += F / T;
                if (Math.Abs(SpecialCoeff) > 1e-30 && Math.Abs(SpecialExp) > 1e-10)
                    G += SpecialCoeff * Math.Pow(T, SpecialExp);
                if (!string.IsNullOrEmpty(RefFunction) && resolveRef != null)
                    G += resolveRef(RefFunction, T);
                return G;
            }
        }

        /// <summary>
        /// 一个完整的 FUNCTION 定义，由若干温度分段组成
        /// </summary>
        internal class GibbsFunction
        {
            public string Name { get; set; }
            public double T_lower { get; set; }      // 起始温度
            public List<GibbsSegment> Segments { get; set; } = new List<GibbsSegment>();

            /// <summary>
            /// 在温度 T 处求值
            /// </summary>
            public double Evaluate(double T, Func<string, double, double> resolveRef)
            {
                // 找到合适的温度段
                foreach (var seg in Segments)
                {
                    if (T <= seg.T_upper)
                        return seg.Evaluate(T, resolveRef);
                }
                // T 超出最高段上限，使用最后一段
                if (Segments.Count > 0)
                    return Segments[Segments.Count - 1].Evaluate(T, resolveRef);
                return double.NaN;
            }
        }

        /// <summary>
        /// ELEMENT 行的数据
        /// </summary>
        internal class ElementData
        {
            public string Symbol { get; set; }
            public string RefPhase { get; set; }   // 参考态 (FCC_A1, BCC_A2, HCP_A3 等)
            public double Mass { get; set; }        // 分子量
            public double H298 { get; set; }        // H(298.15) - H(0) (J/mol)
            public double S298 { get; set; }        // S(298.15) (J/mol/K)
        }

        // 已解析的函数库
        private Dictionary<string, GibbsFunction> _functions = new Dictionary<string, GibbsFunction>(StringComparer.OrdinalIgnoreCase);
        // 已解析的元素信息
        private Dictionary<string, ElementData> _elements = new Dictionary<string, ElementData>(StringComparer.OrdinalIgnoreCase);
        // 是否已加载
        private bool _loaded = false;

        // 单例缓存
        private static TdbParser _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取或创建单例（自动从 data\unary50.tdb 加载）
        /// </summary>
        public static TdbParser Instance
        {
            get
            {
                if (_instance == null || !_instance._loaded)
                {
                    lock (_lock)
                    {
                        if (_instance == null || !_instance._loaded)
                        {
                            _instance = new TdbParser();
                            string path = Path.Combine(Application.StartupPath, "data", "unary50.tdb");
                            if (File.Exists(path))
                                _instance.Parse(path);
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 解析 TDB 文件
        /// </summary>
        public void Parse(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"TDB file not found: {filePath}");

            string content = File.ReadAllText(filePath);

            ParseElements(content);
            ParseFunctions(content);
            _loaded = true;
        }

        /// <summary>
        /// 解析 ELEMENT 行
        /// 格式: ELEMENT XX  PHASE_NAME  mass  H298  S298 !
        /// </summary>
        private void ParseElements(string content)
        {
            var regex = new Regex(
                @"ELEMENT\s+(\w+)\s+(\S+)\s+([\d.E+\-]+)\s+([\d.E+\-]+)\s+([\d.E+\-]+)\s*!",
                RegexOptions.IgnoreCase);

            foreach (Match m in regex.Matches(content))
            {
                string symbol = m.Groups[1].Value.Trim();
                if (symbol == "/-" || symbol == "VA") continue; // 跳过电子气和真空

                var ed = new ElementData
                {
                    Symbol = symbol,
                    RefPhase = m.Groups[2].Value.Trim(),
                    Mass = ParseDouble(m.Groups[3].Value),
                    H298 = ParseDouble(m.Groups[4].Value),
                    S298 = ParseDouble(m.Groups[5].Value)
                };
                _elements[symbol] = ed;
            }
        }

        /// <summary>
        /// 解析所有 FUNCTION 定义
        /// </summary>
        private void ParseFunctions(string content)
        {
            // 匹配 FUNCTION name T_low ... ; T_high [Y|N] ! 的完整块
            // FUNCTION 可以跨多行，以 "!" 结尾
            var funcRegex = new Regex(
                @"FUNCTION\s+(\w+)\s+([\d.]+)\s*(.*?)\s*!",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match m in funcRegex.Matches(content))
            {
                string funcName = m.Groups[1].Value.Trim();
                double T_lower = ParseDouble(m.Groups[2].Value);
                string body = m.Groups[3].Value.Trim();

                var gf = new GibbsFunction { Name = funcName, T_lower = T_lower };

                // 分段: 用 ";" 分隔，每段末尾有 "T_upper Y" 或 "T_upper N"
                var segments = SplitSegments(body);
                foreach (var seg in segments)
                {
                    gf.Segments.Add(seg);
                }

                _functions[funcName] = gf;
            }
        }

        /// <summary>
        /// 将函数体按 ";" 分段，解析每段的表达式和温度上限。
        ///
        /// TDB 格式: expression1; T_upper1 Y expression2; T_upper2 Y expression3; T_upper3 N
        /// 即 ";" 后面紧跟的 "T_upper [Y|N]" 属于前一段的上限温度，
        /// 再之后的内容才是下一段的表达式。
        /// </summary>
        private List<GibbsSegment> SplitSegments(string body)
        {
            var result = new List<GibbsSegment>();

            // 按 ";" 分割
            string[] parts = body.Split(';');

            // parts[0] = first expression (no T_upper prefix)
            // parts[1] = "T_upper1 Y  second_expression" or "T_upper1 N"
            // parts[2] = "T_upper2 Y  third_expression" or "T_upper2 N"
            // ...
            // parts[n] = "T_upperN N"

            // 收集 (expression, T_upper) 对
            var expressions = new List<string>();
            var upperTemps = new List<double>();

            string pendingExpression = parts.Length > 0 ? parts[0].Trim() : "";

            for (int i = 1; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (string.IsNullOrEmpty(part)) continue;

                // 开头是 "T_upper [Y|N]"，后面可能跟下一段的表达式
                // 匹配: 一个浮点数 + 可选的 Y/N + 其余内容
                var headerMatch = Regex.Match(part,
                    @"^\s*([\d.E+\-]+)\s*[YN]?\s*(.*?)\s*$",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);

                if (headerMatch.Success)
                {
                    double T_upper = ParseDouble(headerMatch.Groups[1].Value);
                    string nextExpr = headerMatch.Groups[2].Value.Trim();

                    // 保存当前段
                    expressions.Add(pendingExpression);
                    upperTemps.Add(T_upper);

                    // 下一段的表达式
                    pendingExpression = nextExpr;
                }
            }

            // 构建 GibbsSegment
            for (int i = 0; i < expressions.Count; i++)
            {
                string expr = expressions[i];
                if (string.IsNullOrWhiteSpace(expr)) continue;

                var seg = ParseExpression(expr);
                seg.T_upper = upperTemps[i];
                result.Add(seg);
            }

            return result;
        }

        /// <summary>
        /// 解析 Gibbs 能表达式
        /// 典型格式: a+b*T+c*T*LN(T)+d*T**2+e*T**3+f*T**(-1)+g*T**7+GHSERXX
        ///
        /// 解析策略:
        /// 1) 先提取函数引用 (GHSERXX 等)
        /// 2) 将表达式拆分为独立的"项"（term），每项带自己的正/负号
        ///    拆分规则: 在 + 或 - 处断开，但不拆 科学计数法中的 E+/E-
        /// 3) 对每一项进行模式匹配，归类到 A/B/C/D/E/F/Special
        /// </summary>
        private GibbsSegment ParseExpression(string expr)
        {
            var seg = new GibbsSegment();

            // 替换换行为空格，去除多余空白
            expr = Regex.Replace(expr, @"\s+", " ").Trim();

            // 1) 提取函数引用 (如 +GHSERAL, +GHSERFE)
            var refMatch = Regex.Match(expr, @"[\+]?\s*(G[A-Z]\w+)", RegexOptions.IgnoreCase);
            if (refMatch.Success)
            {
                seg.RefFunction = refMatch.Groups[1].Value;
                expr = expr.Remove(refMatch.Index, refMatch.Length).Trim();
                // 清理遗留的尾部 +
                expr = Regex.Replace(expr, @"\s*\+\s*$", "").Trim();
            }

            // 2) 拆分为独立项
            //    在 + 或 - 处断开，但跳过科学计数法中的 E+/E-
            //    策略: 用正则找到每个"项"——以可选 +/- 开头，后面跟数字或 T
            var terms = SplitIntoTerms(expr);

            // 3) 对每一项进行分类
            foreach (string rawTerm in terms)
            {
                string term = rawTerm.Trim();
                if (string.IsNullOrEmpty(term)) continue;

                // 3a) coeff*T*LN(T)
                var m = Regex.Match(term, @"^([\+\-]?[\d.]+(?:E[\+\-]?\d+)?)\s*\*\s*T\s*\*\s*LN\s*\(\s*T\s*\)$",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    seg.C = ParseDouble(m.Groups[1].Value);
                    continue;
                }

                // 3b) coeff*T**( exp ) — with parentheses around exponent
                m = Regex.Match(term, @"^([\+\-]?[\d.]+(?:E[\+\-]?\d+)?)\s*\*\s*T\s*\*\*\s*\(\s*([\-]?\d+)\s*\)$",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    ClassifyPowerTerm(seg, ParseDouble(m.Groups[1].Value), ParseDouble(m.Groups[2].Value));
                    continue;
                }

                // 3c) coeff*T**exp — without parentheses
                m = Regex.Match(term, @"^([\+\-]?[\d.]+(?:E[\+\-]?\d+)?)\s*\*\s*T\s*\*\*\s*([\-]?\d+)$",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    ClassifyPowerTerm(seg, ParseDouble(m.Groups[1].Value), ParseDouble(m.Groups[2].Value));
                    continue;
                }

                // 3d) coeff*T  (linear T, must not be followed by *)
                m = Regex.Match(term, @"^([\+\-]?[\d.]+(?:E[\+\-]?\d+)?)\s*\*\s*T$",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    seg.B = ParseDouble(m.Groups[1].Value);
                    continue;
                }

                // 3e) 纯常数项
                if (double.TryParse(term, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double constVal))
                {
                    seg.A += constVal;
                    continue;
                }
            }

            return seg;
        }

        /// <summary>
        /// 将表达式拆分为独立项列表。
        /// 在 +/- 处断开，但保留:
        ///   - 科学计数法中的 E+/E- (如 367.516E-23)
        ///   - 括号内的负号 (如 T**(-1), T**(-9))
        /// 例: "12040.17-6.55843*T-367.516E-23*T**7"
        ///   → ["12040.17", "-6.55843*T", "-367.516E-23*T**7"]
        /// 例: "1225.7+77359*T**(-1)"
        ///   → ["1225.7", "+77359*T**(-1)"]
        /// </summary>
        private List<string> SplitIntoTerms(string expr)
        {
            var terms = new List<string>();
            if (string.IsNullOrWhiteSpace(expr)) return terms;

            int start = 0;
            int parenDepth = 0;

            for (int i = 0; i < expr.Length; i++)
            {
                char ch = expr[i];
                if (ch == '(') { parenDepth++; continue; }
                if (ch == ')') { parenDepth--; continue; }

                // 不在括号内时才考虑断开
                if (parenDepth > 0) continue;

                if (i > 0 && (ch == '+' || ch == '-'))
                {
                    // 检查前一个非空白字符是否为 E/e（科学计数法）
                    int j = i - 1;
                    while (j >= start && expr[j] == ' ') j--;
                    if (j >= start && (expr[j] == 'E' || expr[j] == 'e'))
                    {
                        // 科学计数法，不断开
                        continue;
                    }

                    string token = expr.Substring(start, i - start).Trim();
                    if (!string.IsNullOrEmpty(token))
                        terms.Add(token);
                    start = i; // 新项从 +/- 符号开始
                }
            }
            // 最后一项
            string last = expr.Substring(start).Trim();
            if (!string.IsNullOrEmpty(last))
                terms.Add(last);

            return terms;
        }

        /// <summary>
        /// 将 T**n 项归类到对应的系数字段
        /// </summary>
        private void ClassifyPowerTerm(GibbsSegment seg, double coeff, double exp)
        {
            if (Math.Abs(exp - 2.0) < 0.01)
                seg.D = coeff;
            else if (Math.Abs(exp - 3.0) < 0.01)
                seg.E = coeff;
            else if (Math.Abs(exp - (-1.0)) < 0.01)
                seg.F = coeff;
            else
            {
                seg.SpecialCoeff = coeff;
                seg.SpecialExp = exp;
            }
        }

        /// <summary>
        /// 解析科学计数法双精度数
        /// </summary>
        private static double ParseDouble(string s)
        {
            s = s.Trim().Replace(" ", "");
            if (string.IsNullOrEmpty(s)) return 0;
            if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
                return val;
            return 0;
        }

        /// <summary>
        /// 求解函数在指定温度的值（处理函数间引用）
        /// </summary>
        public double EvaluateFunction(string funcName, double T)
        {
            if (!_functions.ContainsKey(funcName))
                return double.NaN;
            return _functions[funcName].Evaluate(T, EvaluateFunction);
        }

        /// <summary>
        /// 获取元素的标准参考态 Gibbs 能 GHSERXX(T)
        /// </summary>
        public double GetGhser(string element, double T)
        {
            string funcName = "GHSER" + element.ToUpper();
            return EvaluateFunction(funcName, T);
        }

        /// <summary>
        /// 获取元素液相的 Gibbs 能 GLIQXX(T)
        /// </summary>
        public double GetGliq(string element, double T)
        {
            string funcName = "GLIQ" + element.ToUpper();
            return EvaluateFunction(funcName, T);
        }

        /// <summary>
        /// 判断某元素是否有液相数据
        /// </summary>
        public bool HasLiquidData(string element)
        {
            string funcName = "GLIQ" + element.ToUpper();
            return _functions.ContainsKey(funcName);
        }

        /// <summary>
        /// 判断某元素是否有标准参考态数据
        /// </summary>
        public bool HasSolidData(string element)
        {
            string funcName = "GHSER" + element.ToUpper();
            return _functions.ContainsKey(funcName);
        }

        /// <summary>
        /// 获取元素数据
        /// </summary>
        public ElementData GetElementData(string symbol)
        {
            _elements.TryGetValue(symbol, out var ed);
            return ed;
        }

        /// <summary>
        /// 从 GLIQXX 的第一段（包含 +GHSERXX 引用）中提取 ΔG_fusion 多项式系数。
        ///
        /// SGTE TDB 中，GLIQ 的第一段（Tm 以下）通常写作:
        ///   ΔG_f多项式 + GHSERXX
        /// 其中 ΔG_f = a + b*T + c*T*LN(T) + d*T**2 + e*T**3 + f*T**(-1) + special
        ///
        /// 因此该段（去掉 GHSER 引用后）的系数就是 ΔG_fusion 的系数。
        /// </summary>
        /// <param name="element">元素符号</param>
        /// <returns>含 GHSER 引用的 GLIQ 第一段 (ΔG_fusion 系数), 若找不到则返回 null</returns>
        internal GibbsSegment GetFusionSegment(string element)
        {
            string funcName = "GLIQ" + element.ToUpper();
            if (!_functions.ContainsKey(funcName))
                return null;

            var gf = _functions[funcName];

            // 查找含有 GHSER 引用的段（通常是第一段，T < Tm）
            string ghserName = "GHSER" + element.ToUpper();
            foreach (var seg in gf.Segments)
            {
                if (!string.IsNullOrEmpty(seg.RefFunction) &&
                    seg.RefFunction.Equals(ghserName, StringComparison.OrdinalIgnoreCase))
                {
                    return seg;
                }
            }

            return null;
        }

        /// <summary>
        /// 从 GibbsSegment 的 ΔG_fusion 多项式解析计算 ΔHf(T)。
        ///
        /// ΔG_f(T) = a + bT + cT·ln(T) + dT² + eT³ + fT⁻¹ + g·T^n
        ///
        /// ΔH_f(T) = ΔG_f - T · dΔG_f/dT
        ///         = a - cT - dT² - 2eT³ + 2fT⁻¹ + (1-n)·g·T^n
        ///
        /// 特例:
        ///   n=7:  (1-7)gT^7  = -6gT^7
        ///   n=-9: (1+9)gT^-9 = 10gT^(-9)
        /// </summary>
        private double CalcEnthalpyFromSegment(GibbsSegment seg, double T)
        {
            double a = seg.A;
            double c = seg.C;   // T*LN(T) 系数
            double d = seg.D;   // T**2 系数
            double e = seg.E;   // T**3 系数
            double f = seg.F;   // T**(-1) 系数
            double gCoeff = seg.SpecialCoeff;
            double gExp = seg.SpecialExp;

            // ΔH = a - c·T - d·T² - 2e·T³ + 2f·T⁻¹ + (1-n)·g·T^n
            double dH = a - c * T - d * T * T - 2.0 * e * T * T * T;
            if (Math.Abs(T) > 1e-10)
                dH += 2.0 * f / T;
            if (Math.Abs(gCoeff) > 1e-30 && Math.Abs(gExp) > 1e-10)
                dH += (1.0 - gExp) * gCoeff * Math.Pow(T, gExp);

            return dH;
        }

        /// <summary>
        /// 计算纯元素在其熔点处的熔化焓 ΔHf (J/mol)。
        ///
        /// 优先使用解析方法（从 GLIQ 中含 GHSER 引用的段提取 ΔG_fusion 多项式，
        /// 再由 ΔH = a - cT - dT² - 2eT³ + 2f/T + (1-n)gT^n 解析求值）。
        ///
        /// 当 GLIQ 没有 GHSER 引用段时（如 B、Ca 等），使用数值方法:
        /// ΔHf = H_liq(Tm) - H_solid(Tm)，在略低于 Tm 处数值微分。
        /// </summary>
        /// <param name="element">元素符号</param>
        /// <param name="Tm">熔点 (K)，若为0则使用 Element.Tm</param>
        /// <returns>ΔHf in J/mol, 若无数据则返回 NaN</returns>
        public double CalcFusionEnthalpy(string element, double Tm = 0)
        {
            if (!HasLiquidData(element) || !HasSolidData(element))
                return double.NaN;

            // 如果未提供 Tm，尝试从 Element 获取
            if (Tm <= 0)
            {
                Element elem = new Element(element);
                if (elem.isExist && elem.Tm > 0)
                    Tm = elem.Tm;
                else
                    return double.NaN;
            }

            // 方法一: 解析法 — 从 GLIQ 中含 +GHSER 的段直接提取 ΔG_fusion 系数
            var fusionSeg = GetFusionSegment(element);
            if (fusionSeg != null)
            {
                double dH = CalcEnthalpyFromSegment(fusionSeg, Tm);
                if (!double.IsNaN(dH) && dH > 0)
                    return dH;
            }

            // 方法二: 数值法 — 当 GLIQ 没有 GHSER 引用时（独立表达式）
            // ΔHf = H_liq(Tm) - H_solid(Tm)，在 Tm-0.5K 处计算避开分段边界
            double T_eval = Tm - 0.5;
            double dT = 0.1;
            double H_liq = NumericalEnthalpy(T => GetGliq(element, T), T_eval, dT);
            double H_solid = NumericalEnthalpy(T => GetGhser(element, T), T_eval, dT);
            double deltaHf = H_liq - H_solid;

            return deltaHf; // J/mol
        }

        /// <summary>
        /// 通过数值微分计算熵 S(T) = -dG/dT
        /// </summary>
        private double NumericalEntropy(Func<double, double> G_func, double T, double dT = 0.1)
        {
            double G_plus = G_func(T + dT);
            double G_minus = G_func(T - dT);
            return -(G_plus - G_minus) / (2.0 * dT);
        }

        /// <summary>
        /// 通过数值计算焓 H(T) = G(T) + T*S(T) = G(T) - T*dG/dT
        /// </summary>
        private double NumericalEnthalpy(Func<double, double> G_func, double T, double dT = 0.1)
        {
            double G = G_func(T);
            double S = NumericalEntropy(G_func, T, dT);
            return G + T * S;
        }

        /// <summary>
        /// 计算纯元素在其熔点处的熔化焓 ΔHf (kJ/mol)
        /// </summary>
        public double CalcFusionEnthalpy_kJ(string element, double Tm = 0)
        {
            double dH = CalcFusionEnthalpy(element, Tm);
            if (double.IsNaN(dH)) return double.NaN;
            return dH / 1000.0;
        }

        /// <summary>
        /// 获取TDB中已加载的所有函数名
        /// </summary>
        public IEnumerable<string> GetFunctionNames() => _functions.Keys;

        /// <summary>
        /// 获取已加载的元素列表
        /// </summary>
        public IEnumerable<string> GetElementSymbols() => _elements.Keys;

        /// <summary>
        /// 检查是否已成功加载数据
        /// </summary>
        public bool IsLoaded => _loaded;
    }
}
