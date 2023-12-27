using CommunityToolkit.Mvvm.ComponentModel;

namespace SyncBookPlayer.ViewModel
{
    public partial class PlayerViewModel : ObservableObject
    {
        [ObservableProperty]
        double toListen;
    }
}
