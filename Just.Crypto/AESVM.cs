using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GenLibrary.MVVM.Base;
using Just.Base;
using Just.Base.Crypto;
using PropertyChanged;

namespace Just.Crypto
{
    [AddINotifyPropertyChangedInterface]
    public class AESVM
    {
        public string Key { get; set; } = string.Empty;
        public string IV { get; set; } = string.Empty;
        public string InputText { get; set; }
        public string OutputText { get; set; }


        private ICommand _Encrypt;
        public ICommand Encrypt
        {
            get
            {
                _Encrypt = _Encrypt ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    Task.Run(() =>
                    {
                        //目录
                        if (string.IsNullOrEmpty(InputText))
                        {
                            MainWindowVM.NotifyWarn("请在上框输入要加密的内容");
                            return;
                        }

                        try
                        {
                            OutputText = AES.Encrypt(InputText, Key, IV);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("加密错误", ex);
                            MainWindowVM.NotifyWarn("加密失败！");
                        }
                    });
                });
                return _Encrypt;
            }
        }

        private ICommand _Decrypt;
        public ICommand Decrypt
        {
            get
            {
                _Decrypt = _Decrypt ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    Task.Run(() =>
                    {
                        //目录
                        if (string.IsNullOrEmpty(InputText))
                        {
                            MainWindowVM.NotifyWarn("请在上框输入要解密的内容");
                            return;
                        }

                        try
                        {
                            OutputText = AES.Decrypt(InputText, Key, IV);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("解密错误", ex);
                            MainWindowVM.NotifyWarn("解密失败！");
                        }
                    });
                });
                return _Decrypt;
            }
        }

        #region Setting
        public void ReadSettings()
        {
            Key = MainWindowVM.ReadSetting($"{nameof(AESCtrl)}.{nameof(Key)}", Key);
            IV = MainWindowVM.ReadSetting($"{nameof(AESCtrl)}.{nameof(IV)}", IV);
        }
        public void WriteSettings()
        {
            MainWindowVM.WriteSetting($"{nameof(AESCtrl)}.{nameof(Key)}", Key);
            MainWindowVM.WriteSetting($"{nameof(AESCtrl)}.{nameof(IV)}", IV);
        }
        #endregion
    }
}
