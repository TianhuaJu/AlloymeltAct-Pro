using System.Text.Json;

namespace AlloyAct_Pro.LLM
{
    /// <summary>
    /// 自定义模型存储 - CRUD + JSON 持久化 + 动态 ToolDefinition 生成
    /// 存储路径：~/.alloyact/custom_models/
    /// 每个模型一个 JSON 文件
    /// </summary>
    public class CustomModelStore
    {
        private readonly string _modelsDir;
        private readonly Dictionary<string, CustomModel> _models = new(StringComparer.OrdinalIgnoreCase);
        private const int MaxModels = 10;  // 防止工具过多影响 LLM 性能

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public CustomModelStore()
        {
            _modelsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".alloyact", "custom_models");
            EnsureDirectory();
            LoadAllModels();
        }

        private void EnsureDirectory()
        {
            if (!Directory.Exists(_modelsDir))
                Directory.CreateDirectory(_modelsDir);
        }

        // ===== CRUD 操作 =====

        /// <summary>
        /// 保存（创建或更新）自定义模型
        /// </summary>
        public string SaveModel(CustomModel model)
        {
            // 验证名称
            if (string.IsNullOrWhiteSpace(model.Name))
                return JsonError("模型名称不能为空");

            // 名称规范化：只允许字母数字下划线
            model.Name = SanitizeName(model.Name);

            // 检查数量限制（新模型时）
            if (!_models.ContainsKey(model.Name) && _models.Count >= MaxModels)
                return JsonError($"自定义模型数量已达上限 ({MaxModels} 个)，请先删除不需要的模型");

            // 验证公式
            if (string.IsNullOrWhiteSpace(model.Formula))
                return JsonError("公式表达式不能为空");

            var paramNames = model.Parameters.Select(p => p.Name).ToList();
            if (!ExpressionEvaluator.TryValidate(model.Formula, paramNames, out string? validationError))
                return JsonError($"公式验证失败: {validationError}");

            // 更新时间
            if (_models.ContainsKey(model.Name))
                model.UpdatedAt = DateTime.Now;
            else
                model.CreatedAt = DateTime.Now;

            model.UpdatedAt = DateTime.Now;
            _models[model.Name] = model;
            PersistModel(model);

            return JsonSuccess(new
            {
                message = $"模型 '{model.DisplayName}' 已保存",
                name = model.Name,
                display_name = model.DisplayName,
                formula = model.Formula,
                parameters = model.Parameters.Select(p => new { p.Name, p.Description, p.DefaultValue, p.Unit }),
                total_models = _models.Count
            });
        }

        /// <summary>
        /// 获取模型
        /// </summary>
        public CustomModel? GetModel(string name)
        {
            name = SanitizeName(name);
            _models.TryGetValue(name, out var model);
            return model;
        }

        /// <summary>
        /// 列出所有自定义模型
        /// </summary>
        public List<CustomModel> ListModels()
        {
            return _models.Values.OrderBy(m => m.CreatedAt).ToList();
        }

        /// <summary>
        /// 删除模型
        /// </summary>
        public string DeleteModel(string name)
        {
            name = SanitizeName(name);
            if (!_models.ContainsKey(name))
                return JsonError($"模型 '{name}' 不存在");

            var model = _models[name];
            _models.Remove(name);

            // 删除文件
            var filePath = Path.Combine(_modelsDir, $"{name}.json");
            if (File.Exists(filePath))
                File.Delete(filePath);

            return JsonSuccess(new { message = $"模型 '{model.DisplayName}' 已删除", remaining = _models.Count });
        }

        // ===== 模型执行 =====

        /// <summary>
        /// 执行自定义模型
        /// </summary>
        public string ExecuteModel(string name, Dictionary<string, double> paramValues)
        {
            name = SanitizeName(name);
            if (!_models.TryGetValue(name, out var model))
                return JsonError($"模型 '{name}' 不存在");

            // 填充默认值
            var allParams = new Dictionary<string, double>(paramValues, StringComparer.OrdinalIgnoreCase);
            foreach (var param in model.Parameters)
            {
                if (!allParams.ContainsKey(param.Name))
                {
                    if (param.DefaultValue.HasValue)
                        allParams[param.Name] = param.DefaultValue.Value;
                    else if (param.IsRequired)
                        return JsonError($"缺少必填参数: {param.Name} ({param.Description})");
                }
            }

            try
            {
                double result = ExpressionEvaluator.Evaluate(model.Formula, allParams);

                return JsonSuccess(new
                {
                    model_name = model.DisplayName,
                    formula = model.Formula,
                    parameters = allParams,
                    result_name = string.IsNullOrEmpty(model.ResultName) ? "计算结果" : model.ResultName,
                    result_value = result,
                    result_unit = model.ResultUnit
                });
            }
            catch (Exception ex)
            {
                return JsonError($"计算错误: {ex.Message}");
            }
        }

        // ===== 动态 ToolDefinition 生成 =====

        /// <summary>
        /// 为自定义模型生成 LLM 工具定义
        /// 工具名格式：custom_model_{name}
        /// </summary>
        public ToolDefinition GenerateToolDefinition(CustomModel model)
        {
            // 构建参数 JSON Schema
            var properties = new Dictionary<string, object>();
            var required = new List<string>();

            foreach (var param in model.Parameters)
            {
                var prop = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = string.IsNullOrEmpty(param.Unit)
                        ? param.Description
                        : $"{param.Description} ({param.Unit})"
                };
                if (param.DefaultValue.HasValue)
                    prop["description"] += $"，默认值: {param.DefaultValue.Value}";

                properties[param.Name] = prop;
                if (param.IsRequired && !param.DefaultValue.HasValue)
                    required.Add(param.Name);
            }

            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required
            };

            string schemaJson = JsonSerializer.Serialize(schema, JsonOpts);

            return new ToolDefinition
            {
                Name = $"custom_model_{model.Name}",
                Description = $"[自定义模型] {model.DisplayName}：{model.Description}\n公式: {model.Formula}",
                Parameters = JsonDocument.Parse(schemaJson).RootElement.Clone()
            };
        }

        // ===== 持久化 =====

        private void PersistModel(CustomModel model)
        {
            try
            {
                var filePath = Path.Combine(_modelsDir, $"{model.Name}.json");
                var json = JsonSerializer.Serialize(model, JsonOpts);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomModelStore] 保存模型失败: {ex.Message}");
            }
        }

        private void LoadAllModels()
        {
            try
            {
                foreach (var file in Directory.GetFiles(_modelsDir, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var model = JsonSerializer.Deserialize<CustomModel>(json, JsonOpts);
                        if (model != null && !string.IsNullOrWhiteSpace(model.Name))
                        {
                            _models[model.Name] = model;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CustomModelStore] 加载模型文件失败 {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomModelStore] 扫描模型目录失败: {ex.Message}");
            }
        }

        // ===== 辅助方法 =====

        private static string SanitizeName(string name)
        {
            // 只保留字母数字下划线，转小写
            return new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray()).ToLowerInvariant();
        }

        private static string JsonSuccess(object data)
        {
            return JsonSerializer.Serialize(new { status = "success", data }, JsonOpts);
        }

        private static string JsonError(string message)
        {
            return JsonSerializer.Serialize(new { status = "error", message }, JsonOpts);
        }
    }
}
