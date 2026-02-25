using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlloyAct_Pro.LLM
{
    #region Data Models

    public class MessageImage
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string MimeType { get; set; } = "image/png";
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public List<MessageImage>? Images { get; set; }
        public List<ToolCall>? ToolCalls { get; set; }
        public string? ToolCallId { get; set; }
    }

    public class ToolCall
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "function";
        public ToolCallFunction Function { get; set; } = new();
    }

    public class ToolCallFunction
    {
        public string Name { get; set; } = "";
        public string Arguments { get; set; } = "{}";
    }

    public class ToolDefinition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public JsonElement Parameters { get; set; }
    }

    public class LlmResponse
    {
        public string Content { get; set; } = "";
        public List<ToolCall> ToolCalls { get; set; } = new();
        public string FinishReason { get; set; } = "stop";
    }

    public enum StreamChunkType
    {
        TextDelta,         // 增量文本 token
        ToolCallComplete,  // 完整的工具调用已组装完毕
        Done               // 流结束
    }

    public class StreamChunk
    {
        public StreamChunkType Type { get; set; }
        public string TextDelta { get; set; } = "";
        public ToolCall? CompletedToolCall { get; set; }
        public string FinishReason { get; set; } = "";
    }

    #endregion

    #region Backend Configs

    public class ProviderConfig
    {
        public string Name { get; set; } = "";
        public string BaseUrl { get; set; } = "";
        public string DefaultModel { get; set; } = "";
        public string EnvKey { get; set; } = "";
        public string[] ModelList { get; set; } = Array.Empty<string>();
        public string ApiKeyHint { get; set; } = "";
    }

    public static class ProviderRegistry
    {
        public static readonly Dictionary<string, ProviderConfig> Providers = new()
        {
            ["ollama"] = new ProviderConfig
            {
                Name = "Ollama",
                BaseUrl = "http://100.91.243.106:11434/v1",
                DefaultModel = "qwen2.5:7b",
                EnvKey = "",
                ApiKeyHint = "本地模型无需填写",
                ModelList = new[] { "qwen2.5:7b", "qwen2.5:14b", "qwen2.5:32b", "llama3.2:3b", "mistral:7b" }
            },
            ["openai"] = new ProviderConfig
            {
                Name = "OpenAI",
                BaseUrl = "https://api.openai.com/v1",
                DefaultModel = "gpt-4o",
                EnvKey = "OPENAI_API_KEY",
                ApiKeyHint = "sk-...",
                ModelList = new[] { "gpt-4o", "gpt-4o-mini", "gpt-4.1", "gpt-4.1-mini", "o3-mini", "o4-mini" }
            },
            ["claude"] = new ProviderConfig
            {
                Name = "Claude",
                BaseUrl = "https://api.anthropic.com",
                DefaultModel = "claude-sonnet-4-5-20250929",
                EnvKey = "ANTHROPIC_API_KEY",
                ApiKeyHint = "sk-ant-...",
                ModelList = new[] { "claude-sonnet-4-5-20250929", "claude-opus-4-6",
                                    "claude-haiku-4-5-20251001", "claude-sonnet-4-20250514" }
            },
            ["gemini"] = new ProviderConfig
            {
                Name = "Gemini",
                BaseUrl = "https://generativelanguage.googleapis.com/v1beta",
                DefaultModel = "gemini-2.0-flash",
                EnvKey = "GOOGLE_API_KEY",
                ApiKeyHint = "AIza...",
                ModelList = new[] { "gemini-2.5-pro", "gemini-2.5-flash", "gemini-2.0-flash" }
            },
            ["deepseek"] = new ProviderConfig
            {
                Name = "DeepSeek",
                BaseUrl = "https://api.deepseek.com/v1",
                DefaultModel = "deepseek-chat",
                EnvKey = "DEEPSEEK_API_KEY",
                ApiKeyHint = "sk-...",
                ModelList = new[] { "deepseek-chat", "deepseek-reasoner" }
            },
            ["kimichat"] = new ProviderConfig
            {
                Name = "Kimi",
                BaseUrl = "https://api.moonshot.cn/v1",
                DefaultModel = "moonshot-v1-8k",
                EnvKey = "KIMI_API_KEY",
                ApiKeyHint = "sk-...",
                ModelList = new[] { "moonshot-v1-8k", "moonshot-v1-32k", "moonshot-v1-128k" }
            }
        };

        public static string[] GetProviderNames() => Providers.Keys.ToArray();

        /// <summary>
        /// 从 Ollama 服务器动态获取已安装的模型列表
        /// </summary>
        public static async Task<string[]> FetchOllamaModelsAsync(string baseUrl, CancellationToken ct = default)
        {
            try
            {
                var url = baseUrl.TrimEnd('/');
                if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                    url = url.Substring(0, url.Length - 3);

                var apiUrl = $"{url}/api/tags";

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await client.GetAsync(apiUrl, ct);
                if (!response.IsSuccessStatusCode) return Array.Empty<string>();

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("models", out var modelsArray))
                    return Array.Empty<string>();

                var models = new List<string>();
                foreach (var model in modelsArray.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                    {
                        var modelName = name.GetString();
                        if (!string.IsNullOrEmpty(modelName))
                            models.Add(modelName);
                    }
                }

                return models.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }

    #endregion

    #region Abstract Backend

    public abstract class LlmBackend
    {
        /// <summary>
        /// 当前已连接的 LLM 后端实例（由 ChatPanel 在连接时设置）
        /// </summary>
        public static LlmBackend? Current { get; set; }

        protected string ApiKey;
        protected string Model;
        protected static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(600) };
        protected static readonly HttpClient StreamingHttpClient = new() { Timeout = Timeout.InfiniteTimeSpan };

        protected LlmBackend(string apiKey, string model)
        {
            ApiKey = apiKey ?? "";
            Model = model ?? "";
        }

        public abstract Task<LlmResponse> ChatAsync(
            List<ChatMessage> messages,
            List<ToolDefinition>? tools = null,
            CancellationToken ct = default);

        /// <summary>
        /// 流式对话（默认回退到非流式）
        /// </summary>
        public virtual async IAsyncEnumerable<StreamChunk> ChatStreamAsync(
            List<ChatMessage> messages,
            List<ToolDefinition>? tools = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var response = await ChatAsync(messages, tools, ct);
            if (!string.IsNullOrEmpty(response.Content))
                yield return new StreamChunk { Type = StreamChunkType.TextDelta, TextDelta = response.Content };
            foreach (var tc in response.ToolCalls)
                yield return new StreamChunk { Type = StreamChunkType.ToolCallComplete, CompletedToolCall = tc };
            yield return new StreamChunk { Type = StreamChunkType.Done, FinishReason = response.FinishReason };
        }

        /// <summary>
        /// 从远程 API 获取可用模型列表
        /// Ollama: GET {baseUrl}/../api/tags  → models[].name
        /// OpenAI兼容: GET {baseUrl}/models   → data[].id
        /// </summary>
        public static async Task<string[]> FetchModelsAsync(string provider, string? baseUrl = null, string? apiKey = null, CancellationToken ct = default)
        {
            if (!ProviderRegistry.Providers.TryGetValue(provider, out var config))
                return Array.Empty<string>();

            var url = string.IsNullOrWhiteSpace(baseUrl) ? config.BaseUrl : baseUrl.Trim();
            var key = apiKey ?? "";

            try
            {
                if (provider == "ollama")
                {
                    // Ollama: baseUrl is like "http://host:11434/v1", api/tags is at "http://host:11434/api/tags"
                    var ollamaBase = url.TrimEnd('/');
                    if (ollamaBase.EndsWith("/v1"))
                        ollamaBase = ollamaBase.Substring(0, ollamaBase.Length - 3);
                    var tagsUrl = ollamaBase.TrimEnd('/') + "/api/tags";

                    var request = new HttpRequestMessage(HttpMethod.Get, tagsUrl);
                    var response = await SharedHttpClient.SendAsync(request, ct);
                    var body = await response.Content.ReadAsStringAsync(ct);
                    if (!response.IsSuccessStatusCode) return Array.Empty<string>();

                    using var doc = JsonDocument.Parse(body);
                    var models = new List<string>();
                    if (doc.RootElement.TryGetProperty("models", out var arr))
                    {
                        foreach (var m in arr.EnumerateArray())
                        {
                            var name = m.TryGetProperty("name", out var n) ? n.GetString() : null;
                            if (!string.IsNullOrEmpty(name))
                                models.Add(name);
                        }
                    }
                    return models.Count > 0 ? models.ToArray() : config.ModelList;
                }

                if (provider is "openai" or "deepseek" or "kimichat")
                {
                    // OpenAI-compatible: GET /models
                    var modelsUrl = url.TrimEnd('/') + "/models";
                    var request = new HttpRequestMessage(HttpMethod.Get, modelsUrl);
                    if (!string.IsNullOrEmpty(key))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    var response = await SharedHttpClient.SendAsync(request, ct);
                    var body = await response.Content.ReadAsStringAsync(ct);
                    if (!response.IsSuccessStatusCode) return config.ModelList;

                    using var doc = JsonDocument.Parse(body);
                    var models = new List<string>();
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        foreach (var m in data.EnumerateArray())
                        {
                            var id = m.TryGetProperty("id", out var idVal) ? idVal.GetString() : null;
                            if (!string.IsNullOrEmpty(id))
                                models.Add(id);
                        }
                    }
                    return models.Count > 0 ? models.ToArray() : config.ModelList;
                }

                // Claude / Gemini — no standard models endpoint, use hardcoded list
                return config.ModelList;
            }
            catch
            {
                return config.ModelList;
            }
        }

        public static LlmBackend Create(string provider, string? apiKey = null, string? model = null, string? baseUrl = null)
        {
            if (!ProviderRegistry.Providers.TryGetValue(provider, out var config))
                throw new ArgumentException($"不支持的提供商: {provider}");

            var key = apiKey ?? Environment.GetEnvironmentVariable(config.EnvKey) ?? "";
            var mdl = model ?? config.DefaultModel;
            var url = string.IsNullOrWhiteSpace(baseUrl) ? config.BaseUrl : baseUrl.Trim();

            if (provider == "claude")
                return new ClaudeBackend(key, mdl);
            if (provider == "gemini")
                return new GeminiBackend(key, mdl);

            // OpenAI-compatible: openai, deepseek, kimichat, ollama
            return new OpenAICompatibleBackend(key, url, mdl);
        }
    }

    #endregion

    #region OpenAI-Compatible Backend

    public class OpenAICompatibleBackend : LlmBackend
    {
        private readonly string _baseUrl;

        public OpenAICompatibleBackend(string apiKey, string baseUrl, string model)
            : base(apiKey, model)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public override async Task<LlmResponse> ChatAsync(
            List<ChatMessage> messages, List<ToolDefinition>? tools, CancellationToken ct)
        {
            var apiMessages = new List<object>();
            foreach (var msg in messages)
            {
                // Multimodal: images + text as content array
                if (msg.Images != null && msg.Images.Count > 0 && msg.Role == "user")
                {
                    var parts = new List<object>();
                    if (!string.IsNullOrEmpty(msg.Content))
                        parts.Add(new { type = "text", text = msg.Content });
                    foreach (var img in msg.Images)
                    {
                        var b64 = Convert.ToBase64String(img.Data);
                        parts.Add(new { type = "image_url", image_url = new { url = $"data:{img.MimeType};base64,{b64}" } });
                    }
                    apiMessages.Add(new Dictionary<string, object> { ["role"] = msg.Role, ["content"] = parts });
                    continue;
                }

                var m = new Dictionary<string, object> { ["role"] = msg.Role, ["content"] = msg.Content ?? "" };
                if (msg.ToolCallId != null) m["tool_call_id"] = msg.ToolCallId;
                if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                {
                    m["tool_calls"] = msg.ToolCalls.Select(tc => new
                    {
                        id = tc.Id,
                        type = tc.Type,
                        function = new { name = tc.Function.Name, arguments = tc.Function.Arguments }
                    }).ToList();
                }
                apiMessages.Add(m);
            }

            var body = new Dictionary<string, object>
            {
                ["model"] = Model,
                ["messages"] = apiMessages
            };

            if (tools != null && tools.Count > 0)
            {
                body["tools"] = tools.Select(t => new
                {
                    type = "function",
                    function = new { name = t.Name, description = t.Description, parameters = t.Parameters }
                }).ToList();
                body["tool_choice"] = "auto";
            }

            var json = JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrEmpty(ApiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            var response = await SharedHttpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"API error ({response.StatusCode}): {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var message = choice.GetProperty("message");

            var result = new LlmResponse
            {
                Content = message.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String
                    ? c.GetString() ?? "" : "",
                FinishReason = choice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() ?? "stop" : "stop"
            };

            if (message.TryGetProperty("tool_calls", out var tcs) && tcs.ValueKind == JsonValueKind.Array)
            {
                foreach (var tc in tcs.EnumerateArray())
                {
                    var fn = tc.GetProperty("function");
                    result.ToolCalls.Add(new ToolCall
                    {
                        Id = tc.GetProperty("id").GetString() ?? "",
                        Type = "function",
                        Function = new ToolCallFunction
                        {
                            Name = fn.GetProperty("name").GetString() ?? "",
                            Arguments = fn.GetProperty("arguments").GetString() ?? "{}"
                        }
                    });
                }
            }

            return result;
        }

        private class ToolCallAccumulator
        {
            public string Id = "";
            public string Name = "";
            public StringBuilder Arguments = new();
        }

        public override async IAsyncEnumerable<StreamChunk> ChatStreamAsync(
            List<ChatMessage> messages, List<ToolDefinition>? tools,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            // Build request body (same as ChatAsync but with stream: true)
            var apiMessages = new List<object>();
            foreach (var msg in messages)
            {
                var m = new Dictionary<string, object> { ["role"] = msg.Role, ["content"] = msg.Content ?? "" };
                if (msg.ToolCallId != null) m["tool_call_id"] = msg.ToolCallId;
                if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                {
                    m["tool_calls"] = msg.ToolCalls.Select(tc => new
                    {
                        id = tc.Id,
                        type = tc.Type,
                        function = new { name = tc.Function.Name, arguments = tc.Function.Arguments }
                    }).ToList();
                }
                apiMessages.Add(m);
            }

            var body = new Dictionary<string, object>
            {
                ["model"] = Model,
                ["messages"] = apiMessages,
                ["stream"] = true
            };

            if (tools != null && tools.Count > 0)
            {
                body["tools"] = tools.Select(t => new
                {
                    type = "function",
                    function = new { name = t.Name, description = t.Description, parameters = t.Parameters }
                }).ToList();
                body["tool_choice"] = "auto";
            }

            var json = JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrEmpty(ApiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            var response = await StreamingHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"API error ({response.StatusCode}): {errBody}");
            }

            var toolAccumulators = new Dictionary<int, ToolCallAccumulator>();
            string finishReason = "stop";

            await foreach (var sse in SseReader.ReadAsync(response, ct))
            {
                if (sse.Data == "[DONE]") break;

                JsonElement root;
                try { root = JsonDocument.Parse(sse.Data).RootElement; }
                catch { continue; }

                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    continue;

                var choice = choices[0];
                if (choice.TryGetProperty("finish_reason", out var fr) && fr.ValueKind == JsonValueKind.String)
                    finishReason = fr.GetString() ?? "stop";

                if (!choice.TryGetProperty("delta", out var delta))
                    continue;

                // Text content delta
                if (delta.TryGetProperty("content", out var contentVal) && contentVal.ValueKind == JsonValueKind.String)
                {
                    var text = contentVal.GetString();
                    if (!string.IsNullOrEmpty(text))
                        yield return new StreamChunk { Type = StreamChunkType.TextDelta, TextDelta = text };
                }

                // Tool call deltas
                if (delta.TryGetProperty("tool_calls", out var tcArr) && tcArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tcDelta in tcArr.EnumerateArray())
                    {
                        int index = tcDelta.TryGetProperty("index", out var idx) ? idx.GetInt32() : 0;
                        if (!toolAccumulators.TryGetValue(index, out var acc))
                        {
                            acc = new ToolCallAccumulator();
                            toolAccumulators[index] = acc;
                        }

                        if (tcDelta.TryGetProperty("id", out var idVal) && idVal.ValueKind == JsonValueKind.String)
                            acc.Id = idVal.GetString() ?? "";

                        if (tcDelta.TryGetProperty("function", out var fn))
                        {
                            if (fn.TryGetProperty("name", out var nameVal) && nameVal.ValueKind == JsonValueKind.String)
                                acc.Name = nameVal.GetString() ?? "";
                            if (fn.TryGetProperty("arguments", out var argsVal) && argsVal.ValueKind == JsonValueKind.String)
                                acc.Arguments.Append(argsVal.GetString() ?? "");
                        }
                    }
                }
            }

            // Emit accumulated tool calls
            foreach (var kvp in toolAccumulators.OrderBy(k => k.Key))
            {
                var acc = kvp.Value;
                yield return new StreamChunk
                {
                    Type = StreamChunkType.ToolCallComplete,
                    CompletedToolCall = new ToolCall
                    {
                        Id = acc.Id,
                        Type = "function",
                        Function = new ToolCallFunction
                        {
                            Name = acc.Name,
                            Arguments = acc.Arguments.ToString()
                        }
                    }
                };
            }

            yield return new StreamChunk { Type = StreamChunkType.Done, FinishReason = finishReason };
        }
    }

    #endregion

    #region Claude Backend

    public class ClaudeBackend : LlmBackend
    {
        public ClaudeBackend(string apiKey, string model) : base(apiKey, model) { }

        public override async Task<LlmResponse> ChatAsync(
            List<ChatMessage> messages, List<ToolDefinition>? tools, CancellationToken ct)
        {
            string systemMsg = "";
            var apiMessages = new List<object>();

            foreach (var msg in messages)
            {
                if (msg.Role == "system")
                {
                    systemMsg = msg.Content;
                    continue;
                }

                if (msg.Role == "tool")
                {
                    apiMessages.Add(new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "tool_result", tool_use_id = msg.ToolCallId ?? "", content = msg.Content ?? "" }
                        }
                    });
                    continue;
                }

                if (msg.Role == "assistant" && msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                {
                    var contentBlocks = new List<object>();
                    if (!string.IsNullOrEmpty(msg.Content))
                        contentBlocks.Add(new { type = "text", text = msg.Content });
                    foreach (var tc in msg.ToolCalls)
                    {
                        contentBlocks.Add(new
                        {
                            type = "tool_use",
                            id = tc.Id,
                            name = tc.Function.Name,
                            input = JsonSerializer.Deserialize<JsonElement>(tc.Function.Arguments)
                        });
                    }
                    apiMessages.Add(new { role = "assistant", content = contentBlocks });
                    continue;
                }

                // Multimodal: images + text as content blocks
                if (msg.Images != null && msg.Images.Count > 0 && msg.Role == "user")
                {
                    var blocks = new List<object>();
                    foreach (var img in msg.Images)
                    {
                        blocks.Add(new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = img.MimeType,
                                data = Convert.ToBase64String(img.Data)
                            }
                        });
                    }
                    if (!string.IsNullOrEmpty(msg.Content))
                        blocks.Add(new { type = "text", text = msg.Content });
                    apiMessages.Add(new { role = msg.Role, content = blocks });
                    continue;
                }

                apiMessages.Add(new { role = msg.Role, content = msg.Content ?? "" });
            }

            var body = new Dictionary<string, object>
            {
                ["model"] = Model,
                ["max_tokens"] = 8192,
                ["messages"] = apiMessages
            };
            if (!string.IsNullOrEmpty(systemMsg))
                body["system"] = systemMsg;
            if (tools != null && tools.Count > 0)
            {
                body["tools"] = tools.Select(t => new
                {
                    name = t.Name,
                    description = t.Description,
                    input_schema = t.Parameters
                }).ToList();
            }

            var json = JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", ApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await SharedHttpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Claude API error ({response.StatusCode}): {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            var result = new LlmResponse
            {
                FinishReason = root.TryGetProperty("stop_reason", out var sr) ? sr.GetString() ?? "end_turn" : "end_turn"
            };

            if (root.TryGetProperty("content", out var contentArr))
            {
                foreach (var block in contentArr.EnumerateArray())
                {
                    var blockType = block.GetProperty("type").GetString();
                    if (blockType == "text")
                        result.Content += block.GetProperty("text").GetString() ?? "";
                    else if (blockType == "tool_use")
                    {
                        result.ToolCalls.Add(new ToolCall
                        {
                            Id = block.GetProperty("id").GetString() ?? "",
                            Type = "function",
                            Function = new ToolCallFunction
                            {
                                Name = block.GetProperty("name").GetString() ?? "",
                                Arguments = block.GetProperty("input").GetRawText()
                            }
                        });
                    }
                }
            }

            return result;
        }

        public override async IAsyncEnumerable<StreamChunk> ChatStreamAsync(
            List<ChatMessage> messages, List<ToolDefinition>? tools,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            // Build request body (same as ChatAsync but with stream: true)
            string systemMsg = "";
            var apiMessages = new List<object>();
            foreach (var msg in messages)
            {
                if (msg.Role == "system") { systemMsg = msg.Content; continue; }
                if (msg.Role == "tool")
                {
                    apiMessages.Add(new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "tool_result", tool_use_id = msg.ToolCallId ?? "", content = msg.Content ?? "" }
                        }
                    });
                    continue;
                }
                if (msg.Role == "assistant" && msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                {
                    var contentBlocks = new List<object>();
                    if (!string.IsNullOrEmpty(msg.Content))
                        contentBlocks.Add(new { type = "text", text = msg.Content });
                    foreach (var tc in msg.ToolCalls)
                    {
                        contentBlocks.Add(new
                        {
                            type = "tool_use",
                            id = tc.Id,
                            name = tc.Function.Name,
                            input = JsonSerializer.Deserialize<JsonElement>(tc.Function.Arguments)
                        });
                    }
                    apiMessages.Add(new { role = "assistant", content = contentBlocks });
                    continue;
                }
                apiMessages.Add(new { role = msg.Role, content = msg.Content ?? "" });
            }

            var body = new Dictionary<string, object>
            {
                ["model"] = Model,
                ["max_tokens"] = 8192,
                ["messages"] = apiMessages,
                ["stream"] = true
            };
            if (!string.IsNullOrEmpty(systemMsg))
                body["system"] = systemMsg;
            if (tools != null && tools.Count > 0)
            {
                body["tools"] = tools.Select(t => new
                {
                    name = t.Name,
                    description = t.Description,
                    input_schema = t.Parameters
                }).ToList();
            }

            var json = JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", ApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await StreamingHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"Claude API error ({response.StatusCode}): {errBody}");
            }

            // Track current content block
            bool inToolUse = false;
            string currentToolId = "";
            string currentToolName = "";
            var toolArgsBuilder = new StringBuilder();
            string finishReason = "end_turn";

            await foreach (var sse in SseReader.ReadAsync(response, ct))
            {
                JsonElement root;
                try { root = JsonDocument.Parse(sse.Data).RootElement; }
                catch { continue; }

                var eventType = sse.Event;
                if (string.IsNullOrEmpty(eventType) && root.TryGetProperty("type", out var typeVal))
                    eventType = typeVal.GetString() ?? "";

                switch (eventType)
                {
                    case "content_block_start":
                        if (root.TryGetProperty("content_block", out var block))
                        {
                            var blockType = block.TryGetProperty("type", out var bt) ? bt.GetString() : "";
                            if (blockType == "tool_use")
                            {
                                inToolUse = true;
                                currentToolId = block.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                                currentToolName = block.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";
                                toolArgsBuilder.Clear();
                            }
                            else
                            {
                                inToolUse = false;
                            }
                        }
                        break;

                    case "content_block_delta":
                        if (root.TryGetProperty("delta", out var delta))
                        {
                            var deltaType = delta.TryGetProperty("type", out var dt) ? dt.GetString() : "";
                            if (deltaType == "text_delta")
                            {
                                var text = delta.TryGetProperty("text", out var tv) ? tv.GetString() ?? "" : "";
                                if (!string.IsNullOrEmpty(text))
                                    yield return new StreamChunk { Type = StreamChunkType.TextDelta, TextDelta = text };
                            }
                            else if (deltaType == "input_json_delta")
                            {
                                var partial = delta.TryGetProperty("partial_json", out var pj) ? pj.GetString() ?? "" : "";
                                toolArgsBuilder.Append(partial);
                            }
                        }
                        break;

                    case "content_block_stop":
                        if (inToolUse)
                        {
                            yield return new StreamChunk
                            {
                                Type = StreamChunkType.ToolCallComplete,
                                CompletedToolCall = new ToolCall
                                {
                                    Id = currentToolId,
                                    Type = "function",
                                    Function = new ToolCallFunction
                                    {
                                        Name = currentToolName,
                                        Arguments = toolArgsBuilder.ToString()
                                    }
                                }
                            };
                            inToolUse = false;
                        }
                        break;

                    case "message_delta":
                        if (root.TryGetProperty("delta", out var msgDelta))
                        {
                            if (msgDelta.TryGetProperty("stop_reason", out var sr) && sr.ValueKind == JsonValueKind.String)
                                finishReason = sr.GetString() ?? "end_turn";
                        }
                        break;

                    case "message_stop":
                        break;

                    case "error":
                        var errMsg = root.TryGetProperty("error", out var err)
                            ? (err.TryGetProperty("message", out var em) ? em.GetString() ?? "Unknown error" : "Unknown error")
                            : "Unknown stream error";
                        throw new Exception($"Claude stream error: {errMsg}");
                }
            }

            yield return new StreamChunk { Type = StreamChunkType.Done, FinishReason = finishReason };
        }
    }

    #endregion

    #region Gemini Backend

    public class GeminiBackend : LlmBackend
    {
        public GeminiBackend(string apiKey, string model) : base(apiKey, model) { }

        /// <summary>
        /// Clean JSON Schema for Gemini compatibility (remove unsupported fields)
        /// </summary>
        private static JsonElement CleanSchemaForGemini(JsonElement schema)
        {
            var supported = new HashSet<string> { "type", "description", "properties", "required", "enum", "items", "format", "nullable" };
            var dict = new Dictionary<string, object>();

            foreach (var prop in schema.EnumerateObject())
            {
                if (!supported.Contains(prop.Name)) continue;

                if (prop.Name == "properties" && prop.Value.ValueKind == JsonValueKind.Object)
                {
                    var cleaned = new Dictionary<string, object>();
                    foreach (var p in prop.Value.EnumerateObject())
                        cleaned[p.Name] = CleanSchemaForGemini(p.Value);
                    dict[prop.Name] = cleaned;
                }
                else if (prop.Name == "items" && prop.Value.ValueKind == JsonValueKind.Object)
                {
                    dict[prop.Name] = CleanSchemaForGemini(prop.Value);
                }
                else
                {
                    dict[prop.Name] = prop.Value;
                }
            }

            var json = JsonSerializer.Serialize(dict);
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        public override async Task<LlmResponse> ChatAsync(
            List<ChatMessage> messages, List<ToolDefinition>? tools, CancellationToken ct)
        {
            // Build contents
            var contents = new List<object>();
            foreach (var msg in messages)
            {
                if (msg.Role == "system") continue;
                var role = msg.Role == "assistant" ? "model" : "user";

                // Multimodal: images + text as parts
                if (msg.Images != null && msg.Images.Count > 0)
                {
                    var parts = new List<object>();
                    foreach (var img in msg.Images)
                        parts.Add(new { inline_data = new { mime_type = img.MimeType, data = Convert.ToBase64String(img.Data) } });
                    if (!string.IsNullOrEmpty(msg.Content))
                        parts.Add(new { text = msg.Content });
                    contents.Add(new { role, parts });
                    continue;
                }

                contents.Add(new { role, parts = new[] { new { text = msg.Content ?? "" } } });
            }

            var body = new Dictionary<string, object> { ["contents"] = contents };

            if (tools != null && tools.Count > 0)
            {
                var declarations = tools.Select(t => new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = CleanSchemaForGemini(t.Parameters)
                }).ToList();
                body["tools"] = new[] { new { function_declarations = declarations } };
            }

            // Add system instruction if present
            var systemMsg = messages.FirstOrDefault(m => m.Role == "system");
            if (systemMsg != null)
                body["system_instruction"] = new { parts = new[] { new { text = systemMsg.Content } } };

            var json = JsonSerializer.Serialize(body);
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await SharedHttpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Gemini API error ({response.StatusCode}): {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            var result = new LlmResponse();

            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() > 0)
            {
                var parts = candidates[0].GetProperty("content").GetProperty("parts");
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text))
                        result.Content += text.GetString() ?? "";
                    if (part.TryGetProperty("functionCall", out var fc))
                    {
                        var name = fc.GetProperty("name").GetString() ?? "";
                        result.ToolCalls.Add(new ToolCall
                        {
                            Id = $"call_{name}_{Guid.NewGuid():N}".Substring(0, 32),
                            Type = "function",
                            Function = new ToolCallFunction
                            {
                                Name = name,
                                Arguments = fc.GetProperty("args").GetRawText()
                            }
                        });
                    }
                }
            }

            return result;
        }

        public override async IAsyncEnumerable<StreamChunk> ChatStreamAsync(
            List<ChatMessage> messages, List<ToolDefinition>? tools,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            // Build contents (same as ChatAsync)
            var contents = new List<object>();
            foreach (var msg in messages)
            {
                if (msg.Role == "system") continue;
                var role = msg.Role == "assistant" ? "model" : "user";
                contents.Add(new { role, parts = new[] { new { text = msg.Content ?? "" } } });
            }

            var body = new Dictionary<string, object> { ["contents"] = contents };
            if (tools != null && tools.Count > 0)
            {
                var declarations = tools.Select(t => new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = CleanSchemaForGemini(t.Parameters)
                }).ToList();
                body["tools"] = new[] { new { function_declarations = declarations } };
            }

            var systemMsg = messages.FirstOrDefault(m => m.Role == "system");
            if (systemMsg != null)
                body["system_instruction"] = new { parts = new[] { new { text = systemMsg.Content } } };

            var json = JsonSerializer.Serialize(body);
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:streamGenerateContent?alt=sse&key={ApiKey}";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await StreamingHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"Gemini API error ({response.StatusCode}): {errBody}");
            }

            await foreach (var sse in SseReader.ReadAsync(response, ct))
            {
                if (string.IsNullOrWhiteSpace(sse.Data)) continue;

                JsonElement root;
                try { root = JsonDocument.Parse(sse.Data).RootElement; }
                catch { continue; }

                if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    continue;

                var candidate = candidates[0];
                if (!candidate.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts))
                    continue;

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text))
                    {
                        var t = text.GetString();
                        if (!string.IsNullOrEmpty(t))
                            yield return new StreamChunk { Type = StreamChunkType.TextDelta, TextDelta = t };
                    }
                    if (part.TryGetProperty("functionCall", out var fc))
                    {
                        var name = fc.GetProperty("name").GetString() ?? "";
                        yield return new StreamChunk
                        {
                            Type = StreamChunkType.ToolCallComplete,
                            CompletedToolCall = new ToolCall
                            {
                                Id = $"call_{name}_{Guid.NewGuid():N}".Substring(0, 32),
                                Type = "function",
                                Function = new ToolCallFunction
                                {
                                    Name = name,
                                    Arguments = fc.GetProperty("args").GetRawText()
                                }
                            }
                        };
                    }
                }
            }

            yield return new StreamChunk { Type = StreamChunkType.Done, FinishReason = "stop" };
        }
    }

    #endregion
}
