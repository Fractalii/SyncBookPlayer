using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using SyncBookPlayer.Extra;
using SyncBookPlayer.Model;
using SyncBookPlayer.ViewModel;
namespace SyncBookPlayer.View;

[QueryProperty("AudioBook", "AudioBook")]
public partial class BookPlayer : ContentPage
{
	public Book AudioBook { get; set; }
    double Speed { get { return AudioBook.Speed; } set { AudioBook.Speed = value; Player.Speed = value; } }
    PlayerViewModel BindingManager;
    //public string ab = " fsdgsd";

    public BookPlayer(PlayerViewModel pv)
	{
		InitializeComponent();
        BindingManager = pv;
        BindingContext = BindingManager;
        //pv.Titler = "бан те";
        //timer1 = new Timer();
    }
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        //BindingManager.Picturka = AudioBook.Cover;
        base.OnNavigatedTo(args);
        //BookCover.Source = AudioBook.Cover;
        Player.Source = AudioBook.Playlist[AudioBook.MarkIndex];
        Player.SeekTo(TimeSpan.FromSeconds(AudioBook.MarkTime));
        //AudioBook.Author = "hfff";
        //Player.Play();
        playlistPicker.ItemsSource = AudioBook.Playlist;
        playlistPicker.SelectedIndex = AudioBook.MarkIndex;
        //if (AudioBook.Speed < 1)
        //    Speed = 1;
        AudioBook.State = Book._State.Started;
        //PositionSlider.Maximum = Player.Duration.TotalSeconds;
        //secret.Text = AudioBook.Playlist[AudioBook.MarkTime];

        /*Task.Run(() =>
        {
            Thread.Sleep(300);
            App.Current.Dispatcher.Dispatch(() =>
            {
                //PositionSlider.Maximum = Player.Duration.TotalSeconds;
            });
        });*/

        /*Binding vvv = new Binding();
        vvv.Source = ab;
        lox.SetBinding(Label.TextProperty, vvv);*/
    }
    protected override void OnDisappearing()
    {
        Player.Pause();
        if (Convert.ToInt32(Player.Position.TotalSeconds) > 2 )
            AudioBook.MarkTime = Convert.ToInt32(Player.Position.TotalSeconds) - 2;
        else
            AudioBook.MarkTime = 0;
        //Player.Stop();
        AudioBook.Save();
        base.OnDisappearing();
        //Console.WriteLine("сдох2");
    }

    public void ExitSave()
    {
        if (Convert.ToInt32(Player.Position.TotalSeconds) > 2)
            AudioBook.MarkTime = Convert.ToInt32(Player.Position.TotalSeconds) - 2;
        AudioBook.Save();
    }

    public void Player_MediaEnded(object? sender, EventArgs e)
    {
        if (AudioBook.Playlist.Count > AudioBook.MarkIndex + 1) {
            AudioBook.MarkIndex++;
            AudioBook.MarkTime = 0;
            AudioBook.ListenedSec += Player.Duration.TotalSeconds;
            AudioBook.Save();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Player.Source = AudioBook.Playlist[AudioBook.MarkIndex];
                //PositionSlider.Maximum = Player.Duration.TotalSeconds;
                playlistPicker.SelectedIndex = AudioBook.MarkIndex;
                Player.Play();
            });
        }
        else
        {
            AudioBook.State = Book._State.Finished;
        }
        
        
        //secret.Text = AudioBook.Playlist[AudioBook.MarkIndex];
    }

    void OnSpeedMinusClicked(object? sender, EventArgs e)
    {
        if (Player.Speed >= 0.5)
        {
            Speed -= 0.25;
        }
    }

    void OnSpeedPlusClicked(object? sender, EventArgs e)
    {
        if (Player.Speed < 10)
        {
            Speed += 0.25;
        }
    }

    async void Slider_DragCompleted(object? sender, EventArgs e)
    {
        ArgumentNullException.ThrowIfNull(sender);

        var newValue = ((Slider)sender).Value;
        Player.SeekTo(TimeSpan.FromSeconds(newValue));

        Player.Play();
        PlayBtn.Source = "pause.png";
        //var x = PositionSlider.Value;
    }

    void Slider_DragStarted(object sender, EventArgs e)
    {
        Player.Pause();
    }
    //windows приколы
    private void Player_MediaOpened(object sender, EventArgs e)
    {
        //Player.Pause();
        Player.Speed = Speed;
        Player.Play();
        //Player.Position = TimeSpan.FromSeconds(AudioBook.MarkTime)
        //PositionSlider.Value = Player.Position.TotalSeconds;
        //PositionSlider.Maximum = Player.Duration.TotalSeconds;
        //int x = 4;
    }

    private void playlistPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (AudioBook.MarkIndex != playlistPicker.SelectedIndex)
        {
            AudioBook.MarkIndex = playlistPicker.SelectedIndex;
            Player.Source = AudioBook.Playlist[AudioBook.MarkIndex];
        }
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        var hg = PositionSlider.Value;
        if (Player.CurrentState == MediaElementState.Playing)
        {
            Player.Pause();
            AudioBook.Save();
            PlayBtn.Source = "play.png";
        }
        else
        {
            Player.Play();
            PlayBtn.Source = "pause.png";
        }

        //isPaused = !isPaused;
        //var x = Player.CurrentState;

        // Change the text of the button to the corresponding symbol
        //PlayBtn.Text = isPaused ? "⏸️" : "▶️";

        // Animate the button to shrink and then return to normal size
        PlayBtn.ScaleTo(0.9, 100, Easing.SinIn).ContinueWith((t) => PlayBtn.ScaleTo(1, 70, Easing.SinOut));
    }

    private void ForwardBtn_Clicked(object sender, EventArgs e)
    {
        ForwardBtn.RotateTo(15, 100, Easing.Linear).ContinueWith((t) => ForwardBtn.RotateTo(0, 70, Easing.Linear));
        Player.SeekTo(Player.Position + TimeSpan.FromSeconds(30));
    }

    private void BackBtn_Clicked(object sender, EventArgs e)
    {
        BackBtn.RotateTo(-15, 100, Easing.Linear).ContinueWith((t) => BackBtn.RotateTo(0, 70, Easing.Linear));
        Player.SeekTo(Player.Position - TimeSpan.FromSeconds(15));
    }

    private void Player_PositionChanged(object sender, MediaPositionChangedEventArgs e)
    {
        //beforeSec.Text = "-" + (string)countdown.Convert((AudioBook.DurationSec - (AudioBook.ListenedSec + Player.Position.TotalSeconds))/Player.Speed, null, null, null);
        BindingManager.ToListen = (AudioBook.DurationSec - (AudioBook.ListenedSec + Player.Position.TotalSeconds)) / Player.Speed;
        //this.logger.LogInformation("еба");
    }
}