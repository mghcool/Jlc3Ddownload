using System.IO;
using System.Net.Http;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace Jlc3Ddownload
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();

        string _downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public MainWindow()
        {
            InitializeComponent();
            PathLabel.Content = _downloadPath;
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadButton.IsEnabled = false;
            try
            {
                string code = CodeTextBox.Text.Trim();
                if(string.IsNullOrEmpty(code)) throw new Exception("请输入元件代码！");

                string filename = $"{code}.step";
                string filepath = Path.Combine(_downloadPath, filename);

                Log($"开始下载模型 {code} ...");

                string hasUrl = "https://pro.lceda.cn/api/eda/product/search";
                var hasFormdata = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("keyword", code),
                    new KeyValuePair<string, string>("needAggs", "true"),
                    new KeyValuePair<string, string>("url", "/api/eda/product/list"),
                    new KeyValuePair<string, string>("currPage", "1"),
                    new KeyValuePair<string, string>("pageSize", "10")
                });

                var r0 = await _httpClient.PostAsync(hasUrl, hasFormdata);
                var jsonData = await r0.Content.ReadAsStringAsync();
                var data = JObject.Parse(jsonData);
                var productList = data["result"]?["productList"];

                if (productList == null || !productList.HasValues) throw new Exception("获取不到产品列表");

                var hasDevice = productList[0]?["hasDevice"]?.ToString();

                if(string.IsNullOrEmpty(hasDevice)) throw new Exception("hasDevice为空");

                Log($"uuid : {hasDevice}");

                var url = "https://pro.lceda.cn/api/devices/searchByIds";
                var formdata = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("uuids[]", hasDevice),
                    new KeyValuePair<string, string>("path", filepath)
                });

                var r1 = await _httpClient.PostAsync(url, formdata);
                var json1Data = await r1.Content.ReadAsStringAsync();
                var data1 = JObject.Parse(json1Data);
                var modelId = data1["result"]?[0]?["attributes"]?["3D Model"]?.ToString();

                if (string.IsNullOrEmpty(modelId)) throw new Exception("未找到模型ID");

                Log($"modelId : {modelId}");

                var url2 = "https://pro.lceda.cn/api/components/searchByIds?forceOnline=1";
                var formdata2 = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("uuids[]", modelId),
                    new KeyValuePair<string, string>("dataStr", "yes"),
                    new KeyValuePair<string, string>("path", filepath)
                });

                var r2 = await _httpClient.PostAsync(url2, formdata2);
                var json2Data = await r2.Content.ReadAsStringAsync();
                var data2 = JObject.Parse(json2Data);
                var modelDataStr = data2["result"]?[0]?["dataStr"]?.ToString();
                var modelData = JObject.Parse(modelDataStr ?? "")["model"];

                Log($"data : {modelData}");

                var modelUrl = $"https://modules.lceda.cn/qAxj6KHrDKw4blvCG8QJPs7Y/{modelData}";
                var r3 = await _httpClient.GetAsync(modelUrl);
                var modelContent = await r3.Content.ReadAsStringAsync();
                await File.WriteAllTextAsync(filepath, modelContent);

                Log($"{code} 模型下载成功！");
            }
            catch (Exception ex)
            {
                Log($"错误：{ex.Message}");
            }
            finally
            {
                DownloadButton.IsEnabled = true;
            }
        }

        private void Log(string message)
        {
            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            LogTextBox.ScrollToEnd();
        }
    }
}