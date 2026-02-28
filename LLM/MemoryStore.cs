using System.Text.Json;

namespace AlloyAct_Pro.LLM
{
    /// <summary>
    /// 记忆系统 - 事实记忆 + 对话历史持久化
    /// 存储位置: ~/.alloyact/
    /// </summary>
    public class MemoryStore
    {
        private readonly string _baseDir;
        private readonly string _memoriesPath;
        private readonly string _sessionsDir;
        private List<MemoryItem> _memories = new();

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public MemoryStore()
        {
            _baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".alloyact");
            _memoriesPath = Path.Combine(_baseDir, "memories.json");
            _sessionsDir = Path.Combine(_baseDir, "sessions");

            EnsureDirectories();
            LoadMemories();
        }

        #region Memory CRUD

        /// <summary>
        /// 保存一条记忆
        /// </summary>
        public string SaveMemory(string content, string category = "general")
        {
            if (string.IsNullOrWhiteSpace(content))
                return JsonSerializer.Serialize(new { status = "error", message = "内容不能为空" }, JsonOpts);

            // 避免重复
            var existing = _memories.FirstOrDefault(m =>
                m.Content.Equals(content, StringComparison.OrdinalIgnoreCase) && m.Category == category);
            if (existing != null)
            {
                existing.UpdatedAt = DateTime.Now;
                PersistMemories();
                return JsonSerializer.Serialize(new { status = "success", message = "记忆已更新", content, category }, JsonOpts);
            }

            _memories.Add(new MemoryItem
            {
                Content = content,
                Category = ValidateCategory(category),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
            PersistMemories();
            return JsonSerializer.Serialize(new { status = "success", message = "记忆已保存", content, category }, JsonOpts);
        }

        /// <summary>
        /// 回忆（关键词搜索或全部）
        /// </summary>
        public string RecallMemories(string? keyword = null)
        {
            var results = _memories.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                results = results.Where(m =>
                    m.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    m.Category.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            var list = results.OrderByDescending(m => m.UpdatedAt).ToList();
            return JsonSerializer.Serialize(new
            {
                status = "success",
                count = list.Count,
                memories = list.Select(m => new { m.Content, m.Category, updated = m.UpdatedAt.ToString("yyyy-MM-dd HH:mm") })
            }, JsonOpts);
        }

        /// <summary>
        /// 更新记忆内容（用于知识面板编辑）
        /// </summary>
        public bool UpdateMemory(string oldContent, string newContent, string? newCategory = null)
        {
            var item = _memories.FirstOrDefault(m =>
                m.Content.Equals(oldContent, StringComparison.OrdinalIgnoreCase));
            if (item == null) return false;

            item.Content = newContent;
            if (newCategory != null)
                item.Category = ValidateCategory(newCategory);
            item.UpdatedAt = DateTime.Now;
            PersistMemories();
            return true;
        }

        /// <summary>
        /// 删除记忆
        /// </summary>
        public string DeleteMemory(string content)
        {
            var removed = _memories.RemoveAll(m =>
                m.Content.Equals(content, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                PersistMemories();
                return JsonSerializer.Serialize(new { status = "success", message = $"已删除 {removed} 条记忆" }, JsonOpts);
            }
            return JsonSerializer.Serialize(new { status = "error", message = "未找到匹配的记忆" }, JsonOpts);
        }

        #endregion

        #region Prompt Injection

        /// <summary>
        /// 格式化记忆，注入到系统提示词
        /// </summary>
        public string FormatForPrompt()
        {
            if (_memories.Count == 0) return "";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\n========== 用户记忆（你必须遵守的设置） ==========");

            // 按优先级排列：preference > calculation > alloy_system > knowledge > general
            var groups = new[] { "preference", "calculation", "alloy_system", "knowledge", "general" };
            var labels = new Dictionary<string, string>
            {
                ["preference"] = "默认计算设置",
                ["calculation"] = "计算规则与经验",
                ["alloy_system"] = "常用合金体系",
                ["knowledge"] = "知识",
                ["general"] = "其他"
            };

            foreach (var cat in groups)
            {
                var items = _memories.Where(m => m.Category == cat).ToList();
                if (items.Count == 0) continue;
                sb.AppendLine($"【{labels.GetValueOrDefault(cat, cat)}】");
                foreach (var item in items)
                    sb.AppendLine($"  - {item.Content}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取最近对话摘要（增强版：包含详细的计算历史和对话上下文）
        /// </summary>
        public string GetRecentSummary(int count = 3)
        {
            if (!Directory.Exists(_sessionsDir)) return "";

            var files = Directory.GetFiles(_sessionsDir, "*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Take(count)
                .ToList();

            if (files.Count == 0) return "";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\n========== 最近对话历史（你可以引用这些信息回答用户问题） ==========");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);

                    var timestamp = doc.RootElement.TryGetProperty("timestamp", out var ts) ? ts.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(timestamp) && DateTime.TryParse(timestamp, out var dt))
                        sb.AppendLine($"\n--- 对话 ({dt:yyyy-MM-dd HH:mm}) ---");

                    // 输出摘要
                    if (doc.RootElement.TryGetProperty("summary", out var summary))
                        sb.AppendLine($"摘要: {summary.GetString()}");

                    // 输出计算历史（工具调用记录）
                    if (doc.RootElement.TryGetProperty("calculations", out var calcs) && calcs.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var calc in calcs.EnumerateArray())
                        {
                            var tool = calc.TryGetProperty("tool", out var t) ? t.GetString() ?? "" : "";
                            var args = calc.TryGetProperty("args_summary", out var a) ? a.GetString() ?? "" : "";
                            var result = calc.TryGetProperty("result_summary", out var r) ? r.GetString() ?? "" : "";
                            if (!string.IsNullOrEmpty(tool))
                                sb.AppendLine($"  计算: {tool}({args}) → {result}");
                        }
                    }

                    // 输出关键对话片段
                    if (doc.RootElement.TryGetProperty("key_exchanges", out var exchanges) && exchanges.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var ex in exchanges.EnumerateArray())
                        {
                            var q = ex.TryGetProperty("user", out var uv) ? uv.GetString() ?? "" : "";
                            var a = ex.TryGetProperty("assistant_brief", out var av) ? av.GetString() ?? "" : "";
                            if (!string.IsNullOrEmpty(q))
                                sb.AppendLine($"  问: {q}");
                            if (!string.IsNullOrEmpty(a))
                                sb.AppendLine($"  答: {a}");
                        }
                    }
                }
                catch { }
            }
            return sb.ToString();
        }

        #endregion

        #region Session Persistence

        /// <summary>
        /// 保存对话会话（增强版：提取计算历史和关键对话交换）
        /// </summary>
        public void SaveSession(List<ChatMessage> messages, string? summary = null)
        {
            try
            {
                EnsureDirectories();
                var sessionFile = Path.Combine(_sessionsDir, $"{DateTime.Now:yyyyMMdd_HHmmss}.json");

                // 提取计算历史（工具调用 + 结果摘要）
                var calculations = ExtractCalculations(messages);

                // 提取关键问答对
                var keyExchanges = ExtractKeyExchanges(messages);

                var sessionData = new
                {
                    timestamp = DateTime.Now.ToString("o"),
                    summary = summary ?? GenerateSessionSummary(messages),
                    message_count = messages.Count,
                    calculations,
                    key_exchanges = keyExchanges,
                    messages = messages.Where(m => m.Role != "system").Select(m => new
                    {
                        role = m.Role,
                        content = m.Content?.Length > 500 ? m.Content.Substring(0, 500) + "..." : m.Content
                    })
                };

                File.WriteAllText(sessionFile, JsonSerializer.Serialize(sessionData, JsonOpts));

                // 保留最多20个会话文件
                CleanOldSessions(20);
            }
            catch { }
        }

        #endregion

        #region Internal

        private void EnsureDirectories()
        {
            try
            {
                if (!Directory.Exists(_baseDir))
                    Directory.CreateDirectory(_baseDir);
                if (!Directory.Exists(_sessionsDir))
                    Directory.CreateDirectory(_sessionsDir);
            }
            catch { }
        }

        private void LoadMemories()
        {
            try
            {
                if (File.Exists(_memoriesPath))
                {
                    var json = File.ReadAllText(_memoriesPath);
                    _memories = JsonSerializer.Deserialize<List<MemoryItem>>(json) ?? new();
                }
            }
            catch
            {
                _memories = new();
            }
        }

        private void PersistMemories()
        {
            try
            {
                EnsureDirectories();
                File.WriteAllText(_memoriesPath, JsonSerializer.Serialize(_memories, JsonOpts));
            }
            catch { }
        }

        private string ValidateCategory(string category)
        {
            var valid = new HashSet<string> { "preference", "alloy_system", "calculation", "knowledge", "general" };
            return valid.Contains(category) ? category : "general";
        }

        private string GenerateSessionSummary(List<ChatMessage> messages)
        {
            var userMsgs = messages.Where(m => m.Role == "user").Select(m => m.Content).ToList();
            if (userMsgs.Count == 0) return "空会话";
            var first = userMsgs[0];
            if (first.Length > 60) first = first.Substring(0, 60) + "...";

            // 统计工具调用数
            int toolCallCount = messages.Count(m => m.Role == "assistant" && m.ToolCalls != null && m.ToolCalls.Count > 0);
            var toolNames = messages
                .Where(m => m.Role == "assistant" && m.ToolCalls != null)
                .SelectMany(m => m.ToolCalls!)
                .Select(tc => tc.Function.Name)
                .Distinct()
                .ToList();

            var sb = new System.Text.StringBuilder();
            sb.Append($"用户提问: {first} (共{userMsgs.Count}条消息)");
            if (toolCallCount > 0)
                sb.Append($" [使用工具: {string.Join(", ", toolNames)}]");
            return sb.ToString();
        }

        /// <summary>
        /// 从对话中提取计算历史（工具名、参数摘要、结果摘要）
        /// </summary>
        private List<object> ExtractCalculations(List<ChatMessage> messages)
        {
            var result = new List<object>();
            try
            {
                for (int i = 0; i < messages.Count; i++)
                {
                    var msg = messages[i];
                    if (msg.Role != "assistant" || msg.ToolCalls == null) continue;

                    foreach (var tc in msg.ToolCalls)
                    {
                        // 找到对应的工具结果
                        var toolResult = messages.Skip(i + 1)
                            .FirstOrDefault(m => m.Role == "tool" && m.ToolCallId == tc.Id);

                        var argsSummary = SummarizeToolArgs(tc.Function.Name, tc.Function.Arguments);
                        var resultSummary = SummarizeToolResult(toolResult?.Content ?? "");

                        result.Add(new
                        {
                            tool = tc.Function.Name,
                            args_summary = argsSummary,
                            result_summary = resultSummary
                        });
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// 提取关键的用户-助手问答对（最多5轮）
        /// </summary>
        private List<object> ExtractKeyExchanges(List<ChatMessage> messages)
        {
            var result = new List<object>();
            try
            {
                for (int i = 0; i < messages.Count && result.Count < 5; i++)
                {
                    if (messages[i].Role != "user") continue;
                    var userMsg = messages[i].Content;
                    if (string.IsNullOrWhiteSpace(userMsg)) continue;

                    // 找到下一个 assistant 文本回复（跳过工具调用中间消息）
                    var assistantReply = messages.Skip(i + 1)
                        .FirstOrDefault(m => m.Role == "assistant"
                            && !string.IsNullOrWhiteSpace(m.Content)
                            && (m.ToolCalls == null || m.ToolCalls.Count == 0));

                    var userBrief = userMsg.Length > 80 ? userMsg.Substring(0, 80) + "..." : userMsg;
                    var assistBrief = "";
                    if (assistantReply != null && !string.IsNullOrWhiteSpace(assistantReply.Content))
                    {
                        assistBrief = assistantReply.Content.Length > 150
                            ? assistantReply.Content.Substring(0, 150) + "..."
                            : assistantReply.Content;
                    }

                    result.Add(new
                    {
                        user = userBrief,
                        assistant_brief = assistBrief
                    });
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// 总结工具调用参数（提取关键字段）
        /// </summary>
        private string SummarizeToolArgs(string toolName, string argsJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(argsJson);
                var root = doc.RootElement;
                var parts = new List<string>();

                // 提取常见关键字段
                if (root.TryGetProperty("solvent", out var sv)) parts.Add($"溶剂={sv.GetString()}");
                if (root.TryGetProperty("solute_i", out var si)) parts.Add($"溶质i={si.GetString()}");
                if (root.TryGetProperty("solute_j", out var sj)) parts.Add($"溶质j={sj.GetString()}");
                if (root.TryGetProperty("temperature", out var temp)) parts.Add($"T={temp.GetDouble()}K");
                if (root.TryGetProperty("composition", out var comp)) parts.Add($"组成={comp.GetRawText()}");
                if (root.TryGetProperty("element", out var elem)) parts.Add(elem.GetString() ?? "");
                if (root.TryGetProperty("base_element", out var be)) parts.Add($"基体={be.GetString()}");

                return parts.Count > 0 ? string.Join(", ", parts) : argsJson.Length > 100 ? argsJson.Substring(0, 100) + "..." : argsJson;
            }
            catch
            {
                return argsJson.Length > 100 ? argsJson.Substring(0, 100) + "..." : argsJson;
            }
        }

        /// <summary>
        /// 总结工具执行结果（提取关键数值）
        /// </summary>
        private string SummarizeToolResult(string resultJson)
        {
            if (string.IsNullOrWhiteSpace(resultJson)) return "无结果";
            try
            {
                using var doc = JsonDocument.Parse(resultJson);
                var root = doc.RootElement;
                var parts = new List<string>();

                // 提取关键结果字段
                if (root.TryGetProperty("liquidus_temperature_K", out var ltk))
                    parts.Add($"液相线={ltk.GetDouble():F1}K");
                if (root.TryGetProperty("activity_coefficient", out var ac))
                    parts.Add($"γ={ac.GetDouble():F4}");
                if (root.TryGetProperty("activity", out var act))
                    parts.Add($"a={act.GetDouble():F4}");
                if (root.TryGetProperty("epsilon", out var eps))
                    parts.Add($"ε={eps.GetDouble():F4}");
                if (root.TryGetProperty("melting_point", out var mp))
                    parts.Add($"Tm={mp.GetDouble():F1}K");
                if (root.TryGetProperty("status", out var st))
                    parts.Add(st.GetString() ?? "");

                if (parts.Count > 0) return string.Join(", ", parts);

                // 回退：截取前100字符
                var text = resultJson.Length > 100 ? resultJson.Substring(0, 100) + "..." : resultJson;
                return text;
            }
            catch
            {
                return resultJson.Length > 100 ? resultJson.Substring(0, 100) + "..." : resultJson;
            }
        }

        private void CleanOldSessions(int maxCount)
        {
            try
            {
                var files = Directory.GetFiles(_sessionsDir, "*.json")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();
                foreach (var f in files.Skip(maxCount))
                    File.Delete(f);
            }
            catch { }
        }

        #endregion
    }

    public class MemoryItem
    {
        public string Content { get; set; } = "";
        public string Category { get; set; } = "general";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
