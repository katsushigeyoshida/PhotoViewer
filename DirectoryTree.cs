using CoreLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
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
    }
}
