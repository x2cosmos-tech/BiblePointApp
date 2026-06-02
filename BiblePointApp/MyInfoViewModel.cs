using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Windows.Input;
using System.Threading.Tasks;

namespace BiblePointApp.ViewModels
{
    public class MyInfoViewModel : BaseViewModel
    {
        public string PointDisplay => $"{PointService.GetCurrentPoints():N0} P";
        public string RankName => PointService.GetRankName();
        public string RankImage => PointService.GetRankImage();
        public string ProgressText => PointService.GetNextLevelProgress();
        public string UserName => Preferences.Default.Get("UserName", "성도");

        public ICommand EditNameCommand { get; }

        public MyInfoViewModel()
        {
            EditNameCommand = new Command(async () => await EditNameAsync());
        }

        public async Task EditNameAsync()
        {
            string result = await Application.Current.MainPage.DisplayPromptAsync("이름 변경", "새로운 이름을 입력해주세요.", "저장", "취소", "이름");
            if (!string.IsNullOrWhiteSpace(result))
            {
                Preferences.Default.Set("UserName", result);
                Refresh();
            }
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(PointDisplay));
            OnPropertyChanged(nameof(RankName));
            OnPropertyChanged(nameof(RankImage));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(UserName));
        }
    }
}