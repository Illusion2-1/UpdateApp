namespace UpdateServer;

//Uptime Kuma被动探针模式
public class Heartbeats {
    private string _url;
    private static readonly HttpClient Client = new();

    public Heartbeats(string url) {
        _url = url;
    }

    public async Task StartCheckingAsync(CancellationToken cancellationToken) {
        Client.Timeout = TimeSpan.FromMinutes(2);
        while (!cancellationToken.IsCancellationRequested) {
            try {
                // 发送GET请求
                var response = await Client.GetAsync(_url, cancellationToken);

                // 检查状态码是否为200
                if (!response.IsSuccessStatusCode)
                    Console.WriteLine("Query failed with status code: " + response.StatusCode);
            } catch (TaskCanceledException ex) {
                Console.WriteLine("Query was canceled.");
                Console.WriteLine(ex);
            } catch (Exception ex) {
                // 打印异常信息
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            // 等待60秒
            await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
        }
    }
}