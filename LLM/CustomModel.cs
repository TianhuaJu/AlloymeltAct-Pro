using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlloyAct_Pro.LLM
{
    /// <summary>
    /// 自定义计算模型 - 由用户通过 AI 对话创建并持久化
    /// 模型基于数学公式，支持变量参数和常量
    /// </summary>
    public class CustomModel
    {
        /// <summary>唯一标识符</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

        /// <summary>模型名称（英文，用作工具名，如 solubility_product）</summary>
        public string Name { get; set; } = "";

        /// <summary>显示名称（中文，如 溶度积计算）</summary>
        public string DisplayName { get; set; } = "";

        /// <summary>模型描述（用于 LLM 工具描述）</summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 数学公式表达式
        /// 支持：+, -, *, /, ^, ()
        /// 函数：ln, log, log10, exp, sqrt, pow, abs, sin, cos, tan
        /// 常数：R=8.314, pi, e, kB
        /// 示例："A + B/T", "exp(-DeltaG/(R*T))", "log10(K) = A + B/T + C*ln(T)"
        /// </summary>
        public string Formula { get; set; } = "";

        /// <summary>模型参数列表</summary>
        public List<ModelParameter> Parameters { get; set; } = new();

        /// <summary>结果单位（如 "K", "kJ/mol", "dimensionless"）</summary>
        public string ResultUnit { get; set; } = "";

        /// <summary>结果名称（如 "溶度积 K", "温度 T"）</summary>
        public string ResultName { get; set; } = "";

        /// <summary>模型分类</summary>
        public string Category { get; set; } = "custom";

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 模型参数定义
    /// </summary>
    public class ModelParameter
    {
        /// <summary>参数名称（在公式中使用的变量名，如 T, A, B）</summary>
        public string Name { get; set; } = "";

        /// <summary>参数描述（如 "温度(K)", "系数A"）</summary>
        public string Description { get; set; } = "";

        /// <summary>默认值（可选，null 表示必填）</summary>
        public double? DefaultValue { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = "";

        /// <summary>是否必填</summary>
        public bool IsRequired { get; set; } = true;
    }
}
