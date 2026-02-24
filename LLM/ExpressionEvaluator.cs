namespace AlloyAct_Pro.LLM
{
    /// <summary>
    /// 轻量级递归下降数学表达式求值器
    /// 支持：+, -, *, /, ^, ()
    /// 函数：ln, log, log10, exp, sqrt, pow, abs, sin, cos, tan, min, max
    /// 常数：R=8.314, pi, e, kB=1.380649e-23
    /// 变量：通过 Dictionary&lt;string, double&gt; 传入
    /// </summary>
    public class ExpressionEvaluator
    {
        private string _expr = "";
        private int _pos;
        private Dictionary<string, double> _variables = new();

        private static readonly Dictionary<string, double> Constants = new(StringComparer.OrdinalIgnoreCase)
        {
            ["R"] = 8.314,              // J/(mol·K) 气体常数
            ["pi"] = Math.PI,
            ["e"] = Math.E,
            ["kB"] = 1.380649e-23,      // J/K 玻尔兹曼常数
            ["NA"] = 6.02214076e23,     // mol⁻¹ 阿伏伽德罗常数
            ["F"] = 96485.33212,        // C/mol 法拉第常数
            ["h"] = 6.62607015e-34      // J·s 普朗克常数
        };

        /// <summary>
        /// 求值入口：传入表达式和变量值
        /// </summary>
        public static double Evaluate(string expression, Dictionary<string, double> variables)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("表达式不能为空");

            var evaluator = new ExpressionEvaluator
            {
                _expr = expression.Replace(" ", ""),  // 去除空格
                _pos = 0,
                _variables = variables ?? new Dictionary<string, double>()
            };

            double result = evaluator.ParseExpression();

            // 确保表达式已完全解析
            if (evaluator._pos < evaluator._expr.Length)
                throw new FormatException($"表达式解析错误：位置 {evaluator._pos} 处有多余字符 '{evaluator._expr[evaluator._pos]}'");

            return result;
        }

        /// <summary>
        /// 验证表达式语法是否正确（不实际计算）
        /// </summary>
        public static bool TryValidate(string expression, IEnumerable<string> parameterNames, out string? error)
        {
            try
            {
                // 用默认值 1.0 填充所有参数进行试算
                var testVars = new Dictionary<string, double>();
                foreach (var name in parameterNames)
                    testVars[name] = 1.0;

                Evaluate(expression, testVars);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // ===== 递归下降解析器 =====

        /// <summary>加减法（最低优先级）</summary>
        private double ParseExpression()
        {
            double result = ParseTerm();

            while (_pos < _expr.Length)
            {
                char ch = _expr[_pos];
                if (ch == '+')
                {
                    _pos++;
                    result += ParseTerm();
                }
                else if (ch == '-')
                {
                    _pos++;
                    result -= ParseTerm();
                }
                else break;
            }
            return result;
        }

        /// <summary>乘除法</summary>
        private double ParseTerm()
        {
            double result = ParsePower();

            while (_pos < _expr.Length)
            {
                char ch = _expr[_pos];
                if (ch == '*')
                {
                    _pos++;
                    result *= ParsePower();
                }
                else if (ch == '/')
                {
                    _pos++;
                    double divisor = ParsePower();
                    if (divisor == 0) return double.NaN;  // 除零保护
                    result /= divisor;
                }
                else break;
            }
            return result;
        }

        /// <summary>幂运算（右结合）</summary>
        private double ParsePower()
        {
            double baseVal = ParseUnary();

            if (_pos < _expr.Length && _expr[_pos] == '^')
            {
                _pos++;
                double exponent = ParsePower();  // 右结合递归
                return Math.Pow(baseVal, exponent);
            }
            return baseVal;
        }

        /// <summary>一元运算符（负号）</summary>
        private double ParseUnary()
        {
            if (_pos < _expr.Length && _expr[_pos] == '-')
            {
                _pos++;
                return -ParsePrimary();
            }
            if (_pos < _expr.Length && _expr[_pos] == '+')
            {
                _pos++;
                return ParsePrimary();
            }
            return ParsePrimary();
        }

        /// <summary>基本单元：数字、变量、常数、函数、括号</summary>
        private double ParsePrimary()
        {
            if (_pos >= _expr.Length)
                throw new FormatException("表达式意外结束");

            char ch = _expr[_pos];

            // 括号
            if (ch == '(')
            {
                _pos++;  // 跳过 '('
                double result = ParseExpression();
                if (_pos >= _expr.Length || _expr[_pos] != ')')
                    throw new FormatException("缺少右括号 ')'");
                _pos++;  // 跳过 ')'
                return result;
            }

            // 数字（包括小数和科学计数法）
            if (char.IsDigit(ch) || ch == '.')
            {
                return ParseNumber();
            }

            // 标识符（变量、常数、函数）
            if (char.IsLetter(ch) || ch == '_')
            {
                string name = ParseIdentifier();

                // 检查是否是函数调用
                if (_pos < _expr.Length && _expr[_pos] == '(')
                {
                    return ParseFunction(name);
                }

                // 常数
                if (Constants.TryGetValue(name, out double constVal))
                    return constVal;

                // 变量
                if (_variables.TryGetValue(name, out double varVal))
                    return varVal;

                // 大小写不敏感查找变量
                foreach (var kv in _variables)
                {
                    if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                        return kv.Value;
                }

                throw new FormatException($"未知变量或常数: '{name}'");
            }

            throw new FormatException($"无法识别的字符: '{ch}' (位置 {_pos})");
        }

        /// <summary>解析数字</summary>
        private double ParseNumber()
        {
            int start = _pos;

            // 整数部分
            while (_pos < _expr.Length && char.IsDigit(_expr[_pos]))
                _pos++;

            // 小数部分
            if (_pos < _expr.Length && _expr[_pos] == '.')
            {
                _pos++;
                while (_pos < _expr.Length && char.IsDigit(_expr[_pos]))
                    _pos++;
            }

            // 科学计数法
            if (_pos < _expr.Length && (_expr[_pos] == 'e' || _expr[_pos] == 'E'))
            {
                _pos++;
                if (_pos < _expr.Length && (_expr[_pos] == '+' || _expr[_pos] == '-'))
                    _pos++;
                while (_pos < _expr.Length && char.IsDigit(_expr[_pos]))
                    _pos++;
            }

            string numStr = _expr.Substring(start, _pos - start);
            if (double.TryParse(numStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
                return value;

            throw new FormatException($"无效数字: '{numStr}'");
        }

        /// <summary>解析标识符</summary>
        private string ParseIdentifier()
        {
            int start = _pos;
            while (_pos < _expr.Length && (char.IsLetterOrDigit(_expr[_pos]) || _expr[_pos] == '_'))
                _pos++;
            return _expr.Substring(start, _pos - start);
        }

        /// <summary>解析函数调用</summary>
        private double ParseFunction(string name)
        {
            _pos++;  // 跳过 '('

            // 双参数函数
            if (name.Equals("pow", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("min", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("max", StringComparison.OrdinalIgnoreCase))
            {
                double arg1 = ParseExpression();
                if (_pos >= _expr.Length || _expr[_pos] != ',')
                    throw new FormatException($"函数 {name} 需要两个参数，缺少逗号");
                _pos++;  // 跳过 ','
                double arg2 = ParseExpression();

                if (_pos >= _expr.Length || _expr[_pos] != ')')
                    throw new FormatException($"函数 {name} 缺少右括号");
                _pos++;  // 跳过 ')'

                return name.ToLowerInvariant() switch
                {
                    "pow" => Math.Pow(arg1, arg2),
                    "min" => Math.Min(arg1, arg2),
                    "max" => Math.Max(arg1, arg2),
                    _ => throw new FormatException($"未知双参数函数: {name}")
                };
            }

            // 单参数函数
            double arg = ParseExpression();

            if (_pos >= _expr.Length || _expr[_pos] != ')')
                throw new FormatException($"函数 {name} 缺少右括号");
            _pos++;  // 跳过 ')'

            return name.ToLowerInvariant() switch
            {
                "ln" => arg > 0 ? Math.Log(arg) : double.NaN,
                "log" => arg > 0 ? Math.Log(arg) : double.NaN,        // log 默认自然对数
                "log10" => arg > 0 ? Math.Log10(arg) : double.NaN,
                "log2" => arg > 0 ? Math.Log2(arg) : double.NaN,
                "exp" => Math.Exp(arg),
                "sqrt" => arg >= 0 ? Math.Sqrt(arg) : double.NaN,
                "abs" => Math.Abs(arg),
                "sin" => Math.Sin(arg),
                "cos" => Math.Cos(arg),
                "tan" => Math.Tan(arg),
                "asin" => Math.Asin(arg),
                "acos" => Math.Acos(arg),
                "atan" => Math.Atan(arg),
                "sinh" => Math.Sinh(arg),
                "cosh" => Math.Cosh(arg),
                "tanh" => Math.Tanh(arg),
                "ceil" => Math.Ceiling(arg),
                "floor" => Math.Floor(arg),
                "round" => Math.Round(arg),
                _ => throw new FormatException($"未知函数: {name}")
            };
        }
    }
}
