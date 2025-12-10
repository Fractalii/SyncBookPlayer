using ATL;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Storage;
using MauiAudio;
using Npgsql;
using SkiaSharp;
using SyncBookPlayer.Model;
using SyncBookPlayer.View;
using SyncBookPlayer.ViewModel;
using System.Text.Json;

namespace SyncBookPlayer
{
    public partial class MainPage : ContentPage
    {
        INativeAudioService Player2;
        List<Book> library;
        public Book AudioBook;
        string login;
        double Speed { get { return AudioBook.Speed; } set { AudioBook.Speed = value; Player2.Speed = value; } }
        public bool isPlaying { get { return Player2.IsPlaying; } }
        PlayerViewModel BindingManager;
        IDispatcherTimer timer;
        bool allowpick = true;
        double startTime;
        double endTime;
        public MainPage()
        {
            InitializeComponent();
            Application.Current.UserAppTheme = AppTheme.Dark;
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
            timer.Interval = TimeSpan.FromSeconds(0.2);
            timer.Tick += (s, e) => newSec();
            Player2 = NativeAudioService.Current;
            //Player2.PlayEnded += Player_MediaEnded;
            Player2.IsPlayingChanged += Player2_IsPlayingChanged;
            Player2.PlayNext += Player2_PlayNext;
            Player2.PlayPrevious += Player2_PlayPrevious;
#if WINDOWS
            Player2.BufferingFinished += Player2_BufferingFinished;
#endif
#if ANDROID
            PositionSlider.Margin = new Thickness(5,0,5,0);
#elif WINDOWS
            PositionSlider.MaximumTrackColor = Color.FromArgb("777978");
#endif
            GetFOlder();
        }
#if WINDOWS
        private void Player2_BufferingFinished(object? sender, EventArgs e)
        {
            if (!AudioBook.isM4b)
            {
                Dispatcher.Dispatch(() =>
                {
                    PositionSlider.Maximum = Player2.Duration;
                });
                if (AudioBook.isYT)
                    AudioBook.DurationSec = Player2.Duration;
            }

        }
#endif

        private async void Player2_PlayPrevious(object? sender, EventArgs e)
        {
            await Player2.SetCurrentTime(Player2.CurrentPosition - 10 * Speed);
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
                AudioBook.Save(login);
                SetPlayImg();
            }
            else
            {
                timer.Start();
                SetPauseiImg();
            }
        }

        async void GetFOlder()
        {

            string MainFolder = await SecureStorage.Default.GetAsync("FolderPath");

            if (MainFolder == null)
            {
                toolbar.IsVisible = false;
                StartLoading.IsRunning = false;
                ChooseFolderbtn.IsVisible = true;
                BookList.IsVisible = false;
            }
            else
            {
                GetBooks(MainFolder);
            }
        }

        public async void GetBooks(string fld)
        {
            BookList.IsVisible = false;
            StartLoading.IsRunning = true;
            toolbar.IsEnabled = false;
            login = await SecureStorage.Default.GetAsync("User");
            library = new List<Book>();
            List<Book> SyncLib = new List<Book>();
            List<string> SyncLibFolders = new();
            //bool connected = false;
            //var conn = new NpgsqlConnection();
            if (login != null)
            {
                try
                {
                    string connString = Settings.database_connection + "SSLMode=Prefer;Timeout=10";
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
                    using (var command = new NpgsqlCommand($"SELECT folder_name, json_data FROM books WHERE account_id={login};", conn))
                    {
                        using var reader = await command.ExecuteReaderAsync();

                        while (reader.Read())
                        {
                            SyncLibFolders.Add(reader.GetString(0));
                            SyncLib.Add(JsonSerializer.Deserialize<Book>(reader.GetString(1)));
                        }
                    }
                    await conn.CloseAsync();
                    //await Toast.Make("Книги загружены", ToastDuration.Short).Show();
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
            }

            await Task.Run(async () =>
            {

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
                    var files = new List<string>();
                    try
                    {
                        files = Directory.GetFiles(folder).Where(s => s.ToLower().EndsWith(".mp3") || s.ToLower().EndsWith(".wav") || s.ToLower().EndsWith(".m4a") || s.ToLower().EndsWith(".m4b") || s.ToLower().EndsWith(".mp4") || s.ToLower().EndsWith(".mkv") || s.ToLower().EndsWith(".ogg") || s.ToLower().EndsWith(".webm") || s.ToLower().EndsWith(".wma") || s.ToLower().EndsWith(".mp2") || s.ToLower().EndsWith(".aac") || s.ToLower().EndsWith(".flac")).ToList();
                    }
                    catch { }
                    if (files.Count > 0)
                    {
                        //#if DEBUG
                        //                    if (folder == "/storage/emulated/0/Книги/Alex Kingston - Doctor Who The Ruby's Curse River Song Novel")
                        //                        System.Diagnostics.Debugger.Break();
                        //#endif

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
                            {
                                book.LoadData(bookSync);
                                book.SaveLocal(JsonSerializer.Serialize(bookSync));
                                book.Downloaded = true;
                            }
                            else
                                book.LoadData(bookLocal);
                        }
                        else if (bookLocal != null)
                            book.LoadData(bookLocal);
                        else if (bookSync != null)
                        {
                            book.LoadData(bookSync);
                            book.SaveLocal(JsonSerializer.Serialize(bookSync));
                            book.Downloaded = true;
                        }
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

                        if (book.Playlist.Count == 1)
                        {
                            if (book.Playlist[0].EndsWith(".m4b"))
                            {
                                book.isM4b = true;
                                //using (var str = File.OpenRead(book.Playlist[0]))
                                //{
                                //    var extractor = new ChapterExtractor(new StreamWrapper(str));
                                //    Debug.WriteLine(extractor.IsMp4a());
                                //    extractor.Run();
                                //    foreach (var c in extractor.Chapters ?? new ChapterInfo[0])
                                //    {
                                //        Debug.WriteLine("{0} -> {1}", c.Time, c.Name);
                                //    }
                                //}
                                //Track theTrack = new Track(book.Playlist[0]);
                                //var n = theTrack.Chapters.ToList();
                            }
                            if (book.Playlist[0].EndsWith("youtube_video.webm"))
                                book.isYT = true;
                        }
                        if (!book.isYT)
                        {
                            Track bookFile;
                            try
                            {
                                bookFile = new Track(book.Playlist[0]);
                            }
                            catch { continue; }
                            book.Title = bookFile.Album;

                            if (book.Title == "")
                                book.Title = Path.GetFileName(folder);
                            book.Author = bookFile.Artist;
                            book.Narrator = bookFile.AlbumArtist;

                            if (book.Cover is null)
                            {
                                var firstPicture = bookFile.EmbeddedPictures;
                                if (firstPicture.Count > 0)
                                {
                                    MemoryStream ms = new MemoryStream(firstPicture[0].PictureData);
                                    await File.WriteAllBytesAsync(Path.Combine(folder, "BookCover.jpg"), ms.ToArray());
                                    book.Cover = Path.Combine(folder, "BookCover.jpg");
                                }
                            }
                        }
                        else
                            book.Title = Path.GetFileName(folder);
                        library.Add(book);
                    }
                }
                //libraryList.ItemsSource = library;
            });
            NotStartedView.ItemsSource = library.Where(x => x.State == Book._State.NotStarted).ToList();
            StartedView.ItemsSource = library.Where(x => x.State == Book._State.Started).ToList().OrderByDescending(x => x.SaveTime);
            FinishedView.ItemsSource = library.Where(x => x.State == Book._State.Finished).ToList();
            StartLoading.IsRunning = false;
            BookList.IsVisible = true;
            toolbar.IsEnabled = true;
            /*if (connected)
            {
                await conn.CloseAsync();
                await Toast.Make("Книги загружены", ToastDuration.Short).Show();
            }*/
        }

        private async void PickFolder(object sender, EventArgs e)
        {
            bool allow = true;
#if ANDROID
            var t = await Permissions.RequestAsync<Permissions.StorageRead>();
            if (t == PermissionStatus.Denied)
                allow = false;
#endif
            if (allow)
            {
                var result = await FolderPicker.Default.PickAsync(default);
                try
                {
                    result.EnsureSuccess();
                    //await Toast.Make($"Folder picked: Name - {result.Folder.Name}, Path - {result.Folder.Path}", ToastDuration.Long).Show();
                    //using (FileStream fs = File.Create(result.Folder.Path + "/ggg.txt")) ;
                    await SecureStorage.Default.SetAsync("FolderPath", result.Folder.Path);
                    GetBooks(result.Folder.Path);


                    ChooseFolderbtn.IsVisible = false;
                    BookList.IsVisible = true;
                    toolbar.IsVisible = true;
                }
                catch { }
            }
            else
            {
                await DisplayAlert("Ошибка", "Требуемые разрешения не предоставлены. Включите их в настройках устройства.", "OK");
            }
            //gs.Source = ImageSource.FromFile(library[0].Cover);
        }

        private async void libraryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((CollectionView)sender).SelectedItem != null)
            {
                Closing();
                SmallPlay.IsVisible = true;

                if ((Book)((CollectionView)sender).SelectedItem != AudioBook)
                {
                    AudioBook = (Book)((CollectionView)sender).SelectedItem;
                    BindingManager.Book = AudioBook;
                    //var watch = System.Diagnostics.Stopwatch.StartNew();
                    AudioBook.ListenedSec = 0;
                    AudioBook.DurationSec = 0;
                    startTime = 0;
                    endTime = 0;
                    ContentView.ItemsSource = null;

                    Task.Run(() =>
                    {
                        AudioBook.Chapters = new();
                        int j = 0;
                        foreach (var item in AudioBook.Playlist)
                        {
                            if (AudioBook.isYT)
                            {
                                AudioBook.Chapters.Add(new Chapter { Path = AudioBook.Playlist[0], Title = "Видео", Id = j });
                                continue;
                            }
                            var bf = new Track(item);
                            AudioBook.DurationSec += bf.Duration;
                            if (!AudioBook.isM4b)
                                AudioBook.Chapters.Add(new Chapter { Path = item, Title = bf.Title, Id = j });
                            else
                            {
                                var n = bf.Chapters.ToList();
                                int i = 0;
                                foreach (var x in n)
                                    AudioBook.Chapters.Add(new Chapter { Title = x.Title, Time = x.StartTime / 1000.0, Id = i++ });
                            }
                            if (j < AudioBook.MarkIndex)
                                AudioBook.ListenedSec += bf.Duration;
                            j++;
                        }
                        Dispatcher.Dispatch(() =>
                        {
                            allowpick = false;
                            ContentView.ItemsSource = AudioBook.Chapters;
                            if (!AudioBook.isM4b)
                                ContentView.SelectedItem = AudioBook.Chapters[AudioBook.MarkIndex];
                            allowpick = true;
                            newSec();
                        });
                    });
                    //var elapsedMs = watch.ElapsedMilliseconds;

                    StartBook();
                    //#if ANDROID
                    //notificationManager.StartNotification();
                    //#endif


                }
#if WINDOWS
                PlayerMenu.TranslationY = Window.Height;
                PlayerMenu.IsVisible = true;
                await PlayerMenu.TranslateTo(0, 0, 250, Easing.CubicInOut);
                //#else
                //                PlayerMenu.TranslationY = 0;
                //                PlayerMenu.IsVisible = true;
#endif
                timer.Start();
                //#if ANDROID
                //                Menu.IsVisible = false;
                //#endif
                //((CollectionView)sender).SelectedItem = null;

            }
        }


        //player

        public async void StartBook()
        {
            if (AudioBook.Cover is not null)
            {
                Task.Run(() =>
                {
                    var col = Blend(Color.FromArgb(GetDominantColor(AudioBook.Cover)), Color.FromHex("131313"), 0.3);
                    PlayerMenu.Dispatcher.Dispatch(() =>
                    {
                        PlayerMenu.BackgroundColor = col;
                    });

                });
            }
            BookCover.Source = AudioBook.Cover;
            //playlistPicker.ItemsSource = AudioBook.Playlist;
            //playlistPicker.SelectedIndex = AudioBook.MarkIndex;
            //await Player2.InitializeAsync(AudioBook.Playlist[AudioBook.MarkIndex]);
            await Player2.InitializeAsync(new MediaPlay { URL = AudioBook.Playlist[AudioBook.MarkIndex], Author = AudioBook.Author, Name = AudioBook.Title, Image = AudioBook.Cover });
            await Player2.PlayAsync();
            await Player2.SetCurrentTime(AudioBook.MarkTime);
            SetPauseiImg();
            if (!AudioBook.isM4b)
            {
                PositionSlider.Maximum = Player2.Duration;
                if (AudioBook.isYT)
                    AudioBook.DurationSec = Player2.Duration;
            }
            Player2.Speed = Speed;
            spt.Text = Speed.ToString();
            AudioBook.State = Book._State.Started;
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
                AudioBook.Save(login);
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
                    AudioBook.Save(login);
                    await Player2.InitializeAsync(new MediaPlay { URL = AudioBook.Playlist[AudioBook.MarkIndex], Author = AudioBook.Author, Name = AudioBook.Title, Image = AudioBook.Cover });
                    //PositionSlider.Maximum = Player.Duration.TotalSeconds;
                    //allowpick = false;
                    PlayerMenu.Dispatcher.Dispatch(() =>
                    {
                        ContentView.SelectedItem = AudioBook.Chapters[AudioBook.MarkIndex];
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
                    AudioBook.Save(login);
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
            if (!AudioBook.isM4b)
                await Player2.SetCurrentTime(((Slider)sender).Value);
            else
                await Player2.SetCurrentTime(((Chapter)ContentView.SelectedItem).Time + ((Slider)sender).Value);
            await Player2.PlayAsync();
            SetPauseiImg();
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

        /*private async void playlistPicker_SelectedIndexChanged(object sender, EventArgs e)
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
                        var bf = new Track(item);
                        if (j < AudioBook.MarkIndex)
                            AudioBook.ListenedSec += bf.Duration;
                        else
                            break;
                        j++;
                    }
                });
            }
        }*/

        void SetPauseiImg()
        {
            PlayBtn.Source = "pause.png";
            PlaySmall.Source = "smallpause.png";
        }
        void SetPlayImg()
        {
            PlayBtn.Source = "play.png";
            PlaySmall.Source = "smallplay.png";
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
                AudioBook.Save(login);
                SetPlayImg();
            }
            else
            {
                await Player2.PlayAsync();
                timer.Start();
                SetPauseiImg();
            }

            //isPaused = !isPaused;
            //var x = Player.CurrentState;

            // Change the text of the button to the corresponding symbol
            //PlayBtn.Text = isPaused ? "⏸️" : "▶️";

            // Animate the button to shrink and then return to normal size
            ((ImageButton)sender).ScaleTo(0.9, 100, Easing.SinIn).ContinueWith((t) => ((ImageButton)sender).ScaleTo(1, 70, Easing.SinOut));
            //PlaySmall.ScaleTo(0.9, 100, Easing.SinIn).ContinueWith((t) => PlaySmall.ScaleTo(1, 70, Easing.SinOut));
        }

        private async void ForwardBtn_Clicked(object sender, EventArgs e)
        {
            await Player2.SetCurrentTime(Player2.CurrentPosition + 30 * Speed);
            newSec();
            ForwardBtn.RotateTo(15, 100, Easing.Linear).ContinueWith((t) => ForwardBtn.RotateTo(0, 70, Easing.Linear));
        }

        private async void BackBtn_Clicked(object sender, EventArgs e)
        {
            await Player2.SetCurrentTime(Player2.CurrentPosition - 10 * Speed);
            newSec();
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
            statusBar.StatusBarColor = BackgroundColor;
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
            for (int x = 0; x < bmp.Width; x += bmp.Width / 15)
            {
                for (int y = 0; y < bmp.Height; y += bmp.Height / 15)
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
            //PositionSlider.Value = Player2.CurrentPosition;
            if (!AudioBook.isM4b)
                PositionSlider.Value = Player2.CurrentPosition;
            else
            {
                if (ContentView.SelectedItem == null || startTime > Player2.CurrentPosition || endTime <= Player2.CurrentPosition)
                {
                    for (int i = 0; i < AudioBook.Chapters.Count; i++)
                    {
                        if (Player2.CurrentPosition >= AudioBook.Chapters[i].Time && (i == AudioBook.Chapters.Count - 1 || Player2.CurrentPosition < AudioBook.Chapters[i + 1].Time - 1))
                        {
                            if (ContentView.SelectedItem != AudioBook.Chapters[i])
                            {
                                allowpick = false;
                                ContentView.SelectedItem = AudioBook.Chapters[i];
                                allowpick = true;
                            }
                            startTime = AudioBook.Chapters[i].Time;
                            if (i < AudioBook.Chapters.Count - 1)
                                endTime = AudioBook.Chapters[i + 1].Time;
                            else
                                endTime = Player2.Duration;
                            PositionSlider.Maximum = endTime - startTime;
                            break;
                        }
                    }
                }
                if (ContentView.SelectedItem != null)
                    PositionSlider.Value = Player2.CurrentPosition - ((Chapter)ContentView.SelectedItem).Time;
            }
        }

        private async void Button_Clicked_2(object sender, EventArgs e)
        {
            if (BookCover.IsVisible)
            {
                BookCover.IsVisible = false;
                ContentView.IsVisible = true;
                ChapterName.IsVisible = false;
                ContentView.ScrollTo(ContentView.SelectedItem, ScrollToPosition.MakeVisible, false);
            }
            else
            {
                BookCover.IsVisible = true;
                ContentView.IsVisible = false;
                ChapterName.IsVisible = true;
            }
            MenuBtn.ScaleTo(0.9, 100, Easing.SinIn).ContinueWith((t) => MenuBtn.ScaleTo(1, 70, Easing.SinOut));
        }

        private async void ContentView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (PlayerMenu.IsVisible && allowpick && ContentView.SelectedItem != null)
            {
                if (!AudioBook.isM4b)
                {
                    AudioBook.MarkIndex = ((Chapter)ContentView.SelectedItem).Id;
                    await Player2.InitializeAsync(new MediaPlay { URL = AudioBook.Playlist[AudioBook.MarkIndex], Author = AudioBook.Author, Name = AudioBook.Title, Image = AudioBook.Cover });
                    await Player2.PlayAsync();
                    SetPauseiImg();
                    Player2.Speed = Speed;
                    PositionSlider.Maximum = Player2.Duration;
                    timer.Start();
                    Task.Run(() =>
                    {
                        AudioBook.ListenedSec = 0;
                        int j = 0;
                        foreach (var item in AudioBook.Playlist)
                        {
                            var bf = new Track(item);
                            if (j < AudioBook.MarkIndex)
                                AudioBook.ListenedSec += bf.Duration;
                            else
                                break;
                            j++;
                        }
                    });
                }
                else
                {
                    //if (((Chapter)ContentView.SelectedItem).Id == AudioBook.Chapters.Count - 1)
                    //    PositionSlider.Maximum = Player2.Duration - ((Chapter)ContentView.SelectedItem).Time;
                    //else
                    //    PositionSlider.Maximum = AudioBook.Chapters[((Chapter)ContentView.SelectedItem).Id + 1].Time - ((Chapter)ContentView.SelectedItem).Time;
                    await Player2.SetCurrentTime(((Chapter)ContentView.SelectedItem).Time);
                    await Player2.PlayAsync();
                    SetPauseiImg();
                    timer.Start();
                }
            }
        }

        private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            PlayerMenu.TranslationY = Window.Height;
            PlayerMenu.IsVisible = true;
#if ANDROID
            if (BookCover.IsVisible)
            {
                Dispatcher.Dispatch(() =>
                {
                    BookCover.IsVisible = false;
                    BookCover.IsVisible = true;
                });
            }
#endif
            if (Player2.IsPlaying)
                timer.Start();
            await PlayerMenu.TranslateTo(0, 0, 250, Easing.CubicInOut);
            statusBar.StatusBarColor = PlayerMenu.BackgroundColor;
#if ANDROID
            Menu.IsVisible = false;
#endif
        }

        private async void Button_Clicked_3(object sender, EventArgs e)
        {
            string MainFolder = await SecureStorage.Default.GetAsync("FolderPath");
            GetBooks(MainFolder);
        }

        private async void Button_Clicked_4(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddBook(await SecureStorage.Default.GetAsync("FolderPath")));
        }

        private async void Button_Clicked_5(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Account());
        }
    }
}
