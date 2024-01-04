//using Microsoft.UI;
using SyncBookPlayer.View;
//using Windows.Graphics;

namespace SyncBookPlayer
{
    public partial class App : Application
    {
        //public string SharedData { get; set; }
        //public DateTime LastConnection { get; set; }
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
            //MainPage = new NavigationPage(new MainPage());
            //Application.Current.MainPage = new MainPage();
        }
        protected override Window CreateWindow(IActivationState activationState)
        {
            Window window = base.CreateWindow(activationState);
            var timer = window.Dispatcher.CreateTimer();
#if WINDOWS
            window.Height = 700;
            window.Width = 600;
#endif
            window.Resumed += (s, e) =>
            {
#if ANDROID
                timer.Stop();
#endif
            };

            /*window.Deactivated += (s, e) =>
            {

            };
            window.Destroying += (s, e) =>
            {
                //MainPage.Sunc()
                //int y = 3;
                //SharedData = "2";

                var currentPage = Shell.Current.CurrentPage as BookPlayer;
                if (currentPage != null)
                {
                    currentPage.ExitSave();
                }

                //var mainPage = Current.MainPage as MainPage;

                //mainPage.Sync();
            };*/
            window.Stopped += (s, e) =>
            {
#if ANDROID
                var currentPage = Shell.Current.CurrentPage as MainPage;
                if (currentPage != null)
                {
                    if (currentPage.isPlaying)
                    {
                        currentPage.Closing();
                        
                        timer.Interval = TimeSpan.FromSeconds(15);
                        timer.Tick += (s, e) => Saving();
                        timer.Start();
                    }
                        
                }
#endif
                //MainPage.Sunc()
                //int y = 3;
                //var mainPage = Application.Current.MainPage as MainPage;
                //mainPage.Sync();
            };

            return window;
        }

        void Saving()
        {
            var currentPage = Shell.Current.CurrentPage as MainPage;
            if (currentPage != null)
            {
                if (currentPage.isPlaying)
                {
                    currentPage.Closing();
                }
            }
        }

    }
}
