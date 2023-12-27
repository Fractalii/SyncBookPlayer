using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics.Platform;
using Npgsql;
using SyncBookPlayer.Model;
using SyncBookPlayer.View;
using System.Drawing;
using System.Formats.Tar;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SyncBookPlayer
{
    public partial class MainPage : ContentPage
    {
        List<Book> library = new List<Book>();

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


            GetFOlder();
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
                string connString = "Server=ep-falling-cake-416088.eu-central-1.aws.neon.tech;Username=DAROMON;Database=neondb;Port=5432;Password=NVgYsqK8hyP6;SSLMode=Prefer;Timeout=150";
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

                if (isBook(folder))
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


                    book.Playlist = Directory.GetFiles(folder).Where(s => s.ToLower().EndsWith(".mp3") || s.ToLower().EndsWith(".wav") || s.ToLower().EndsWith(".m4a") || s.ToLower().EndsWith(".m4b") || s.ToLower().EndsWith(".mp4") || s.ToLower().EndsWith(".mkv") || s.ToLower().EndsWith(".ogg") || s.ToLower().EndsWith(".webm") || s.ToLower().EndsWith(".wma") || s.ToLower().EndsWith(".mp2") || s.ToLower().EndsWith(".aac") || s.ToLower().EndsWith(".flac")).ToList();
                    book.Playlist.Sort();

                    var bookFile = TagLib.File.Create(book.Playlist[0]);
                    book.Title = bookFile.Tag.Album;

                    if (book.Title is null)
                        book.Title = Path.GetFileName(folder);
                    book.Author = bookFile.Tag.FirstPerformer;
                    book.Narrator = bookFile.Tag.FirstAlbumArtist;
                    int j = 0;
                    foreach (var item in book.Playlist)
                    {
                        var bf = TagLib.File.Create(item);
                        book.DurationSec += bf.Properties.Duration.TotalSeconds;
                        if (j < book.MarkIndex)
                            book.ListenedSec += bf.Properties.Duration.TotalSeconds;
                        j++;
                    }
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
            var x = Directory.EnumerateFiles(folder);
            var files = Directory.GetFiles(folder).Where(s => s.ToLower().EndsWith(".mp3") || s.ToLower().EndsWith(".wav") || s.ToLower().EndsWith(".m4a") || s.ToLower().EndsWith(".m4b") || s.ToLower().EndsWith(".mp4") || s.ToLower().EndsWith(".mkv") || s.ToLower().EndsWith(".ogg") || s.ToLower().EndsWith(".webm") || s.ToLower().EndsWith(".wma") || s.ToLower().EndsWith(".mp2") || s.ToLower().EndsWith(".aac") || s.ToLower().EndsWith(".flac")).ToArray();
            if (files.Length > 0)
                return true;
            
            return false;
        }

        private async void libraryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((CollectionView)sender).SelectedItem != null) { 
            
                await Shell.Current.GoToAsync(nameof(BookPlayer), true,
                new Dictionary<string, object>
                {
                    {"AudioBook",((CollectionView)sender).SelectedItem}
                });
                ((CollectionView)sender).SelectedItem = null;
            }   
        }


    }
}
