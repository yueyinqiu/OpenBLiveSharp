using Newtonsoft.Json;
using OpenBLive.Client;
using OpenBLive.Client.Data;
using OpenBLive.Runtime;
using OpenBLive.Runtime.Data;
using OpenBLive.Runtime.Utilities;
using System.Text;

namespace OpenBLiveSample
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            //是否为测试环境
            BApi.isTestEnv = false;

            Console.WriteLine("输入accessKeySecret");
            var keySecInput = Console.ReadLine();
            if (!string.IsNullOrEmpty(keySecInput))
            {
                SignUtility.accessKeySecret = keySecInput;
            }
            Console.WriteLine("输入accessKeyId");
            var keyIdInput = Console.ReadLine();
            if (!string.IsNullOrEmpty(keyIdInput))
            {
                SignUtility.accessKeyId = keyIdInput;
            }


            Console.Clear();
            Console.WriteLine("请输入数字，对应相应功能:");
            Console.WriteLine("0 - 长链接测试");
            Console.WriteLine("1 - 互动游戏测试");
            var feat = Console.ReadLine();
        cwAppId:
            Console.WriteLine("请输入appId");
            var appId = Console.ReadLine();
            if (string.IsNullOrEmpty(appId))
            {
                goto cwAppId;
            }
        cwCode:
            Console.WriteLine("请输入主播身份码Code");
            var code = Console.ReadLine();
            if (string.IsNullOrEmpty(code))
            {
                goto cwCode;
            }


            IBApiClient bApiClient = new BApiClient();
            var startInfo = new AppStartInfo();

            switch (feat)
            {
                case "0":
                    WebSocketBLiveClient m_WebSocketBLiveClient;
                    //获取房间信息
                    startInfo = await bApiClient.StartInteractivePlay(code, appId);
                    if (startInfo?.Code != 0)
                    {
                        Console.WriteLine(startInfo?.Message);
                        return;
                    }


                    m_WebSocketBLiveClient = new WebSocketBLiveClient(startInfo.GetWssLink(), startInfo.GetAuthBody());
                    m_WebSocketBLiveClient.OnDanmaku += WebSocketBLiveClientOnDanmaku;
                    m_WebSocketBLiveClient.OnGift += WebSocketBLiveClientOnGift;
                    m_WebSocketBLiveClient.OnGuardBuy += WebSocketBLiveClientOnGuardBuy;
                    m_WebSocketBLiveClient.OnSuperChat += WebSocketBLiveClientOnSuperChat;
                    //m_WebSocketBLiveClient.Connect();
                    m_WebSocketBLiveClient.Connect(TimeSpan.FromSeconds(30));

                    Console.ReadLine();
                    break;
                case "1":
                    Console.WriteLine("请输入自动关闭时间,不输入默认10秒");
                    var closeTimeStr = Console.ReadLine();
                    if (string.IsNullOrEmpty(closeTimeStr))
                    {
                        closeTimeStr = "10";
                    }

                    if (!string.IsNullOrEmpty(appId))
                    {
                        startInfo = await bApiClient.StartInteractivePlay(code, appId);
                        if (startInfo?.Code != 0)
                        {
                            Console.WriteLine(startInfo?.Message);
                            return;
                        }

                        var gameId = startInfo?.Data?.GameInfo?.GameId;
                        if (gameId != null)
                        {
                            Console.WriteLine("成功开启，开始心跳，游戏ID: " + gameId);
                            InteractivePlayHeartBeat m_PlayHeartBeat = new InteractivePlayHeartBeat(gameId);
                            m_PlayHeartBeat.HeartBeatError += M_PlayHeartBeat_HeartBeatError;
                            m_PlayHeartBeat.HeartBeatSucceed += M_PlayHeartBeat_HeartBeatSucceed;
                            m_PlayHeartBeat.Start();
                        }
                        else
                        {
                            Console.WriteLine("开启游戏错误: " + startInfo.ToString());
                        }
                        await Task.Run(async () =>
                        {
                            var closeTime = int.Parse(closeTimeStr);
                            await Task.Delay(closeTime * 1000);
                            var ret = await bApiClient.EndInteractivePlay(appId, gameId);
                            Console.WriteLine("关闭游戏: " + ret.ToString());
                            return;
                        });
                    }
                    break;
            }
            while (true)
            {
                Console.ReadKey(true);
            }
        }



        private static void M_PlayHeartBeat_HeartBeatSucceed()
        {
            Logger.Log("心跳成功");
        }

        private static void M_PlayHeartBeat_HeartBeatError(string json)
        {
            JsonConvert.DeserializeObject<EmptyInfo>(json);
            Logger.Log("心跳失败" + json);
        }

        private static void WebSocketBLiveClientOnSuperChat(SuperChat superChat)
        {
            StringBuilder sb = new StringBuilder("收到SC!");
            sb.AppendLine();
            sb.Append("来自用户：");
            sb.AppendLine(superChat.userName);
            sb.Append("留言内容：");
            sb.AppendLine(superChat.message);
            sb.Append("金额：");
            sb.Append(superChat.rmb);
            sb.Append("元");
            Logger.Log(sb.ToString());
        }

        private static void WebSocketBLiveClientOnGuardBuy(Guard guard)
        {
            StringBuilder sb = new StringBuilder("收到大航海!");
            sb.AppendLine();
            sb.Append("来自用户：");
            sb.AppendLine(guard.userInfo.userName);
            sb.Append("赠送了");
            sb.Append(guard.guardUnit);
            Logger.Log(sb.ToString());
        }

        private static void WebSocketBLiveClientOnGift(SendGift sendGift)
        {
            StringBuilder sb = new StringBuilder("收到礼物!");
            sb.AppendLine();
            sb.Append("来自用户：");
            sb.AppendLine(sendGift.userName);
            sb.Append("赠送了");
            sb.Append(sendGift.giftNum);
            sb.Append("个");
            sb.Append(sendGift.giftName);
            Logger.Log(sb.ToString());
        }

        private static void WebSocketBLiveClientOnDanmaku(Dm dm)
        {
            StringBuilder sb = new StringBuilder("收到弹幕!");
            sb.AppendLine();
            sb.Append("用户：");
            sb.AppendLine(dm.userName);
            sb.Append("弹幕内容：");
            sb.Append(dm.msg);
            Logger.Log(sb.ToString());
        }
    }
}