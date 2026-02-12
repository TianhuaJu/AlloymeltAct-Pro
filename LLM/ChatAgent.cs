using System.Text.Json;

namespace AlloyAct_Pro.LLM
{
    /// <summary>
    /// 对话代理 - 协调 LLM 后端与工具调用，实现自然语言交互式计算
    /// </summary>
    public class ChatAgent
    {
        private const string SYSTEM_PROMPT = @"你是一个专业的热力学计算助手（AlloyAct Pro AI Assistant），可以帮助用户进行合金热力学性质计算。

你可以使用以下工具进行计算：

1. **calculate_liquidus_temperature** - 计算合金的液相线温度（开始凝固温度）
   - 输入溶剂和合金成分（摩尔分数），返回液相线温度

2. **calculate_activity** - 计算活度
   - 计算指定组元在给定温度下的活度 a = γ × x

3. **calculate_activity_coefficient** - 计算活度系数
   - 计算指定组元的活度系数 lnγ，返回Wagner、Pelton、Elliot三种模型结果

4. **calculate_interaction_coefficient** - 计算一阶活度相互作用系数
   - 计算 εi_j（溶质j对溶质i的影响），包含UEM1理论值和实验值对比

5. **calculate_infinite_dilution_coefficient** - 计算无限稀活度系数
   - 计算溶质i在溶剂中的无限稀活度系数 lnγi⁰

6. **calculate_second_order_coefficient** - 计算二阶活度相互作用系数
   - 计算 ρi_jk

7. **get_element_properties** - 获取元素属性
   - 获取元素的熔点、摩尔体积、电负性等Miedema参数

8. **calculate_melting_point_depression** - 计算熔点降低
   - 计算指定溶质含量对溶剂熔点的降低程度

9. **convert_unit** - 单位转换
   - 在质量百分比(wt%)和摩尔分数之间转换

10. **plot_chart** - 绘制图表
   - 在对话中直接绘制折线图、散点图或柱状图
   - 支持多条数据曲线对比，用于将计算结果可视化
   - 当用户要求可视化或绘图时，先进行计算，再调用此工具

使用指南：
- 用户可能使用中文或英文描述问题
- 成分可以用摩尔分数或质量百分比表示，需要正确解析
- 温度单位可能是K（开尔文）或°C（摄氏度），注意转换（K = °C + 273.15）
- 如果用户没有指定参数，使用默认值
- 计算结果要清晰解释物理意义

回答格式：
- 使用中文回答
- 先解释计算目标
- 调用工具获取结果
- 解释结果的物理意义
- 如有必要，提供进一步分析建议";

        private readonly LlmBackend _backend;
        private readonly List<ChatMessage> _history = new();
        private readonly int _maxToolIterations;
        private const int MaxHistoryMessages = 50;

        /// <summary>
        /// 工具调用回调
        /// </summary>
        public Action<string, string>? OnToolCall { get; set; }

        /// <summary>
        /// 图表请求回调
        /// </summary>
        public Action<Dictionary<string, object>>? OnChartRequested { get; set; }

        public ChatAgent(string provider, string? apiKey = null, string? model = null, int maxToolIterations = 5)
        {
            _backend = LlmBackend.Create(provider, apiKey, model);
            _maxToolIterations = maxToolIterations;
            _history.Add(new ChatMessage { Role = "system", Content = SYSTEM_PROMPT });
        }

        /// <summary>
        /// 发送消息并获取回复（异步）
        /// </summary>
        public async Task<string> ChatAsync(string userMessage, CancellationToken ct = default)
        {
            _history.Add(new ChatMessage { Role = "user", Content = userMessage });
            TrimHistory();

            var tools = ThermodynamicTools.GetToolDefinitions();

            for (int i = 0; i < _maxToolIterations; i++)
            {
                LlmResponse response;
                try
                {
                    response = await _backend.ChatAsync(_history, tools, ct);
                }
                catch (Exception ex)
                {
                    var errMsg = $"LLM 调用失败: {ex.Message}";
                    _history.Add(new ChatMessage { Role = "assistant", Content = errMsg });
                    return errMsg;
                }

                if (response.ToolCalls.Count > 0)
                {
                    // Add assistant message with tool calls
                    _history.Add(new ChatMessage
                    {
                        Role = "assistant",
                        Content = response.Content,
                        ToolCalls = response.ToolCalls
                    });

                    // Execute each tool call
                    foreach (var tc in response.ToolCalls)
                    {
                        var toolName = tc.Function.Name;
                        var arguments = tc.Function.Arguments;

                        // Notify callback
                        OnToolCall?.Invoke(toolName, arguments);

                        // Check for chart request
                        var chartData = ThermodynamicTools.TryGetChartData(toolName, arguments);
                        if (chartData != null)
                            OnChartRequested?.Invoke(chartData);

                        // Execute tool
                        var result = ThermodynamicTools.ExecuteTool(toolName, arguments);

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
                    // No tool calls - final response
                    _history.Add(new ChatMessage { Role = "assistant", Content = response.Content });
                    return response.Content;
                }
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
            var systemMsg = _history.FirstOrDefault(m => m.Role == "system");
            _history.Clear();
            if (systemMsg != null)
                _history.Add(systemMsg);
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
    }
}
