using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Npgsql;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SyncBookPlayer.Model
{
    public class Book
    {
        [JsonIgnore]
        public string Title { get; set; }
        [JsonIgnore]
        public string Cover { get; set; }
        [JsonIgnore]
        public string Author { get; set; }
        [JsonIgnore]
        public string Narrator { get; set; }
        [JsonIgnore]
        public List<string> Playlist { get; set; }
        public int MarkIndex { get; set; }
        public int MarkTime { get; set; }
        public double ListenedSec { get; set; }
        [JsonIgnore]
        public string Folder { get; set; }
        public double Speed { get; set; }
        [JsonIgnore]
        public double DurationSec { get; set; }
        public bool isM4b = false;
        public bool isYT = false;
        [JsonIgnore]
        public bool Downloaded { get; set; } = false;
        [JsonIgnore]
        public List<Chapter> Chapters { get; set; }
        public enum _State
        {
            NotStarted,
            Started,
            Finished
        }
        public _State State { get; set; }

        public DateTime SaveTime { get; set; }

        public async void Save(string login = null)
        {
            //var app = Application.Current as App;
            //if (app.LastConnection + TimeSpan.FromSeconds(10) < DateTime.UtcNow)
            if (SaveTime + TimeSpan.FromSeconds(10) < DateTime.UtcNow)
            {
                SaveTime = DateTime.UtcNow;
                var data = JsonSerializer.Serialize(this);

                SaveLocal(data);
                if (login != null)
                    await Task.Run(() => SaveSync(data, login));
            }


            /*
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"INSERT INTO neyavki VALUES ('test4')";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }*/
        }
        public async void SaveLocal(string data)
        {
            string filename = Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(Folder) + ".json");
            File.WriteAllText(filename, data);
        }
        public async void SaveSync(string data, string login)
        {
            if (MarkIndex > 0 || MarkTime > 100)
            {
                try
                {
                    string connString = Settings.database_connection + "SSLMode=Prefer";
                    using (var conn = new NpgsqlConnection(connString))
                    {
                        //Console.Out.WriteLine("Opening connection");
                        conn.Open();

                        using (var command = new NpgsqlCommand($"INSERT INTO books (folder_name, json_data, account_id) VALUES (@folder_name, @json_data, {login}) ON CONFLICT (folder_name, account_id) DO UPDATE SET json_data = EXCLUDED.json_data", conn))
                        {
                            command.Parameters.AddWithValue("@folder_name", this.Folder);
                            command.Parameters.AddWithValue("@json_data", data);
                            await command.ExecuteNonQueryAsync();
                            //Console.Out.WriteLine("Finished dropping table (if existed)");
                        }
                        conn.Close();
                        //SaveTime = DateTime.UtcNow;
                        //await Toast.Make("Книга сохранена", ToastDuration.Short).Show();
                    }
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Dispatch(() =>
                    {
                        Toast.Make(ex.Message, ToastDuration.Short).Show();
                    });
                    SaveTime = DateTime.UtcNow - TimeSpan.FromSeconds(9);
                }
            }
        }
        public void LoadData(Book book)
        {
            MarkIndex = book.MarkIndex;
            MarkTime = book.MarkTime;
            State = book.State;
            Speed = book.Speed;
            SaveTime = book.SaveTime;
        }

    }
}
