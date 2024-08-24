using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ConsoleApp1
{
    public class DirInfoInCombobox
    {
        public DirectoryInfo _directoryInfo;
        public DirInfoInCombobox(DirectoryInfo directoryInfo)
        {
            this._directoryInfo = directoryInfo;
        }
        public override string ToString()
        {
            return _directoryInfo.Name;
        }
    }
    public class CharcterSynchoSettings :INotifyPropertyChanged
    {
        public DirectoryInfo _directoryInfo;
        public DirectoryInfo _serverInfo;

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool _enableSyncho = false;
        public bool enableSyncho { get 
            { 
                return _enableSyncho; 
            } 
            set 
            { 
                _enableSyncho = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("enableSyncho"));
            } 
        }
        public string GetString
        {
            get {
                return ToString();
            }
        }
        public CharcterSynchoSettings(DirectoryInfo directoryInfo, DirectoryInfo serverInfo)
        {
            this._directoryInfo = directoryInfo;
            _serverInfo = serverInfo;
            
        }
        public override string ToString()
        {
            return _directoryInfo.Name + " - " + _serverInfo.Name;
        }
    }
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window
    {
        DirectoryInfo? wowdirinfo;
        DirectoryInfo? wtfdirinfo;
        ObservableCollection<CharcterSynchoSettings> _charctersyncholist = new ObservableCollection<CharcterSynchoSettings>();
        public Window1()
        {
            InitializeComponent();
            btnBrowse.Click += BtnBrowse_Click;
            cbAccount.SelectionChanged += CbAccount_SelectionChanged;
            cbServer.SelectionChanged += CbServer_SelectionChanged;
            charcterList.ItemsSource = _charctersyncholist;
            cbCheckAll.Checked += CbCheckAll_Checked;
            cbCheckAll.Unchecked += CbCheckAll_Unchecked;
            btnStart.Click += BtnStart_Click;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var confirmresult = System.Windows.MessageBox.Show("被选中角色配置将被替换，确认？","",MessageBoxButton.YesNo);
            if(confirmresult == MessageBoxResult.Yes)
            {
                bool baddontxt = cbAddontxt.IsChecked == true;
                bool bsavedvariables = cbSavedVariables.IsChecked == true;
                SynchoConfigs(baddontxt, bsavedvariables);
            }
        }

        private void CbCheckAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var charcter in _charctersyncholist)
            {
                charcter.enableSyncho = false;
            }
            charcterList.InvalidateVisual();
        }

        private void CbCheckAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var charcter in _charctersyncholist)
            {
                charcter.enableSyncho = true;
            }
            charcterList.InvalidateVisual();
        }

        private void ReadDirInfo(DirectoryInfo directoryInfo,System.Windows.Controls.ComboBox comboBox)
        {
            var subdirs = directoryInfo.EnumerateDirectories();
            foreach (DirectoryInfo dir in subdirs)
            {
                if (dir.Name != "SavedVariables")
                {
                    comboBox.Items.Add(new DirInfoInCombobox(dir));
                }
            }
        }
        private void CbServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DirectoryInfo selectiondir = (cbServer.SelectedItem as DirInfoInCombobox)._directoryInfo;
            ReadDirInfo(selectiondir, cbCharcter);
        }

        private void CbAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DirectoryInfo selectiondir = (cbAccount.SelectedItem as DirInfoInCombobox)._directoryInfo;
            ReadDirInfo(selectiondir, cbServer);
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileDialog dlg = new OpenFileDialog();
            dlg.Filter = "wow.exe|wow.exe";
            var result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK &&  dlg.CheckFileExists)
            {
                //选中wow.exe
                FileInfo wowexeinfo = new FileInfo(dlg.FileName);
                if (wowexeinfo != null && wowexeinfo.Exists)
                {
                    wowdirinfo = wowexeinfo.Directory;
                    wtfdirinfo = wowdirinfo.GetDirectories().First(dir=>dir.Name == "WTF");
                    LoadServerAndCharcters();
                }
            }
        }
        private void LoadServerAndCharcters()
        {
            DirectoryInfo accountdirinfo = wtfdirinfo.GetDirectories().First(dir => dir.Name == "Account");
            var accountDirs = accountdirinfo.GetDirectories();
            foreach (DirectoryInfo dir in accountDirs)
            {
                if(dir.Name != "SavedVariables")
                {
                    cbAccount.Items.Add(new DirInfoInCombobox(dir));
                    //获取服务器列表
                    var serverDirs = dir.GetDirectories();
                    foreach (var serverdir in serverDirs)
                    {
                        if(serverdir.Name != "SavedVariables")
                        {
                            var charcterDirs = serverdir.GetDirectories();
                            foreach(var charcterdir in charcterDirs)
                            {
                                CharcterSynchoSettings dirInfoInListview = new CharcterSynchoSettings(charcterdir, serverdir);
                                _charctersyncholist.Add(dirInfoInListview);
                            }
                        }
                    }
                }
            }
        }
    
        private async void SynchoConfigs(bool bSynchoAddontxt,bool bSynchoSavedvariables)
        {
            var selectedCharcterItem = cbCharcter.SelectedItem as DirInfoInCombobox;
            DirectoryInfo selectiondir = selectedCharcterItem?._directoryInfo;
            if(selectiondir != null)
            {
                FileInfo addonstxtfile = selectiondir.GetFiles().First(fileinfo=>fileinfo.Name.Equals("AddOns.txt",StringComparison.CurrentCultureIgnoreCase));
                DirectoryInfo savedvariablesdir = selectiondir.GetDirectories().First(dirinfo => dirinfo.Name.Equals("SavedVariables", StringComparison.CurrentCultureIgnoreCase));
                foreach(var charcterSynchosetting in _charctersyncholist)
                {
                    if (charcterSynchosetting._directoryInfo.FullName == selectiondir.FullName)
                    {
                        continue;
                    }
                    await Task.Run(new Action(
                        () => {
                            if (charcterSynchosetting.enableSyncho)
                            {
                                if (bSynchoAddontxt)
                                {
                                    string destpath = charcterSynchosetting._directoryInfo.FullName + "\\Addons.txt";
                                    addonstxtfile.CopyTo(destpath, true);
                                    Dispatcher.BeginInvoke(new Action(() => {
                                        rtbInfo.AppendText("已复制" + destpath + "\n");
                                    }));
                                }
                                if (bSynchoSavedvariables)
                                {
                                    DirectoryInfo destdirinfo = charcterSynchosetting._directoryInfo.CreateSubdirectory("SavedVariables");
                                    foreach (var file in savedvariablesdir.GetFiles())
                                    {
                                        string destpath = destdirinfo.FullName + "\\" + file.Name;
                                        file.CopyTo(destpath, true);
                                        Dispatcher.BeginInvoke(new Action(() => {
                                            rtbInfo.AppendText("已复制" + destpath + "\n");
                                        }));
                                    }
                                }
                            }

                        }
                        ));
                    rtbInfo.ScrollToEnd();
                    //if(charcterSynchosetting.enableSyncho)
                    //{
                    //    if(bSynchoAddontxt)
                    //    {
                    //        string destpath = charcterSynchosetting._directoryInfo.FullName + "\\Addons.txt";
                    //        addonstxtfile.CopyTo(destpath, true);
                    //        rtbInfo.AppendText("已复制" + destpath + "\n");
                    //    }
                    //    if(bSynchoSavedvariables)
                    //    {
                    //        DirectoryInfo destdirinfo = charcterSynchosetting._directoryInfo.CreateSubdirectory("SavedVariables");
                    //        foreach (var file in savedvariablesdir.GetFiles())
                    //        {
                    //            string destpath = destdirinfo.FullName + "\\"+file.Name;
                    //            file.CopyTo(destpath, true);
                    //            rtbInfo.AppendText("已复制" + destpath + "\n");
                    //        }
                    //    }
                    //}
                }

            }
        }

    }
}
