using AngleSharp.Io;
using Microsoft.Maui.Storage;
using System.Net;
using YoutubeReExplode;
using YoutubeReExplode.Common;
using YoutubeReExplode.Videos.Streams;

namespace SyncBookPlayer.View;

public partial class AddBook : ContentPage
{
    YoutubeReExplode.Videos.Video video;
    string MainFolder;
    Thumbnail biggestThumbnail;
    bool download = false;

    public AddBook(string fld)
	{
		InitializeComponent();
        MainFolder = fld;
	}

    private async void Button_Clicked(object sender, EventArgs e)
    {
        vidurl.IsEnabled = false;
        vidurl.IsEnabled = true;
        if (!Uri.TryCreate(vidurl.Text, UriKind.Absolute, out _))
        {
            await DisplayAlert("Ошибка", "Некорректная ссылка.", "OK");
            return;
        }
        try
        {
            YoutubeClient youtubeClient = new YoutubeClient();
            video = await youtubeClient.Videos.GetAsync(vidurl.Text);
            title.Text = video.Title;
            biggestThumbnail = video.Thumbnails.GetWithHighestResolution();

            preview.Source = ImageSource.FromUri(new Uri(biggestThumbnail.Url));
            downbut.IsEnabled = true;

        }
        catch (YoutubeReExplode.Exceptions.RequestLimitExceededException)
        {
            downbut.IsEnabled = false;
            await DisplayAlert("Ошибка", "Вы превысили лимит скачивания.", "OK");
        }
        catch (System.Net.Http.HttpRequestException)
        {
            downbut.IsEnabled = false;
            await DisplayAlert("Ошибка", "Проверьте подключение к интернету.", "OK");
        }
        catch (System.ArgumentException)
        {
            downbut.IsEnabled = false;
            await DisplayAlert("Ошибка", "Введенная ссылка не относится к YouTube-видео.", "OK");
        }
        catch (Exception exception)
        {
            downbut.IsEnabled = false;
            await DisplayAlert("Ошибка", "Не удалось открыть ссылку.", "OK");
            //Console.WriteLine(exception);
        }
    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        if (!Directory.Exists(Path.Combine(MainFolder, string.Concat(video.Title.Split(Path.GetInvalidFileNameChars())))))
        {
            preview.IsVisible = false;
            findbut.IsEnabled = false;
            downbut.IsEnabled = false;
            download = true;
            await Task.Run(async () =>
            {
                string dir = string.Concat(video.Title.Split(Path.GetInvalidFileNameChars())).Replace("|", "");
                Directory.CreateDirectory(Path.Combine(MainFolder, dir));
                var youtube = new YoutubeClient();
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Url);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                var stream = await youtube.Videos.Streams.GetAsync(streamInfo);
                using (WebClient client = new WebClient())
                {
                    client.DownloadFileAsync(new Uri(biggestThumbnail.Url), Path.Combine(MainFolder, dir, "cover.jpg"));
                }
                IProgress<double> progress = new Progress<double>(percentage =>
                {
                    MainThread.BeginInvokeOnMainThread(() => downloadprog.Progress = percentage);
                });
                await youtube.Videos.Streams.DownloadAsync(streamInfo, Path.Combine(MainFolder, dir, "youtube_video.webm"), progress);
            });
            await DisplayAlert("Готово", "Видео скачано. Обновите главную страницу, чтобы увидеть его в списке книг.", "OK");
            download = false;
        }
        else
        {
            await DisplayAlert("Ошибка", "Это видео уже скачано. Удалите папку, если хотите скачать заново.", "OK");
        }
        findbut.IsEnabled = true;
    }
    protected override bool OnBackButtonPressed()
    {
        if (download) { 
            DisplayAlert("Подождите!", "Видео еще скачивается. Пожалуйтса, не закрывайте страницу.", "OK");
            return true;
        }
        else
            return base.OnBackButtonPressed();
    }
}