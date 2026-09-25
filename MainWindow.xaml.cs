using CoreLib;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PhotoViewer
{
    /// <summary>
    /// 写真データをリストに登録するためのクラス
    /// </summary>
    public class Photo
    {
        public string title { get; set; }
        public string path { get; set; }
        public BitmapImage image { get; set; }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double mWindowWidth;
        private double mWindowHeight;

        private List<Photo> Photos;

        private int mThumbnailWidth = 80;        //  サムネイル表示のための画像縮小サイズ
        private int mThumbnailHeight = 50;
        private int mDataFolderMax = 100;        //  登録フォルダの最大数
        private string mCurFolder = "";
        private bool mRecursiveFolder = false;    //  フォルダのデータを再帰検索

        private enum SORTTYPE { path, filename, date, size }     //  ソートタイプ
        private List<string> mSortTitle = new List<string> { "パス名", "ファイル名", "日付", "サイズ", "逆順" };
        private SORTTYPE mSortType = SORTTYPE.path;  //  ソート
        private bool mSortReverse = false;           //  逆順
        private DirectoryTree mDirectoryTree;

        private ImageView mImageView;
        private string mFolderListPath = "FolderList.csv";
        private YLib ylib = new();

        public MainWindow()
        {
            InitializeComponent();

            WindowFormLoad();

            cbSort.ItemsSource = mSortTitle;
            loadFolderList(mFolderListPath);
            tvComponent.Items.Clear();
            mDirectoryTree = new DirectoryTree();
            tvComponent.Items.Add(mDirectoryTree);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mImageView != null)
                mImageView.Close();
            saveFolderList(mFolderListPath);
            WindowFormSave();
        }


        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            mWindowWidth = Width;
            mWindowHeight = Height;

            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MainWindowWidth < 100 || Properties.Settings.Default.MainWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.MainWindowHeight) {
                Properties.Settings.Default.MainWindowWidth = mWindowWidth;
                Properties.Settings.Default.MainWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.MainWindowTop;
                Left = Properties.Settings.Default.MainWindowLeft;
                Width = Properties.Settings.Default.MainWindowWidth;
                Height = Properties.Settings.Default.MainWindowHeight;
                //  GridのalbumListの幅は直接設定できないのでGridLength()に変換して設定
                GridLength ln = new GridLength(Properties.Settings.Default.MainWindowDirectoryTreeWidth);
                directoryTree.Width = ln;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MainWindowTop = Top;
            Properties.Settings.Default.MainWindowLeft = Left;
            Properties.Settings.Default.MainWindowWidth = Width;
            Properties.Settings.Default.MainWindowHeight = Height;
            Properties.Settings.Default.MainWindowDirectoryTreeWidth = directoryTree.ActualWidth;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// TreeViewのフォルダ選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvComponent_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is DirectoryTree) {
                DirectoryTree item = e.NewValue as DirectoryTree;
                if (item.mDirectory != null) {
                    System.Diagnostics.Debug.WriteLine(item.mDirectory.FullName);
                    mCurFolder = item.mDirectory.FullName;
                    setPhotoData(mCurFolder);
                }
            }
        }

        /// <summary>
        /// ListViewのアイテム マウスダブルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvPhotoList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (0 <= lvPhotoList.SelectedIndex) {
                //  ファイル選択
                int index = lvPhotoList.SelectedIndex;
                if (cbOutViewer.IsChecked == true) {
                    ylib.openUrl(Photos[index].path);
                } else {
                    dispPhotoData(Photos[index].path);
                }
            }

        }

        /// <summary>
        /// ListViewのアイテム選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvPhotoList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= lvPhotoList.SelectedIndex) {
                //  ファイル選択
                int index = lvPhotoList.SelectedIndex;
                setPhotoInfo(Photos[index].path);
            }
        }

        /// <summary>
        /// ソートの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbSort_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch (cbSort.SelectedIndex) {
                case 0: mSortType = SORTTYPE.path; break;
                case 1: mSortType = SORTTYPE.filename; break;
                case 2: mSortType = SORTTYPE.date; break;
                case 3: mSortType = SORTTYPE.size; break;
                case 4: mSortReverse = !mSortReverse; break;
            }
            setPhotoData(mCurFolder);
        }

        /// <summary>
        /// データの再帰検索チェックボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbRecursive_Click(object sender, RoutedEventArgs e)
        {
            mRecursiveFolder = cbRecursive.IsChecked == true;
            setPhotoData(mCurFolder);
        }

        /// <summary>
        /// お気に入りフォルダの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbSelectFolder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int index = cbSelectFolder.SelectedIndex;
            if (0 <= index && cbSelectFolder.Items[index] != mCurFolder) {
                mCurFolder = cbSelectFolder.Items[index].ToString();
                treeExpand(mCurFolder);
                setPhotoData(mCurFolder);
                cbSelectFolder.Items.RemoveAt(index);
                cbSelectFolder.Items.Insert(0, mCurFolder);
            }
        }

        /// <summary>
        /// お気に入りフォルダの登録
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            if (mCurFolder.Length > 0) {
                int index = cbSelectFolder.Items.IndexOf(mCurFolder);
                if (0 <= index)
                    cbSelectFolder.Items.RemoveAt(index);
                cbSelectFolder.Items.Insert(0, mCurFolder);
                cbSelectFolder.SelectedIndex = 0;
            }
        }

        private void lvPhotoList_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (0 <= lvPhotoList.SelectedIndex) {
                //  ファイル選択
                int index = lvPhotoList.SelectedIndex;
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
                    if (e.Key == Key.I) {
                        infoImage(Photos[index].path);
                    }
                }
            }
        }

        /// <summary>
        /// イメージファイルをダイヤログ表示
        /// </summary>
        /// <param name="path"></param>
        private void dispPhotoData(string path)
        {
            if (mImageView != null) {
                mImageView.Close();
            }
            mImageView = new ImageView();
            mImageView.mImageList = Photos.ConvertAll(p => p.path);
            mImageView.mImagePath = path;
            mImageView.Show();
        }

        /// <summary>
        /// ListViewにフォルダ内の画像ファイルを設定
        /// </summary>
        /// <param name="folder">フォルダパス</param>

        private bool setPhotoData(string folder)
        {
            if (Photos == null)
                Photos = new List<Photo>();

            Photos.Clear();
            List<string> files = new List<string>(ylib.getFiles(Path.Combine(folder, "*.jpg"), mRecursiveFolder));
            if (files == null) return false;
            files.AddRange(new List<string>(ylib.getFiles(Path.Combine(folder, "*.png"), mRecursiveFolder)));
            tbFolderInfo.Text = "ファイル数: " + files.Count;
            List<string> fileList = sortFiles(files, mSortType, mSortReverse);

            tbPgTitle.Text = "読込中";
            pbLoadPhoto.Minimum = 0;
            pbLoadPhoto.Maximum = files.Count;
            pbLoadPhoto.Value = 0;
            foreach (string file in fileList) {
                Photo photo = new Photo();
                //photo.image = ylib.getBitmapImage(file, mThumbnailWidth);
                photo.image = ylib.getThumbnailImage(file, mThumbnailWidth, mThumbnailHeight);
                photo.title = Path.GetFileName(file);
                photo.path = file;
                Photos.Add(photo);
                pbLoadPhoto.Value++;
                ylib.DoEvents();
            }
            tbPgTitle.Text = "読込完了";
            lvPhotoList.ItemsSource = new ReadOnlyCollection<Photo>(Photos);
            pbLoadPhoto.Value = 0;

            return true;
        }

        /// <summary>
        /// 写真リストのソート
        /// </summary>
        /// <param name="files">ファイルリスト</param>
        /// <param name="sortType">ソートタイプ</param>
        /// <param name="sortReverse">逆順</param>
        /// <returns>ソートしたファイルリスト</returns>
        private List<string> sortFiles(List<string> files, SORTTYPE sortType, bool sortReverse)
        {
            List<FileInfo> fileList = new List<FileInfo>();
            foreach (string path in files)
                fileList.Add(new FileInfo(path));
            switch (sortType) {
                case SORTTYPE.path:     //  フルパスで比較
                    if (sortReverse)
                        fileList.Sort((b, a) => a.FullName.CompareTo(b.FullName));
                    else
                        fileList.Sort((a, b) => a.FullName.CompareTo(b.FullName));
                    break;
                case SORTTYPE.filename: //  拡張子を除くファイル名で比較
                    if (sortReverse)
                        fileList.Sort((b, a) => Path.GetFileNameWithoutExtension(a.Name).CompareTo(Path.GetFileNameWithoutExtension(b.Name)));
                    else
                        fileList.Sort((a, b) => Path.GetFileNameWithoutExtension(a.Name).CompareTo(Path.GetFileNameWithoutExtension(b.Name)));
                    break;
                case SORTTYPE.date:     //  ファイル日付で比較
                    if (sortReverse)
                        fileList.Sort((b, a) => a.LastWriteTime.CompareTo(b.LastWriteTime));
                    else
                        fileList.Sort((a, b) => a.LastWriteTime.CompareTo(b.LastWriteTime));
                    break;
                case SORTTYPE.size:     //  ファイルサイズで比較
                    if (sortReverse)
                        fileList.Sort((b, a) => a.Length.CompareTo(b.Length));
                    else
                        fileList.Sort((a, b) => a.Length.CompareTo(b.Length));
                    break;
            }
            return fileList.ConvertAll(p => p.FullName);
        }

        /// <summary>
        /// 写真ファイルのExif情報をステータスバーに表示
        /// </summary>
        /// <param name="path">ファイルパス</param>
        private void setPhotoInfo(string path)
        {
            if (!File.Exists(path))
                return;
            Title = "フォトリスト [" + Path.GetFileName(path) + "]";
            //  ファイルプロパティ表示
            BitmapImage bmpImage = ylib.getBitmapImage(path);
            ExifInfo exifInfo = new ExifInfo(path);
            Point coodinate = exifInfo.getExifGpsCoordinate();
            string[] datetime = exifInfo.getDateTime().Split(':');
            tbFolderInfo.Text = datetime.Length == 5 ?
                $"{datetime[0]}/{datetime[1]}/{datetime[2]}:{datetime[3]}:{datetime[4]}" : exifInfo.getDateTime();
            if (coodinate.X < 0 || coodinate.Y < 0)
                tbFolderInfo.Text += " (座標なし)";
            List<string> iptc = ylib.getIPTC(path);
            tbFolderInfo.Text += " " + (iptc.Count > 3 ? iptc[4] : "") + exifInfo.getUserComment();
            tbFolderInfo.Text += " [" + bmpImage.PixelWidth + "x" + bmpImage.PixelHeight + "]";
            tbFolderInfo.Text += " " + exifInfo.getCamera("カメラ {0} {1}");
            tbFolderInfo.Text += " " + exifInfo.getCameraSetting(" 1/{0} s F{1} ISO {2} 焦点距離 {3} mm");
        }

        /// <summary>
        /// イメージのプロパティ表示
        /// </summary>
        /// <param name="path"></param>
        private void infoImage(string path)
        {
            string buf = ylib.getIPTCall(path);
            ExifInfo exifInfo = new ExifInfo(path);
            buf += "\n" + exifInfo.getExifInfoAll();
            messageBox(buf, "属性表示[" + Path.GetFileName(path) + "]");
        }

        /// <summary>
        /// フォルダリストの保存
        /// </summary>
        /// <param name="listPath"></param>
        private void saveFolderList(string listPath)
        {
            List<string> folderList = new List<string>();
            foreach (string path in cbSelectFolder.Items)
                folderList.Add(path);
            ylib.saveListData(listPath, folderList);
        }

        /// <summary>
        /// フォルダリストの読込
        /// </summary>
        /// <param name="listPath"></param>
        private void loadFolderList(string listPath)
        {
            List<string> list = ylib.loadListData(listPath);
            if (list == null)
                return;
            int n = 0;
            foreach (var path in list) {
                if (n < 50 && 0 < path.Length && !cbSelectFolder.Items.Contains(path)) {
                    cbSelectFolder.Items.Add(path);
                    n++;
                }
            }
        }

        /// <summary>
        /// DirectoryTreeをパスに合わせて開く
        /// </summary>
        /// <param name="path"></param>
        private void treeExpand(string path)
        {
            string[] dirs = path.Split(Path.DirectorySeparatorChar);
            DirectoryTree item = mDirectoryTree;
            for (int i = 0; i < dirs.Length; i++) {
                item.IsExpanded = true;
                bool abort = false;
                foreach (DirectoryTree dirTree in item.Items) {
                    System.Diagnostics.Debug.WriteLine(dirTree.mDirectory.Name);
                    if (dirTree.mDirectory.Name.Trim('\\') == dirs[i]) {
                        item = dirTree;
                        abort = true;
                        break;
                    }
                }
                if (!abort) break;
            }
        }

        /// <summary>
        /// メッセージ表示ダイヤログ
        /// </summary>
        /// <param name="buf">メッセージ</param>
        /// <param name="title">タイトル</param>
        private void messageBox(string buf, string title)
        {
            InputBox dlg = new InputBox();
            //dlg.mMainWindow = this;           //  親Windowの中心に表示
            dlg.Title = title;
            dlg.mWindowSizeOutSet = true;
            dlg.mWindowWidth = 500.0;
            dlg.mWindowHeight = 400.0;
            dlg.mMultiLine = true;
            dlg.mReadOnly = true;
            dlg.mEditText = buf;
            dlg.ShowDialog();
        }
    }
}