namespace AlloyAct_Pro.DFT
{
    /// <summary>
    /// DFT 输出文件解析器接口
    /// 每个 DFT 软件实现一个解析器
    /// </summary>
    public interface IDftParser
    {
        /// <summary>支持的 DFT 软件名称</summary>
        string SoftwareName { get; }

        /// <summary>
        /// 常见文件扩展名或文件名模式（用于文件对话框过滤）
        /// 如 "OUTCAR", "*.xml", "*.castep"
        /// </summary>
        string[] FilePatterns { get; }

        /// <summary>
        /// 用于识别文件类型的签名字符串
        /// 在文件前 50 行中搜索这些字符串
        /// </summary>
        string[] SignatureStrings { get; }

        /// <summary>
        /// 快速检查文件是否可由此解析器处理
        /// 读取文件前若干行，检查签名字符串
        /// </summary>
        bool CanParse(string filePath);

        /// <summary>
        /// 解析 DFT 输出文件，提取热力学数据
        /// </summary>
        DftResult Parse(string filePath);
    }
}
