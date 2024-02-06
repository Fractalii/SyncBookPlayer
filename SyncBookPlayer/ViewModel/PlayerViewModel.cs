using CommunityToolkit.Mvvm.ComponentModel;
using SyncBookPlayer.Model;

namespace SyncBookPlayer.ViewModel
{
    [QueryProperty("Cover", "Cover")]
    public partial class PlayerViewModel : ObservableObject
    {
        [ObservableProperty]
        double toListen;
        [ObservableProperty]
        int percent;
        [ObservableProperty]
        int listened;
        //[ObservableProperty]
        //string cover;
    }
}
