using ManagedCommon;
using System.Windows;
using System.Windows.Input;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.TimeZone;

public class Main : IPlugin, IContextMenu, IDisposable
{
    public static string PluginID => "A646217CEE134F3185E5AB8FEE7B91EA";

    public string Name => "Time Zone Converter";

    public string Description => "Convert a time to a requested timezone or UTC offset.";

    private PluginInitContext? Context { get; set; }

    private string IconPath { get; set; } = "Images/timezone.dark.png";

    private bool Disposed { get; set; }

    public List<Result> Query(Query query)
    {
        var search = query.Search.Trim();
        return TimeZoneConverter.Convert(search).Select(conversion => new Result
        {
            QueryTextDisplay = search,
            IcoPath = IconPath,
            Title = conversion.Title,
            SubTitle = conversion.Subtitle,
            ToolTipData = new ToolTipData(Name, conversion.Subtitle),
            Action = _ =>
            {
                if (!conversion.Success)
                {
                    return false;
                }

                Clipboard.SetDataObject(conversion.ClipboardText);
                return true;
            },
            ContextData = conversion,
            Score = conversion.Success ? 100 : 10,
        }).ToList();
    }

    public void Init(PluginInitContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Context.API.ThemeChanged += OnThemeChanged;
        UpdateIconPath(Context.API.GetCurrentTheme());
    }

    public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
    {
        if (selectedResult.ContextData is not ConversionResult { Success: true } conversion)
        {
            return [];
        }

        return
        [
            new ContextMenuResult
            {
                PluginName = Name,
                Title = "Copy converted time (Ctrl+C)",
                Glyph = "\xE8C8",
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                AcceleratorKey = Key.C,
                AcceleratorModifiers = ModifierKeys.Control,
                Action = _ =>
                {
                    Clipboard.SetDataObject(conversion.ClipboardText);
                    return true;
                },
            }
        ];
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed || !disposing)
        {
            return;
        }

        if (Context?.API != null)
        {
            Context.API.ThemeChanged -= OnThemeChanged;
        }

        Disposed = true;
    }

    private void UpdateIconPath(Theme theme)
    {
        IconPath = theme == Theme.Light || theme == Theme.HighContrastWhite
            ? "Images/timezone.light.png"
            : "Images/timezone.dark.png";
    }

    private void OnThemeChanged(Theme currentTheme, Theme newTheme)
    {
        UpdateIconPath(newTheme);
    }
}
