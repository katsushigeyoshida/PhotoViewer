using CoreLib;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PhotoViewer
{
    /// <summary>
    /// ディレクトリリリークラス
    /// </summary>
    public class DirectoryTree : TreeViewItem
    {
        public DirectoryInfo mDirectory { get; set; }       //  ディレクトリ情報
        public bool mIsAdd { get; set; }                    //  取得済み
        public TreeViewItem dummy;                          //  子のディレトリ情報

        private YLib ylib = new();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">フルパス</param>
        /// <param name="iconPath">アイコンのパス</param>
        public DirectoryTree(string path = null, string iconPath = "")
        {
            if (path == null || path.Length == 0) {
                mDirectory = null;
                Header = CreateHeader("PC", "Icon\\Computer.ico");
                getDrive();
                this.IsExpanded = true;
            } else {
                mDirectory = new DirectoryInfo(path);
                Header = CreateHeader(mDirectory.Name, iconPath);
                dummy = new TreeViewItem();
                Items.Add(dummy);
            }

            //  展開処理
            this.Expanded += (s, e) => {
                if (mIsAdd) return;
                getDirectory();
            };

            //  折りたたみ処理
            this.Collapsed += (s, e) => {
                System.Diagnostics.Debug.WriteLine($"Collapsed: {HeaderToString((StackPanel)Header)}");
                if (mDirectory != null)
                    updateDirectory();
                else
                    updateDrive();
            };
        }

        /// <summary>
        /// Headerの作成(StackPanel(Text,Icon))
        /// </summary>
        /// <param name="name">テキスト</param>
        /// <param name="iconPath">アイコン</param>
        /// <returns></returns>
        private StackPanel CreateHeader(string name, string iconPath)
        {
            StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Image() {
                Source = new BitmapImage(new Uri(iconPath, UriKind.Relative)),
                Width = 15,
                Height = 18,
            });
            sp.Children.Add(new TextBlock() { Text = name });
            return sp;
        }

        /// <summary>
        /// ドライブリストの作成してツリーに登録
        /// </summary>
        private void getDrive()
        {
            Items.Remove(dummy);
            List<DirectoryInfo> drives = ylib.getDrivesInfo();
            foreach (DirectoryInfo drive in drives) {
                Items.Add(new DirectoryTree(drive.FullName, "Icon\\HardDisk.ico"));
            }
            mIsAdd = true;
        }

        /// <summary>
        /// ディレトリリストの取得してツリーに登録
        /// </summary>
        private void getDirectory()
        {
            Items.Remove(dummy);
            List<DirectoryInfo> directories = ylib.getDirectoriesInfo(mDirectory.FullName);
            foreach (DirectoryInfo directory in directories) {
                if ((directory.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                    Items.Add(new DirectoryTree(directory.FullName, "Icon\\FolderClose.ico"));
            }
            mIsAdd = true;
        }

        /// <summary>
        /// 折りたたみ時にドライブ状態を更新する
        /// </summary>
        private void updateDrive()
        {
            if (mDirectory ==null) {
                List<DirectoryInfo> drives = ylib.getDrivesInfo();
                if (drives.Count == Items.Count) {
                    foreach (var drive in drives) {
                        if (indexOfItemName(drive.FullName) < 0) {
                            removeItemsAll();
                            foreach (DirectoryInfo dr in drives)
                                Items.Add(new DirectoryTree(dr.FullName, "Icon\\HardDisk.ico"));
                            return;
                        }
                    }
                } else {
                    removeItemsAll();
                    foreach (DirectoryInfo dr in drives)
                        Items.Add(new DirectoryTree(dr.FullName, "Icon\\HardDisk.ico"));
                    return;
                }
            }
        }

        /// <summary>
        /// 折りたたみ時にディレクトリ状態を更新する
        /// </summary>
        private void updateDirectory()
        {
            if (mDirectory == null)
                return;
            if (Items.Contains(dummy))
                Items.Remove(dummy);
            List<DirectoryInfo> directories = ylib.getDirectoriesInfo(mDirectory.FullName);
            if (directories == null) {
                removeItemsAll();
                return;
            }
            //  削除されたサブディレトリの除外
            if (Items != null && 0 < Items.Count) {
                for (int i = Items.Count - 1; 0 <= i; i--) {
                    System.Diagnostics.Debug.WriteLine($"updateDirectory: {i} {Items[i].ToString()}");
                    DirectoryTree dt = (DirectoryTree)Items[i];
                    if (directories.Count == 0 || directories.Find(x => x.Name == dt.mDirectory.Name) == null) {
                        System.Diagnostics.Debug.WriteLine($"not directory: {i} [{dt.mDirectory.Name}]");
                        Items.RemoveAt(i);
                    } else {
                        dt.checkedSubDirectory();
                    }
                }
            }
            //  増えたディレクトリの追加
            foreach (var di in directories) {
                if ((di.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0) {
                    if (indexOfItemName(di.Name) < 0) {
                        System.Diagnostics.Debug.WriteLine($"not exist: [{di.Name}]");
                        Items.Add(new DirectoryTree(di.FullName, "Icon\\FolderClose.ico"));
                    }
                }
            }
        }

        /// <summary>
        /// すべてのItemsを削除する
        /// </summary>
        private void removeItemsAll()
        {
            for (int i = Items.Count - 1; 0 <= i; i--)
                Items.RemoveAt(i);
        }

        /// <summary>
        /// サブディレトリの有無をチェックする
        /// サブディレクトリがある時はdummyを登録
        /// </summary>
        private void checkedSubDirectory()
        {
            if (Items.Count == 0) {
                List<DirectoryInfo> directories = ylib.getDirectoriesInfo(mDirectory.FullName);
                if (0 < directories.Count) {
                    dummy = new TreeViewItem();
                    Items.Add(dummy);
                }

            }
        }

        /// <summary>
        /// ディレクトリ名の位置を検索(ない時は-1を返す)
        /// </summary>
        /// <param name="name">検索名</param>
        /// <returns>検索位置</returns>
        private int indexOfItemName(string name)
        {
            for (int i = 0; i < Items.Count; i++) {
                DirectoryTree dt = (DirectoryTree)Items[i];
                if (dt.mDirectory.Name == name)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Header名の取得
        /// </summary>
        /// <param name="sp">キャストしたHeader</param>
        /// <returns>HeaderのText</returns>
        private string HeaderToString(StackPanel sp)
        {
            TextBlock tb = (TextBlock)sp.Children[1];
            return tb.Text;
        }
    }
}
