using Functional.Logger;
using KWeb.BaseServices;
using KWeb.DependencyInjection;
using KWeb.HttpOption;

namespace WebServer.Controllers;

[Route("api/test")]
public class TestController(IConfiguration config,IKLogger<TestController> logger) : ControllerBase
{
    [ServiceInjection]
    public readonly string a;

    [HttpGet("A")]
    public string GetA()
    {
        logger.Debug("收到Get请求！");
        return $"随机数{a},配置信息之服务器链接定义：{string.Join(",", config.Get<string[]>("Server:Urls"))}";
    }
}