using System.Text.Json;

namespace AlloyAct_Pro.LLM
{
    /// <summary>
    /// 热力学计算工具集 - 为 LLM 提供可调用的计算函数接口（20个工具）
    /// 包装现有的 Activity_Coefficient、LiquidusCalculator、Element、Ternary_melts、Binary_model 等类
    /// </summary>
    /// <summary>
    /// 自定义 JSON double 转换器：禁止科学计数法，保留4位有效数字，输出普通小数
    /// </summary>
    public class PlainDecimalJsonConverter : System.Text.Json.Serialization.JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDouble();

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsNaN(value)) { writer.WriteNullValue(); return; }
            if (double.IsInfinity(value)) { writer.WriteNullValue(); return; }

            // 根据数值大小选择合适格式：
            // 避免科学计数法，同时保持合理精度
            double abs = Math.Abs(value);
            string formatted;
            if (abs == 0)
                formatted = "0";
            else if (abs >= 1000)
                formatted = value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);   // 909.09
            else if (abs >= 1)
                formatted = value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);   // 1.2345
            else if (abs >= 0.001)
                formatted = value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);   // 0.001234
            else
                formatted = value.ToString("E4", System.Globalization.CultureInfo.InvariantCulture);   // 极小值才用科学计数

            // 去掉末尾多余的0（保留至少小数点后1位）
            if (formatted.Contains('.') && !formatted.Contains('E'))
            {
                formatted = formatted.TrimEnd('0').TrimEnd('.');
                // 对于整数结果，如果原值有小数部分（如 909.10），保留至少1位小数
                if (!formatted.Contains('.') && abs < 1e10)
                    formatted += ".0";
            }

            writer.WriteRawValue(formatted);
        }
    }

    public class ThermodynamicTools
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new PlainDecimalJsonConverter() }
        };

        // 记忆系统引用（由 ChatAgent 设置）
        public static MemoryStore? Memory { get; set; }

        // 自定义模型存储引用（由 ChatAgent 设置）
        public static CustomModelStore? CustomModels { get; set; }

        // DFT 数据引用（由 DftPanel 设置）
        public static List<DFT.DftResult>? DftResults { get; set; }

        #region Tool Definitions (20 tools)

        public static List<ToolDefinition> GetToolDefinitions()
        {
            return new List<ToolDefinition>
            {
                // ===== 活度相互作用系数（3个） =====
                MakeToolDef("get_interaction_coefficient",
                    "计算一阶活度相互作用系数 εi_j（溶质j对溶质i的一阶相互作用系数），基于UEM-Miedema模型。包含UEM1理论值和实验值对比。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂/基体元素符号，如 Fe, Al, Cu"" },
                            ""solute_i"": { ""type"": ""string"", ""description"": ""溶质i元素符号"" },
                            ""solute_j"": { ""type"": ""string"", ""description"": ""溶质j元素符号"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" },
                            ""extrapolation_model"": { ""type"": ""string"", ""enum"": [""UEM1"", ""UEM2"", ""Muggianu"", ""Toop_Muggianu"", ""Toop_Kohler""], ""description"": ""外推模型，默认UEM1"" }
                        },
                        ""required"": [""solvent"", ""solute_i"", ""solute_j"", ""temperature""]
                    }"),

                MakeToolDef("get_second_order_interaction_coefficient",
                    "计算二阶活度相互作用系数 ρ（支持 ρ_ii, ρ_jj, ρ_ij 三种类型），基于UEM模型。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""solute_i"": { ""type"": ""string"", ""description"": ""溶质i元素符号"" },
                            ""solute_j"": { ""type"": ""string"", ""description"": ""溶质j元素符号"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""coefficient_type"": { ""type"": ""string"", ""enum"": [""rho_ii"", ""rho_jj"", ""rho_ij"", ""all""], ""description"": ""系数类型，默认all返回全部"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" },
                            ""extrapolation_model"": { ""type"": ""string"", ""enum"": [""UEM1"", ""UEM2"", ""Muggianu"", ""Toop_Muggianu"", ""Toop_Kohler""], ""description"": ""外推模型，默认UEM1"" }
                        },
                        ""required"": [""solvent"", ""solute_i"", ""solute_j"", ""temperature""]
                    }"),

                MakeToolDef("get_infinite_dilution_activity_coefficient",
                    "计算溶质i在溶剂中的无限稀释活度系数 ln(γ⁰)，基于Miedema模型。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂/基体元素符号"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""溶质元素符号"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""temperature""]
                    }"),

                // ===== 溶剂活度系数（2个） =====
                MakeToolDef("calculate_solvent_activity_coefficient",
                    "计算多组元合金熔体中溶剂（基体）的活度系数 lnγ_solvent。基于 Gibbs-Duhem 方程，由溶质的活度相互作用系数积分得到溶剂活度系数。支持 Wagner/Darken(Pelton)/Elliott 三种模型。同时返回溶剂活度 a_solvent = γ_solvent × x_solvent。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂/基体元素符号，如 Fe, Al, Cu"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分（溶质部分），键为元素符号，值为摩尔分数。不需要包含溶剂，系统自动计算"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""activity_model"": { ""type"": ""string"", ""enum"": [""Wagner"", ""Darken"", ""Elliott"", ""all""], ""description"": ""活度模型，默认all返回全部三种模型结果"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" },
                            ""extrapolation_model"": { ""type"": ""string"", ""enum"": [""UEM1"", ""UEM2"", ""Muggianu"", ""Toop_Muggianu"", ""Toop_Kohler""], ""description"": ""外推模型，默认UEM1"" }
                        },
                        ""required"": [""solvent"", ""composition"", ""temperature""]
                    }"),

                MakeToolDef("calculate_solvent_activity_darken",
                    "Darken 二次式直接解析计算溶剂（基体）的活度系数 lnγ_solvent。无需 Gibbs-Duhem 积分，直接利用 Wagner 活度相互作用系数通过 Darken 二次式公式求解。公式: lnγ₁ = -½·Σᵢ εᵢⁱ·Xᵢ² - Σᵢ<ⱼ εᵢʲ·Xᵢ·Xⱼ。适用于稀溶液。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂/基体元素符号，如 Fe, Al, Cu"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分（溶质部分），键为元素符号，值为摩尔分数。不需要包含溶剂，系统自动计算"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" },
                            ""extrapolation_model"": { ""type"": ""string"", ""enum"": [""UEM1"", ""UEM2"", ""Muggianu"", ""Toop_Muggianu"", ""Toop_Kohler""], ""description"": ""外推模型，默认UEM1"" }
                        },
                        ""required"": [""solvent"", ""composition"", ""temperature""]
                    }"),

                // ===== 热力学性质（7个） =====
                MakeToolDef("calculate_activity",
                    "计算合金中指定组元的活度 a = γ × x，其中γ是活度系数，x是摩尔分数。支持Wagner/Pelton/Elliott三种活度模型。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""要计算活度的溶质元素符号"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""activity_model"": { ""type"": ""string"", ""enum"": [""Wagner"", ""Darken"", ""Elliott"", ""all""], ""description"": ""活度模型，默认all返回全部"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" },
                            ""extrapolation_model"": { ""type"": ""string"", ""enum"": [""UEM1"", ""UEM2""], ""description"": ""外推模型，默认UEM1"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""composition"", ""temperature""]
                    }"),

                MakeToolDef("calculate_activity_coefficient",
                    "计算合金中指定组元的活度系数 lnγ。支持Wagner/Pelton/Elliott三种活度模型。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""溶质元素符号"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""activity_model"": { ""type"": ""string"", ""enum"": [""Wagner"", ""Darken"", ""Elliott"", ""all""], ""description"": ""活度模型，默认all返回全部"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" },
                            ""extrapolation_model"": { ""type"": ""string"", ""enum"": [""UEM1"", ""UEM2""], ""description"": ""外推模型，默认UEM1"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""composition"", ""temperature""]
                    }"),

                MakeToolDef("calculate_chemical_potential",
                    "计算合金中指定组元的化学势 μ = μ⁰ + RT·ln(a)，基于活度计算。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""要计算化学势的溶质元素符号"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""activity_model"": { ""type"": ""string"", ""enum"": [""Wagner"", ""Darken"", ""Elliott"", ""all""], ""description"": ""活度模型，默认all返回全部"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""composition"", ""temperature""]
                    }"),

                MakeToolDef("calculate_mixing_enthalpy",
                    "计算合金的混合焓 ΔH_mix，基于二元相互作用模型（Miedema）。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""composition"", ""temperature""]
                    }"),

                MakeToolDef("calculate_gibbs_energy",
                    "计算合金的Gibbs自由能变化 ΔG = ΔH - TΔS = RT·Σ(xi·ln(ai))。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""activity_model"": { ""type"": ""string"", ""enum"": [""Wagner"", ""Darken"", ""Elliott"", ""all""], ""description"": ""活度模型，默认all返回全部"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""composition"", ""temperature""]
                    }"),

                MakeToolDef("calculate_entropy",
                    "计算合金的摩尔混合熵 ΔS_mix。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""activity_model"": { ""type"": ""string"", ""enum"": [""Wagner"", ""Darken"", ""Elliott"", ""all""], ""description"": ""活度模型，默认all返回全部"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""composition"", ""temperature""]
                    }"),

                MakeToolDef("calculate_all_properties",
                    "一次性计算指定组元的所有热力学性质（活度、活度系数、化学势、混合焓、Gibbs自由能、熵）。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""溶质元素符号"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""activity_model"": { ""type"": ""string"", ""enum"": [""Wagner"", ""Darken"", ""Elliott"", ""all""], ""description"": ""活度模型，默认all返回全部"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""composition"", ""temperature""]
                    }"),

                // ===== 相图与温度（3个） =====
                MakeToolDef("calculate_liquidus_temperature",
                    "计算合金的液相线温度（开始凝固温度）。基于修正的Schroder-van Laar方程，考虑溶质相互作用对溶剂活度的影响。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂/基体元素符号，如 Fe, Al, Cu"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数。不需要包含溶剂的值，系统会自动计算"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""composition""]
                    }"),

                MakeToolDef("calculate_precipitation_temperature",
                    "计算指定溶质在合金中的析出温度（溶质开始从熔体中析出的温度）。通过计算溶质的液相线温度实现。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""析出溶质元素符号"" },
                            ""composition"": { ""type"": ""object"", ""description"": ""合金成分，键为元素符号，值为摩尔分数"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""composition""]
                    }"),

                MakeToolDef("calculate_melting_point_depression",
                    "计算指定溶质含量对溶剂熔点的降低值。基于液相线温度计算。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""solute"": { ""type"": ""string"", ""description"": ""溶质元素符号"" },
                            ""solute_content_percent"": { ""type"": ""number"", ""description"": ""溶质摩尔百分比 (0-100)"" }
                        },
                        ""required"": [""solvent"", ""solute"", ""solute_content_percent""]
                    }"),

                // ===== 合金设计（1个） =====
                MakeToolDef("screen_elements_liquidus_effect",
                    "批量筛选元素对合金液相线温度的影响。计算多种溶质在给定浓度下对溶剂熔点的降低程度，用于合金成分设计。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""candidate_elements"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""description"": ""候选溶质元素列表"" },
                            ""solute_content_percent"": { ""type"": ""number"", ""description"": ""溶质摩尔百分比(0-100)，默认1%"" }
                        },
                        ""required"": [""solvent"", ""candidate_elements""]
                    }"),

                // ===== 辅助（2个） =====
                MakeToolDef("get_element_properties",
                    "获取元素的基本热力学性质，包括熔点、原子体积、电负性、电子密度等Miedema参数。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""element"": { ""type"": ""string"", ""description"": ""元素符号，如 Fe, Al, Cu"" }
                        },
                        ""required"": [""element""]
                    }"),

                MakeToolDef("plot_chart",
                    "在对话中绘制图表。支持折线图、散点图、柱状图。可同时绘制多条数据曲线进行对比。当用户要求可视化或绘图时，先进行计算，再调用此工具。",
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
                    }"),

                // ===== 贡献系数（1个） =====
                MakeToolDef("get_contribution_coefficients",
                    "获取三元合金外推的贡献系数（UEM模型的yeta参数），用于评估不同元素对外推精度的贡献。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""solvent"": { ""type"": ""string"", ""description"": ""溶剂元素符号"" },
                            ""solute_i"": { ""type"": ""string"", ""description"": ""溶质i元素符号"" },
                            ""solute_j"": { ""type"": ""string"", ""description"": ""溶质j元素符号"" },
                            ""temperature"": { ""type"": ""number"", ""description"": ""温度(K)"" },
                            ""phase"": { ""type"": ""string"", ""enum"": [""liquid"", ""solid""], ""description"": ""相态，默认liquid"" }
                        },
                        ""required"": [""solvent"", ""solute_i"", ""solute_j"", ""temperature""]
                    }"),

                // ===== 记忆工具（3个） =====
                MakeToolDef("save_memory",
                    "保存用户偏好或常用设置到记忆中。当用户表达偏好或常用参数时使用。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""content"": { ""type"": ""string"", ""description"": ""要记忆的内容"" },
                            ""category"": { ""type"": ""string"", ""enum"": [""preference"", ""alloy_system"", ""calculation"", ""general""], ""description"": ""记忆分类：preference(默认计算设置), alloy_system(常用合金体系), calculation(计算规则与经验), general(其他)"" }
                        },
                        ""required"": [""content"", ""category""]
                    }"),

                MakeToolDef("recall_memories",
                    "回忆之前保存的信息。可选关键词搜索或返回全部记忆。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""keyword"": { ""type"": ""string"", ""description"": ""搜索关键词（可选，不提供则返回全部）"" }
                        },
                        ""required"": []
                    }"),

                MakeToolDef("delete_memory",
                    "删除一条之前保存的记忆。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""content"": { ""type"": ""string"", ""description"": ""要删除的记忆内容"" }
                        },
                        ""required"": [""content""]
                    }"),

                // ===== 自定义模型工具（4个） =====
                MakeToolDef("create_custom_model",
                    "创建一个自定义计算模型。用户描述计算公式和参数后，使用此工具保存为可重复使用的模型。模型在重启后仍然可用。支持数学运算(+,-,*,/,^)、函数(ln,log,exp,sqrt,pow,abs,sin,cos,tan)和常数(R=8.314,pi,e)。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""name"": { ""type"": ""string"", ""description"": ""模型名称（英文，如 solubility_product, debye_temperature）"" },
                            ""display_name"": { ""type"": ""string"", ""description"": ""中文显示名称（如 溶度积计算）"" },
                            ""description"": { ""type"": ""string"", ""description"": ""模型描述，说明用途和适用范围"" },
                            ""formula"": { ""type"": ""string"", ""description"": ""数学公式表达式，如 A+B/T、exp(-DG/(R*T))、pow(10,A+B/T)"" },
                            ""parameters"": {
                                ""type"": ""array"",
                                ""description"": ""参数列表"",
                                ""items"": {
                                    ""type"": ""object"",
                                    ""properties"": {
                                        ""name"": { ""type"": ""string"", ""description"": ""参数名（在公式中使用）"" },
                                        ""description"": { ""type"": ""string"", ""description"": ""参数描述"" },
                                        ""default_value"": { ""type"": ""number"", ""description"": ""默认值（可选）"" },
                                        ""unit"": { ""type"": ""string"", ""description"": ""单位（可选）"" },
                                        ""is_required"": { ""type"": ""boolean"", ""description"": ""是否必填，默认true"" }
                                    },
                                    ""required"": [""name"", ""description""]
                                }
                            },
                            ""result_name"": { ""type"": ""string"", ""description"": ""结果名称（如 溶度积 K）"" },
                            ""result_unit"": { ""type"": ""string"", ""description"": ""结果单位（如 K, kJ/mol）"" }
                        },
                        ""required"": [""name"", ""display_name"", ""description"", ""formula"", ""parameters""]
                    }"),

                MakeToolDef("execute_custom_model",
                    "执行一个已创建的自定义模型，传入参数值进行计算。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""model_name"": { ""type"": ""string"", ""description"": ""模型名称"" },
                            ""parameter_values"": { ""type"": ""object"", ""description"": ""参数值，键为参数名，值为数值"" }
                        },
                        ""required"": [""model_name"", ""parameter_values""]
                    }"),

                MakeToolDef("list_custom_models",
                    "列出所有已创建的自定义计算模型。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {},
                        ""required"": []
                    }"),

                MakeToolDef("delete_custom_model",
                    "删除一个自定义模型。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""model_name"": { ""type"": ""string"", ""description"": ""要删除的模型名称"" }
                        },
                        ""required"": [""model_name""]
                    }"),

                // ===== DFT 数据工具（2个） =====
                MakeToolDef("import_dft_result",
                    "解析并导入 DFT（密度泛函理论）计算结果文件。支持 VASP、Quantum ESPRESSO、ABINIT、CP2K、CASTEP、SIESTA、Wien2k、FHI-aims、Elk、GPAW、FLEUR、OpenMX、Exciting、DFTB+ 等软件的输出文件。自动检测软件类型。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""file_path"": { ""type"": ""string"", ""description"": ""DFT 输出文件的完整路径"" }
                        },
                        ""required"": [""file_path""]
                    }"),

                MakeToolDef("query_dft_data",
                    "查询已导入的 DFT 计算数据。可按软件类型、化学式或属性进行筛选。",
                    @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""software"": { ""type"": ""string"", ""description"": ""DFT 软件名称（可选筛选）"" },
                            ""formula"": { ""type"": ""string"", ""description"": ""化学式筛选（可选）"" },
                            ""list_all"": { ""type"": ""boolean"", ""description"": ""列出所有已导入数据，默认false"" }
                        },
                        ""required"": []
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
                // 参数强制转换
                argumentsJson = CoerceArguments(toolName, argumentsJson);

                using var doc = JsonDocument.Parse(argumentsJson);
                var args = doc.RootElement;

                return toolName switch
                {
                    // 活度相互作用系数
                    "get_interaction_coefficient" => CalcInteractionCoefficient(args),
                    "get_second_order_interaction_coefficient" => CalcSecondOrder(args),
                    "get_infinite_dilution_activity_coefficient" => CalcInfiniteDilution(args),

                    // 溶剂活度系数
                    "calculate_solvent_activity_coefficient" => CalcSolventActivityCoefficient(args),
                    "calculate_solvent_activity_darken" => CalcSolventActivityDarken(args),

                    // 热力学性质
                    "calculate_activity" => CalcActivity(args),
                    "calculate_activity_coefficient" => CalcActivityCoefficient(args),
                    "calculate_chemical_potential" => CalcChemicalPotential(args),
                    "calculate_mixing_enthalpy" => CalcMixingEnthalpy(args),
                    "calculate_gibbs_energy" => CalcGibbsEnergy(args),
                    "calculate_entropy" => CalcEntropy(args),
                    "calculate_all_properties" => CalcAllProperties(args),

                    // 相图与温度
                    "calculate_liquidus_temperature" => CalcLiquidus(args),
                    "calculate_precipitation_temperature" => CalcPrecipitationTemperature(args),
                    "calculate_melting_point_depression" => CalcMeltingPointDepression(args),

                    // 合金设计
                    "screen_elements_liquidus_effect" => ScreenElementsLiquidus(args),

                    // 辅助
                    "get_element_properties" => GetElementProps(args),
                    "plot_chart" => HandlePlotChart(args),

                    // 贡献系数
                    "get_contribution_coefficients" => GetContributionCoefficients(args),

                    // 记忆工具
                    "save_memory" => ExecSaveMemory(args),
                    "recall_memories" => ExecRecallMemories(args),
                    "delete_memory" => ExecDeleteMemory(args),

                    // 自定义模型工具
                    "create_custom_model" => ExecCreateCustomModel(args),
                    "execute_custom_model" => ExecExecuteCustomModel(args),
                    "list_custom_models" => ExecListCustomModels(args),
                    "delete_custom_model" => ExecDeleteCustomModel(args),

                    // DFT 数据工具
                    "import_dft_result" => ExecImportDft(args),
                    "query_dft_data" => ExecQueryDft(args),

                    // 动态自定义模型路由（custom_model_xxx）
                    _ when toolName.StartsWith("custom_model_") => ExecDynamicCustomModel(toolName, args),

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

        #region Parameter Coercion

        /// <summary>
        /// LLM经常把数值传为字符串，执行前自动转换
        /// </summary>
        private static string CoerceArguments(string toolName, string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var dict = new Dictionary<string, object>();

                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Name == "composition" && prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        // 确保 composition 值为 double
                        var compDict = new Dictionary<string, double>();
                        foreach (var cp in prop.Value.EnumerateObject())
                        {
                            compDict[cp.Name] = cp.Value.ValueKind == JsonValueKind.String
                                ? double.Parse(cp.Value.GetString()!, System.Globalization.CultureInfo.InvariantCulture)
                                : cp.Value.GetDouble();
                        }
                        dict[prop.Name] = compDict;
                    }
                    else if ((prop.Name == "temperature" || prop.Name == "solute_content_percent" ||
                              prop.Name == "solute_mole_percent") && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        dict[prop.Name] = double.Parse(prop.Value.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (prop.Name == "candidate_elements" && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        // 逗号分隔字符串 → 列表
                        var elements = prop.Value.GetString()!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        dict[prop.Name] = elements;
                    }
                    else
                    {
                        dict[prop.Name] = prop.Value;
                    }
                }

                return JsonSerializer.Serialize(dict);
            }
            catch
            {
                return json; // 无法转换则原样返回
            }
        }

        #endregion

        #region Tool Implementations

        // --- 活度相互作用系数 ---

        private static string CalcInteractionCoefficient(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var soluteI = args.GetProperty("solute_i").GetString()!;
            var soluteJ = args.GetProperty("solute_j").GetString()!;
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");
            var extModel = GetString(args, "extrapolation_model", "UEM1");

            var solv = new Element(solvent);
            var solui = new Element(soluteI);
            var soluj = new Element(soluteJ);

            var bm = new Binary_model();
            bm.setTemperature(T);
            bm.setState(phase);

            var geoModel = GetGeoModel(bm, extModel);
            var ternary = new Ternary_melts(T, phase);
            double epsilon = ternary.Activity_Interact_Coefficient_1st(solv, solui, soluj, geoModel, extModel);

            var melt = new Melt(solvent, soluteI, soluteJ, T);

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute_i = soluteI,
                solute_j = soluteJ,
                temperature_K = T,
                temperature_C = T - 273.15,
                phase,
                extrapolation_model = extModel,
                epsilon_ij_theoretical = epsilon,
                epsilon_ij_exp_molar = melt.sji,
                epsilon_ij_exp_weight = melt.eji
            });
        }

        private static string CalcSecondOrder(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var soluteI = args.GetProperty("solute_i").GetString()!;
            var soluteJ = args.GetProperty("solute_j").GetString()!;
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");
            var coeffType = GetString(args, "coefficient_type", "all");
            var extModel = GetString(args, "extrapolation_model", "UEM1");

            var solv = new Element(solvent);
            var solui = new Element(soluteI);
            var soluj = new Element(soluteJ);

            var bm = new Binary_model();
            bm.setTemperature(T);
            bm.setState(phase);

            var geoModel = GetGeoModel(bm, extModel);
            var ternary = new Ternary_melts(T, phase);

            double rho_ii = double.NaN, rho_jj = double.NaN, rho_ij = double.NaN;
            if (coeffType == "all" || coeffType == "rho_ii")
                rho_ii = ternary.Roui_ii(solv, solui, geoModel);
            if (coeffType == "all" || coeffType == "rho_jj")
                rho_jj = ternary.Roui_jj(solv, solui, soluj, geoModel);
            if (coeffType == "all" || coeffType == "rho_ij")
                rho_ij = ternary.Roui_ij(solv, solui, soluj, geoModel);

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute_i = soluteI,
                solute_j = soluteJ,
                temperature_K = T,
                temperature_C = T - 273.15,
                phase,
                extrapolation_model = extModel,
                rho_i_ii = rho_ii,
                rho_i_jj = rho_jj,
                rho_i_ij = rho_ij
            });
        }

        private static string CalcInfiniteDilution(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var solute = args.GetProperty("solute").GetString()!;
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");

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
                temperature_C = T - 273.15,
                phase,
                lnGamma0 = lnY0,
                gamma0 = double.IsNaN(lnY0) ? double.NaN : Math.Exp(lnY0)
            });
        }

        // --- 溶剂活度系数 (Gibbs-Duhem) ---

        private static string CalcSolventActivityCoefficient(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");
            var extModel = GetString(args, "extrapolation_model", "UEM1");
            var activityModel = GetString(args, "activity_model", "all");

            EnsureSolvent(comp, solvent);
            NormalizeComposition(comp);

            double xSolvent = comp.ContainsKey(solvent) ? comp[solvent] : 1.0;

            var ac = new Activity_Coefficient();
            ac.set_CompositionDict(CompDictToString(comp));

            var bm = new Binary_model();
            bm.setTemperature(T);
            bm.setState(phase);
            var geoModel = GetGeoModel(bm, extModel);

            double lnGamma_w = double.NaN, gd_w = double.NaN;
            double lnGamma_p = double.NaN, gd_p = double.NaN;
            double lnGamma_e = double.NaN, gd_e = double.NaN;

            if (activityModel == "all" || activityModel == "Wagner")
                (lnGamma_w, gd_w) = ac.solvent_activity_coefficient_Wagner(comp, solvent, T, geoModel, extModel, phase);
            if (activityModel == "all" || activityModel == "Darken")
                (lnGamma_p, gd_p) = ac.solvent_activity_coefficient_Pelton(comp, solvent, T, geoModel, extModel, phase);
            if (activityModel == "all" || activityModel == "Elliott")
                (lnGamma_e, gd_e) = ac.solvent_activity_coefficient_Elliott(comp, solvent, T, geoModel, extModel, phase);

            return JsonResult(new
            {
                status = "success",
                solvent,
                temperature_K = T,
                temperature_C = T - 273.15,
                solvent_mole_fraction = xSolvent,
                composition = comp,
                phase,
                extrapolation_model = extModel,
                method = "Numerical Gibbs-Duhem integration (dense mesh at dilute end)",
                wagner = new
                {
                    lnGamma_solvent = lnGamma_w,
                    gamma_solvent = double.IsNaN(lnGamma_w) ? double.NaN : Math.Exp(lnGamma_w),
                    activity_solvent = double.IsNaN(lnGamma_w) ? double.NaN : xSolvent * Math.Exp(lnGamma_w),
                    GD_residual = gd_w
                },
                pelton = new
                {
                    lnGamma_solvent = lnGamma_p,
                    gamma_solvent = double.IsNaN(lnGamma_p) ? double.NaN : Math.Exp(lnGamma_p),
                    activity_solvent = double.IsNaN(lnGamma_p) ? double.NaN : xSolvent * Math.Exp(lnGamma_p),
                    GD_residual = gd_p
                },
                elliott = new
                {
                    lnGamma_solvent = lnGamma_e,
                    gamma_solvent = double.IsNaN(lnGamma_e) ? double.NaN : Math.Exp(lnGamma_e),
                    activity_solvent = double.IsNaN(lnGamma_e) ? double.NaN : xSolvent * Math.Exp(lnGamma_e),
                    GD_residual = gd_e
                }
            });
        }

        /// <summary>
        /// Darken 二次式解析计算溶剂活度系数
        /// </summary>
        private static string CalcSolventActivityDarken(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");
            var extModel = GetString(args, "extrapolation_model", "UEM1");

            EnsureSolvent(comp, solvent);
            NormalizeComposition(comp);

            double xSolvent = comp.ContainsKey(solvent) ? comp[solvent] : 1.0;

            var ac = new Activity_Coefficient();
            ac.set_CompositionDict(CompDictToString(comp));

            var bm = new Binary_model();
            bm.setTemperature(T);
            bm.setState(phase);
            var geoModel = GetGeoModel(bm, extModel);

            double lnGamma = ac.solvent_activity_coefficient_Darken(
                comp, solvent, T, geoModel, extModel, phase);

            double gamma = double.IsNaN(lnGamma) ? double.NaN : Math.Exp(lnGamma);
            double activity = double.IsNaN(lnGamma) ? double.NaN : xSolvent * Math.Exp(lnGamma);

            return JsonResult(new
            {
                status = "success",
                solvent,
                temperature_K = T,
                temperature_C = T - 273.15,
                solvent_mole_fraction = xSolvent,
                composition = comp,
                phase,
                extrapolation_model = extModel,
                method = "Darken quadratic formalism (analytical, no G-D integration)",
                lnGamma_solvent = lnGamma,
                gamma_solvent = gamma,
                activity_solvent = activity
            });
        }

        // --- 热力学性质 ---

        private static (double w, double p, double e) CalcThreeModelLnGamma(
            string solvent, string solute, Dictionary<string, double> comp, double T, string phase, string extModel)
        {
            EnsureSolvent(comp, solvent);
            NormalizeComposition(comp);

            var ac = new Activity_Coefficient();
            ac.set_CompositionDict(CompDictToString(comp));

            var bm = new Binary_model();
            bm.setTemperature(T);
            bm.setState(phase);

            var geoModel = GetGeoModel(bm, extModel);
            var info = (state: phase, T: T);
            double lnGamma_w = ac.activity_Coefficient_Wagner(comp, solvent, solute, geoModel, extModel, info);
            double lnGamma_p = ac.activity_coefficient_Pelton(comp, solute, solvent, T, geoModel, extModel, phase);
            double lnGamma_e = ac.activity_coefficient_Elloit(comp, solute, solvent, T, geoModel, extModel, phase);

            return (lnGamma_w, lnGamma_p, lnGamma_e);
        }

        private static string CalcActivity(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var solute = args.GetProperty("solute").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");
            var extModel = GetString(args, "extrapolation_model", "UEM1");

            var (w, p, e) = CalcThreeModelLnGamma(solvent, solute, comp, T, phase, extModel);
            double x = comp.ContainsKey(solute) ? comp[solute] : 0;

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute,
                temperature_K = T,
                temperature_C = T - 273.15,
                mole_fraction = x,
                phase,
                wagner = new { lnGamma = w, gamma = Math.Exp(w), activity = x * Math.Exp(w) },
                pelton = new { lnGamma = p, gamma = Math.Exp(p), activity = x * Math.Exp(p) },
                elliott = new { lnGamma = e, gamma = Math.Exp(e), activity = x * Math.Exp(e) }
            });
        }

        private static string CalcActivityCoefficient(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var solute = args.GetProperty("solute").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");
            var extModel = GetString(args, "extrapolation_model", "UEM1");

            var (w, p, e) = CalcThreeModelLnGamma(solvent, solute, comp, T, phase, extModel);

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute,
                temperature_K = T,
                temperature_C = T - 273.15,
                phase,
                wagner = new { lnGamma = w, gamma = Math.Exp(w) },
                pelton = new { lnGamma = p, gamma = Math.Exp(p) },
                elliott = new { lnGamma = e, gamma = Math.Exp(e) }
            });
        }

        private static string CalcChemicalPotential(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var solute = args.GetProperty("solute").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");

            var (w, p, e) = CalcThreeModelLnGamma(solvent, solute, comp, T, phase, "UEM1");
            double x = comp.ContainsKey(solute) ? comp[solute] : 0;

            double R = 8.314; // J/(mol·K)
            // μ - μ⁰ = RT·ln(a) = RT·(lnγ + ln(x))
            double lnX = x > 0 ? Math.Log(x) : double.NegativeInfinity;

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute,
                temperature_K = T,
                temperature_C = T - 273.15,
                phase,
                wagner = new { delta_mu_J = R * T * (w + lnX), delta_mu_kJ = R * T * (w + lnX) / 1000 },
                pelton = new { delta_mu_J = R * T * (p + lnX), delta_mu_kJ = R * T * (p + lnX) / 1000 },
                elliott = new { delta_mu_J = R * T * (e + lnX), delta_mu_kJ = R * T * (e + lnX) / 1000 }
            });
        }

        private static string CalcMixingEnthalpy(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");

            EnsureSolvent(comp, solvent);
            NormalizeComposition(comp);

            var bm = new Binary_model();
            bm.setTemperature(T);
            bm.setState(phase);

            // 计算混合焓：Σ(xi * xj * f_ij) ，对每一对元素求和
            double totalH = 0;
            var elements = comp.Keys.ToList();
            for (int i = 0; i < elements.Count; i++)
            {
                for (int j = i + 1; j < elements.Count; j++)
                {
                    double xi = comp[elements[i]];
                    double xj = comp[elements[j]];
                    try
                    {
                        double hij = bm.binary_Model(elements[i], elements[j], xi, xj);
                        totalH += hij;
                    }
                    catch { }
                }
            }

            return JsonResult(new
            {
                status = "success",
                solvent,
                composition = comp,
                temperature_K = T,
                temperature_C = T - 273.15,
                phase,
                mixing_enthalpy_kJ_mol = totalH,
                mixing_enthalpy_J_mol = totalH * 1000
            });
        }

        private static string CalcGibbsEnergy(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");

            EnsureSolvent(comp, solvent);
            NormalizeComposition(comp);

            double R = 8.314;
            // ΔG_mix = RT·Σ(xi·ln(ai)) for each component
            var solutes = comp.Keys.Where(k => k != solvent).ToList();
            double sumIdeal = 0;
            foreach (var kvp in comp)
            {
                if (kvp.Value > 0)
                    sumIdeal += kvp.Value * Math.Log(kvp.Value);
            }
            double G_ideal = R * T * sumIdeal;

            // Also compute excess via activity coefficients for each solute
            double G_excess_w = 0, G_excess_p = 0, G_excess_e = 0;
            foreach (var solute in solutes)
            {
                if (comp[solute] <= 0) continue;
                try
                {
                    var (w, p, e_val) = CalcThreeModelLnGamma(solvent, solute, new Dictionary<string, double>(comp), T, phase, "UEM1");
                    double x = comp[solute];
                    G_excess_w += R * T * x * w;
                    G_excess_p += R * T * x * p;
                    G_excess_e += R * T * x * e_val;
                }
                catch { }
            }

            return JsonResult(new
            {
                status = "success",
                solvent,
                temperature_K = T,
                temperature_C = T - 273.15,
                phase,
                ideal_mixing_gibbs_J = G_ideal,
                ideal_mixing_gibbs_kJ = G_ideal / 1000,
                excess_gibbs_wagner_J = G_excess_w,
                excess_gibbs_pelton_J = G_excess_p,
                excess_gibbs_elliott_J = G_excess_e,
                total_gibbs_wagner_J = G_ideal + G_excess_w,
                total_gibbs_pelton_J = G_ideal + G_excess_p,
                total_gibbs_elliott_J = G_ideal + G_excess_e,
                total_gibbs_wagner_kJ = (G_ideal + G_excess_w) / 1000,
                total_gibbs_pelton_kJ = (G_ideal + G_excess_p) / 1000,
                total_gibbs_elliott_kJ = (G_ideal + G_excess_e) / 1000
            });
        }

        private static string CalcEntropy(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");

            EnsureSolvent(comp, solvent);
            NormalizeComposition(comp);

            double R = 8.314;
            // ΔS_ideal = -R·Σ(xi·ln(xi))
            double sumIdeal = 0;
            foreach (var kvp in comp)
            {
                if (kvp.Value > 0)
                    sumIdeal += kvp.Value * Math.Log(kvp.Value);
            }
            double S_ideal = -R * sumIdeal;

            return JsonResult(new
            {
                status = "success",
                solvent,
                temperature_K = T,
                temperature_C = T - 273.15,
                phase,
                ideal_mixing_entropy_J_molK = S_ideal,
                ideal_entropy_contribution_J = T * S_ideal,
                ideal_entropy_contribution_kJ = T * S_ideal / 1000
            });
        }

        private static string CalcAllProperties(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var solute = args.GetProperty("solute").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");

            EnsureSolvent(comp, solvent);
            NormalizeComposition(comp);

            var (w, p, e) = CalcThreeModelLnGamma(solvent, solute, new Dictionary<string, double>(comp), T, phase, "UEM1");
            double x = comp.ContainsKey(solute) ? comp[solute] : 0;
            double R = 8.314;
            double lnX = x > 0 ? Math.Log(x) : double.NegativeInfinity;

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute,
                temperature_K = T,
                temperature_C = T - 273.15,
                mole_fraction = x,
                phase,
                activity_coefficient = new
                {
                    wagner = new { lnGamma = w, gamma = Math.Exp(w) },
                    pelton = new { lnGamma = p, gamma = Math.Exp(p) },
                    elliott = new { lnGamma = e, gamma = Math.Exp(e) }
                },
                activity = new
                {
                    wagner = x * Math.Exp(w),
                    pelton = x * Math.Exp(p),
                    elliott = x * Math.Exp(e)
                },
                chemical_potential_kJ = new
                {
                    wagner = R * T * (w + lnX) / 1000,
                    pelton = R * T * (p + lnX) / 1000,
                    elliott = R * T * (e + lnX) / 1000
                }
            });
        }

        // --- 相图与温度 ---

        private static string CalcLiquidus(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var phase = GetString(args, "phase", "liquid");

            EnsureSolvent(comp, solvent);
            NormalizeComposition(comp);

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
                liquidus_Wagner_C = SafeDouble(result.T_liquidus_Wagner - 273.15),
                liquidus_Pelton_K = result.T_liquidus_Pelton,
                liquidus_Pelton_C = SafeDouble(result.T_liquidus_Pelton - 273.15),
                liquidus_Elliot_K = result.T_liquidus_Elliot,
                liquidus_Elliot_C = SafeDouble(result.T_liquidus_Elliot - 273.15),
                depression_Wagner_K = result.DeltaT_Wagner,
                depression_Pelton_K = result.DeltaT_Pelton,
                depression_Elliot_K = result.DeltaT_Elliot,
                activity_Wagner = result.SolventActivity_Wagner,
                activity_Pelton = result.SolventActivity_Pelton,
                activity_Elliot = result.SolventActivity_Elliot,
                error = result.ErrorMessage
            });
        }

        private static string CalcPrecipitationTemperature(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var solute = args.GetProperty("solute").GetString()!;
            var comp = ParseComposition(args.GetProperty("composition"));
            var phase = GetString(args, "phase", "liquid");

            EnsureSolvent(comp, solvent);
            NormalizeComposition(comp);

            // 析出温度 = 以析出溶质为"溶剂"计算液相线温度
            var precipComp = new Dictionary<string, double>(comp);
            var bm = new Binary_model();
            bm.setTemperature(new Element(solute).Tm);
            bm.setState(phase);

            var calc = new LiquidusCalculator();

            // 先尝试用溶质元素为基准计算
            try
            {
                var result = calc.CalculateLiquidus(solute, precipComp, phase, bm.UEM1, "UEM1");
                if (result.Converged || !double.IsNaN(result.T_liquidus_Wagner))
                {
                    return JsonResult(new
                    {
                        status = "success",
                        solvent,
                        precipitating_solute = solute,
                        composition = result.Composition,
                        solute_melting_point_K = new Element(solute).Tm,
                        precipitation_temperature_Wagner_K = result.T_liquidus_Wagner,
                        precipitation_temperature_Wagner_C = SafeDouble(result.T_liquidus_Wagner - 273.15),
                        precipitation_temperature_Pelton_K = result.T_liquidus_Pelton,
                        precipitation_temperature_Pelton_C = SafeDouble(result.T_liquidus_Pelton - 273.15),
                        precipitation_temperature_Elliot_K = result.T_liquidus_Elliot,
                        precipitation_temperature_Elliot_C = SafeDouble(result.T_liquidus_Elliot - 273.15)
                    });
                }
            }
            catch { }

            return JsonResult(new { status = "error", message = $"无法计算{solute}在{solvent}基合金中的析出温度" });
        }

        private static string CalcMeltingPointDepression(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var solute = args.GetProperty("solute").GetString()!;
            var percent = args.GetProperty("solute_content_percent").GetDouble();

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
                pure_melting_point_C = result.T_pure_melting - 273.15,
                liquidus_Wagner_K = result.T_liquidus_Wagner,
                liquidus_Wagner_C = SafeDouble(result.T_liquidus_Wagner - 273.15),
                depression_Wagner_K = result.DeltaT_Wagner,
                depression_Pelton_K = result.DeltaT_Pelton,
                depression_Elliot_K = result.DeltaT_Elliot,
                depression_per_percent = percent > 0 ? result.DeltaT_Wagner / percent : 0
            });
        }

        // --- 合金设计 ---

        private static string ScreenElementsLiquidus(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var candidates = args.GetProperty("candidate_elements").EnumerateArray()
                .Select(e => e.GetString()!).ToList();
            var percent = args.TryGetProperty("solute_content_percent", out var sp) ? sp.GetDouble() : 1.0;

            var results = new List<object>();
            foreach (var elem in candidates)
            {
                try
                {
                    var x_solute = percent / 100.0;
                    var comp = new Dictionary<string, double>
                    {
                        [solvent] = 1.0 - x_solute,
                        [elem] = x_solute
                    };

                    var bm = new Binary_model();
                    bm.setTemperature(new Element(solvent).Tm);
                    bm.setState("liquid");

                    var calc = new LiquidusCalculator();
                    var r = calc.CalculateLiquidus(solvent, comp, "liquid", bm.UEM1, "UEM1");

                    results.Add(new
                    {
                        element = elem,
                        depression_Wagner_K = r.DeltaT_Wagner,
                        depression_Pelton_K = r.DeltaT_Pelton,
                        depression_Elliot_K = r.DeltaT_Elliot,
                        liquidus_Wagner_K = r.T_liquidus_Wagner,
                        converged = r.Converged
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        element = elem,
                        depression_Wagner_K = double.NaN,
                        depression_Pelton_K = double.NaN,
                        depression_Elliot_K = double.NaN,
                        liquidus_Wagner_K = double.NaN,
                        converged = false
                    });
                }
            }

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute_mole_percent = percent,
                results
            });
        }

        // --- 辅助 ---

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

        private static string HandlePlotChart(JsonElement args)
        {
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

        // --- 贡献系数 ---

        private static string GetContributionCoefficients(JsonElement args)
        {
            var solvent = args.GetProperty("solvent").GetString()!;
            var soluteI = args.GetProperty("solute_i").GetString()!;
            var soluteJ = args.GetProperty("solute_j").GetString()!;
            var T = args.GetProperty("temperature").GetDouble();
            var phase = GetString(args, "phase", "liquid");

            var bm = new Binary_model();
            bm.setTemperature(T);
            bm.setState(phase);

            // 获取yeta贡献系数
            double yeta_ki = bm.yeta(solvent, soluteI, soluteJ);
            double yeta_kj = bm.yeta(solvent, soluteJ, soluteI);
            double yeta_ij = bm.yeta(soluteI, soluteJ, solvent);

            return JsonResult(new
            {
                status = "success",
                solvent,
                solute_i = soluteI,
                solute_j = soluteJ,
                temperature_K = T,
                temperature_C = T - 273.15,
                phase,
                yeta_solvent_soluteI = yeta_ki,
                yeta_solvent_soluteJ = yeta_kj,
                yeta_soluteI_soluteJ = yeta_ij,
                note = "yeta参数表示在三元外推中各二元子系统的贡献权重"
            });
        }

        // --- 记忆工具 ---

        private static string ExecSaveMemory(JsonElement args)
        {
            var content = args.GetProperty("content").GetString()!;
            var category = GetString(args, "category", "general");
            if (Memory == null) return JsonResult(new { status = "error", message = "记忆系统未初始化" });
            return Memory.SaveMemory(content, category);
        }

        private static string ExecRecallMemories(JsonElement args)
        {
            var keyword = args.TryGetProperty("keyword", out var kw) ? kw.GetString() : null;
            if (Memory == null) return JsonResult(new { status = "error", message = "记忆系统未初始化" });
            return Memory.RecallMemories(keyword);
        }

        private static string ExecDeleteMemory(JsonElement args)
        {
            var content = args.GetProperty("content").GetString()!;
            if (Memory == null) return JsonResult(new { status = "error", message = "记忆系统未初始化" });
            return Memory.DeleteMemory(content);
        }

        #endregion

        #region Helpers

        private static Dictionary<string, double> ParseComposition(JsonElement comp)
        {
            var dict = new Dictionary<string, double>();
            foreach (var prop in comp.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                    dict[prop.Name] = double.Parse(prop.Value.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                else
                    dict[prop.Name] = prop.Value.GetDouble();
            }
            return dict;
        }

        private static void EnsureSolvent(Dictionary<string, double> comp, string solvent)
        {
            if (!comp.ContainsKey(solvent))
            {
                double soluteSum = comp.Values.Sum();
                comp[solvent] = Math.Max(0, 1.0 - soluteSum);
            }
        }

        /// <summary>
        /// 成分归一化：确保所有摩尔分数之和 = 1.0
        /// </summary>
        private static void NormalizeComposition(Dictionary<string, double> comp)
        {
            double total = comp.Values.Sum();
            if (total > 0 && Math.Abs(total - 1.0) > 0.001)
            {
                var keys = comp.Keys.ToList();
                foreach (var key in keys)
                    comp[key] = comp[key] / total;
            }
        }

        private static string CompDictToString(Dictionary<string, double> comp)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in comp)
                sb.Append($"{kvp.Key}{kvp.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            return sb.ToString();
        }

        private static string GetString(JsonElement args, string name, string defaultValue)
        {
            return args.TryGetProperty(name, out var val) && val.ValueKind == JsonValueKind.String
                ? val.GetString() ?? defaultValue : defaultValue;
        }

        private static double SafeDouble(double val)
        {
            return double.IsNaN(val) ? double.NaN : val;
        }

        /// <summary>
        /// 根据外推模型名获取对应的 Geo_Model 委托
        /// </summary>
        private static Geo_Model GetGeoModel(Binary_model bm, string extModel)
        {
            return extModel switch
            {
                "UEM2" => bm.UEM2,
                "GSM" => bm.GSM,
                _ => bm.UEM1
            };
        }

        private static string JsonResult(object obj)
        {
            return JsonSerializer.Serialize(obj, JsonOpts);
        }

        #endregion

        #region Fallback Formatting

        /// <summary>
        /// 格式化数值为普通小数（禁止科学计数法），保持4位有效数字
        /// </summary>
        private static string Fmt(double val)
        {
            if (double.IsNaN(val) || double.IsInfinity(val)) return "N/A";
            double abs = Math.Abs(val);
            if (abs == 0) return "0";
            if (abs >= 1000) return val.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            if (abs >= 1) return val.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
            if (abs >= 0.001) return val.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            return val.ToString("E4", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static double GetNum(JsonElement root, string key)
        {
            if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number)
                return v.GetDouble();
            return double.NaN;
        }

        private static string GetStr(JsonElement root, string key, string def = "")
        {
            if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString() ?? def;
            return def;
        }

        /// <summary>
        /// 当 LLM 返回空文本但有工具结果时，自动格式化为自然语言可读输出
        /// 按工具类型生成结构化摘要（表格 + 自然语言总结），不逐行列 JSON 字段
        /// </summary>
        public static string FormatFallback(List<(string toolName, string result)> toolResults)
        {
            if (toolResults.Count == 0) return "计算完成，但没有获取到结果。";

            var sb = new System.Text.StringBuilder();
            foreach (var (toolName, result) in toolResults)
            {
                try
                {
                    using var doc = JsonDocument.Parse(result);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("status", out var status) && status.GetString() == "error")
                    {
                        var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "未知错误";
                        sb.AppendLine($"**错误**: {msg}\n");
                        continue;
                    }

                    // 按工具类型分派格式化
                    var formatted = toolName switch
                    {
                        "calculate_solvent_activity_coefficient" => FormatSolventActivityCoefficient(root),
                        "calculate_solvent_activity_darken" => FormatSolventActivityDarken(root),
                        "calculate_liquidus_temperature" => FormatLiquidus(root),
                        "calculate_activity" => FormatActivity(root),
                        "calculate_activity_coefficient" => FormatActivityCoefficient(root),
                        "calculate_chemical_potential" => FormatChemicalPotential(root),
                        "calculate_all_properties" => FormatAllProperties(root),
                        "calculate_mixing_enthalpy" => FormatMixingEnthalpy(root),
                        "calculate_gibbs_energy" => FormatGibbsEnergy(root),
                        "calculate_entropy" => FormatEntropy(root),
                        "get_interaction_coefficient" => FormatInteraction(root),
                        "get_second_order_interaction_coefficient" => FormatSecondOrder(root),
                        "get_infinite_dilution_activity_coefficient" => FormatInfiniteDilution(root),
                        "calculate_precipitation_temperature" => FormatPrecipitation(root),
                        "calculate_melting_point_depression" => FormatMeltingDepression(root),
                        "screen_elements_liquidus_effect" => FormatScreenElements(root),
                        "get_element_properties" => FormatElementProps(root),
                        "get_contribution_coefficients" => FormatContribution(root),
                        _ => FormatGeneric(root)
                    };
                    sb.AppendLine(formatted);
                }
                catch
                {
                    sb.AppendLine($"工具 {toolName} 返回结果: {result}\n");
                }
            }
            return sb.ToString().TrimEnd();
        }

        // ====== 按工具类型格式化（自然语言 + Markdown 表格） ======

        private static string FormatLiquidus(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var Tm = GetNum(r, "pure_melting_point_K");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}基合金液相线温度计算结果**\n");
            sb.AppendLine($"{solvent}纯金属熔点: {Fmt(Tm)} K ({Fmt(Tm - 273.15)} °C)\n");
            sb.AppendLine("| 模型 | 液相线温度 (K) | 液相线温度 (°C) | 熔点降低 (K) |");
            sb.AppendLine("|---|---|---|---|");
            sb.AppendLine($"| Wagner | {Fmt(GetNum(r, "liquidus_Wagner_K"))} | {Fmt(GetNum(r, "liquidus_Wagner_C"))} | {Fmt(GetNum(r, "depression_Wagner_K"))} |");
            sb.AppendLine($"| Pelton | {Fmt(GetNum(r, "liquidus_Pelton_K"))} | {Fmt(GetNum(r, "liquidus_Pelton_C"))} | {Fmt(GetNum(r, "depression_Pelton_K"))} |");
            sb.AppendLine($"| Elliott | {Fmt(GetNum(r, "liquidus_Elliot_K"))} | {Fmt(GetNum(r, "liquidus_Elliot_C"))} | {Fmt(GetNum(r, "depression_Elliot_K"))} |");
            return sb.ToString();
        }

        private static string FormatActivity(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var solute = GetStr(r, "solute");
            var T = GetNum(r, "temperature_K");
            var x = GetNum(r, "mole_fraction");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}基合金中{solute}的活度** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C, x<sub>{solute}</sub> = {Fmt(x)})\n");
            sb.AppendLine("| 模型 | lnγ | γ | 活度 a |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var model in new[] { "wagner", "pelton", "elliott" })
            {
                if (r.TryGetProperty(model, out var m) && m.ValueKind == JsonValueKind.Object)
                {
                    var label = model[0].ToString().ToUpper() + model.Substring(1);
                    sb.AppendLine($"| {label} | {Fmt(GetNum(m, "lnGamma"))} | {Fmt(GetNum(m, "gamma"))} | {Fmt(GetNum(m, "activity"))} |");
                }
            }
            return sb.ToString();
        }

        private static string FormatActivityCoefficient(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var solute = GetStr(r, "solute");
            var T = GetNum(r, "temperature_K");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}基合金中{solute}的活度系数** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C)\n");
            sb.AppendLine("| 模型 | lnγ | γ |");
            sb.AppendLine("|---|---|---|");
            foreach (var model in new[] { "wagner", "pelton", "elliott" })
            {
                if (r.TryGetProperty(model, out var m) && m.ValueKind == JsonValueKind.Object)
                {
                    var label = model[0].ToString().ToUpper() + model.Substring(1);
                    sb.AppendLine($"| {label} | {Fmt(GetNum(m, "lnGamma"))} | {Fmt(GetNum(m, "gamma"))} |");
                }
            }
            return sb.ToString();
        }

        private static string FormatSolventActivityCoefficient(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var T = GetNum(r, "temperature_K");
            var xSolv = GetNum(r, "solvent_mole_fraction");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}基合金中溶剂{solvent}的活度系数** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C, x<sub>{solvent}</sub> = {Fmt(xSolv)})\n");
            sb.AppendLine("| 模型 | lnγ<sub>solvent</sub> | γ<sub>solvent</sub> | a<sub>solvent</sub> | G-D残差 |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var model in new[] { "wagner", "pelton", "elliott" })
            {
                if (r.TryGetProperty(model, out var m) && m.ValueKind == JsonValueKind.Object)
                {
                    var label = model[0].ToString().ToUpper() + model.Substring(1);
                    var gdr = GetNum(m, "GD_residual");
                    string gdStr = double.IsNaN(gdr) ? "N/A" : gdr.ToString("E2", System.Globalization.CultureInfo.InvariantCulture);
                    sb.AppendLine($"| {label} | {Fmt(GetNum(m, "lnGamma_solvent"))} | {Fmt(GetNum(m, "gamma_solvent"))} | {Fmt(GetNum(m, "activity_solvent"))} | {gdStr} |");
                }
            }
            sb.AppendLine($"\n方法: 数值 Gibbs-Duhem 积分（稀溶液端加密网格）");
            sb.AppendLine($"G-D残差: 越小表示结果越满足 Gibbs-Duhem 方程（理想值为 0）");
            return sb.ToString();
        }

        private static string FormatSolventActivityDarken(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var T = GetNum(r, "temperature_K");
            var xSolv = GetNum(r, "solvent_mole_fraction");
            var lnG = GetNum(r, "lnGamma_solvent");
            var gamma = GetNum(r, "gamma_solvent");
            var activity = GetNum(r, "activity_solvent");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**Darken 二次式 — {solvent}基合金中溶剂{solvent}的活度系数** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C, x<sub>{solvent}</sub> = {Fmt(xSolv)})\n");
            sb.AppendLine("| 参数 | 数值 |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| lnγ<sub>{solvent}</sub> | {Fmt(lnG)} |");
            sb.AppendLine($"| γ<sub>{solvent}</sub> | {Fmt(gamma)} |");
            sb.AppendLine($"| a<sub>{solvent}</sub> | {Fmt(activity)} |");
            sb.AppendLine($"\n方法: Darken 二次式解析公式（lnγ₁ = -½·Σᵢ εᵢⁱ·Xᵢ² - Σᵢ<ⱼ εᵢʲ·Xᵢ·Xⱼ）");
            return sb.ToString();
        }

        private static string FormatChemicalPotential(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var solute = GetStr(r, "solute");
            var T = GetNum(r, "temperature_K");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}基合金中{solute}的化学势** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C)\n");
            sb.AppendLine("| 模型 | Δμ (J/mol) | Δμ (kJ/mol) |");
            sb.AppendLine("|---|---|---|");
            foreach (var model in new[] { "wagner", "pelton", "elliott" })
            {
                if (r.TryGetProperty(model, out var m) && m.ValueKind == JsonValueKind.Object)
                {
                    var label = model[0].ToString().ToUpper() + model.Substring(1);
                    sb.AppendLine($"| {label} | {Fmt(GetNum(m, "delta_mu_J"))} | {Fmt(GetNum(m, "delta_mu_kJ"))} |");
                }
            }
            return sb.ToString();
        }

        private static string FormatAllProperties(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var solute = GetStr(r, "solute");
            var T = GetNum(r, "temperature_K");
            var x = GetNum(r, "mole_fraction");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}基合金中{solute}的综合热力学性质** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C, x<sub>{solute}</sub> = {Fmt(x)})\n");

            // 活度系数 + 活度表
            sb.AppendLine("| 模型 | lnγ | γ | 活度 a | Δμ (kJ/mol) |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var model in new[] { "wagner", "pelton", "elliott" })
            {
                var label = model[0].ToString().ToUpper() + model.Substring(1);
                double lnG = double.NaN, gam = double.NaN, act = double.NaN, mu = double.NaN;
                if (r.TryGetProperty("activity_coefficient", out var ac) && ac.TryGetProperty(model, out var acm))
                {
                    lnG = GetNum(acm, "lnGamma");
                    gam = GetNum(acm, "gamma");
                }
                if (r.TryGetProperty("activity", out var av) && av.TryGetProperty(model, out var avm))
                    act = avm.GetDouble();
                if (r.TryGetProperty("chemical_potential_kJ", out var cp) && cp.TryGetProperty(model, out var cpm))
                    mu = cpm.GetDouble();
                sb.AppendLine($"| {label} | {Fmt(lnG)} | {Fmt(gam)} | {Fmt(act)} | {Fmt(mu)} |");
            }
            return sb.ToString();
        }

        private static string FormatMixingEnthalpy(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var T = GetNum(r, "temperature_K");
            var H_kJ = GetNum(r, "mixing_enthalpy_kJ_mol");
            var H_J = GetNum(r, "mixing_enthalpy_J_mol");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}基合金的混合焓** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C)\n");
            sb.AppendLine($"ΔH<sub>mix</sub> = {Fmt(H_kJ)} kJ/mol ({Fmt(H_J)} J/mol)");
            return sb.ToString();
        }

        private static string FormatGibbsEnergy(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var T = GetNum(r, "temperature_K");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}基合金的Gibbs自由能** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C)\n");
            sb.AppendLine($"理想混合项 ΔG<sub>ideal</sub> = {Fmt(GetNum(r, "ideal_mixing_gibbs_kJ"))} kJ/mol\n");
            sb.AppendLine("| 模型 | ΔG<sub>excess</sub> (J/mol) | ΔG<sub>total</sub> (kJ/mol) |");
            sb.AppendLine("|---|---|---|");
            sb.AppendLine($"| Wagner | {Fmt(GetNum(r, "excess_gibbs_wagner_J"))} | {Fmt(GetNum(r, "total_gibbs_wagner_kJ"))} |");
            sb.AppendLine($"| Pelton | {Fmt(GetNum(r, "excess_gibbs_pelton_J"))} | {Fmt(GetNum(r, "total_gibbs_pelton_kJ"))} |");
            sb.AppendLine($"| Elliott | {Fmt(GetNum(r, "excess_gibbs_elliott_J"))} | {Fmt(GetNum(r, "total_gibbs_elliott_kJ"))} |");
            return sb.ToString();
        }

        private static string FormatEntropy(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var T = GetNum(r, "temperature_K");
            var S = GetNum(r, "ideal_mixing_entropy_J_molK");
            var TS = GetNum(r, "ideal_entropy_contribution_kJ");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}基合金的混合熵** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C)\n");
            sb.AppendLine($"ΔS<sub>ideal</sub> = {Fmt(S)} J/(mol·K)");
            sb.AppendLine($"TΔS<sub>ideal</sub> = {Fmt(TS)} kJ/mol");
            return sb.ToString();
        }

        private static string FormatInteraction(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var si = GetStr(r, "solute_i");
            var sj = GetStr(r, "solute_j");
            var T = GetNum(r, "temperature_K");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}基合金中{sj}对{si}的一阶相互作用系数** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C)\n");
            sb.AppendLine($"| 参数 | 数值 |");
            sb.AppendLine($"|---|---|");
            sb.AppendLine($"| ε<sub>{si}</sub><sup>{sj}</sup> (UEM理论值) | {Fmt(GetNum(r, "epsilon_ij_theoretical"))} |");
            sb.AppendLine($"| ε<sub>{si}</sub><sup>{sj}</sup> (实验值, 摩尔分数) | {Fmt(GetNum(r, "epsilon_ij_exp_molar"))} |");
            sb.AppendLine($"| e<sub>{si}</sub><sup>{sj}</sup> (实验值, 质量分数) | {Fmt(GetNum(r, "epsilon_ij_exp_weight"))} |");
            return sb.ToString();
        }

        private static string FormatSecondOrder(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var si = GetStr(r, "solute_i");
            var sj = GetStr(r, "solute_j");
            var T = GetNum(r, "temperature_K");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}基合金中的二阶相互作用系数** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C)\n");
            sb.AppendLine($"- ρ<sub>{si},{si}</sub> = {Fmt(GetNum(r, "rho_i_ii"))}");
            sb.AppendLine($"- ρ<sub>{si},{sj}</sub> = {Fmt(GetNum(r, "rho_i_ij"))}");
            sb.AppendLine($"- ρ<sub>{sj},{sj}</sub> = {Fmt(GetNum(r, "rho_i_jj"))}");
            return sb.ToString();
        }

        private static string FormatInfiniteDilution(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var solute = GetStr(r, "solute");
            var T = GetNum(r, "temperature_K");
            var lnG0 = GetNum(r, "lnGamma0");
            var g0 = GetNum(r, "gamma0");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solute}在{solvent}中的无限稀释活度系数** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C)\n");
            sb.AppendLine($"- ln(γ<sup>0</sup><sub>{solute}</sub>) = {Fmt(lnG0)}");
            sb.AppendLine($"- γ<sup>0</sup><sub>{solute}</sub> = {Fmt(g0)}");
            return sb.ToString();
        }

        private static string FormatPrecipitation(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var solute = GetStr(r, "precipitating_solute");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solute}在{solvent}基合金中的析出温度**\n");
            sb.AppendLine("| 模型 | 析出温度 (K) | 析出温度 (°C) |");
            sb.AppendLine("|---|---|---|");
            sb.AppendLine($"| Wagner | {Fmt(GetNum(r, "precipitation_temperature_Wagner_K"))} | {Fmt(GetNum(r, "precipitation_temperature_Wagner_C"))} |");
            sb.AppendLine($"| Pelton | {Fmt(GetNum(r, "precipitation_temperature_Pelton_K"))} | {Fmt(GetNum(r, "precipitation_temperature_Pelton_C"))} |");
            sb.AppendLine($"| Elliott | {Fmt(GetNum(r, "precipitation_temperature_Elliot_K"))} | {Fmt(GetNum(r, "precipitation_temperature_Elliot_C"))} |");
            return sb.ToString();
        }

        private static string FormatMeltingDepression(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var solute = GetStr(r, "solute");
            var pct = GetNum(r, "solute_mole_percent");
            var Tm = GetNum(r, "pure_melting_point_K");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}中添加{Fmt(pct)}%{solute}的熔点降低**\n");
            sb.AppendLine($"{solvent}纯金属熔点: {Fmt(Tm)} K ({Fmt(Tm - 273.15)} °C)\n");
            sb.AppendLine("| 模型 | 熔点降低 (K) |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| Wagner | {Fmt(GetNum(r, "depression_Wagner_K"))} |");
            sb.AppendLine($"| Pelton | {Fmt(GetNum(r, "depression_Pelton_K"))} |");
            sb.AppendLine($"| Elliott | {Fmt(GetNum(r, "depression_Elliot_K"))} |");
            var perPct = GetNum(r, "depression_per_percent");
            if (!double.IsNaN(perPct))
                sb.AppendLine($"\n每增加1%{solute}，熔点降低约 {Fmt(Math.Abs(perPct))} K。");
            return sb.ToString();
        }

        private static string FormatScreenElements(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var pct = GetNum(r, "solute_mole_percent");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**元素对{solvent}合金液相线的影响** (溶质含量: {Fmt(pct)}%)\n");
            sb.AppendLine("| 元素 | 熔点降低/Wagner (K) | 熔点降低/Pelton (K) | 熔点降低/Elliott (K) |");
            sb.AppendLine("|---|---|---|---|");
            if (r.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in results.EnumerateArray())
                {
                    var elem = GetStr(item, "element");
                    sb.AppendLine($"| {elem} | {Fmt(GetNum(item, "depression_Wagner_K"))} | {Fmt(GetNum(item, "depression_Pelton_K"))} | {Fmt(GetNum(item, "depression_Elliot_K"))} |");
                }
            }
            return sb.ToString();
        }

        private static string FormatElementProps(JsonElement r)
        {
            var elem = GetStr(r, "element");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{elem}元素的基本热力学性质**\n");
            sb.AppendLine($"| 性质 | 数值 |");
            sb.AppendLine($"|---|---|");
            sb.AppendLine($"| 熔点 | {Fmt(GetNum(r, "melting_point_K"))} K ({Fmt(GetNum(r, "melting_point_C"))} °C) |");
            sb.AppendLine($"| 沸点 | {Fmt(GetNum(r, "boiling_point_K"))} K |");
            sb.AppendLine($"| 摩尔质量 | {Fmt(GetNum(r, "molar_mass"))} g/mol |");
            sb.AppendLine($"| 摩尔体积 V | {Fmt(GetNum(r, "molar_volume_V"))} cm³/mol |");
            sb.AppendLine($"| 电负性 φ* | {Fmt(GetNum(r, "electronegativity_Phi"))} |");
            sb.AppendLine($"| 电子密度 n<sub>ws</sub> | {Fmt(GetNum(r, "electron_density_nws"))} |");
            return sb.ToString();
        }

        private static string FormatContribution(JsonElement r)
        {
            var solvent = GetStr(r, "solvent");
            var si = GetStr(r, "solute_i");
            var sj = GetStr(r, "solute_j");
            var T = GetNum(r, "temperature_K");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{solvent}-{si}-{sj}三元体系贡献系数** (T = {Fmt(T)} K / {Fmt(T - 273.15)} °C)\n");
            sb.AppendLine($"| 二元子系统 | yeta值 |");
            sb.AppendLine($"|---|---|");
            sb.AppendLine($"| {solvent}-{si} | {Fmt(GetNum(r, "yeta_solvent_soluteI"))} |");
            sb.AppendLine($"| {solvent}-{sj} | {Fmt(GetNum(r, "yeta_solvent_soluteJ"))} |");
            sb.AppendLine($"| {si}-{sj} | {Fmt(GetNum(r, "yeta_soluteI_soluteJ"))} |");
            return sb.ToString();
        }

        /// <summary>
        /// 通用兜底格式化（不识别的工具类型）
        /// </summary>
        private static string FormatGeneric(JsonElement root)
        {
            var skipKeys = new HashSet<string> {
                "status", "iterations", "phase", "solvent", "solute", "solute_i", "solute_j",
                "composition", "extrapolation_model", "error", "type", "message", "converged", "note"
            };
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("**计算结果：**\n");
            foreach (var prop in root.EnumerateObject())
            {
                if (skipKeys.Contains(prop.Name)) continue;
                if (prop.Value.ValueKind == JsonValueKind.Number)
                {
                    var val = prop.Value.GetDouble();
                    sb.AppendLine($"- {prop.Name}: {Fmt(val)}");
                }
                else if (prop.Value.ValueKind == JsonValueKind.String)
                    sb.AppendLine($"- {prop.Name}: {prop.Value.GetString()}");
                else if (prop.Value.ValueKind == JsonValueKind.True)
                    sb.AppendLine($"- {prop.Name}: 是");
                else if (prop.Value.ValueKind == JsonValueKind.False)
                    sb.AppendLine($"- {prop.Name}: 否");
            }
            return sb.ToString();
        }

        #endregion

        #region Custom Model Execution

        private static string ExecCreateCustomModel(JsonElement args)
        {
            if (CustomModels == null)
                return JsonResult(new { status = "error", message = "自定义模型系统未初始化" });

            var model = new CustomModel
            {
                Name = args.GetProperty("name").GetString() ?? "",
                DisplayName = args.GetProperty("display_name").GetString() ?? "",
                Description = args.GetProperty("description").GetString() ?? "",
                Formula = args.GetProperty("formula").GetString() ?? "",
                ResultName = args.TryGetProperty("result_name", out var rn) ? rn.GetString() ?? "" : "",
                ResultUnit = args.TryGetProperty("result_unit", out var ru) ? ru.GetString() ?? "" : ""
            };

            if (args.TryGetProperty("parameters", out var paramsArr))
            {
                foreach (var p in paramsArr.EnumerateArray())
                {
                    model.Parameters.Add(new ModelParameter
                    {
                        Name = p.GetProperty("name").GetString() ?? "",
                        Description = p.GetProperty("description").GetString() ?? "",
                        DefaultValue = p.TryGetProperty("default_value", out var dv) && dv.ValueKind == JsonValueKind.Number ? dv.GetDouble() : null,
                        Unit = p.TryGetProperty("unit", out var u) ? u.GetString() ?? "" : "",
                        IsRequired = !p.TryGetProperty("is_required", out var ir) || ir.ValueKind != JsonValueKind.False
                    });
                }
            }

            return CustomModels.SaveModel(model);
        }

        private static string ExecExecuteCustomModel(JsonElement args)
        {
            if (CustomModels == null)
                return JsonResult(new { status = "error", message = "自定义模型系统未初始化" });

            var modelName = args.GetProperty("model_name").GetString() ?? "";
            var paramValues = new Dictionary<string, double>();

            if (args.TryGetProperty("parameter_values", out var pv))
            {
                foreach (var prop in pv.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Number)
                        paramValues[prop.Name] = prop.Value.GetDouble();
                    else if (prop.Value.ValueKind == JsonValueKind.String &&
                             double.TryParse(prop.Value.GetString(), out double val))
                        paramValues[prop.Name] = val;
                }
            }

            return CustomModels.ExecuteModel(modelName, paramValues);
        }

        private static string ExecListCustomModels(JsonElement args)
        {
            if (CustomModels == null)
                return JsonResult(new { status = "error", message = "自定义模型系统未初始化" });

            var models = CustomModels.ListModels();
            if (models.Count == 0)
                return JsonResult(new { status = "success", message = "尚未创建任何自定义模型", models = Array.Empty<object>() });

            var list = models.Select(m => new
            {
                name = m.Name,
                display_name = m.DisplayName,
                description = m.Description,
                formula = m.Formula,
                parameters = m.Parameters.Select(p => new { p.Name, p.Description, p.DefaultValue, p.Unit }),
                result_unit = m.ResultUnit,
                created_at = m.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            });

            return JsonResult(new { status = "success", total = models.Count, models = list });
        }

        private static string ExecDeleteCustomModel(JsonElement args)
        {
            if (CustomModels == null)
                return JsonResult(new { status = "error", message = "自定义模型系统未初始化" });

            var modelName = args.GetProperty("model_name").GetString() ?? "";
            return CustomModels.DeleteModel(modelName);
        }

        /// <summary>
        /// 动态自定义模型执行（工具名格式：custom_model_{name}）
        /// </summary>
        private static string ExecDynamicCustomModel(string toolName, JsonElement args)
        {
            if (CustomModels == null)
                return JsonResult(new { status = "error", message = "自定义模型系统未初始化" });

            var modelName = toolName.Substring("custom_model_".Length);
            var paramValues = new Dictionary<string, double>();

            foreach (var prop in args.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Number)
                    paramValues[prop.Name] = prop.Value.GetDouble();
                else if (prop.Value.ValueKind == JsonValueKind.String &&
                         double.TryParse(prop.Value.GetString(), out double val))
                    paramValues[prop.Name] = val;
            }

            return CustomModels.ExecuteModel(modelName, paramValues);
        }

        #endregion

        #region DFT Tool Execution

        private static string ExecImportDft(JsonElement args)
        {
            var filePath = args.GetProperty("file_path").GetString() ?? "";
            if (string.IsNullOrWhiteSpace(filePath))
                return JsonResult(new { status = "error", message = "文件路径不能为空" });

            if (!File.Exists(filePath))
                return JsonResult(new { status = "error", message = $"文件不存在: {filePath}" });

            try
            {
                var result = DFT.DftParserRegistry.AutoParse(filePath);
                if (result == null)
                    return JsonResult(new { status = "error", message = "无法识别该文件的 DFT 软件类型，支持 VASP、Quantum ESPRESSO、ABINIT、CP2K、CASTEP、SIESTA、Wien2k、FHI-aims、Elk、GPAW、FLEUR、OpenMX、Exciting、DFTB+" });

                // 添加到全局结果列表
                DftResults ??= new List<DFT.DftResult>();
                DftResults.Add(result);

                return JsonResult(new
                {
                    status = "success",
                    software = result.SourceSoftware,
                    formula = result.Formula,
                    atom_count = result.AtomCount,
                    total_energy_eV = result.TotalEnergy_eV,
                    total_energy_kJ_mol = result.TotalEnergy_kJ_mol,
                    fermi_energy_eV = result.FermiEnergy_eV,
                    band_gap_eV = result.BandGap_eV,
                    volume_A3 = result.Volume,
                    is_converged = result.IsConverged,
                    method = result.Method,
                    lattice_parameters = result.LatticeParameters,
                    pressure_GPa = result.Pressure_GPa,
                    max_force_eV_A = result.MaxForce_eV_A,
                    formation_energy_eV_atom = result.FormationEnergy_eV_atom,
                    mixing_enthalpy_kJ_mol = result.MixingEnthalpy_kJ_mol
                });
            }
            catch (Exception ex)
            {
                return JsonResult(new { status = "error", message = $"解析失败: {ex.Message}" });
            }
        }

        private static string ExecQueryDft(JsonElement args)
        {
            if (DftResults == null || DftResults.Count == 0)
                return JsonResult(new { status = "success", message = "尚未导入任何 DFT 数据", results = Array.Empty<object>() });

            var results = DftResults.AsEnumerable();

            // 按软件筛选
            if (args.TryGetProperty("software", out var sw) && sw.ValueKind == JsonValueKind.String)
            {
                var software = sw.GetString() ?? "";
                if (!string.IsNullOrEmpty(software))
                    results = results.Where(r => r.SourceSoftware.Contains(software, StringComparison.OrdinalIgnoreCase));
            }

            // 按化学式筛选
            if (args.TryGetProperty("formula", out var fm) && fm.ValueKind == JsonValueKind.String)
            {
                var formula = fm.GetString() ?? "";
                if (!string.IsNullOrEmpty(formula))
                    results = results.Where(r => r.Formula.Contains(formula, StringComparison.OrdinalIgnoreCase));
            }

            var list = results.Select(r => new
            {
                software = r.SourceSoftware,
                formula = r.Formula,
                total_energy_eV = r.TotalEnergy_eV,
                total_energy_kJ_mol = r.TotalEnergy_kJ_mol,
                volume_A3 = r.Volume,
                is_converged = r.IsConverged,
                method = r.Method,
                formation_energy_eV_atom = r.FormationEnergy_eV_atom,
                source_file = Path.GetFileName(r.SourceFile)
            }).ToList();

            return JsonResult(new { status = "success", total = list.Count, results = list });
        }

        #endregion
    }
}
