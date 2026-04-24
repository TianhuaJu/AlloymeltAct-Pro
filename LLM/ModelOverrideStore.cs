using System.Text.Json;

namespace AlloyAct_Pro.LLM
{
    /// <summary>
    /// 单个模型条目（用户覆盖表中）
    /// </summary>
    public class ModelEntry
    {
        public string Name { get; set; } = "";
        public bool SupportsTools { get; set; } = true;
        public string Note { get; set; } = "";
    }

    /// <summary>
    /// 单个提供商的覆盖数据
    /// </summary>
    public class ProviderOverride
    {
        public string? DefaultModel { get; set; }
        public List<ModelEntry> Models { get; set; } = new();
    }

    /// <summary>
    /// 用户模型覆盖存储：允许在 UI 中增删改每个提供商的模型列表并持久化
    /// 存储路径：~/.alloyact/model_overrides.json
    /// 语义：若某提供商存在 override，则使用 override 的 Models 列表替换内置 ModelList；
    ///      否则回退到 ProviderRegistry 内置列表。
    /// </summary>
    public class ModelOverrideStore
    {
        private static readonly Lazy<ModelOverrideStore> _instance = new(() => new ModelOverrideStore());
        public static ModelOverrideStore Instance => _instance.Value;

        private readonly string _filePath;
        private Dictionary<string, ProviderOverride> _overrides = new(StringComparer.OrdinalIgnoreCase);

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private ModelOverrideStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".alloyact");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "model_overrides.json");
            Load();
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    _overrides = new Dictionary<string, ProviderOverride>(StringComparer.OrdinalIgnoreCase);
                    return;
                }
                var json = File.ReadAllText(_filePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, ProviderOverride>>(json, JsonOpts);
                _overrides = dict != null
                    ? new Dictionary<string, ProviderOverride>(dict, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, ProviderOverride>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ModelOverrideStore] Load 失败: {ex.Message}");
                _overrides = new Dictionary<string, ProviderOverride>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_overrides, JsonOpts);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ModelOverrideStore] Save 失败: {ex.Message}");
            }
        }

        public bool HasOverride(string provider) => _overrides.ContainsKey(provider);

        public ProviderOverride? GetOverride(string provider)
        {
            _overrides.TryGetValue(provider, out var ov);
            return ov;
        }

        /// <summary>
        /// 写入/替换某提供商的整个覆盖
        /// </summary>
        public void SetOverride(string provider, ProviderOverride ov)
        {
            _overrides[provider] = ov;
            Save();
        }

        /// <summary>
        /// 还原某提供商到内置默认（删除覆盖）
        /// </summary>
        public void ResetProvider(string provider)
        {
            if (_overrides.Remove(provider))
                Save();
        }

        /// <summary>
        /// 获取生效的模型名列表：有覆盖走覆盖，否则用内置
        /// </summary>
        public string[] GetEffectiveModelList(string provider, string[] fallback)
        {
            if (_overrides.TryGetValue(provider, out var ov) && ov.Models.Count > 0)
                return ov.Models.Select(m => m.Name).ToArray();
            return fallback;
        }

        /// <summary>
        /// 获取生效的默认模型：有覆盖默认则用之，否则用内置
        /// </summary>
        public string GetEffectiveDefaultModel(string provider, string fallback)
        {
            if (_overrides.TryGetValue(provider, out var ov) && !string.IsNullOrWhiteSpace(ov.DefaultModel))
                return ov.DefaultModel!;
            return fallback;
        }

        /// <summary>
        /// 查询某模型的工具调用支持标记（用户覆盖优先）
        /// 返回 null 表示未在覆盖表中声明，由调用方回退到名字模式匹配
        /// </summary>
        public bool? GetToolsSupportOverride(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName)) return null;
            foreach (var ov in _overrides.Values)
            {
                foreach (var m in ov.Models)
                {
                    if (string.Equals(m.Name, modelName, StringComparison.OrdinalIgnoreCase))
                        return m.SupportsTools;
                }
            }
            return null;
        }
    }
}
