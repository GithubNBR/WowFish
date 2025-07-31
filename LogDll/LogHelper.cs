namespace LogDll
{
    public class LogHelper
    {
        public static void Info(string message)
        {
            // 这里可以实现日志记录的逻辑，比如写入文件或输出到控制台
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
        }
    }
}
