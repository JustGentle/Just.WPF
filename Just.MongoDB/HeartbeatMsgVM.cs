using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GenLibrary.MVVM.Base;
using Just.Base;
using MongoDB.Driver;
using PropertyChanged;

namespace Just.MongoDB
{
    [AddINotifyPropertyChangedInterface]
    public class HeartbeatMsgVM
    {
        public bool Doing { get; set; } = false;
        public string MongoDBAddress { get; set; } = "127.0.0.1:27017";
        public int CheckSpan { get; set; } = 1;
        public int TimeOut { get; set; } = 10;
        public int StopTimes { get; set; } = 3;
        public bool IsGet { get; set; } = true;
        public string Urls { get; set; }
        public string Mobiles { get; set; }

        #region Do
        private MongoDBHelper mongo = null;
        private HttpClient client = null;
        private ICommand _check;
        public ICommand Check
        {
            get
            {
                _check = _check ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    if (Doing)
                    {
                        Doing = false;
                        MainWindowVM.NotifyInfo("停止！");
                    }
                    else
                    {
                        CheckSpan = Math.Max(CheckSpan, 1);
                        TimeOut = Math.Max(TimeOut, 1);
                        if (string.IsNullOrWhiteSpace(Urls))
                        {
                            MainWindowVM.NotifyWarn("请先配置监测地址！");
                            return;
                        }
                        var urlList = Urls.Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var mbList = (Mobiles ?? string.Empty).Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        Doing = true;
                        try
                        {
                            var timeoutMS = 3000;
                            mongo = new MongoDBHelper(
                                $"mongodb://{MongoDBAddress}/?serverSelectionTimeoutMS={timeoutMS};connectTimeoutMS={timeoutMS};socketTimeoutMS={timeoutMS}",
                                "iOfficeCommunication",
                                nameof(ShortMessageSend));
                        }
                        catch (TimeoutException ex)
                        {
                            Doing = false;
                            Logger.Error("MongoDB连接超时", ex);
                            MainWindowVM.NotifyError("连接超时！", "执行错误");
                        }
                        catch (MongoConfigurationException ex)
                        {
                            Doing = false;
                            Logger.Error("MongoDB链接地址错误", ex);
                            MainWindowVM.NotifyError("请检查【链接地址】是否正确！\n" + ex.Message, "同步错误");
                        }
                        catch (Exception ex)
                        {
                            Doing = false;
                            Logger.Error("站点监测错误", ex);
                            MainWindowVM.NotifyError(ex.Message, "执行错误");
                        }
                        if (!Doing)
                            return;
                        var timeoutReq = TimeSpan.FromSeconds(TimeOut);
                        var span = TimeSpan.FromMinutes(CheckSpan);
                        var counter = 0;
                        var times = 0;
                        Task.Run(() =>
                        {
                            Logger.Info($"监测开始:{Task.CurrentId}");
                            while (Doing)
                            {
                                if(counter <= 0)
                                {
                                    counter = (int)span.TotalSeconds;
                                    foreach (var url in urlList)
                                    {
                                        try
                                        {
                                            using (client = new HttpClient { BaseAddress = new Uri(url), Timeout = timeoutReq })
                                            {
                                                if (IsGet)
                                                {
                                                    using (var rsp = client.GetAsync(url).Result)
                                                    {
                                                        if (rsp.IsSuccessStatusCode)
                                                            Logger.Info($"{rsp.StatusCode}={url}");
                                                        else
                                                        {
                                                            times++;
                                                            Logger.Warn($"{rsp.StatusCode}={url}");
                                                            MainWindowVM.NotifyWarn($"{rsp.StatusCode}={url}");
                                                            if (mbList.Any())
                                                                mongo.InsertMany(mbList.Select(mobile => new ShortMessageSend
                                                                {
                                                                    SendToMobile = mobile,
                                                                    Content = $"站点状态异常：{url}"
                                                                }));
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    using (var rsp = client.PostAsync(url, new FormUrlEncodedContent(Enumerable.Empty<KeyValuePair<string, string>>())).Result)
                                                    {
                                                        if (rsp.IsSuccessStatusCode)
                                                            Logger.Info($"{rsp.StatusCode}={url}");
                                                        else
                                                        {
                                                            times++;
                                                            Logger.Warn($"{rsp.StatusCode}={url}");
                                                            MainWindowVM.NotifyWarn($"{rsp.StatusCode}={url}");
                                                            if (mbList.Any())
                                                                mongo.InsertMany(mbList.Select(mobile => new ShortMessageSend
                                                                {
                                                                    SendToMobile = mobile,
                                                                    Content = $"站点状态异常：{url}"
                                                                }));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            times++;
                                            Logger.Error($"站点监测失败：{url}", ex);
                                            MainWindowVM.NotifyError(ex.Message + url, "站点监测失败");
                                            if (mbList.Any())
                                                mongo.InsertMany(mbList.Select(mobile => new ShortMessageSend
                                                {
                                                    SendToMobile = mobile,
                                                    Content = $"站点监测失败：{url}"
                                                }));
                                        }
                                    }
                                }
                                if(times >= StopTimes)
                                {
                                    Doing = false;
                                    MainWindowVM.ShowStatus();
                                    Logger.Info($"监测结束:{Task.CurrentId}");
                                    return;
                                }
                                Task.Delay(2000).Wait();
                                counter -= 2;
                                MainWindowVM.ShowStatus($"下次检测：{counter}秒");
                            }
                            MainWindowVM.ShowStatus();
                            Logger.Info($"监测结束:{Task.CurrentId}");
                        });
                    }
                });
                return _check;
            }
        }
        #endregion

        #region Setting
        public void ReadSettings(string[] args)
        {
            CheckSpan = MainWindowVM.ReadSetting($"{nameof(HeartbeatMsgCtrl)}.{nameof(CheckSpan)}", CheckSpan);
            TimeOut = MainWindowVM.ReadSetting($"{nameof(HeartbeatMsgCtrl)}.{nameof(TimeOut)}", TimeOut);
            IsGet = MainWindowVM.ReadSetting($"{nameof(HeartbeatMsgCtrl)}.{nameof(IsGet)}", IsGet);
            Urls = MainWindowVM.ReadSetting($"{nameof(HeartbeatMsgCtrl)}.{nameof(Urls)}", Urls);
            Mobiles = MainWindowVM.ReadSetting($"{nameof(HeartbeatMsgCtrl)}.{nameof(Mobiles)}", Mobiles);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(HeartbeatMsgCtrl)}.{nameof(CheckSpan)}", CheckSpan);
            MainWindowVM.WriteSetting($"{nameof(HeartbeatMsgCtrl)}.{nameof(TimeOut)}", TimeOut);
            MainWindowVM.WriteSetting($"{nameof(HeartbeatMsgCtrl)}.{nameof(IsGet)}", IsGet);
            MainWindowVM.WriteSetting($"{nameof(HeartbeatMsgCtrl)}.{nameof(Urls)}", Urls);
            MainWindowVM.WriteSetting($"{nameof(HeartbeatMsgCtrl)}.{nameof(Mobiles)}", Mobiles);
        }
        #endregion
    }
}
