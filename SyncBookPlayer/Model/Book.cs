using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        public List<Chapter> Chapters { get; set; }
        public enum _State
        {
            NotStarted,
            Started,
            Finished
        }
        public _State State { get; set; }

        public DateTime SaveTime { get; set; }

        public async void Save()
        {
            //var app = Application.Current as App;
            //if (app.LastConnection + TimeSpan.FromSeconds(10) < DateTime.UtcNow)
            if (SaveTime + TimeSpan.FromSeconds(10) < DateTime.UtcNow)
            {
                SaveTime = DateTime.UtcNow;
                var data = JsonSerializer.Serialize(this);

                SaveLocal(data);
                await Task.Run(() => SaveSync(data));
            }


            /*
             Server=ep-falling-cake-416088.eu-central-1.aws.neon.tech;Database=neondb;User Id=DAROMON;Password=NVgYsqK8hyP6;Port=5432 
             string connectionString = "postgresql://DAROMON@ep-falling-cake-416088.eu-central-1.aws.neon.tech/neondb?sslmode=require";
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
        public async void SaveSync(string data)
        {
            if (MarkIndex > 0 || MarkTime > 100)
            {
                try
                {
                    string connString = "Server=ep-falling-cake-416088.eu-central-1.aws.neon.tech;Username=DAROMON;Database=neondb;Port=5432;Password=NVgYsqK8hyP6;SSLMode=Prefer";
                    using (var conn = new NpgsqlConnection(connString))
                    {
                        //Console.Out.WriteLine("Opening connection");
                        conn.Open();

                        using (var command = new NpgsqlCommand("INSERT INTO fractalis (folder_name, json_data) VALUES (@folder_name, @json_data) ON CONFLICT (folder_name) DO UPDATE SET json_data = EXCLUDED.json_data", conn))
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
        }

    }
}
