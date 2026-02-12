using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlloyAct_Pro.LLM
{
    #region Data Models

    public class ChatMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
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
                BaseUrl = "http://localhost:11434/v1",
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
    }

    #endregion

    #region Abstract Backend

    public abstract class LlmBackend
    {
        protected string ApiKey;
        protected string Model;
        protected static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(120) };

        protected LlmBackend(string apiKey, string model)
        {
            ApiKey = apiKey ?? "";
            Model = model ?? "";
        }

        public abstract Task<LlmResponse> ChatAsync(
            List<ChatMessage> messages,
            List<ToolDefinition>? tools = null,
            CancellationToken ct = default);

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

                apiMessages.Add(new { role = msg.Role, content = msg.Content ?? "" });
            }

            var body = new Dictionary<string, object>
            {
                ["model"] = Model,
                ["max_tokens"] = 4096,
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
    }

    #endregion
}
