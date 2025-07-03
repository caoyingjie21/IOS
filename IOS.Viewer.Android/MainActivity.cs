using Android.App;
using Android.Content.PM;
using Android.Content;
using Android.Net;

using Avalonia;
using Avalonia.Android;

namespace IOS.Viewer.Android;

[Activity(
    Label = "IOS.Viewer.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    private static MainActivity? _instance;
    
    protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _instance = this;
    }
    
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
    
    /// <summary>
    /// 打开文件管理器到指定路径
    /// </summary>
    public static void OpenFileManager(string path)
    {
        try
        {
            var context = _instance ?? global::Android.App.Application.Context;
            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(Uri.Parse($"file://{path}"), "*/*");
            intent.AddFlags(ActivityFlags.NewTask);
            
            try
            {
                context.StartActivity(intent);
            }
            catch
            {
                // 如果无法直接打开文件夹，尝试打开系统文件管理器
                try
                {
                    var fileManagerIntent = new Intent(Intent.ActionMain);
                    fileManagerIntent.AddCategory(Intent.CategoryLauncher);
                    fileManagerIntent.SetPackage("com.android.documentsui");
                    fileManagerIntent.AddFlags(ActivityFlags.NewTask);
                    context.StartActivity(fileManagerIntent);
                }
                catch
                {
                    // 最后尝试：打开通用文件选择器
                    var genericIntent = new Intent(Intent.ActionGetContent);
                    genericIntent.SetType("*/*");
                    genericIntent.AddFlags(ActivityFlags.NewTask);
                    context.StartActivity(Intent.CreateChooser(genericIntent, "选择文件管理器"));
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"打开文件管理器失败: {ex.Message}");
        }
    }
}
