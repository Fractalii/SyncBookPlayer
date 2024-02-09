/*#if ANDROID
using Android.App;
using Android.Content;
using Android.Media.Session;
#endif*/
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;
using MauiAudio;
using Npgsql;
using SkiaSharp;
using SyncBookPlayer.Model;
using SyncBookPlayer.ViewModel;
using System;
using System.Reflection;
using System.Text.Json;

namespace SyncBookPlayer
{
    public partial class MainPage : ContentPage
    {
        INativeAudioService Player2;
        List<Book> library = new List<Book>();
        public Book AudioBook;
        double Speed { get { return AudioBook.Speed; } set { AudioBook.Speed = value; Player2.Speed = value; } }
        public bool isPlaying { get { return Player2.IsPlaying; } }
        PlayerViewModel BindingManager;
        IDispatcherTimer timer;
        bool allowpick = true;
/*#if ANDROID
        NotificationManager notificationManager;
        MediaSession mediaSession;
#endif*/
        public MainPage()
        {
            InitializeComponent();
            //var app = Application.Current as App;
            //app.SharedData = "44";
            //var b = Shell.Current;
            //Application.Current.MainPage = this;
            //Shell.Current = this;
            //Shell.Current.Navigation
            //var c = Shell.Current;
            BindingManager = new PlayerViewModel();
            BindingContext = BindingManager;
            timer = Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(0.5);
            timer.Tick += (s, e) => newSec();
            Player2 = NativeAudioService.Current;
            //Player2.PlayEnded += Player_MediaEnded;
            Player2.IsPlayingChanged += Player2_IsPlayingChanged;
            Player2.PlayNext += Player2_PlayNext;
#if ANDROID
            PositionSlider.Margin = new Thickness(5,0,5,0);

            /*notificationManager = (NotificationManager)Android.App.Application.Context.GetSystemService(Context.NotificationService);
            var notificationChannel = new NotificationChannel("media_player_channel", "Media Player Channel", NotificationImportance.Low);
            notificationManager.CreateNotificationChannel(notificationChannel);

            mediaSession = new MediaSession(Android.App.Application.Context, "MediaSessionTag");
            var mediaSessionCallback = new MediaSessionCallback();
            mediaSession.SetCallback(mediaSessionCallback);

            var pendingIntent = PendingIntent.GetActivity(Android.App.Application.Context, 0, new Intent(Android.App.Application.Context, typeof(MainActivity)), PendingIntentFlags.UpdateCurrent);
            //var mediaStyle = new Android.Support.V7.App.NotificationCompat.MediaStyle();
            //mediaStyle.SetMediaSession(mediaSession.SessionToken);
            mediaSession.SetSessionActivity(pendingIntent);

            var mediaStyle = new Android.Support.V4.Media.App.NotificationCompat.MediaStyle();
            mediaStyle.SetMediaSession(mediaSession.SessionToken);*/
#elif WINDOWS
            PositionSlider.MaximumTrackColor = Color.FromArgb("777978");
#endif
            GetFOlder();
        }

        private void Player2_IsPlayingChanged(object? sender, bool e)
        {
            if (!Player2.IsPlaying)
            {
                timer.Stop();
                if (Convert.ToInt32(Player2.CurrentPosition) > 2)
                    AudioBook.MarkTime = Convert.ToInt32(Player2.CurrentPosition) - 2;
                else
                    AudioBook.MarkTime = 0;
                AudioBook.Save();
                PlayBtn.Source = "play.png";
            }
            else
            {
                timer.Start();
                PlayBtn.Source = "pause.png";
            }
        }

        async void GetFOlder()
        {

            string MainFolder = await SecureStorage.Default.GetAsync("FolderPath");

            if (MainFolder == null)
            {
                ChooseFolderbtn.IsVisible = true;
            }
            else
            {
                GetBooks(MainFolder);
            }
        }

        public async void GetBooks(string fld)
        {
            List<Book> SyncLib = new List<Book>();
            List<string> SyncLibFolders = new();
            //bool connected = false;
            //var conn = new NpgsqlConnection();
            try
            {
                string connString = "Server=ep-falling-cake-416088.eu-central-1.aws.neon.tech;Username=DAROMON;Database=neondb;Port=5432;Password=NVgYsqK8hyP6;SSLMode=Prefer;Timeout=30";
                var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();
                /*using (var command = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS fractalis ( folder_name TEXT PRIMARY KEY, json_data TEXT);", conn))
                {
                    command.ExecuteNonQuery();
                }*/
                /*using (var command = new NpgsqlCommand("TRUNCATE TABLE fractalis;", conn))
                {
                    command.ExecuteNonQuery();
                }*/
                //connected = true;
                using (var command = new NpgsqlCommand($"SELECT * FROM fractalis;", conn))
                {
                    using var reader = await command.ExecuteReaderAsync();

                    while (reader.Read())
                    {
                        SyncLibFolders.Add(reader.GetString(0));
                        SyncLib.Add(JsonSerializer.Deserialize<Book>(reader.GetString(1)));
                    }
                }
                conn.Close();
                await Toast.Make("Книги загружены", ToastDuration.Short).Show();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Синхронизация не удалась", ex.Message, "OK");
            }
            /*using (var command = new NpgsqlCommand("SELECT json_data FROM fractalis;", conn))
            {
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    SyncLib.Add(JsonSerializer.Deserialize<Book>(reader.GetString(0)));
                }
            }*/

            //await conn.CloseAsync();



            List<string> bookFolders = Directory.GetDirectories(fld, "*", SearchOption.AllDirectories).ToList();
            bookFolders.Add(fld);
            foreach (string folder in bookFolders)
            {
                /*if(File.Exists(Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(folder) + ".json")))
                {
                    Book book = new Book();
                    var rawFata = File.ReadAllText(Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(folder) + ".json"));
                    book = JsonSerializer.Deserialize<Book>(rawFata);
                    library.Add(book);
                    continue;
                }*/
                /*using (var command = new NpgsqlCommand($"SELECT json_data FROM fractalis;", conn))
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    await using var reader = await command.ExecuteReaderAsync();
                    var elapsedMs = watch.ElapsedMilliseconds;

                    while (await reader.ReadAsync())
                    {
                        watch = System.Diagnostics.Stopwatch.StartNew();
                        string x = reader.GetString(0);
                        var elapsedMs2 = watch.ElapsedMilliseconds;
                        int xfs = 0;
                    }
                }*/
                var files = Directory.GetFiles(folder).Where(s => s.ToLower().EndsWith(".mp3") || s.ToLower().EndsWith(".wav") || s.ToLower().EndsWith(".m4a") || s.ToLower().EndsWith(".m4b") || s.ToLower().EndsWith(".mp4") || s.ToLower().EndsWith(".mkv") || s.ToLower().EndsWith(".ogg") || s.ToLower().EndsWith(".webm") || s.ToLower().EndsWith(".wma") || s.ToLower().EndsWith(".mp2") || s.ToLower().EndsWith(".aac") || s.ToLower().EndsWith(".flac")).ToList();
                if (files.Count > 0)
                {
                    Book bookLocal = null;
                    Book bookSync = null;
                    Book book = new Book();

                    //bool loaded = true;

                    book.Folder = Path.GetFileName(folder);
                    /*if (connected)
                    {
                        using (var command = new NpgsqlCommand($"SELECT json_data FROM fractalis WHERE folder_name = @folder_name;", conn))
                        {
                            command.Parameters.AddWithValue("@folder_name", book.Folder);
                            await using var reader = await command.ExecuteReaderAsync();

                            while (await reader.ReadAsync())
                            {
                                bookSync = (JsonSerializer.Deserialize<Book>(reader.GetString(0)));
                            }
                        }
                    }*/
                    try
                    {
                        bookSync = SyncLib[SyncLibFolders.IndexOf(book.Folder)];
                    }
                    catch { }
                    if (File.Exists(Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(folder) + ".json")))
                    {
                        var rawFata = File.ReadAllText(Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(folder) + ".json"));
                        bookLocal = JsonSerializer.Deserialize<Book>(rawFata);
                    }
                    if (bookLocal != null && bookSync != null)
                    {
                        if (bookLocal.SaveTime < bookSync.SaveTime)
                            book.LoadData(bookSync);
                        else
                            book.LoadData(bookLocal);
                    }
                    else if (bookLocal != null)
                        book.LoadData(bookLocal);
                    else if (bookSync != null)
                        book.LoadData(bookSync);
                    else
                    {
                        book.MarkIndex = 0;
                        book.MarkTime = 0;
                        book.State = Book._State.NotStarted;
                        book.Speed = 1;
                    }
                    var cover = Directory.GetFiles(folder).Where(s => s.EndsWith(".jpg") || s.EndsWith(".png")).ToArray();
                    if (cover.Length > 0)
                        book.Cover = cover[0];


                    book.Playlist = files;
                    book.Playlist.Sort();

                    var bookFile = TagLib.File.Create(book.Playlist[0]);
                    book.Title = bookFile.Tag.Album;

                    if (book.Title is null)
                        book.Title = Path.GetFileName(folder);
                    book.Author = bookFile.Tag.FirstPerformer;
                    book.Narrator = bookFile.Tag.FirstAlbumArtist;
                    
                    if (book.Cover is null)
                    {
                        //var mStream = new MemoryStream();
                        var firstPicture = bookFile.Tag.Pictures.FirstOrDefault();
                        if (firstPicture != null)
                        {
                            MemoryStream ms = new MemoryStream(firstPicture.Data.Data);
                            //Microsoft.Maui.Graphics.IImage image = PlatformImage.FromStream(ms);
                            await File.WriteAllBytesAsync(Path.Combine(folder, "BookCover.jpg"), ms.ToArray());
                            //System.Drawing.Image image = System.Drawing.Image.FromStream(ms);
                            //image.Save(Path.Combine(folder, "BookCover.jpg"));
                            book.Cover = Path.Combine(folder, "BookCover.jpg");
                            /*byte[] pData = firstPicture.Data.Data;
                            mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
                            var bm = new Bitmap(mStream, false);
                            mStream.Dispose();
                            bm.Save(Path.Combine(folder, "BookCover.jpg"));
                            book.Cover = Path.Combine(folder, "BookCover.jpg");*/
                        }
                    }
                    //if (!loaded)
                    //    book.Save();
                    library.Add(book);
                }
            }
            //libraryList.ItemsSource = library;
            NotStartedView.ItemsSource = library.Where(x => x.State == Book._State.NotStarted).ToList();
            StartedView.ItemsSource = library.Where(x => x.State == Book._State.Started).ToList();
            FinishedView.ItemsSource = library.Where(x => x.State == Book._State.Finished).ToList();
            /*if (connected)
            {
                await conn.CloseAsync();
                await Toast.Make("Книги загружены", ToastDuration.Short).Show();
            }*/
        }

        private async void PickFolder(object sender, EventArgs e)
        {
#if ANDROID
            await Permissions.RequestAsync<Permissions.StorageRead>();
#endif
            var result = await FolderPicker.Default.PickAsync(default);
            result.EnsureSuccess();
            //await Toast.Make($"Folder picked: Name - {result.Folder.Name}, Path - {result.Folder.Path}", ToastDuration.Long).Show();
            //using (FileStream fs = File.Create(result.Folder.Path + "/ggg.txt")) ;
            await SecureStorage.Default.SetAsync("FolderPath", result.Folder.Path);
            GetBooks(result.Folder.Path);


            ChooseFolderbtn.IsVisible = false;
            //gs.Source = ImageSource.FromFile(library[0].Cover);
        }

        private bool isBook(string folder)
        {
            var files = Directory.GetFiles(folder).Where(s => s.ToLower().EndsWith(".mp3") || s.ToLower().EndsWith(".wav") || s.ToLower().EndsWith(".m4a") || s.ToLower().EndsWith(".m4b") || s.ToLower().EndsWith(".mp4") || s.ToLower().EndsWith(".mkv") || s.ToLower().EndsWith(".ogg") || s.ToLower().EndsWith(".webm") || s.ToLower().EndsWith(".wma") || s.ToLower().EndsWith(".mp2") || s.ToLower().EndsWith(".aac") || s.ToLower().EndsWith(".flac")).ToArray();
            if (files.Length > 0)
                return true;
            return false;
        }

        private async void libraryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((CollectionView)sender).SelectedItem != null)
            {

                /*await Shell.Current.GoToAsync(nameof(BookPlayer), true,
                new Dictionary<string, object>
                {
                    {"AudioBook",((CollectionView)sender).SelectedItem},
                    {"Cover",((Book)((CollectionView)sender).SelectedItem).Cover}
                });*/
                Closing();

                if ((Book)((CollectionView)sender).SelectedItem != AudioBook)
                {
                    AudioBook = (Book)((CollectionView)sender).SelectedItem;
                    //var watch = System.Diagnostics.Stopwatch.StartNew();
                    AudioBook.ListenedSec = 0;
                    AudioBook.DurationSec = 0;

                    Task.Run(() =>
                    {
                        int j = 0;
                        foreach (var item in AudioBook.Playlist)
                        {
                            var bf = TagLib.File.Create(item);
                            AudioBook.DurationSec += bf.Properties.Duration.TotalSeconds;
                            if (j < AudioBook.MarkIndex)
                                AudioBook.ListenedSec += bf.Properties.Duration.TotalSeconds;
                            j++;
                        }
                    });
                    //var elapsedMs = watch.ElapsedMilliseconds;

                    StartBook();
//#if ANDROID
                    //notificationManager.StartNotification();
//#endif


                }
                
                PlayerMenu.TranslationY = Window.Height;
                PlayerMenu.IsVisible = true;
                await PlayerMenu.TranslateTo(0, 0, 250, Easing.CubicInOut);
#if ANDROID
                Menu.IsVisible = false;
#endif
                ((CollectionView)sender).SelectedItem = null;
                
            }
        }


        //player

        public async void StartBook()
        {
            if (AudioBook.Cover is not null) { 
                Task.Run(() =>
                {
                    var col = Blend(Color.FromArgb(GetDominantColor(AudioBook.Cover)), BackgroundColor, 0.3);
                    PlayerMenu.Dispatcher.Dispatch(() =>
                    {
                        PlayerMenu.BackgroundColor = col;
                    });
                
                });
            }
            BookCover.Source = AudioBook.Cover;
            playlistPicker.ItemsSource = AudioBook.Playlist;
            playlistPicker.SelectedIndex = AudioBook.MarkIndex;
            //Player2 = NativeAudioService.Current;
            //await Player2.InitializeAsync(AudioBook.Playlist[AudioBook.MarkIndex]);
            await Player2.InitializeAsync(new MediaPlay { URL= AudioBook.Playlist[AudioBook.MarkIndex] , Author=AudioBook.Author, Name=AudioBook.Title, Image=AudioBook.Cover});
            await Player2.PlayAsync();
            await Player2.SetCurrentTime(AudioBook.MarkTime);
            PlayBtn.Source = "pause.png";
            PositionSlider.Maximum = Player2.Duration;
            timer.Start();
            Player2.Speed = Speed;
            spt.Text = Speed.ToString();
            //Player.Source = AudioBook.Playlist[AudioBook.MarkIndex];
            //Player.SeekTo(TimeSpan.FromSeconds(AudioBook.MarkTime));
            
            AudioBook.State = Book._State.Started;
            newSec();
        }
        protected override void OnDisappearing()
        {
            Closing();
        }
        public void Closing()
        {
            if (isPlaying)
            {
                if (Convert.ToInt32(Player2.CurrentPosition) > 2)
                    AudioBook.MarkTime = Convert.ToInt32(Player2.CurrentPosition) - 2;
                else
                    AudioBook.MarkTime = 0;
                AudioBook.Save();
            }
        }

        private async void Player2_PlayNext(object? sender, EventArgs e)
        {
            if (allowpick)
            {
                if (AudioBook.Playlist.Count > AudioBook.MarkIndex + 1)
                {
                    allowpick = false;
                    AudioBook.MarkIndex++;
                    AudioBook.MarkTime = 0;
                    AudioBook.ListenedSec += Player2.Duration;
                    AudioBook.Save();
                    await Player2.InitializeAsync(new MediaPlay { URL = AudioBook.Playlist[AudioBook.MarkIndex], Author = AudioBook.Author, Name = AudioBook.Title, Image = AudioBook.Cover });
                    //PositionSlider.Maximum = Player.Duration.TotalSeconds;
                    //allowpick = false;
                    PlayerMenu.Dispatcher.Dispatch(() =>
                    {
                        playlistPicker.SelectedIndex = AudioBook.MarkIndex;
                    });
                    //allowpick = true;
                    //await Player2.SetCurrentTime(0);
                    await Player2.PlayAsync();
                    Player2.Speed = Speed;
                    PlayerMenu.Dispatcher.Dispatch(() =>
                    {
                        PositionSlider.Maximum = Player2.Duration;
                    });
                    allowpick = true;
                }
                else
                {
                    AudioBook.State = Book._State.Finished;
                    timer.Stop();
                    AudioBook.Save();
                }
            }

            //secret.Text = AudioBook.Playlist[AudioBook.MarkIndex];
        }

        void OnSpeedMinusClicked(object? sender, EventArgs e)
        {
            if (Player2.Speed >= 0.5)
            {
                Speed -= 0.25;
                spt.Text = Speed.ToString();
            }
        }

        void OnSpeedPlusClicked(object? sender, EventArgs e)
        {
            if (Player2.Speed < 10)
            {
                Speed += 0.25;
                spt.Text = Speed.ToString();
            }
        }

        async void Slider_DragCompleted(object? sender, EventArgs e)
        {
            ArgumentNullException.ThrowIfNull(sender);

            var newValue = ((Slider)sender).Value;
            await Player2.SetCurrentTime(newValue);

            await Player2.PlayAsync();
            PlayBtn.Source = "pause.png";
            timer.Start();
            //var x = PositionSlider.Value;
        }

        void Slider_DragStarted(object sender, EventArgs e)
        {
            Player2.PauseAsync();
            timer.Stop();
        }
        //windows приколы
        /*private void Player_MediaOpened(object sender, EventArgs e)
        {
            //Player.Pause();
            Player.Speed = Speed;
            Player.Play();
            timer.Start();
            PlayBtn.Source = "pause.png";
            //Player.Position = TimeSpan.FromSeconds(AudioBook.MarkTime)
            //PositionSlider.Value = Player.Position.TotalSeconds;
            //PositionSlider.Maximum = Player.Duration.TotalSeconds;
            //int x = 4;
        }*/

        private async void playlistPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AudioBook.MarkIndex != playlistPicker.SelectedIndex && PlayerMenu.IsVisible && allowpick)
            {
                AudioBook.MarkIndex = playlistPicker.SelectedIndex;
                await Player2.InitializeAsync(new MediaPlay { URL = AudioBook.Playlist[AudioBook.MarkIndex], Author = AudioBook.Author, Name = AudioBook.Title, Image = AudioBook.Cover });
                await Player2.PlayAsync();
                PlayBtn.Source = "pause.png";
                Player2.Speed = Speed;
                PositionSlider.Maximum = Player2.Duration;
                Task.Run(() =>
                {
                    AudioBook.ListenedSec = 0;
                    int j = 0;
                    foreach (var item in AudioBook.Playlist)
                    {
                        var bf = TagLib.File.Create(item);
                        if (j < AudioBook.MarkIndex)
                            AudioBook.ListenedSec += bf.Properties.Duration.TotalSeconds;
                        else
                            break;
                        j++;
                    }
                });
            }
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            //var hg = PositionSlider.Value;
            if (Player2.IsPlaying)
            {
                await Player2.PauseAsync();
                timer.Stop();
                if (Convert.ToInt32(Player2.CurrentPosition) > 2)
                    AudioBook.MarkTime = Convert.ToInt32(Player2.CurrentPosition) - 2;
                else
                    AudioBook.MarkTime = 0;
                AudioBook.Save();
                PlayBtn.Source = "play.png";
            }
            else
            {
                await Player2.PlayAsync();
                timer.Start();
                PlayBtn.Source = "pause.png";
            }

            //isPaused = !isPaused;
            //var x = Player.CurrentState;

            // Change the text of the button to the corresponding symbol
            //PlayBtn.Text = isPaused ? "⏸️" : "▶️";

            // Animate the button to shrink and then return to normal size
            PlayBtn.ScaleTo(0.9, 100, Easing.SinIn).ContinueWith((t) => PlayBtn.ScaleTo(1, 70, Easing.SinOut));
        }

        private async void ForwardBtn_Clicked(object sender, EventArgs e)
        {
            await Player2.SetCurrentTime(Player2.CurrentPosition + 30 * Speed);
            ForwardBtn.RotateTo(15, 100, Easing.Linear).ContinueWith((t) => ForwardBtn.RotateTo(0, 70, Easing.Linear));
        }

        private async void BackBtn_Clicked(object sender, EventArgs e)
        {
            await Player2.SetCurrentTime(Player2.CurrentPosition - 15 * Speed);
            BackBtn.RotateTo(-15, 100, Easing.Linear).ContinueWith((t) => BackBtn.RotateTo(0, 70, Easing.Linear));
        }

        /*private void Player_PositionChanged(object sender, MediaPositionChangedEventArgs e)
        {
            //beforeSec.Text = "-" + (string)countdown.Convert((AudioBook.DurationSec - (AudioBook.ListenedSec + Player.Position.TotalSeconds))/Player.Speed, null, null, null);
            BindingManager.ToListen = (AudioBook.DurationSec - (AudioBook.ListenedSec + Player.Position.TotalSeconds)) / Player.Speed;
            BindingManager.Percent = (AudioBook.ListenedSec + Player.Position.TotalSeconds) / AudioBook.DurationSec;
        }*/

        private async void Button_Clicked_1(object sender, EventArgs e)
        {
            timer.Stop();
            Closing();
#if ANDROID
            Menu.IsVisible = true;
#endif
            await PlayerMenu.TranslateTo(0, Window.Height, 200, Easing.CubicInOut);
            PlayerMenu.IsVisible = false;
        }

        protected override bool OnBackButtonPressed()
        {
            if (PlayerMenu.IsVisible)
            {
                Button_Clicked_1(null, null);
                return true;
            }
            else
                return base.OnBackButtonPressed();
        }

        /*private Color GetCoverColor()
        {
            Stream imageStream = null;
            if (AudioBook.Cover is not null)
                imageStream = File.OpenRead(AudioBook.Cover);
            byte[] imageData = null;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                imageStream.CopyTo(memoryStream);
                imageData = memoryStream.ToArray();
            }
            return Color.FromRgb(imageData[2], imageData[1], imageData[0]);


        }*/

        public string GetDominantColor(string path)
        {
            SkiaSharp.SKBitmap bmp;
            Stream stream = File.OpenRead(path);
            bmp = SkiaSharp.SKBitmap.Decode(stream);
            //Used for tally
            int r = 0;
            int g = 0;
            int b = 0;

            int total = 0;

            //int x = bmp.Width / 2;

            /*for (int y = 0; y < bmp.Height; y++)
            {
                SkiaSharp.SKColor clr = bmp.GetPixel(x, y);

                r += clr.Red;
                g += clr.Green;
                b += clr.Blue;

                total++;
                if (total > 500)
                    break;
            }*/
            for (int x = 0; x < bmp.Width; x += bmp.Width/15)
            {
                for (int y = 0; y < bmp.Height; y += bmp.Height/15)
                {
                    SKColor clr = bmp.GetPixel(x, y);

                    r += clr.Red;
                    g += clr.Green;
                    b += clr.Blue;

                    total++;
                }
            }

            //Calculate average
            r /= total;
            g /= total;
            b /= total;

            return $"{r:X2}{g:X2}{b:X2}";
        }
        public Color Blend(Color color, Color backColor, double amount)
        {
            byte r = (byte)(color.GetByteRed() * amount + backColor.GetByteRed() * (1 - amount));
            byte g = (byte)(color.GetByteGreen() * amount + backColor.GetByteGreen() * (1 - amount));
            byte b = (byte)(color.GetByteBlue() * amount + backColor.GetByteBlue() * (1 - amount));
            return Color.FromRgb(r, g, b);
        }
        public void newSec()
        {
            BindingManager.ToListen = (AudioBook.DurationSec - (AudioBook.ListenedSec + Player2.CurrentPosition)) / Player2.Speed;
            BindingManager.Percent = (int)(((AudioBook.ListenedSec + Player2.CurrentPosition) / AudioBook.DurationSec) * 100);
            PositionSlider.Value = Player2.CurrentPosition;
#if WINDOWS
            PositionSlider.Maximum = Player2.Duration;
#endif
        }
    }
}
