using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace RegexTextEditor.Services
{
    public class WindowAppearanceService
    {
        private MicaBackdrop? _micaBackdrop;
        private AppWindow? _appWindow;

        public void SetupMica(Window window)
        {
            try
            {
                _micaBackdrop = new MicaBackdrop
                {
                    Kind = MicaKind.Base
                };

                window.SystemBackdrop = _micaBackdrop;
            }
            catch
            {
            }
        }

        public void SetupCustomTitleBar(
            Window window,
            UIElement dragRegion,
            FrameworkElement appTitleBar,
            ColumnDefinition leftInsetColumn,
            ColumnDefinition rightInsetColumn)
        {
            try
            {
                _appWindow = window.AppWindow;

                window.ExtendsContentIntoTitleBar = true;
                window.SetTitleBar(dragRegion);

                if (AppWindowTitleBar.IsCustomizationSupported())
                {
                    _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                    _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    _appWindow.TitleBar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(32, 255, 255, 255);
                    _appWindow.TitleBar.ButtonPressedBackgroundColor = ColorHelper.FromArgb(56, 255, 255, 255);

                    _appWindow.TitleBar.ButtonForegroundColor = Colors.White;
                    _appWindow.TitleBar.ButtonInactiveForegroundColor = ColorHelper.FromArgb(140, 255, 255, 255);
                    _appWindow.TitleBar.ButtonHoverForegroundColor = Colors.White;
                    _appWindow.TitleBar.ButtonPressedForegroundColor = Colors.White;
                }

                void UpdateInsets()
                {
                    try
                    {
                        if (_appWindow is null || appTitleBar.XamlRoot is null)
                            return;

                        double scale = appTitleBar.XamlRoot.RasterizationScale;

                        leftInsetColumn.Width = new GridLength(_appWindow.TitleBar.LeftInset / scale);
                        rightInsetColumn.Width = new GridLength(_appWindow.TitleBar.RightInset / scale);
                    }
                    catch
                    {
                    }
                }

                appTitleBar.Loaded += (_, _) => UpdateInsets();
                window.SizeChanged += (_, _) => UpdateInsets();

                UpdateInsets();
            }
            catch
            {
            }
        }
    }
}