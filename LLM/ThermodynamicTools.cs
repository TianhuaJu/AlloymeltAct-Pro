using System.Text.Json;

namespace AlloyAct_Pro.LLM
{
    /// <summary>
    /// 热力学计算工具集 - 为 LLM 提供可调用的计算函数接口
    /// 包装现有的 Activity_Coefficient、LiquidusCalculator、Element 等类
    /// </summary>
    public class ThermodynamicTools
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        #region Tool Definitions

        /// <summary>
        /// 获取所有工具定义（供 LLM 使用）
        /// </summary>
        public static List<ToolDefinition> GetToolDefinitions()
        {
            return new List<ToolDefinition>
            {
                MakeToolDef("calculate_liquidus_temperature",
                    "计算合金的液相线温度（开始凝固温度）。基于修正的Schroder-van Laar方程，考虑溶质相互作用对溶剂活度的影响。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂/基体元素符号，如 Fe, Al, Cu"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数。如: {\"Al\": 0.95, \"Cu\": 0.05}。不需要包含溶剂的值，系统会自动计算"" },
                            ""phase_state"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""composition""]
                    }"),

                MakeToolDef("calculate_activity",
                    "计算合金中指定组元的活度 a = γ × x，其中γ是活度系数，x是摩尔分数。使用Wagner/Pelton/Elliot三种模型。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂/基体元素符号"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""要计算活度的溶质元素符号"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""phase_state"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""composition"", ""temperature""]
                    }"),

                MakeToolDef("calculate_activity_coefficient",
                    "计算合金中指定组元的活度系数 lnγ，基于UEM-Miedema模型框架。返回Wagner、Pelton、Elliot三种模型结果。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂/基体元素符号"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""要计算活度系数的溶质元素符号"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""phase_state"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""composition"", ""temperature""]
                    }"),

                MakeToolDef("calculate_interaction_coefficient",
                    "计算一阶活度相互作用系数 εi_j（溶质j对溶质i的影响），基于UEM1模型。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂/基体元素符号"" },
                            ""solute_i"": { ""type"": ""string"", ""description"": ""溶质i元素符号"" },
                            ""solute_j"": { ""type"": ""string"", ""description"": ""溶质j元素符号"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""phase_state"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""solute_i"", ""solute_j"", ""temperature""]
                    }"),

                MakeToolDef("calculate_infinite_dilution_coefficient",
                    "计算溶质i在溶剂中的无限稀活度系数 lnγi⁰，基于Miedema模型。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂/基体元素符号"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""溶质元素符号"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""phase_state"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""temperature""]
                    }"),

                MakeToolDef("calculate_second_order_coefficient",
                    "计算二阶活度相互作用系数 ρi_jk，基于UEM1模型。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂/基体元素符号"" },
                            ""solute_i"": { ""type"": ""string"", ""description"": ""溶质i元素符号"" },
                            ""solute_j"": { ""type"": ""string"", ""description"": ""溶质j元素符号"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""phase_state"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""solute_i"", ""solute_j"", ""temperature""]
                    }"),

                MakeToolDef("get_element_properties",
                    "获取元素的基本热力学性质，包括熔点、原子体积、电负性、电子密度等Miedema参数。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""element"": { ""type"": ""string"", ""description"": ""元素符号，如 Fe, Al, Cu"" }
                        },
                        ""required"": [""element""]
                    }"),

                MakeToolDef("calculate_melting_point_depression",
                    "计算指定溶质含量对溶剂熔点的降低值。基于液相线温度计算。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号，如 Al"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""溶质元素符号，如 Cu"" },
                            ""solute_mole_percent"": { ""type"": ""number"", ""description"": ""溶质摩尔百分比 (0-100)"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""solute_mole_percent""]
                    }"),

                MakeToolDef("convert_unit",
                    "在质量百分比(wt%)和摩尔分数(atom fraction)之间转换。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""溶质元素符号"" },
                            ""value"": { ""type"": ""number"", ""description"": ""要转换的数值"" },
                            ""direction"": { ""type"": ""string"", ""enum"": [""wt_to_mol"", ""mol_to_wt""], ""description"": ""转换方向: wt_to_mol(质量%→摩尔分数) 或 mol_to_wt(摩尔分数→质量%)"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""value"", ""direction""]
                    }"),

                MakeToolDef("plot_chart",
                    "在对话中绘制图表。支持折线图、散点图、柱状图。可同时绘制多条数据曲线进行对比。用于将计算结果可视化展示。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""chart_type"": { ""type"": ""string"", ""enum"": [""line"", ""scatter"", ""bar""], ""description"": ""图表类型，默认line"" },
                            ""title"": { ""type"": ""string"", ""description"": ""图表标题"" },
                            ""x_label"": { ""type"": ""string"", ""description"": ""X轴标签"" },
                            ""y_label"": { ""type"": ""string"", ""description"": ""Y轴标签"" },
                            ""data_series"": {
                                ""type"": ""array"",
                                ""description"": ""数据系列列表"",
                                ""items"": {
                                    ""type"": ""object"",
                                    ""properties"": {
                                        ""name"": { ""type"": ""string"", ""description"": ""数据系列名称"" },
                                        ""x_values"": { ""type"": ""array"", ""items"": { ""type"": ""number"" } },
                                        ""y_values"": { ""type"": ""array"", ""items"": { ""type"": ""number"" } }
                                    },
                                    ""required"": [""name"", ""x_values"", ""y_values""]
                                }
                            }
                        },
                        ""required"": [""title"", ""x_label"", ""y_label"", ""data_series""]
                    }")
            };
        }

        private static ToolDefinition MakeToolDef(string name, string description, string parametersJson)
        {
            return new ToolDefinition
            {
                Name = name,
                Description = description,
                Parameters = JsonDocument.Parse(parametersJson).RootElement.Clone()
            };
        }

        #endregion

        #region Tool Execution

        /// <summary>
        /// 执行工具调用
        /// </summary>
        public static string ExecuteTool(string toolName, string argumentsJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var args = doc.RootElement;

                return toolName switch
                {
                    "calculate_liquidus_temperature" => CalcLiquidus(args),
                    "calculate_activity" => CalcActivity(args),
                    "calculate_activity_coefficient" => CalcActivityCoefficient(args),
                    "calculate_interaction_coefficient" => CalcInteractionCoefficient(args),
                    "calculate_infinite_dilution_coefficient" => CalcInfiniteDilution(args),
                    "calculate_second_order_coefficient" => CalcSecondOrder(args),
                    "get_element_properties" => GetElementProps(args),
                    "calculate_melting_point_depression" => CalcMeltingPointDepression(args),
                    "convert_unit" => ConvertUnit(args),
                    "plot_chart" => HandlePlotChart(args),
                    _ => JsonResult(new { status = "error", message = $"未知工具: {toolName}" })
                };
            }
            catch (Exception ex)
            {
                return JsonResult(new { status = "error", message = ex.Message });
            }
        }

        /// <summary>
        /// 检查工具调用是否为图表请求，返回图表数据
        /// </summary>
        public static Dictionary<string, object>? TryGetChartData(string toolName, string argumentsJson)
        {
            if (toolName != "plot_chart") return null;
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var args = doc.RootElement;
                var chartData = new Dictionary<string, object>
                {
                    ["chart_type"] = args.TryGetProperty("chart_type", out var ct) ? ct.GetString() ?? "line" : "line",
                    ["title"] = args.GetProperty("title").GetString() ?? "",
                    ["x_label"] = args.GetProperty("x_label").GetString() ?? "",
                    ["y_label"] = args.GetProperty("y_label").GetString() ?? ""
                };

                var series = new List<Dictionary<string, object>>();
                foreach (var s in args.GetProperty("data_series").EnumerateArray())
                {
                    var sd = new Dictionary<string, object>
                    {
                        ["name"] = s.GetProperty("name").GetString() ?? "",
                        ["x_values"] = s.GetProperty("x_values").EnumerateArray().Select(v => v.GetDouble()).ToList(),
                        ["y_values"] = s.GetProperty("y_values").EnumerateArray().Select(v => v.GetDouble()).ToList()
                    };
                    series.Add(sd);
                }
                chartData["data_series"] = series;
                return chartData;
            }
            catch { return null; }
        }

        #endregion

        #region Individual Tool Implementations

        private static string CalcLiquidus(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var phase = args.TryGetProperty("phase_state", out var ps) ? ps.GetString() ?? "liquid" : "liquid";

            // Ensure solvent is in composition
            double soluteSum = comp.Values.Sum();
            if (!comp.ContainsKey(solvent))
                comp[solvent] = 1.0 - soluteSum;

            var bm = new Binary_model();
            bm.setTemperature(new Element(solvent).Tm);
            bm.setState(phase);

            var calc = new LiquidusCalculator();
            var result = calc.CalculateLiquidus(solvent, comp, phase, bm.UEM1, "UEM1");

            if (!result.Converged && result.ErrorMessage?.StartsWith("NEED_DELTAHF") == true)
            {
                return JsonResult(new
                {
                    status = "error",
                    message = $"无法从TDB数据库获取{solvent}的熔化焓ΔHf，该功能需要用户手动输入ΔHf值"
                });
            }

            return JsonResult(new
            {
                status = result.Converged ? "success" : "partial",
                solvent,
                composition = result.Composition,
                pure_melting_point_K = result.T_pure_melting,
                pure_melting_point_C = result.T_pure_melting - 273.15,
                delta_Hf_kJ_mol = result.DeltaHf,
                liquidus_Wagner_K = result.T_liquidus_Wagner,
                liquidus_Wagner_C = double.IsNaN(result.T_liquidus_Wagner) ? double.NaN : result.T_liquidus_Wagner - 273.15,
                liquidus_Pelton_K = result.T_liquidus_Pelton,
                liquidus_Pelton_C = double.IsNaN(result.T_liquidus_Pelton) ? double.NaN : result.T_liquidus_Pelton - 273.15,
                liquidus_Elliot_K = result.T_liquidus_Elliot,
                liquidus_Elliot_C = double.IsNaN(result.T_liquidus_Elliot) ? double.NaN : result.T_liquidus_Elliot - 273.15,
                depression_Wagner_K = result.DeltaT_Wagner,
                depression_Pelton_K = result.DeltaT_Pelton,
                depression_Elliot_K = result.DeltaT_Elliot,
                activity_Wagner = result.SolventActivity_Wagner,
                activity_Pelton = result.SolventActivity_Pelton,
                activity_Elliot = result.SolventActivity_Elliot,
                error = result.ErrorMessage
            });
        }

        private static string CalcActivity(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var solute = args.GetProperty("solute").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var T = args.GetProperty("temperature").GetDouble();
            var phase = args.TryGetProperty("phase_state", out var ps) ? ps.GetString() ?? "liquid" : "liquid";

            EnsureSolvent(comp, solvent);

            var ac = new Activity_Coefficient();
            ac.set_CompositionDict(CompDictToString(comp));

            var bm = new Binary_model();
            bm.setTemperature(T);
            bm.setState(phase);

            var info = (state: phase, T: T);
            double lnGamma_w = ac.activity_Coefficient_Wagner(comp, solvent, solute, bm.UEM1, "UEM1", info);
            double lnGamma_p = ac.activity_coefficient_Pelton(comp, solute, solvent, T, bm.UEM1, "UEM1", phase);
            double lnGamma_e = ac.activity_coefficient_Elloit(comp, solute, solvent, T, bm.UEM1, "UEM1", phase);

            double x = comp.ContainsKey(solute) ? comp[solute] : 0;

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute,
                temperature_K = T,
                mole_fraction = x,
                phase,
                wagner = new { lnGamma = lnGamma_w, gamma = Math.Exp(lnGamma_w), activity = x * Math.Exp(lnGamma_w), lnActivity = lnGamma_w + Math.Log(x) },
                pelton = new { lnGamma = lnGamma_p, gamma = Math.Exp(lnGamma_p), activity = x * Math.Exp(lnGamma_p), lnActivity = lnGamma_p + Math.Log(x) },
                elliot = new { lnGamma = lnGamma_e, gamma = Math.Exp(lnGamma_e), activity = x * Math.Exp(lnGamma_e), lnActivity = lnGamma_e + Math.Log(x) }
            });
        }

        private static string CalcActivityCoefficient(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var solute = args.GetProperty("solute").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var T = args.GetProperty("temperature").GetDouble();
            var phase = args.TryGetProperty("phase_state", out var ps) ? ps.GetString() ?? "liquid" : "liquid";

            EnsureSolvent(comp, solvent);

            var ac = new Activity_Coefficient();
            ac.set_CompositionDict(CompDictToString(comp));

            var bm = new Binary_model();
            bm.setTemperature(T);
            bm.setState(phase);

            var info = (state: phase, T: T);
            double lnGamma_w = ac.activity_Coefficient_Wagner(comp, solvent, solute, bm.UEM1, "UEM1", info);
            double lnGamma_p = ac.activity_coefficient_Pelton(comp, solute, solvent, T, bm.UEM1, "UEM1", phase);
            double lnGamma_e = ac.activity_coefficient_Elloit(comp, solute, solvent, T, bm.UEM1, "UEM1", phase);

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute,
                temperature_K = T,
                phase,
                wagner = new { lnGamma = lnGamma_w, gamma = Math.Exp(lnGamma_w) },
                pelton = new { lnGamma = lnGamma_p, gamma = Math.Exp(lnGamma_p) },
                elliot = new { lnGamma = lnGamma_e, gamma = Math.Exp(lnGamma_e) }
            });
        }

        private static string CalcInteractionCoefficient(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var soluteI = args.GetProperty("solute_i").GetString()!;
            var soluteJ = args.GetProperty("solute_j").GetString()!;
            var T = args.GetProperty("temperature").GetDouble();
            var phase = args.TryGetProperty("phase_state", out var ps) ? ps.GetString() ?? "liquid" : "liquid";

            var solv = new Element(solvent);
            var solui = new Element(soluteI);
            var soluj = new Element(soluteJ);

            var bm = new Binary_model();
            bm.setTemperature(T);
            bm.setState(phase);

            var ternary = new Ternary_melts(T, phase);
            double epsilon = ternary.Activity_Interact_Coefficient_1st(solv, solui, soluj, bm.UEM1, "UEM1");

            // Also get experimental value
            var melt = new Melt(solvent, soluteI, soluteJ, T);

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute_i = soluteI,
                solute_j = soluteJ,
                temperature_K = T,
                phase,
                epsilon_ij_UEM1 = epsilon,
                epsilon_ij_exp_molar = melt.sji,
                epsilon_ij_exp_weight = melt.eji
            });
        }

        private static string CalcInfiniteDilution(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var solute = args.GetProperty("solute").GetString()!;
            var T = args.GetProperty("temperature").GetDouble();
            var phase = args.TryGetProperty("phase_state", out var ps) ? ps.GetString() ?? "liquid" : "liquid";

            var solv = new Element(solvent);
            var solui = new Element(solute);

            var ternary = new Ternary_melts(T, phase);
            double lnY0 = ternary.lnY0(solv, solui);

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute,
                temperature_K = T,
                phase,
                lnGamma0 = lnY0,
                gamma0 = double.IsNaN(lnY0) ? double.NaN : Math.Exp(lnY0)
            });
        }

        private static string CalcSecondOrder(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var soluteI = args.GetProperty("solute_i").GetString()!;
            var soluteJ = args.GetProperty("solute_j").GetString()!;
            var T = args.GetProperty("temperature").GetDouble();
            var phase = args.TryGetProperty("phase_state", out var ps) ? ps.GetString() ?? "liquid" : "liquid";

            var solv = new Element(solvent);
            var solui = new Element(soluteI);
            var soluj = new Element(soluteJ);

            var bm = new Binary_model();
            bm.setTemperature(T);
            bm.setState(phase);

            var ternary = new Ternary_melts(T, phase);
            double rho_ii = ternary.Roui_ii(solv, solui, bm.UEM1);
            double rho_jj = ternary.Roui_jj(solv, solui, soluj, bm.UEM1);
            double rho_ij = ternary.Roui_ij(solv, solui, soluj, bm.UEM1);

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute_i = soluteI,
                solute_j = soluteJ,
                temperature_K = T,
                phase,
                rho_i_ii = rho_ii,
                rho_i_jj = rho_jj,
                rho_i_ij = rho_ij
            });
        }

        private static string GetElementProps(JsonElement args)
        {
            var name = args.GetProperty("element").GetString()!;
            var elem = new Element(name);

            if (!elem.isExist)
                return JsonResult(new { status = "error", message = $"元素 {name} 不存在于数据库中" });

            return JsonResult(new
            {
                status = "success",
                element = name,
                melting_point_K = elem.Tm,
                melting_point_C = elem.Tm - 273.15,
                boiling_point_K = elem.Tb,
                molar_mass = elem.M,
                molar_volume_V = elem.V,
                electronegativity_Phi = elem.Phi,
                electron_density_nws = elem.N_WS,
                hybrid_parameter_u = elem.u,
                is_transition_group = elem.isTrans_group,
                fusion_heat = elem.fuse_heat,
                bulk_modulus = elem.Bkm,
                shear_modulus = elem.Shm
            });
        }

        private static string CalcMeltingPointDepression(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var solute = args.GetProperty("solute").GetString()!;
            var percent = args.GetProperty("solute_mole_percent").GetDouble();

            var x_solute = percent / 100.0;
            var comp = new Dictionary<string, double>
            {
                [solvent] = 1.0 - x_solute,
                [solute] = x_solute
            };

            var bm = new Binary_model();
            bm.setTemperature(new Element(solvent).Tm);
            bm.setState("liquid");

            var calc = new LiquidusCalculator();
            var result = calc.CalculateLiquidus(solvent, comp, "liquid", bm.UEM1, "UEM1");

            if (!result.Converged && result.ErrorMessage?.StartsWith("NEED_DELTAHF") == true)
            {
                return JsonResult(new { status = "error", message = $"无法获取{solvent}的熔化焓ΔHf" });
            }

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute,
                solute_mole_percent = percent,
                pure_melting_point_K = result.T_pure_melting,
                liquidus_Wagner_K = result.T_liquidus_Wagner,
                liquidus_Wagner_C = double.IsNaN(result.T_liquidus_Wagner) ? double.NaN : result.T_liquidus_Wagner - 273.15,
                depression_Wagner_K = result.DeltaT_Wagner,
                depression_per_percent = percent > 0 ? result.DeltaT_Wagner / percent : 0
            });
        }

        private static string ConvertUnit(JsonElement args)
        {
            var solventName = args.GetProperty("solvent").GetString()!;
            var soluteName = args.GetProperty("solute").GetString()!;
            var value = args.GetProperty("value").GetDouble();
            var direction = args.GetProperty("direction").GetString()!;

            var solvent = new Element(solventName);
            var solute = new Element(soluteName);

            if (!solvent.isExist || !solute.isExist)
                return JsonResult(new { status = "error", message = "元素不存在" });

            double result;
            string fromUnit, toUnit;

            if (direction == "wt_to_mol")
            {
                // wt% → mole fraction
                double w = value / 100.0;
                double n_solute = w / solute.M;
                double n_solvent = (1.0 - w) / solvent.M;
                result = n_solute / (n_solute + n_solvent);
                fromUnit = "wt%";
                toUnit = "mole fraction";
            }
            else
            {
                // mole fraction → wt%
                double x = value;
                double w_solute = x * solute.M;
                double w_solvent = (1.0 - x) * solvent.M;
                result = w_solute / (w_solute + w_solvent) * 100.0;
                fromUnit = "mole fraction";
                toUnit = "wt%";
            }

            return JsonResult(new
            {
                status = "success",
                solvent = solventName,
                solute = soluteName,
                input_value = value,
                input_unit = fromUnit,
                result_value = result,
                result_unit = toUnit
            });
        }

        private static string HandlePlotChart(JsonElement args)
        {
            // Just validate and return success - actual rendering is handled by UI
            var series = args.GetProperty("data_series");
            if (series.GetArrayLength() == 0)
                return JsonResult(new { status = "error", message = "数据系列不能为空" });

            return JsonResult(new
            {
                status = "success",
                type = "chart",
                message = "图表已生成"
            });
        }

        #endregion

        #region Helpers

        private static Dictionary<string, double> ParseComposition(JsonElement comp)
        {
            var dict = new Dictionary<string, double>();
            foreach (var prop in comp.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.GetDouble();
            }
            return dict;
        }

        private static void EnsureSolvent(Dictionary<string, double> comp, string solvent)
        {
            if (!comp.ContainsKey(solvent))
            {
                double soluteSum = comp.Values.Sum();
                comp[solvent] = 1.0 - soluteSum;
            }
        }

        private static string CompDictToString(Dictionary<string, double> comp)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in comp)
                sb.Append($"{kvp.Key}{kvp.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            return sb.ToString();
        }

        private static string JsonResult(object obj)
        {
            return JsonSerializer.Serialize(obj, JsonOpts);
        }

        #endregion
    }
}
