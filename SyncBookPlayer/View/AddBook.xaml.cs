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

    public AddBook(string fld)
	{
		InitializeComponent();
        MainFolder = fld;
	}

    private async void Button_Clicked(object sender, EventArgs e)
    {
        if (!Uri.TryCreate(vidurl.Text, UriKind.Absolute, out _))
        {
            await DisplayAlert("Invalid URL", "Please enter a valid url", "OK");
            return;
        }
        try
        {
            YoutubeClient youtubeClient = new YoutubeClient();
            video = await youtubeClient.Videos.GetAsync(vidurl.Text);
            title.Text = video.Title;
            biggestThumbnail = video.Thumbnails.GetWithHighestResolution();

            preview.Source = ImageSource.FromUri(new Uri(biggestThumbnail.Url));

        }
        catch (YoutubeReExplode.Exceptions.RequestLimitExceededException)
        {
            await DisplayAlert("Error", "You have exceeded the youtube rate limit. Possible causes:\n\n1) Using this app too much\n2) Using a VPN", "OK");
        }
        catch (System.Net.Http.HttpRequestException)
        {
            await DisplayAlert("Error", "Please check your internet connection.", "OK");
        }
        catch (System.ArgumentException)
        {
            await DisplayAlert("Error", "Your entered URL was not a Youtube video.", "OK");
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", "We couldn't open the URL", "OK");
            Console.WriteLine(exception);
        }
    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        if (!Directory.Exists(Path.Combine(MainFolder, video.Title)))
        {
            await Task.Run(async () =>
            {
                Directory.CreateDirectory(Path.Combine(MainFolder, video.Title));
                var youtube = new YoutubeClient();
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Url);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                var stream = await youtube.Videos.Streams.GetAsync(streamInfo);
                using (WebClient client = new WebClient())
                {
                    client.DownloadFileAsync(new Uri(biggestThumbnail.Url), Path.Combine(MainFolder, video.Title, "cover.jpg"));
                }
                IProgress<double> progress = new Progress<double>(percentage =>
                {
                    MainThread.BeginInvokeOnMainThread(() => downloadprog.Progress = percentage);
                });
                await youtube.Videos.Streams.DownloadAsync(streamInfo, Path.Combine(MainFolder, video.Title, "youtube_video.webm"), progress);
            });
            await DisplayAlert("Готово", "Видео скачано. Обновите главную страницу, чтобы увидеть его в списке книг.", "OK");
        }
        else
        {
            await DisplayAlert("Ошибка", "Это видео уже скачано. Удалите папку, если хотите скачать заново.", "OK");
        }
    }
}