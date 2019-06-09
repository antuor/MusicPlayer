using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using WPFSoundVisualizationLib;
using System.Windows.Data;
using System.Windows.Input;

namespace MusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NAudioEngine soundEngine = NAudioEngine.Instance;
        private class Audio
        {
            public string path;
            public string title;
            public string artist;
            public TimeSpan lenght;
        }
        private List<Audio> songs;
        private int song_index = 0;
        private bool isLoaded = false;
        private string SavePlaylistPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Playlist.txt");

        public MainWindow()
        {
            InitializeComponent();

            soundEngine.PropertyChanged += NAudioEngine_PropertyChanged;

            if (NAudioEngine.Instance.CanPlay)
            {
                PlayButton.Content = "Pause";
            }
            else if (NAudioEngine.Instance.CanPause)
            {
                PlayButton.Content = "Play";
            }
            UIHelper.Bind(soundEngine, "CanStop", StopButton, Button.IsEnabledProperty);
            //UIHelper.Bind(soundEngine, "CanPlay", PlayButton, Button.IsEnabledProperty);
            //UIHelper.Bind(soundEngine, "CanPause", PauseButton, Button.IsEnabledProperty);
            //UIHelper.Bind(soundEngine, "SelectionBegin", repeatStartTimeEdit, TimeEditor.ValueProperty, BindingMode.TwoWay);
            //UIHelper.Bind(soundEngine, "SelectionEnd", repeatStopTimeEdit, TimeEditor.ValueProperty, BindingMode.TwoWay);     

            spectrumAnalyzer.RegisterSoundPlayer(soundEngine);
            waveformTimeline.RegisterSoundPlayer(soundEngine);

            //LoadExpressionDarkTheme();
            LoadDefaultTheme();
        }

        public NAudioEngine GetNaudioEng()
        {
            return soundEngine;
        }

        #region NAudio Engine Events
        private void NAudioEngine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NAudioEngine engine = NAudioEngine.Instance;
            switch (e.PropertyName)
            {
                case "FileTag":
                    if (engine.FileTag != null)
                    {
                        TagLib.File file = engine.FileTag;
                        if (file.Tag.Pictures.Length > 0)
                        {
                            using (MemoryStream albumArtworkMemStream = new MemoryStream(file.Tag.Pictures[0].Data.Data))
                            {
                                try
                                {
                                    BitmapImage albumImage = new BitmapImage();
                                    albumImage.BeginInit();
                                    albumImage.CacheOption = BitmapCacheOption.OnLoad;
                                    albumImage.StreamSource = albumArtworkMemStream;
                                    albumImage.EndInit();
                                    albumArtPanel.AlbumArtImage = albumImage;
                                }
                                catch (NotSupportedException)
                                {
                                    albumArtPanel.AlbumArtImage = null;
                                    // System.NotSupportedException:
                                    // No imaging component suitable to complete this operation was found.
                                }
                                albumArtworkMemStream.Close();
                            }
                        }

                        //string[] allFoundFiles = Directory.GetFiles(file.Name, ".jpg", SearchOption.AllDirectories);

                        else
                        {
                            albumArtPanel.AlbumArtImage = null;
                        }
                        
                        if (file.Tag.Title != null)
                        {
                            SongTitle.Content = file.Tag.Title;
                        }
                        else SongTitle.Content = Path.GetFileNameWithoutExtension(file.Name);

                        if (file.Tag.Performers.Length > 0)
                        {
                            SongTitle.Content = SongTitle.Content + " - " + file.Tag.Performers[0];
                        }

                        if (file.Tag.Album != null)
                        {
                            Album.Content = file.Tag.Album;
                        }
                        else Album.Content = "Unknown Album";
                    }
                    else
                    {
                        albumArtPanel.AlbumArtImage = null;
                    }
                    break;
                case "ChannelPosition":
                    clockDisplay.Time = TimeSpan.FromSeconds(engine.ChannelPosition);
                    if (engine.FileTag != null)
                    {
                        TagLib.File file = engine.FileTag;
                        if (TimeSpan.FromSeconds(engine.ChannelPosition) <= TimeSpan.FromSeconds(file.Properties.Duration.TotalSeconds) &&
                            TimeSpan.FromSeconds(engine.ChannelPosition) >= TimeSpan.FromSeconds(file.Properties.Duration.TotalSeconds - 2))
                        {
                            if (song_index < songs.Count - 1)
                            {
                                song_index++;
                                NAudioEngine.Instance.OpenFile(songs[song_index].path);
                                if (NAudioEngine.Instance.CanPlay)
                                    NAudioEngine.Instance.Play();
                                PrevButton.IsEnabled = true;
                                PlayButton.Content = "Pause";
                            }
                            if (song_index >= songs.Count - 1)
                                NextButton.IsEnabled = false;
                        }
                    }
                    break;   
                default:
                    // Do Nothing
                    break;
            }

        }
        #endregion

        protected override void OnActivated(EventArgs e)
        {
            //base.OnActivated(e);
            if (isLoaded == false)
            {
                int count = 0;
                if (File.Exists(SavePlaylistPath) && (count = File.ReadAllLines(SavePlaylistPath).Length) != 0)
                {
                    FileStream saveFile = new FileStream(SavePlaylistPath, FileMode.Open);
                    if (saveFile != null)
                    {
                        //Playlist.Items.Clear();
                        songs = new List<Audio>(count - 1);
                        StreamReader sr = new StreamReader(saveFile);
                        string path;
                        Int32.TryParse(sr.ReadLine(), out song_index);
                        while ((path = sr.ReadLine()) != null)
                        {
                            if (File.Exists(path))
                            {
                                TagLib.File file = TagLib.File.Create(path);
                                Audio audio = new Audio();
                                audio.path = path;

                                if (file.Tag.Title != null)
                                    audio.title = file.Tag.Title;
                                else audio.title = Path.GetFileNameWithoutExtension(path);
                                audio.lenght = file.Properties.Duration;
                                if (file.Tag.Performers.Length > 0)
                                    audio.artist = file.Tag.Performers[0] + " - ";

                                songs.Add(audio);
                                Playlist.Items.Add(audio.artist + audio.title);
                            }
                        }
                        if (songs.Count > 0)
                        {
                            NAudioEngine.Instance.OpenFile(songs[song_index].path);

                            if (NAudioEngine.Instance.CanPlay)
                                NAudioEngine.Instance.Play();

                            PlayButton.IsEnabled = true;
                            PrevButton.IsEnabled = true;
                            NextButton.IsEnabled = true;
                        }

                        NAudioEngine.Instance.VolumeLevelChange((float)VolumeSlider.Value / 100f);
                        sr.Close();
                    }
                    saveFile.Close();
                }
                isLoaded = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            string[] allPaths;
            if (File.Exists(SavePlaylistPath) && (allPaths = File.ReadAllLines(SavePlaylistPath)).Length != 0)
            {
                FileStream saveFile = new FileStream(SavePlaylistPath, FileMode.Create);
                StreamWriter sw = new StreamWriter(saveFile);
                sw.WriteLine(song_index.ToString());
                for (int i = 1; i < allPaths.Length; i++)
                {
                    sw.WriteLine(allPaths[i]);
                }
                sw.Close();
                saveFile.Close();
            }
            NAudioEngine.Instance.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void HotKeys(object sender, KeyEventArgs e)
        {
            if (NAudioEngine.Instance.CanPlay)
            {
                if (e.Key == Key.P || e.Key == Key.Space)
                {
                    NAudioEngine.Instance.Play();
                    PlayButton.Content = "Pause";
                }
            }
            else if (NAudioEngine.Instance.CanPause)
            {
                if (e.Key == Key.P || e.Key == Key.Space)
                {
                    NAudioEngine.Instance.Pause();
                    PlayButton.Content = "Play";
                }
            }

            if (NAudioEngine.Instance.CanStop)
            {
                if (e.Key == Key.S)
                    NAudioEngine.Instance.Stop();
            }

            if (e.Key == Key.O)
                OpenFile();

            if (e.Key == Key.Escape)
                Close();

            if (e.Key == Key.Up)
            {
                NAudioEngine.Instance.ChannelPosition += 5.0;
            }
                
            else if (e.Key == Key.Down)
            {
                NAudioEngine.Instance.ChannelPosition -= 5.0;
            }

            if (e.Key == Key.Right)
            {
                if (song_index < songs.Count - 1)
                {
                    song_index++;
                    NAudioEngine.Instance.OpenFile(songs[song_index].path);
                    if (NAudioEngine.Instance.CanPlay)
                        NAudioEngine.Instance.Play();
                    PrevButton.IsEnabled = true;
                    PlayButton.Content = "Pause";
                }
                if (song_index >= songs.Count - 1)
                    NextButton.IsEnabled = false;
            }

            if (e.Key == Key.Left)
            {
                if (song_index > 0)
                {
                    song_index--;
                    NAudioEngine.Instance.OpenFile(songs[song_index].path);
                    if (NAudioEngine.Instance.CanPlay)
                        NAudioEngine.Instance.Play();
                    NextButton.IsEnabled = true;
                    PlayButton.Content = "Pause";
                }
                if (song_index <= 0)
                    PrevButton.IsEnabled = false;
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (NAudioEngine.Instance.CanPlay)
            {
                NAudioEngine.Instance.Play();
                PlayButton.Content = "Pause";
            }
            else if (NAudioEngine.Instance.CanPause)
            {
                NAudioEngine.Instance.Pause();
                PlayButton.Content = "Play";
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (song_index > 0)
            {
                song_index--;
                NAudioEngine.Instance.OpenFile(songs[song_index].path);
                if (NAudioEngine.Instance.CanPlay)
                    NAudioEngine.Instance.Play();
                NextButton.IsEnabled = true;
                PlayButton.Content = "Pause";
            }
            if (song_index <= 0)
                PrevButton.IsEnabled = false;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (song_index < songs.Count - 1)
            {
                song_index++;
                NAudioEngine.Instance.OpenFile(songs[song_index].path);
                if (NAudioEngine.Instance.CanPlay)
                    NAudioEngine.Instance.Play();
                PrevButton.IsEnabled = true;
                PlayButton.Content = "Pause";
            }
            if (song_index >= songs.Count - 1)
                NextButton.IsEnabled = false;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (NAudioEngine.Instance.CanStop)
                NAudioEngine.Instance.Stop();
        }

        private void OpenFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            Grid.SetColumn(grid.Children[grid.Children.Count - 1], 0);
            Grid.SetRow(grid.Children[grid.Children.Count - 1], 1);
            Row2.Height = new GridLength(1, GridUnitType.Star);
            Row3.Height = new GridLength(0);
            Row4.Height = new GridLength(0);
            Row5.Height = new GridLength(0);

            Column2.Width = new GridLength(0);
            Column3.Width = new GridLength(0);
            Column4.Width = new GridLength(0);
            for (int i = 1; i < grid.Children.Count - 1; i++)
            {
                grid.Children[i].Visibility = Visibility.Hidden;
            }
        }

        private void SSMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Grid.SetColumn(grid.Children[grid.Children.Count - 1], 2);
            Grid.SetRow(grid.Children[grid.Children.Count - 1], 2);
            Row2.Height = new GridLength(50);
            Row3.Height = new GridLength(1, GridUnitType.Star);
            Row4.Height = new GridLength(100);
            Row5.Height = new GridLength(50);

            Column2.Width = new GridLength(0.6, GridUnitType.Star);
            Column3.Width = new GridLength(1, GridUnitType.Star);
            Column4.Width = new GridLength(1, GridUnitType.Star);
            for (int i = 1; i < grid.Children.Count - 1; i++)
            {
                grid.Children[i].Visibility = Visibility.Visible;
            }
        }

        private void Playlist_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Playlist.SelectedIndex >= 0)
                song_index = Playlist.SelectedIndex;
            NAudioEngine.Instance.OpenFile(songs[song_index].path);
            if (NAudioEngine.Instance.CanPlay)
                NAudioEngine.Instance.Play();

            if (song_index >= songs.Count - 1)
                NextButton.IsEnabled = false;
            else NextButton.IsEnabled = true;

            if (song_index <= 0)
                PrevButton.IsEnabled = false;
            else PrevButton.IsEnabled = true;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            NAudioEngine.Instance.VolumeLevelChange((float)VolumeSlider.Value / 100f);
        }

        private void LoadDefaultTheme()
        {
            DefaultThemeMenuItem.IsChecked = true;
            DefaultThemeMenuItem.IsEnabled = false;
            ExpressionDarkMenuItem.IsChecked = false;
            ExpressionDarkMenuItem.IsEnabled = true;
            ExpressionLightMenuItem.IsChecked = false;
            ExpressionLightMenuItem.IsEnabled = true;

            Resources.MergedDictionaries.Clear();
        }

        private void LoadDarkBlueTheme()
        {
            DefaultThemeMenuItem.IsChecked = false;
            DefaultThemeMenuItem.IsEnabled = true;
            ExpressionDarkMenuItem.IsChecked = false;
            ExpressionDarkMenuItem.IsEnabled = true;
            ExpressionLightMenuItem.IsChecked = false;
            ExpressionLightMenuItem.IsEnabled = true;

            Resources.MergedDictionaries.Clear();
            ResourceDictionary themeResources = Application.LoadComponent(new Uri("DarkBlue.xaml", UriKind.Relative)) as ResourceDictionary;
            Resources.MergedDictionaries.Add(themeResources);
        }

        private void LoadExpressionDarkTheme()
        {
            DefaultThemeMenuItem.IsChecked = false;
            DefaultThemeMenuItem.IsEnabled = true;
            ExpressionDarkMenuItem.IsChecked = true;
            ExpressionDarkMenuItem.IsEnabled = false;
            ExpressionLightMenuItem.IsChecked = false;
            ExpressionLightMenuItem.IsEnabled = true;

            Resources.MergedDictionaries.Clear();
            ResourceDictionary themeResources = Application.LoadComponent(new Uri("ExpressionDark.xaml", UriKind.Relative)) as ResourceDictionary;
            Resources.MergedDictionaries.Add(themeResources);
        }

        private void LoadExpressionLightTheme()
        {
            DefaultThemeMenuItem.IsChecked = false;
            DefaultThemeMenuItem.IsEnabled = true;
            ExpressionDarkMenuItem.IsChecked = false;
            ExpressionDarkMenuItem.IsEnabled = true;
            ExpressionLightMenuItem.IsChecked = true;
            ExpressionLightMenuItem.IsEnabled = false;

            Resources.MergedDictionaries.Clear();
            ResourceDictionary themeResources = Application.LoadComponent(new Uri("ExpressionLight.xaml", UriKind.Relative)) as ResourceDictionary;
            Resources.MergedDictionaries.Add(themeResources);
        }

        private void DefaultThemeMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            LoadDefaultTheme();
        }

        private void ExpressionDarkMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            LoadExpressionDarkTheme();
        }

        private void ExpressionLightMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            LoadExpressionLightTheme();
        }

        private void OpenFile()
        {
            Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
            openDialog.Filter = "(*.mp3)|*.mp3";
            openDialog.Multiselect = true;
            if (openDialog.ShowDialog() == true)
            {
                FileStream saveFile = new FileStream(SavePlaylistPath, FileMode.Create);
                StreamWriter sw = new StreamWriter(saveFile);
                Playlist.Items.Clear();
                songs = new List<Audio>(openDialog.FileNames.Length);
                foreach (string path in openDialog.FileNames)
                {
                    TagLib.File file = TagLib.File.Create(path);
                    Audio audio = new Audio();
                    audio.path = path;

                    if (file.Tag.Title != null)
                        audio.title = file.Tag.Title;
                    else audio.title = Path.GetFileNameWithoutExtension(path);

                    audio.lenght = file.Properties.Duration;
                    if (file.Tag.Performers.Length > 0)
                        audio.artist = file.Tag.Performers[0] + " - ";

                    songs.Add(audio);
                    Playlist.Items.Add(audio.artist + audio.title);

                    sw.WriteLine(path);
                }
                sw.Close();
                saveFile.Close();

                song_index = 0;
                NAudioEngine.Instance.OpenFile(songs[song_index].path);
                if (NAudioEngine.Instance.CanPlay)
                    NAudioEngine.Instance.Play();

                PlayButton.IsEnabled = true;
                PrevButton.IsEnabled = true;
                NextButton.IsEnabled = true;

                NAudioEngine.Instance.VolumeLevelChange((float)VolumeSlider.Value / 100f);
            }
        }

    }
}
