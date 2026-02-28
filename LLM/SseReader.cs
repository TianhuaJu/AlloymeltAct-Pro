using System.Runtime.CompilerServices;
using System.Text;

namespace AlloyAct_Pro.LLM
{
    /// <summary>
    /// SSE (Server-Sent Events) 行读取器
    /// 从 HTTP 响应流中逐行读取并解析 event: / data: 格式
    /// </summary>
    internal static class SseReader
    {
        /// <summary>
        /// 从 HttpResponseMessage 流中读取 SSE 事件
        /// </summary>
        public static async IAsyncEnumerable<SseLine> ReadAsync(
            HttpResponseMessage response,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string currentEvent = "";
            while (true)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync().WaitAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    // 不吞掉取消异常，让它正确传播给调用方
                    throw;
                }

                if (line == null) break; // Stream ended

                if (line.StartsWith("event:"))
                {
                    currentEvent = line.Substring(6).Trim();
                    continue;
                }

                if (line.StartsWith("data:"))
                {
                    var data = line.Substring(5).Trim();
                    yield return new SseLine { Event = currentEvent, Data = data };
                    currentEvent = "";
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    currentEvent = "";
                }
            }
        }
    }

    internal struct SseLine
    {
        public string Event;
        public string Data;
    }
}
