using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.IO;

namespace BiblePointApp
{
    public partial class App : Application
    {
        // 앱 전체에서 DI 컨테이너에 접근할 수 있도록 보관합니다.
        public static IServiceProvider Services { get; private set; } = null!;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            Services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // 기본 테마
            Application.Current.UserAppTheme = AppTheme.Light;

            // AppShell을 DI에서 resolve 하거나, 실패시 새로 생성합니다.
            var shell = Services.GetService(typeof(AppShell)) as AppShell;
            MainPage = shell ?? new AppShell();
        }
    }
}