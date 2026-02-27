using System.Text;
using System.Text.Json;

namespace AlloyAct_Pro.LLM
{
    /// <summary>
    /// 对话代理 - 协调 LLM 后端与工具调用，实现自然语言交互式计算
    /// 支持 10 轮工具循环、记忆注入、兜底格式化、流式输出
    /// </summary>
    public class ChatAgent
    {
        /// <summary>
        /// 标记当前后端是否支持工具调用（某些模型如 deepseek-r1 不支持）
        /// 一旦检测到不支持，后续请求自动跳过工具
        /// </summary>
        private bool _toolsSupported = true;

        /// <summary>
        /// 外部可读取工具支持状态
        /// </summary>
        public bool ToolsSupported => _toolsSupported;
        private const string BASE_SYSTEM_PROMPT = @"你是合金热力学计算软件 AlloyAct Pro 的 AI 助手，帮助用户进行合金熔体热力学性质计算。

## 核心规则

1. 收到计算请求后立即调用工具计算，不要解释理论
2. 回答必须简短精练：
   - 计算结果：数值表格 + 1-2句物理意义总结
   - 概念问题：不超过3-4句话
   - 严禁输出理论解释和公式推导
   - 严禁在计算结果后附加理论背景说明
3. 未指定参数时使用默认值：外推模型=UEM1，活度模型=全部（Wagner/Pelton/Elliott），相态=liquid
4. 温度单位自动转换：°C → K（K = °C + 273.15）
5. 百分比自动转换为摩尔分数（除非明确指定为wt%需要先转换）
6. 每个工具只调用一次，避免重复调用

## 输出长度控制（严格遵守）

- 单次回复总长度不超过300字（不含表格内容）
- 计算结果必须以表格形式给出，表格前后各不超过1句话
- 严禁复述用户的问题
- 严禁重复工具返回的原始JSON数据
- 严禁逐字段解释计算结果的含义
- 严禁使用""让我来为您...""、""下面是计算结果...""等冗余开头
- 严禁主动提供""如果您需要...""、""您还可以...""等引导语
- 简单问题（如""Fe的熔点是多少""）用一句话回答
- 批量结果直接给表格，不需要对每一行都写解释
- 直接给结果，不要先复述用户的问题

## 回复结构（严格遵守）

1. [可选] 一句话概要（10-20字）
2. [必须] 结果表格
3. [可选] 一句物理意义总结（10-30字）
4. [禁止] 理论解释、公式推导、进一步建议

## 成分解析

- ""Fe-5%C合金"" → composition: {""Fe"": 0.95, ""C"": 0.05}，溶剂=Fe
- ""Al中加3%Cu和2%Si"" → composition: {""Cu"": 0.03, ""Si"": 0.02}，溶剂=Al
- 百分比默认为摩尔百分比，余量自动补给溶剂
- 溶剂不需要在composition中指定，系统会自动计算

## 输出格式

- 使用中文回答
- 温度同时给出 K 和 °C 值
- 数值使用普通小数格式（如 909.10 K，636.00 °C），禁止使用科学计数法（如 9.091×10²），除非数值极小（<0.001）或极大（>10⁸）
- 数值保留4位有效数字
- 多模型对比结果必须使用 Markdown 表格展示（| 列1 | 列2 | 和 |---|---| 分隔行）
- 上下标使用 HTML 标签：<sub>下标</sub> 和 <sup>上标</sup>
- 粗体使用 **加粗** 格式
- 工具返回的 null 值显示为 N/A

## 结果展示模板

### 液相线温度结果
一句话概要 + 表格：
| 模型 | 液相线温度 (K) | 液相线温度 (°C) | 熔点降低 (K) |
|---|---|---|---|
| Wagner | 908.50 | 635.35 | 24.65 |
| Pelton | ... | ... | ... |

### 活度/活度系数结果
| 模型 | lnγ | γ | 活度 a |
|---|---|---|---|
| Wagner | -0.1234 | 0.8839 | 0.04420 |

### 溶剂活度系数结果
| 模型 | lnγ<sub>solvent</sub> | γ<sub>solvent</sub> | a<sub>solvent</sub> |
|---|---|---|---|
| Wagner | -0.0052 | 0.9948 | 0.9449 |

### 多组元/批量结果
表格列出每个组元/元素的数据，概要一句话即可。

## 三种活度模型

- **Wagner**: 稀溶液模型 lnγ<sub>i</sub> = lnγ<sub>i</sub><sup>0</sup> + Σε<sub>i</sub><sup>j</sup>·x<sub>j</sub>，适用于稀溶液
- **Pelton(Darken)**: 修正Wagner模型，改善中等浓度精度
- **Elliott**: 包含二阶项 ρ，扩展到较高浓度

以上三种模型均可用于溶质活度系数和溶剂活度系数的计算。溶剂活度系数通过 Gibbs-Duhem 方程积分获得。
此外，还可使用 **Darken 二次式**直接解析计算溶剂活度系数（无需 G-D 积分）。

## 五种外推模型

- **UEM1**: 统一外推模型（默认），基于二元数据外推到三元
- **UEM2**: 统一外推模型变体
- **Muggianu**: 对称外推模型
- **Toop_Muggianu**: Toop-Muggianu混合模型
- **Toop_Kohler**: Toop-Kohler混合模型

## 工具目录（22个工具）

### 活度相互作用系数（3个）
- `get_interaction_coefficient` — 一阶 ε<sub>i</sub><sup>j</sup>
- `get_second_order_interaction_coefficient` — 二阶 ρ
- `get_infinite_dilution_activity_coefficient` — 无限稀释 ln(γ<sup>0</sup>)

### 溶剂活度系数（2个）
- `calculate_solvent_activity_coefficient` — 溶剂（基体）活度系数 γ<sub>solvent</sub>，基于 Gibbs-Duhem 方程积分
- `calculate_solvent_activity_darken` — Darken 二次式解析计算溶剂活度系数（无需 G-D 积分，直接公式法）

### 热力学性质（7个）
- `calculate_activity` — 活度 a=γ×x
- `calculate_activity_coefficient` — 活度系数 γ
- `calculate_chemical_potential` — 化学势 μ
- `calculate_mixing_enthalpy` — 混合焓 ΔH<sub>mix</sub>
- `calculate_gibbs_energy` — Gibbs自由能 ΔG
- `calculate_entropy` — 混合熵 ΔS<sub>mix</sub>
- `calculate_all_properties` — 全部性质（一次调用）

### 相图与温度（3个）
- `calculate_liquidus_temperature` — 液相线温度
- `calculate_precipitation_temperature` — 析出温度
- `calculate_melting_point_depression` — 熔点降低

### 合金设计（1个）
- `screen_elements_liquidus_effect` — 批量筛选元素对液相线的影响

### 辅助（2个）
- `get_element_properties` — 元素性质查询
- `plot_chart` — 绘制图表（折线图/散点图/柱状图）

### 贡献系数（1个）
- `get_contribution_coefficients` — 三元外推贡献系数（yeta参数）

### 记忆工具（3个）
- `save_memory` — 保存用户偏好
- `recall_memories` — 回忆
- `delete_memory` — 删除记忆

## 记忆使用规则

你具有持久记忆功能，能记住用户的偏好设置和常用参数。重启程序后记忆依然有效。

### 何时保存记忆（主动触发）
- 用户说""以后默认用XX模型""、""我习惯用XX"" → 调用 save_memory(content, ""preference"")
- 用户表达温度偏好（如""以后温度都用摄氏度""） → save_memory(content, ""preference"")
- 用户多次计算同一合金体系 → save_memory(""用户常用合金体系: XX"", ""alloy_system"")
- 用户给出特定体系温度约定 → save_memory(content, ""calculation"")
- 用户说""记住""、""保存""、""下次还用"" → 一定要调用 save_memory

### 何时回忆（自动触发）
- 记忆已经在系统提示词中注入，你可以直接使用
- 如果用户问""我之前设置了什么""，调用 recall_memories 展示

### 何时删除
- 用户说""取消默认设置""、""忘记XX"" → 调用 delete_memory

## 关键词→工具映射

| 关键词 | 工具 |
|---|---|
| 活度、活度值、a值 | calculate_activity |
| 活度系数、γ、gamma | calculate_activity_coefficient |
| 溶剂活度系数、基体活度、溶剂γ、Gibbs-Duhem | calculate_solvent_activity_coefficient |
| Darken、Darken二次式、溶剂活度解析 | calculate_solvent_activity_darken |
| 相互作用、ε、epsilon | get_interaction_coefficient |
| 二阶、ρ、rho | get_second_order_interaction_coefficient |
| 无限稀、γ0 | get_infinite_dilution_activity_coefficient |
| 液相线、凝固温度、liquidus | calculate_liquidus_temperature |
| 析出温度、析出 | calculate_precipitation_temperature |
| 熔点降低、降低多少 | calculate_melting_point_depression |
| 化学势、μ | calculate_chemical_potential |
| 混合焓、焓 | calculate_mixing_enthalpy |
| Gibbs、自由能 | calculate_gibbs_energy |
| 熵、entropy | calculate_entropy |
| 全部性质、所有性质 | calculate_all_properties |
| 筛选、元素影响、合金设计 | screen_elements_liquidus_effect |
| 元素性质、熔点查询 | get_element_properties |
| 绘图、画图、可视化 | plot_chart |
| 贡献系数、yeta | get_contribution_coefficients |
| 记住、偏好、默认 | save_memory |
| 创建模型、新增公式 | create_custom_model |
| 执行模型、自定义计算 | execute_custom_model |
| DFT、第一性原理 | import_dft_result |

## 合金设计工作流

当用户需要进行合金成分设计时：
1. 使用 screen_elements_liquidus_effect 批量筛选候选元素
2. 将结果整理成表格展示
3. 如果用户要求可视化，调用 plot_chart 绘制对比图
4. 给出简要建议

## 自定义模型

用户可以要求你创建自定义计算模型。当用户描述一个新的计算公式时：
1. 使用 create_custom_model 创建模型，定义清晰的参数列表和默认值
2. 创建后立即用 execute_custom_model 执行一次验证
3. 创建的模型在重启后仍然可用
4. 支持的数学表达式：+, -, *, /, ^, ()
5. 支持的函数：ln, log, log10, exp, sqrt, pow, abs, sin, cos, tan, min, max
6. 内置常数：R=8.314 (气体常数), pi, e, kB (玻尔兹曼常数), NA (阿伏伽德罗常数), F (法拉第常数)

## DFT 数据集成

软件支持导入 DFT（密度泛函理论）第一性原理计算结果：
- 使用 import_dft_result 导入 DFT 输出文件（自动检测软件类型）
- 使用 query_dft_data 查询已导入的 DFT 数据
- 支持：VASP, Quantum ESPRESSO, ABINIT, CP2K, CASTEP, SIESTA, Wien2k, FHI-aims, Elk, GPAW, FLEUR, OpenMX, Exciting, DFTB+";

        private readonly LlmBackend _backend;
        private readonly MemoryStore _memory;
        private readonly CustomModelStore _customModelStore;
        private readonly List<ChatMessage> _history = new();
        private readonly int _maxToolIterations;
        private const int MaxHistoryMessages = 80;

        /// <summary>
        /// 工具调用回调
        /// </summary>
        public Action<string, string>? OnToolCall { get; set; }

        /// <summary>
        /// 图表请求回调
        /// </summary>
        public Action<Dictionary<string, object>>? OnChartRequested { get; set; }

        /// <summary>
        /// 流式文本增量回调（每收到一个 token 就调用）
        /// </summary>
        public Action<string>? OnTextDelta { get; set; }

        /// <summary>
        /// 流式完成回调（一轮流式输出结束时调用）
        /// </summary>
        public Action<string>? OnStreamComplete { get; set; }

        public ChatAgent(string provider, string? apiKey = null, string? model = null, string? baseUrl = null, int maxToolIterations = 10)
        {
            _backend = LlmBackend.Create(provider, apiKey, model, baseUrl);
            _maxToolIterations = maxToolIterations;
            _memory = new MemoryStore();
            _customModelStore = new CustomModelStore();

            // 预检测模型是否支持工具调用
            _toolsSupported = !IsToolUnsupportedModel(model ?? "");

            // 设置记忆系统和自定义模型引用
            ThermodynamicTools.Memory = _memory;
            ThermodynamicTools.CustomModels = _customModelStore;

            // 构建动态系统提示词（基础 + 记忆注入）
            var systemPrompt = BuildSystemPrompt();
            _history.Add(new ChatMessage { Role = "system", Content = systemPrompt });
        }

        /// <summary>
        /// 根据模型名称预判是否不支持工具调用
        /// 仅列入确认不支持的模型，未知模型默认支持（运行时自动降级兜底）
        /// 公开供 UI 层调用（灰显不支持工具的模型）
        /// </summary>
        public static bool IsToolUnsupportedModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return false;
            var name = modelName.ToLowerInvariant();

            // deepseek-r1 系列（推理模型，不支持工具调用）
            if (name.Contains("deepseek-r1") || name.Contains("deepseek_r1")) return true;
            // deepseek-reasoner（API 版推理模型）
            if (name.Contains("deepseek-reasoner")) return true;
            // QwQ（推理模型）
            if (name.StartsWith("qwq")) return true;

            return false;
        }

        /// <summary>
        /// 构建动态系统提示词：基础提示 + 用户记忆 + 最近对话历史（包含计算记录）
        /// </summary>
        private string BuildSystemPrompt()
        {
            var sb = new StringBuilder(BASE_SYSTEM_PROMPT);

            // 注入用户偏好记忆
            var memoryPrompt = _memory.FormatForPrompt();
            if (!string.IsNullOrEmpty(memoryPrompt))
                sb.Append(memoryPrompt);

            // 注入最近对话历史（包含计算记录和关键问答）
            var recentSummary = _memory.GetRecentSummary(5);
            if (!string.IsNullOrEmpty(recentSummary))
                sb.Append(recentSummary);

            sb.AppendLine("\n\n## 上下文使用规则");
            sb.AppendLine("- 你可以引用上方的历史计算结果来回答用户的后续问题");
            sb.AppendLine("- 如果用户问\"之前计算的...\"、\"上次的结果...\"，请参考历史记录");
            sb.AppendLine("- 当前对话中的消息历史也包含完整的上下文，可以直接引用");
            sb.AppendLine("- 如果历史记录中有相关数据，先告知用户之前的结果，再询问是否需要重新计算");

            // 注入自定义模型列表
            var customModels = _customModelStore.ListModels();
            if (customModels.Count > 0)
            {
                sb.AppendLine("\n\n## 当前已注册的自定义模型");
                foreach (var model in customModels)
                {
                    sb.AppendLine($"- **{model.DisplayName}** (`custom_model_{model.Name}`)：{model.Formula}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取全部工具定义：内置工具 + 自定义模型生成的动态工具
        /// </summary>
        private List<ToolDefinition> GetAllToolDefinitions()
        {
            var tools = ThermodynamicTools.GetToolDefinitions();

            // 追加自定义模型生成的动态工具
            foreach (var model in _customModelStore.ListModels())
            {
                tools.Add(_customModelStore.GenerateToolDefinition(model));
            }

            return tools;
        }

        /// <summary>
        /// 发送消息并获取回复（非流式，异步）
        /// </summary>
        public async Task<string> ChatAsync(string userMessage, CancellationToken ct = default)
        {
            _history.Add(new ChatMessage { Role = "user", Content = userMessage });
            TrimHistory();

            var tools = _toolsSupported ? GetAllToolDefinitions() : null;
            var toolResults = new List<(string toolName, string result)>();

            for (int i = 0; i < _maxToolIterations; i++)
            {
                LlmResponse response;
                try
                {
                    response = await _backend.ChatAsync(_history, tools, ct);
                }
                catch (Exception ex)
                {
                    // 检测模型不支持工具调用
                    if (tools != null && IsToolNotSupportedError(ex.Message))
                    {
                        _toolsSupported = false;
                        var warnMsg = "⚠ 该模型不支持工具调用，无法执行热力学计算。\n请切换到支持工具调用的模型（如 qwen2.5、llama3.2、gemma2 等）。";
                        _history.Add(new ChatMessage { Role = "assistant", Content = warnMsg });
                        return warnMsg;
                    }
                    var errMsg = $"LLM 调用失败: {ex.Message}";
                    _history.Add(new ChatMessage { Role = "assistant", Content = errMsg });
                    return errMsg;
                }

                if (response.ToolCalls.Count > 0)
                {
                    _history.Add(new ChatMessage
                    {
                        Role = "assistant",
                        Content = response.Content,
                        ToolCalls = response.ToolCalls
                    });

                    foreach (var tc in response.ToolCalls)
                    {
                        var toolName = tc.Function.Name;
                        var arguments = tc.Function.Arguments;

                        OnToolCall?.Invoke(toolName, arguments);

                        var chartData = ThermodynamicTools.TryGetChartData(toolName, arguments);
                        if (chartData != null)
                            OnChartRequested?.Invoke(chartData);

                        var result = ThermodynamicTools.ExecuteTool(toolName, arguments);
                        toolResults.Add((toolName, result));

                        _history.Add(new ChatMessage
                        {
                            Role = "tool",
                            Content = result,
                            ToolCallId = tc.Id
                        });
                    }
                }
                else
                {
                    var content = response.Content;
                    if (string.IsNullOrWhiteSpace(content) && toolResults.Count > 0)
                        content = ThermodynamicTools.FormatFallback(toolResults);

                    _history.Add(new ChatMessage { Role = "assistant", Content = content });
                    return content;
                }
            }

            if (toolResults.Count > 0)
            {
                var fallback = ThermodynamicTools.FormatFallback(toolResults);
                _history.Add(new ChatMessage { Role = "assistant", Content = fallback });
                return fallback;
            }

            var maxMsg = "已达到最大工具调用次数限制。";
            _history.Add(new ChatMessage { Role = "assistant", Content = maxMsg });
            return maxMsg;
        }

        /// <summary>
        /// 流式发送消息（边生成边通过回调输出）
        /// </summary>
        public async Task<string> ChatStreamAsync(string userMessage, CancellationToken ct = default)
        {
            _history.Add(new ChatMessage { Role = "user", Content = userMessage });
            TrimHistory();

            var tools = _toolsSupported ? GetAllToolDefinitions() : null;
            var toolResults = new List<(string toolName, string result)>();

            for (int i = 0; i < _maxToolIterations; i++)
            {
                var contentBuilder = new StringBuilder();
                var toolCalls = new List<ToolCall>();
                string finishReason = "stop";

                try
                {
                    await foreach (var chunk in _backend.ChatStreamAsync(_history, tools, ct))
                    {
                        switch (chunk.Type)
                        {
                            case StreamChunkType.TextDelta:
                                contentBuilder.Append(chunk.TextDelta);
                                OnTextDelta?.Invoke(chunk.TextDelta);
                                break;
                            case StreamChunkType.ToolCallComplete:
                                if (chunk.CompletedToolCall != null)
                                    toolCalls.Add(chunk.CompletedToolCall);
                                break;
                            case StreamChunkType.Done:
                                finishReason = chunk.FinishReason;
                                break;
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // 检测模型不支持工具调用（运行时兜底，正常应在连接时预检测）
                    if (tools != null && IsToolNotSupportedError(ex.Message))
                    {
                        _toolsSupported = false;
                        tools = null;
                        // 不重试，直接告知用户
                        var warnMsg = "⚠ 该模型不支持工具调用，无法执行热力学计算。\n请切换到支持工具调用的模型（如 qwen2.5、llama3.2、gemma2 等）。";
                        _history.Add(new ChatMessage { Role = "assistant", Content = warnMsg });
                        OnStreamComplete?.Invoke(warnMsg);
                        return warnMsg;
                    }

                    var errMsg = $"LLM 调用失败: {ex.Message}";
                    _history.Add(new ChatMessage { Role = "assistant", Content = errMsg });
                    OnStreamComplete?.Invoke(errMsg);
                    return errMsg;
                }

                string content = contentBuilder.ToString();

                if (toolCalls.Count > 0)
                {
                    // 工具调用轮：传空字符串让 UI 清理当前流式气泡（移除空气泡）
                    OnStreamComplete?.Invoke("");

                    _history.Add(new ChatMessage
                    {
                        Role = "assistant",
                        Content = content,
                        ToolCalls = toolCalls
                    });

                    foreach (var tc in toolCalls)
                    {
                        OnToolCall?.Invoke(tc.Function.Name, tc.Function.Arguments);

                        var chartData = ThermodynamicTools.TryGetChartData(tc.Function.Name, tc.Function.Arguments);
                        if (chartData != null)
                            OnChartRequested?.Invoke(chartData);

                        var result = ThermodynamicTools.ExecuteTool(tc.Function.Name, tc.Function.Arguments);
                        toolResults.Add((tc.Function.Name, result));

                        _history.Add(new ChatMessage
                        {
                            Role = "tool",
                            Content = result,
                            ToolCallId = tc.Id
                        });
                    }
                    // 继续循环，下一轮流式输出会通过 OnTextDelta 自动创建新气泡
                }
                else
                {
                    // 兜底格式化
                    if (string.IsNullOrWhiteSpace(content) && toolResults.Count > 0)
                        content = ThermodynamicTools.FormatFallback(toolResults);

                    _history.Add(new ChatMessage { Role = "assistant", Content = content });
                    OnStreamComplete?.Invoke(content);
                    return content;
                }
            }

            if (toolResults.Count > 0)
            {
                var fallback = ThermodynamicTools.FormatFallback(toolResults);
                _history.Add(new ChatMessage { Role = "assistant", Content = fallback });
                OnStreamComplete?.Invoke(fallback);
                return fallback;
            }

            var maxMsg = "已达到最大工具调用次数限制。";
            _history.Add(new ChatMessage { Role = "assistant", Content = maxMsg });
            return maxMsg;
        }

        /// <summary>
        /// 重置会话
        /// </summary>
        public void Reset()
        {
            // 保存当前会话
            if (_history.Count > 2)
                _memory.SaveSession(_history);

            var systemPrompt = BuildSystemPrompt();
            _history.Clear();
            _history.Add(new ChatMessage { Role = "system", Content = systemPrompt });
        }

        private void TrimHistory()
        {
            if (_history.Count <= MaxHistoryMessages) return;
            var systemMsgs = _history.Where(m => m.Role == "system").ToList();
            var otherMsgs = _history.Where(m => m.Role != "system").ToList();
            int keep = MaxHistoryMessages - systemMsgs.Count;
            _history.Clear();
            _history.AddRange(systemMsgs);
            _history.AddRange(otherMsgs.Skip(Math.Max(0, otherMsgs.Count - keep)));
        }

        /// <summary>
        /// 检测 API 错误是否为"模型不支持工具调用"
        /// 适配 Ollama、OpenAI 等不同错误格式
        /// </summary>
        private static bool IsToolNotSupportedError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage)) return false;
            var msg = errorMessage.ToLowerInvariant();
            return msg.Contains("does not support tools")
                || msg.Contains("tools are not supported")
                || msg.Contains("tool use is not supported")
                || msg.Contains("does not support function")
                || msg.Contains("tool_use is not supported");
        }
    }
}
