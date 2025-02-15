using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using D4Companion.Localization;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace D4Companion.Services
{
    public class OverlayHandler : IOverlayHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly IAffixManager _affixManager;
        private readonly ISettingsManager _settingsManager;

        private GraphicsWindow? _window = null;
        private readonly Dictionary<string, SolidBrush> _brushes = new Dictionary<string, SolidBrush>();
        private readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();
        private readonly Dictionary<string, Image> _images = new Dictionary<string, Image>();

        private string _currentAffixPreset = string.Empty;
        private bool _currentAffixPresetVisible = false;
        private DispatcherTimer _currentAffixPresetTimer = new();
        private ItemTooltipDescriptor _currentTooltip = new ItemTooltipDescriptor();
        private object _lockItemTooltip = new object();
        private object _lockWindowHandle = new object();
        private string _notificationText = string.Empty;
        private DispatcherTimer _notificationTimer = new();
        private bool _notificationVisible = false;
        private List<OverlayMenuItem> _overlayMenuItems = new List<OverlayMenuItem>();
        private DispatcherTimer _paragonStepTimer = new();
        HWND _windowHandle = HWND.Null;

        private string _currentParagonBoard = string.Empty;
        private int _currentParagonBoardIndex = 0;
        private int _currentParagonBoardsListIndex = 0;
        private int _currentParagonBoardPanelWidth = 0;
        private int _currentParagonBuildPanelWidth = 0;

        // Start of Constructors region

        #region Constructors

        public OverlayHandler(IEventAggregator eventAggregator, ILogger<ScreenProcessHandler> logger, IAffixManager affixManager, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AffixPresetChangedEvent>().Subscribe(HandleAffixPresetChangedEvent);
            _eventAggregator.GetEvent<MenuLockedEvent>().Subscribe(HandleMenuLockedEvent);
            _eventAggregator.GetEvent<MenuUnlockedEvent>().Subscribe(HandleMenuUnlockedEvent);
            _eventAggregator.GetEvent<MouseUpdatedEvent>().Subscribe(HandleMouseUpdatedEvent);
            _eventAggregator.GetEvent<ToggleDebugLockScreencaptureKeyBindingEvent>().Subscribe(HandleToggleDebugLockScreencaptureKeyBindingEvent);
            _eventAggregator.GetEvent<ToggleOverlayFromGUIEvent>().Subscribe(HandleToggleOverlayFromGUIEvent);
            _eventAggregator.GetEvent<TooltipDataReadyEvent>().Subscribe(HandleTooltipDataReadyEvent);
            _eventAggregator.GetEvent<WindowHandleUpdatedEvent>().Subscribe(HandleWindowHandleUpdatedEvent);
            
            // Init logger
            _logger = logger;

            // Init services
            _affixManager = affixManager;
            _settingsManager = settingsManager;

            // Init overlay objects
            InitOverlayObjects();

            // Init timers
            _currentAffixPresetTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2000),
                IsEnabled = false
            };
            _currentAffixPresetTimer.Tick += CurrentAffixPresetTimer_Tick;
            _notificationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2000),
                IsEnabled = false
            };
            _notificationTimer.Tick += NotificationTimer_Tick;
            _paragonStepTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500),
                IsEnabled = false
            };
            _paragonStepTimer.Tick += ParagonStepTimer_Tick;
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void CurrentAffixPresetTimer_Tick(object? sender, EventArgs e)
        {
            (sender as DispatcherTimer)?.Stop();
            _currentAffixPresetVisible = false;
        }

        private void DrawGraphics(object? sender, DrawGraphicsEventArgs e)
        {
            try
            {
                // Clear
                var gfx = e.Graphics;
                gfx.ClearScene();

                if (_window == null) return;

                // Tooltip
                lock (_lockItemTooltip)
                {
                    var overlayMenuItem = _overlayMenuItems.FirstOrDefault(o => o.Id.Equals("diablo"));
                    if (!overlayMenuItem?.IsLocked ?? true) _currentTooltip = new ItemTooltipDescriptor();

                    if (overlayMenuItem != null)
                    {
                        overlayMenuItem.Left = _window.Width * (_settingsManager.Settings.OverlayIconPosX / 1000f);
                        overlayMenuItem.Top = _window.Height * (_settingsManager.Settings.OverlayIconPosY / 1000f);
                    }

                    bool itemPowerLimitCheckOk = (_settingsManager.Settings.IsItemPowerLimitEnabled && _settingsManager.Settings.ItemPowerLimit <= _currentTooltip.ItemPower) ||
                        !_settingsManager.Settings.IsItemPowerLimitEnabled ||
                        _currentTooltip.ItemType.Equals(ItemTypeConstants.Sigil);

                     
                    if (_settingsManager.Settings.IsParagonModeActive)
                    {
                        // Paragon mode
                        DrawGraphicsParagon(sender, e);
                    }
                    else
                    {
                        // Affix mode
                        if (_settingsManager.Settings.IsMultiBuildModeEnabled)
                        {
                            DrawGraphicsAffixesMulti(sender, e, itemPowerLimitCheckOk);
                            DrawGraphicsAspectsMulti(sender, e, itemPowerLimitCheckOk);
                        }
                        else
                        {
                            DrawGraphicsAffixes(sender, e, itemPowerLimitCheckOk);
                            DrawGraphicsAspects(sender, e, itemPowerLimitCheckOk);
                        }

                        // Trading
                        DrawGraphicsTrading(sender, e, itemPowerLimitCheckOk);
                    }
                }

                // Menu items
                float stroke = 1; // Border arround menu items
                float captionOffset = 5; // Left margin for menu item caption
                float activationBarSize = 5; // Size for the activation bar
                foreach (OverlayMenuItem menuItem in _overlayMenuItems.OfType<OverlayMenuItem>())
                {
                    if (menuItem.IsVisible)
                    {
                        gfx.FillRectangle(_brushes["background"], menuItem.Left, menuItem.Top, menuItem.Left + menuItem.Width, menuItem.Top + menuItem.Height);
                        gfx.DrawRectangle(_brushes["border"], menuItem.Left, menuItem.Top, menuItem.Left + menuItem.Width, menuItem.Top + menuItem.Height, stroke);
                        gfx.DrawText(_fonts["consolasBold"], _brushes[menuItem.CaptionColor], menuItem.Left + captionOffset, menuItem.Top + menuItem.Height - activationBarSize - _fonts["consolasBold"].FontSize - captionOffset, menuItem.Caption);
                        gfx.DrawImage(_images[menuItem.Image], menuItem.Left + (menuItem.Width / 2) - (_images[menuItem.Image].Width / 2), menuItem.Top + (menuItem.Height / 3) - (_images[menuItem.Image].Height / 3));
                        float lockProgressAsWidth = (float)Math.Min(menuItem.LockWatch.ElapsedMilliseconds / OverlayConstants.LockTimer, 1.0) * menuItem.Width;
                        gfx.FillRectangle(_brushes[Colors.Goldenrod.ToString()], menuItem.Left, menuItem.Top + menuItem.Height - activationBarSize, menuItem.Left + lockProgressAsWidth, menuItem.Top + menuItem.Height);
                    }
                }

                void DrawNotification(string notificationText)
                {
                    float textOffset = 20;
                    float initialPresetPanelHeight = 50;
                    float fontSize = _settingsManager.Settings.OverlayFontSize;

                    var textWidth = gfx.MeasureString(_fonts["consolasBold"], fontSize, notificationText).X;
                    var textHeight = gfx.MeasureString(_fonts["consolasBold"], fontSize, notificationText).Y;
                    float presetPanelWidth = textWidth + 2 * textOffset;

                    // Calculate the position of the panel to center it on the screen
                    float presetPanelLeft = (_window.Width - presetPanelWidth) / 2;
                    float presetPanelTop = (_window.Height - initialPresetPanelHeight) / 2;
                    float presetPanelWidthCentered = (_window.Width + presetPanelWidth) / 2;
                    float presetPanelHeightCentered = (_window.Height + initialPresetPanelHeight) / 2;

                    // Draw the panel as a filled rectangle behind the text
                    gfx.FillRectangle(_brushes["background"], presetPanelLeft, presetPanelTop, presetPanelWidthCentered, presetPanelHeightCentered);

                    // Draw the border of the panel
                    gfx.DrawRectangle(_brushes["border"], presetPanelLeft, presetPanelTop, presetPanelWidthCentered, presetPanelHeightCentered, stroke);

                    // Center the text inside the panel
                    float textLeft = presetPanelLeft + textOffset;
                    float textTop = presetPanelTop + (initialPresetPanelHeight - textHeight) / 2;
                    gfx.DrawText(_fonts["consolasBold"], fontSize, _brushes["text"], textLeft, textTop, notificationText);
                }

                // Notification - Affix preset changed
                if (_currentAffixPresetVisible)
                {
                    // Limit preset text.
                    string presetText = _currentAffixPreset.Length <= 150 ? string.Format(TranslationSource.Instance["rsFormatPresetActivated"], _currentAffixPreset) :
                        string.Format(TranslationSource.Instance["rsFormatPresetActivated"], _currentAffixPreset.Substring(0, 150));

                    DrawNotification(presetText);
                }

                // Notification - General notifications
                if (_notificationVisible)
                {
                    DrawNotification(_notificationText);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private void DrawGraphicsAffixes(object? sender, DrawGraphicsEventArgs e, bool itemPowerLimitCheckOk)
        {
            if (_currentTooltip.ItemAffixLocations.Any())
            {
                var gfx = e.Graphics;
                int radius = 10;
                int length = 20;

                for (int i = 0; i < _currentTooltip.ItemAffixLocations.Count; i++)
                {
                    var itemAffixLocation = _currentTooltip.ItemAffixLocations[i];

                    float left = _currentTooltip.Location.X + _currentTooltip.OffsetX;
                    float top = _currentTooltip.Location.Y + itemAffixLocation.Location.Y;

                    var itemAffix = _currentTooltip.ItemAffixes.FirstOrDefault(affix => affix.Item1 == i);
                    if (itemAffix != null)
                    {
                        var affixColor = itemAffix.Item2.Color;

                        // Overwrite affix colors for unique items
                        if (_settingsManager.Settings.IsUniqueDetectionEnabled &&
                            !_currentTooltip.ItemAspectLocation.IsEmpty && _currentTooltip.IsUniqueItem &&
                            !string.IsNullOrEmpty(_currentTooltip.ItemAspect.Id))
                        {
                            affixColor = _currentTooltip.ItemAspect.Color == Colors.Red ? itemAffix.Item2.Color : _currentTooltip.ItemAspect.Color;
                        }

                        // Cases
                        // (1) Show all. Always show all markers
                        // (2) Hide unwanted. Show when color is not equal to red.
                        if (_settingsManager.Settings.SelectedOverlayMarkerMode.Equals("Show All") ||
                            !affixColor.ToString().Equals(Colors.Red.ToString()))
                        {
                            if (itemPowerLimitCheckOk)
                            {
                                if (_currentTooltip.ItemType.Contains(ItemTypeConstants.Sigil) && _affixManager.GetSigilType(itemAffix.Item2.Id).Equals("Dungeon"))
                                {
                                    // Handle sigil dungeon locations
                                    gfx.OutlineFillRectangle(_brushes[Colors.Black.ToString()], _brushes[affixColor.ToString()], left - length / 2, top, left - length / 2 + length, top + length, 2);

                                    if (_settingsManager.Settings.DungeonTiers)
                                    {
                                        string tier = _affixManager.GetSigilDungeonTier(itemAffix.Item2.Id);
                                        SolidBrush GetContrastColor(System.Windows.Media.Color backgroundColor)
                                        {
                                            return (backgroundColor.R + backgroundColor.G + backgroundColor.B) / 3 <= 128 ? _brushes["text"] : _brushes["textdark"];
                                        }
                                        gfx.DrawText(_fonts["consolasBold"], GetContrastColor(_currentTooltip.ItemAspect.Color), left - length / 4, top, tier);
                                    }
                                }
                                else
                                {
                                    // Handle different shapes
                                    // - Circle: For all normal affixes.
                                    // - Rectangle: For affixes set to ignore the specified item type.
                                    // - Rectangle: For affixes below minimal value.
                                    // - Triangle: For affixes set to greater affix.
                                    if (itemAffix.Item2.IsAnyType)
                                    {
                                        gfx.OutlineFillRectangle(_brushes[Colors.Black.ToString()], _brushes[affixColor.ToString()], left - length / 2, top, left - length / 2 + length, top + length, 1);
                                    }
                                    else if (itemAffix.Item2.IsGreater)
                                    {
                                        Triangle triangle = new Triangle(left - (length / 2), top + length, left + (length / 2), top + length, left, top);
                                        gfx.FillTriangle(_brushes[affixColor.ToString()], triangle);
                                        gfx.DrawTriangle(_brushes[Colors.Black.ToString()], triangle, 2);
                                    }
                                    else if (_settingsManager.Settings.IsMinimalAffixValueFilterEnabled &&
                                        _currentTooltip.ItemAffixAreas[i].AffixValue < _currentTooltip.ItemAffixAreas[i].AffixThresholdValue)
                                    {
                                        gfx.OutlineFillRectangle(_brushes[Colors.Black.ToString()], _brushes[affixColor.ToString()], left - length / 2, top, left - length / 2 + length, top + length, 1);
                                    }
                                    else
                                    {
                                        gfx.OutlineFillCircle(_brushes[Colors.Black.ToString()], _brushes[affixColor.ToString()], left, top + (itemAffixLocation.Location.Height / 2), radius, 2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawGraphicsAffixesMulti(object? sender, DrawGraphicsEventArgs e, bool itemPowerLimitCheckOk)
        {
            if (!_currentTooltip.ItemAffixLocations.Any()) return;

            for (int i = 0; i < _currentTooltip.ItemAffixesBuildList.Count; i++)
            {
                DrawGraphicsAffixesMulti(sender, e, itemPowerLimitCheckOk, _currentTooltip.ItemAffixesBuildList[i], _currentTooltip.ItemAspectBuildList[i], 5 - (i * 20));
            }
        }

        private void DrawGraphicsAffixesMulti(object? sender, DrawGraphicsEventArgs e, bool itemPowerLimitCheckOk, List<Tuple<int, ItemAffix>> itemAffixes, ItemAffix itemAspect, int offset)
        {
            if (_currentTooltip.ItemAffixLocations.Any())
            {
                var gfx = e.Graphics;
                int radius = 10;
                int length = 20;

                for (int i = 0; i < _currentTooltip.ItemAffixLocations.Count; i++)
                {
                    var itemAffixLocation = _currentTooltip.ItemAffixLocations[i];

                    float left = _currentTooltip.Location.X + _currentTooltip.OffsetX;
                    float top = _currentTooltip.Location.Y + itemAffixLocation.Location.Y;

                    // Apply offset
                    left = left + offset;

                    var itemAffix = itemAffixes.FirstOrDefault(affix => affix.Item1 == i);
                    if (itemAffix != null)
                    {
                        var affixColor = itemAffix.Item2.Color;

                        // Overwrite affix colors for unique items
                        if (_settingsManager.Settings.IsUniqueDetectionEnabled &&
                            !_currentTooltip.ItemAspectLocation.IsEmpty && _currentTooltip.IsUniqueItem &&
                            !string.IsNullOrEmpty(_currentTooltip.ItemAspect.Id))
                        {
                            affixColor = itemAspect.Color == Colors.Red ? itemAffix.Item2.Color : itemAspect.Color;
                        }

                        // Hide all unwanted affixes.
                        if (!affixColor.ToString().Equals(Colors.Red.ToString()))
                        {
                            if (itemPowerLimitCheckOk)
                            {
                                if (_currentTooltip.ItemType.Contains(ItemTypeConstants.Sigil) && _affixManager.GetSigilType(itemAffix.Item2.Id).Equals("Dungeon"))
                                {
                                    // Handle sigil dungeon locations
                                    gfx.OutlineFillRectangle(_brushes[Colors.Black.ToString()], _brushes[affixColor.ToString()], left - length / 2, top, left - length / 2 + length, top + length, 2);

                                    if (_settingsManager.Settings.DungeonTiers)
                                    {
                                        string tier = _affixManager.GetSigilDungeonTier(itemAffix.Item2.Id);
                                        SolidBrush GetContrastColor(System.Windows.Media.Color backgroundColor)
                                        {
                                            return (backgroundColor.R + backgroundColor.G + backgroundColor.B) / 3 <= 128 ? _brushes["text"] : _brushes["textdark"];
                                        }
                                        gfx.DrawText(_fonts["consolasBold"], GetContrastColor(affixColor), left - length / 4, top, tier);
                                    }
                                }
                                else
                                {
                                    // Handle different shapes
                                    // - Circle: For all normal affixes.
                                    // - Rectangle: For affixes set to ignore the specified item type.
                                    // - Rectangle: For affixes below minimal value.
                                    // - Triangle: For affixes set to greater affix.
                                    if (itemAffix.Item2.IsAnyType)
                                    {
                                        gfx.OutlineFillRectangle(_brushes[Colors.Black.ToString()], _brushes[affixColor.ToString()], left - length / 2, top, left - length / 2 + length, top + length, 1);
                                    }
                                    else if (itemAffix.Item2.IsGreater)
                                    {
                                        Triangle triangle = new Triangle(left - (length / 2), top + length, left + (length / 2), top + length, left, top);
                                        gfx.FillTriangle(_brushes[affixColor.ToString()], triangle);
                                        gfx.DrawTriangle(_brushes[Colors.Black.ToString()], triangle, 2);
                                    }
                                    else if (_settingsManager.Settings.IsMinimalAffixValueFilterEnabled &&
                                        _currentTooltip.ItemAffixAreas[i].AffixValue < _currentTooltip.ItemAffixAreas[i].AffixThresholdValue)
                                    {
                                        gfx.OutlineFillRectangle(_brushes[Colors.Black.ToString()], _brushes[affixColor.ToString()], left - length / 2, top, left - length / 2 + length, top + length, 1);
                                    }
                                    else
                                    {
                                        gfx.OutlineFillCircle(_brushes[Colors.Black.ToString()], _brushes[affixColor.ToString()], left, top + (itemAffixLocation.Location.Height / 2), radius, 2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawGraphicsAspects(object? sender, DrawGraphicsEventArgs e, bool itemPowerLimitCheckOk)
        {
            if (!_currentTooltip.ItemAspectLocation.IsEmpty && itemPowerLimitCheckOk)
            {
                var gfx = e.Graphics;
                int length = 10;

                var itemAspectLocation = _currentTooltip.ItemAspectLocation;
                float left = _currentTooltip.Location.X + _currentTooltip.OffsetX;
                float top = _currentTooltip.Location.Y + itemAspectLocation.Y;

                if (_settingsManager.Settings.SelectedOverlayMarkerMode.Equals("Show All") ||
                    (!_currentTooltip.ItemAspect.Color.ToString().Equals(Colors.Red.ToString())))
                {
                    gfx.OutlineFillCircle(_brushes[Colors.Black.ToString()], _brushes[_currentTooltip.ItemAspect.Color.ToString()], left, top + (itemAspectLocation.Height / 2), length, 2);
                }
            }
        }

        private void DrawGraphicsAspectsMulti(object? sender, DrawGraphicsEventArgs e, bool itemPowerLimitCheckOk)
        {
            if (_currentTooltip.ItemAspectLocation.IsEmpty) return;

            for (int i = 0; i < _currentTooltip.ItemAspectBuildList.Count; i++)
            {
                DrawGraphicsAspectsMulti(sender, e, itemPowerLimitCheckOk, _currentTooltip.ItemAspectBuildList[i], 5 - (i * 20));
            }
        }

        private void DrawGraphicsAspectsMulti(object? sender, DrawGraphicsEventArgs e, bool itemPowerLimitCheckOk, ItemAffix itemAspect, int offset)
        {
            if (!_currentTooltip.ItemAspectLocation.IsEmpty && itemPowerLimitCheckOk &&
                !string.IsNullOrWhiteSpace(itemAspect.Id))
            {
                var gfx = e.Graphics;
                int length = 10;

                var itemAspectLocation = _currentTooltip.ItemAspectLocation;
                float left = _currentTooltip.Location.X + _currentTooltip.OffsetX;
                float top = _currentTooltip.Location.Y + itemAspectLocation.Y;

                // Apply offset
                left = left + offset;

                // Hide unwanted aspect.
                var aspectColor = itemAspect.Color;
                if (!aspectColor.ToString().Equals(Colors.Red.ToString()))
                {
                    gfx.OutlineFillCircle(_brushes[Colors.Black.ToString()], _brushes[aspectColor.ToString()], left, top + (itemAspectLocation.Height / 2), length, 2);
                }
            }
        }

        private void DrawGraphicsTrading(object? sender, DrawGraphicsEventArgs e, bool itemPowerLimitCheckOk)
        {
            if (_settingsManager.Settings.IsTradeOverlayEnabled && _currentTooltip.TradeItem != null)
            {
                var gfx = e.Graphics;

                string tradeItemValue = _currentTooltip.TradeItem.Value;

                float textOffset = 20;
                float initialPresetPanelHeight = 50;
                float fontSize = _settingsManager.Settings.OverlayFontSize;

                var textHeight = gfx.MeasureString(_fonts["consolasBold"], fontSize, tradeItemValue).Y;
                var textHeightTrade = gfx.MeasureString(_fonts["consolasBold"], fontSize, "$").Y;
                var textWidthTrade = gfx.MeasureString(_fonts["consolasBold"], fontSize, "$").X;

                // Set the position of the panel
                float presetPanelLeft = _currentTooltip.Location.X + _currentTooltip.OffsetX;
                float presetPanelTop = _currentTooltip.Location.Y + _currentTooltip.Location.Height - initialPresetPanelHeight;
                float presetPanelRight = _currentTooltip.Location.X + _currentTooltip.OffsetX + _currentTooltip.Location.Width;
                float presetPanelBottom = _currentTooltip.Location.Y + _currentTooltip.Location.Height;

                // Draw the panel as a filled rectangle behind the text
                gfx.FillRectangle(_brushes["background"], presetPanelLeft, presetPanelTop, presetPanelRight, presetPanelBottom);

                // Draw the border of the panel
                gfx.DrawRectangle(_brushes["border"], presetPanelLeft, presetPanelTop, presetPanelRight, presetPanelBottom, stroke: 1);

                // Center the text inside the panel
                float textLeft = presetPanelLeft + textOffset;
                float textTop = presetPanelTop + (initialPresetPanelHeight - textHeight) / 2;
                gfx.DrawText(_fonts["consolasBold"], fontSize, _brushes["text"], textLeft, textTop, tradeItemValue);

                // Center icon inside the panel
                float iconLeft = presetPanelLeft;
                float iconTop = presetPanelTop + (initialPresetPanelHeight) / 2;
                gfx.OutlineFillCircle(_brushes[Colors.Black.ToString()], _brushes[Colors.Goldenrod.ToString()], iconLeft, iconTop, radius: 18, stroke: 2);
                gfx.DrawText(_fonts["consolasBold"], fontSize, _brushes[Colors.Black.ToString()], iconLeft - (textWidthTrade / 2), iconTop - (textHeightTrade / 2), "$");
            }
        }

        private void DrawGraphicsParagon(object? sender, DrawGraphicsEventArgs e)
        {
            var preset = _affixManager.AffixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;
            if (preset.ParagonBoardsList.Count == 0 || (_currentParagonBoardsListIndex >= preset.ParagonBoardsList.Count)) return;

            var currentBoards = preset.ParagonBoardsList[_currentParagonBoardsListIndex];
            _currentParagonBoard = string.IsNullOrWhiteSpace(_currentParagonBoard) ? currentBoards[0].Name : _currentParagonBoard;

            if (_currentParagonBoardIndex >= 0 && _currentParagonBoardIndex < currentBoards.Count)
            {
                _currentParagonBoard = currentBoards[_currentParagonBoardIndex].Name;
            }
            else if(!currentBoards.Any(b => b.Name.Equals(_currentParagonBoard)))
            {
                _currentParagonBoard = currentBoards[0].Name;
            }

            var currentBoard = currentBoards.FirstOrDefault(board => board.Name.Equals(_currentParagonBoard));
            if (currentBoard == null) return;

            var gfx = e.Graphics;

            // Draw info
            float textOffset = 20;
            float fontSize = _settingsManager.Settings.OverlayFontSize;

            string currentBuildText = preset.Name;
            var textWidthBuild = gfx.MeasureString(_fonts["consolasBold"], fontSize, currentBuildText).X;
            var textHeightBuild = gfx.MeasureString(_fonts["consolasBold"], fontSize, currentBuildText).Y;
            float panelWidthBuild = textWidthBuild + 2 * textOffset;
            _currentParagonBuildPanelWidth = (int)panelWidthBuild;

            float panelLeftBuild = 0;
            float panelTopBuild = 100;
            float panelHeightBuild = 50;
            float strokeBuild = 1;
            gfx.FillRectangle(_brushes["background"], panelLeftBuild, panelTopBuild, panelLeftBuild + panelWidthBuild, panelTopBuild + panelHeightBuild);
            gfx.DrawRectangle(_brushes["border"], panelLeftBuild, panelTopBuild, panelLeftBuild + panelWidthBuild, panelTopBuild + panelHeightBuild, strokeBuild);

            float textLeftBuild = panelLeftBuild + textOffset;
            float textTopBuild = panelTopBuild + (panelHeightBuild - textHeightBuild) / 2;
            gfx.DrawText(_fonts["consolasBold"], fontSize, _brushes["text"], textLeftBuild, textTopBuild, currentBuildText);

            // Draw board entries
            float panelLeftBoard = 0;
            float panelTopBoard = panelTopBuild + panelHeightBuild + panelHeightBuild + 2;
            float panelHeightBoard = 50;
            float strokeBoard = 1;
            var longestBoard = currentBoards.MaxBy(t => $"{t.Name} {t.Glyph}".Length);
            string longestBoardText = $"{longestBoard.Name} {longestBoard.Glyph}";
            var textWidthBoard = gfx.MeasureString(_fonts["consolasBold"], fontSize, longestBoardText).X;
            var textHeightBoard = gfx.MeasureString(_fonts["consolasBold"], fontSize, longestBoardText).Y;
            float panelWidthBoard = textWidthBoard + 2 * textOffset;
            _currentParagonBoardPanelWidth = (int)panelWidthBoard;

            for (int i = 0; i < currentBoards.Count; i++)
            {
                bool isActive = currentBoards[i].Name.Equals(_currentParagonBoard);
                string currentBoardText = $"{currentBoards[i].Name} ({currentBoards[i].Glyph})";

                gfx.FillRectangle(_brushes["background"], panelLeftBoard, panelTopBoard + 2, panelLeftBoard + panelWidthBoard, panelTopBoard + panelHeightBoard);
                if (isActive)
                {
                    gfx.DrawRectangle(_brushes[Colors.Goldenrod.ToString()], panelLeftBoard, panelTopBoard + 2, panelLeftBoard + panelWidthBoard, panelTopBoard + panelHeightBoard, strokeBoard);
                }
                else
                {
                    gfx.DrawRectangle(_brushes["border"], panelLeftBoard, panelTopBoard + 2, panelLeftBoard + panelWidthBoard, panelTopBoard + panelHeightBoard, strokeBoard);
                }

                float textLeftBoard = panelLeftBoard + textOffset;
                float textTopBoard = panelTopBoard + 2 + (panelHeightBoard - textHeightBoard) / 2;
                gfx.DrawText(_fonts["consolasBold"], fontSize, _brushes["text"], textLeftBoard, textTopBoard, currentBoardText);

                panelTopBoard = panelTopBoard + panelHeightBoard + 2;
            }

            // Draw board steps
            string currentStepText = $"Step {_currentParagonBoardsListIndex + 1} / {preset.ParagonBoardsList.Count}";
            float panelTopStep = panelTopBuild + panelHeightBuild + 2;
            gfx.FillRectangle(_brushes["background"], panelLeftBuild, panelTopStep, panelLeftBuild + panelWidthBoard, panelTopStep + panelHeightBuild);
            gfx.DrawRectangle(_brushes["border"], panelLeftBuild, panelTopStep, panelLeftBuild + panelWidthBoard, panelTopStep + panelHeightBuild, strokeBuild);

            float textLeftStep = panelLeftBuild + textOffset;
            float textTopStep = panelTopStep + (panelHeightBuild - textHeightBuild) / 2;
            gfx.DrawText(_fonts["consolasBold"], fontSize, _brushes["text"], textLeftStep, textTopStep, currentStepText);

            // Draw board
            int tileCount = 21;
            float tileWidth = _settingsManager.Settings.ParagonNodeSize;
            float boardLeft = (_window.Width - (tileWidth * tileCount)) / 2;
            float boardTop = (_window.Height - (tileWidth * tileCount)) / 2;
            for (int y = 0; y < 21; y++)
            {
                for (int x = 0; x < 21; x++)
                {
                    float tileLeft = x * (tileWidth + 5) + boardLeft;
                    float tileTop = y * (tileWidth + 5) + boardTop;
                    float tileRight = tileLeft + tileWidth;
                    float tileBottom = tileTop + tileWidth;

                    if (currentBoard.Nodes[y*21 + x])
                    {
                        gfx.DrawRectangle(_brushes["borderactive"], tileLeft, tileTop, tileRight, tileBottom, stroke: 1);
                    }
                    else
                    {
                        gfx.DrawRectangle(_brushes["border"], tileLeft, tileTop, tileRight, tileBottom, stroke: 1);
                    }
                }
            }
        }

        private void DestroyGraphics(object? sender, DestroyGraphicsEventArgs e)
        {
            try
            {
                foreach (var pair in _brushes) pair.Value.Dispose();
                foreach (var pair in _fonts) pair.Value.Dispose();
                foreach (var pair in _images) pair.Value.Dispose();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private void SetupGraphics(object? sender, SetupGraphicsEventArgs e)
        {
            try
            {
                var gfx = e.Graphics;
                float fontSize = _settingsManager.Settings.OverlayFontSize;

                if (e.RecreateResources)
                {
                    foreach (var pair in _brushes) pair.Value.Dispose();
                    foreach (var pair in _images) pair.Value.Dispose();
                }

                var colorInfoList = GetColors();
                foreach (var colorInfo in colorInfoList) 
                {
                    _brushes[colorInfo.Value.ToString()] = gfx.CreateSolidBrush(colorInfo.Value.R, colorInfo.Value.G, colorInfo.Value.B);
                }
                _brushes["background"] = gfx.CreateSolidBrush(25, 25, 25);
                _brushes["border"] = gfx.CreateSolidBrush(75, 75, 75);
                _brushes["borderactive"] = gfx.CreateSolidBrush(20, 220, 80);
                _brushes["text"] = gfx.CreateSolidBrush(200, 200, 200);
                _brushes["textdark"] = gfx.CreateSolidBrush(20, 20, 20);

                _images["diablo"] = gfx.CreateImage("./Images/Menu/icon_diablo.png");

                if (e.RecreateResources) return;

                //_fonts["arial"] = gfx.CreateFont("Arial", 12);
                //_fonts["consolas"] = gfx.CreateFont("Consolas", 14);
                _fonts["consolasBold"] = gfx.CreateFont("Consolas", fontSize, true);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private void HandleAffixPresetChangedEvent(AffixPresetChangedEventParams affixPresetChangedEventParams)
        {
            _currentAffixPreset = affixPresetChangedEventParams.PresetName;
            _currentAffixPresetVisible = true;
            _currentAffixPresetTimer.Stop();
            _currentAffixPresetTimer.Start();
        }

        private void HandleMenuLockedEvent(MenuLockedEventParams menuLockedEventParams)
        {
            // Handle related actions
            HandleMenuItemAction(menuLockedEventParams.Id, true);
        }

        private void HandleMenuUnlockedEvent(MenuUnlockedEventParams menuUnlockedEventParams)
        {
            // Handle related actions
            HandleMenuItemAction(menuUnlockedEventParams.Id, false);
        }

        private void HandleMouseUpdatedEvent(MouseUpdatedEventParams mouseUpdatedEventParams)
        {
            // Handle mouse events for paragon mode
            if (_settingsManager.Settings.IsParagonModeActive)
            {
                int panelTopBuild = 100;
                int panelHeightBuild = 50;
                int panelTopStep = panelTopBuild + panelHeightBuild + 2;
                int panelHeightStep = 50;
                int panelTopBoard = panelTopBuild + panelHeightBuild + panelHeightBuild + 2;
                int panelHeightBoard = 50;

                int mouseCoordsY = mouseUpdatedEventParams.CoordsMouseY - panelTopBoard;
                if (mouseUpdatedEventParams.CoordsMouseX < Math.Max(_currentParagonBoardPanelWidth, 100) && mouseCoordsY > 0)
                {
                    _currentParagonBoardIndex = mouseCoordsY / (panelHeightBoard + 2);
                }

                if (mouseUpdatedEventParams.CoordsMouseX < Math.Max(_currentParagonBoardPanelWidth, 100) &&
                    mouseUpdatedEventParams.CoordsMouseY > panelTopStep && mouseUpdatedEventParams.CoordsMouseY < panelTopStep + panelHeightStep)
                {
                    _paragonStepTimer.Start();
                }
                else
                {
                    _paragonStepTimer.Stop();
                }
            }
        }

        private void HandleToggleDebugLockScreencaptureKeyBindingEvent()
        {
            _notificationText = TranslationSource.Instance["rsCapToggleDebugLockScreencapture"];
            _notificationVisible = true;
            _notificationTimer.Stop();
            _notificationTimer.Start();
        }

        private void HandleToggleOverlayFromGUIEvent(ToggleOverlayFromGUIEventParams toggleOverlayFromGUIEventParams)
        {
            var overlayMenuItem = _overlayMenuItems.FirstOrDefault(o => o.Id.Equals("diablo"));
            if (overlayMenuItem != null)
            {
                overlayMenuItem.IsLocked = toggleOverlayFromGUIEventParams.IsEnabled;
            }

            _notificationText = toggleOverlayFromGUIEventParams.IsEnabled ? TranslationSource.Instance["rsCapOverlayEnabled"] : TranslationSource.Instance["rsCapOverlayDisabled"];
            _notificationVisible = true;
            _notificationTimer.Stop();
            _notificationTimer.Start();
        }

        private void HandleTooltipDataReadyEvent(TooltipDataReadyEventParams tooltipDataReadyEventParams)
        {
            lock (_lockItemTooltip)
            {
                _currentTooltip = tooltipDataReadyEventParams.Tooltip;
            }
        }

        private void HandleWindowHandleUpdatedEvent(WindowHandleUpdatedEventParams windowHandleUpdatedEventParams)
        {
            // Check if the new WindowHandle is valid
            if (!IsValidWindowSize(windowHandleUpdatedEventParams.WindowHandle))
            {
                return;
            }

            // Check if the window bounds have changed
            if (HasNewWindowBounds(windowHandleUpdatedEventParams.WindowHandle)) 
            {
                _window?.FitTo(windowHandleUpdatedEventParams.WindowHandle);
                return;
            }

            // Check if there is a new windowhandle
            if (!_windowHandle.Equals(windowHandleUpdatedEventParams.WindowHandle))
            {
                _windowHandle = windowHandleUpdatedEventParams.WindowHandle;

                if (_window != null)
                {
                    _window?.FitTo(windowHandleUpdatedEventParams.WindowHandle);
                }
                else
                {
                    InitOverlayWindow();
                }
                return;
            }
        }

        private void NotificationTimer_Tick(object? sender, EventArgs e)
        {
            (sender as DispatcherTimer)?.Stop();
            _notificationVisible = false;
        }

        private void ParagonStepTimer_Tick(object? sender, EventArgs e)
        {
            (sender as DispatcherTimer)?.Stop();

            var preset = _affixManager.AffixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            _currentParagonBoardsListIndex = (_currentParagonBoardsListIndex + 1) % preset.ParagonBoardsList.Count;
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitOverlayObjects()
        {
            _overlayMenuItems.Add(new OverlayMenuItem
            {
                Id = "diablo",
                Left = _settingsManager.Settings.OverlayIconPosX,
                Top = _settingsManager.Settings.OverlayIconPosY,
                Width = 50,
                Height = 50,
                Image = "diablo"
            });
        }

        private void InitOverlayWindow()
        {
            try
            {
                var gfx = new Graphics()
                {
                    MeasureFPS = true,
                    PerPrimitiveAntiAliasing = true,
                    TextAntiAliasing = true
                };

                _window = new GraphicsWindow(gfx)
                {
                    FPS = 60,
                    IsTopmost = true,
                    IsVisible = true
                };

                _window.DestroyGraphics += DestroyGraphics;
                _window.DrawGraphics += DrawGraphics;
                _window.SetupGraphics += SetupGraphics;

                Task.Run(() =>
                {
                    lock (_lockWindowHandle)
                    {
                        _window.Create();
                        _window.FitTo(_windowHandle);
                        _window.Join();
                    }
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private void HandleMenuItemAction(string id, bool isLocked)
        {
            switch (id)
            {
                case "diablo":
                    if (isLocked)
                    {
                        // Turn overlay on
                        _eventAggregator.GetEvent<ToggleOverlayEvent>().Publish(new ToggleOverlayEventParams { IsEnabled = true });
                    }
                    else
                    {
                        // Turn overlay off
                        _eventAggregator.GetEvent<ToggleOverlayEvent>().Publish(new ToggleOverlayEventParams { IsEnabled = false });
                    }
                    break;
            }
        }

        private bool IsValidWindowSize(HWND windowHandle)
        {
            RECT rect;
            PInvoke.GetWindowRect(windowHandle, out rect);

            //Debug.WriteLine($"Left: {rect.left}, Right: {rect.right}, Top: {rect.bottom}, Bottom: {rect.bottom}");

            var height = (rect.bottom - rect.top);

            return height > 100;
        }

        private bool HasNewWindowBounds(HWND windowHandle)
        {
            bool result = false;

            // Compare window bounds
            if (_window != null)
            {
                RECT rect;
                PInvoke.GetWindowRect(windowHandle, out rect);

                result = _window.Height != (rect.bottom - rect.top) || _window.Width != (rect.right - rect.left) ||
                    rect.left != _window.X || rect.top != _window.Y;
            }

            return result;
        }

        private IEnumerable<KeyValuePair<string, System.Windows.Media.Color>> GetColors()
        {
            return typeof(Colors)
                .GetProperties()
                .Where(prop =>
                    typeof(System.Windows.Media.Color).IsAssignableFrom(prop.PropertyType))
                .Select(prop =>
                    new KeyValuePair<string, System.Windows.Media.Color>(prop.Name, (System.Windows.Media.Color)prop.GetValue(null)));
        }

        #endregion
    }

    public class OverlayMenuItem
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISettingsManager _settingsManager;

        // Start of Constructors region

        #region Constructors

        public OverlayMenuItem()
        {
            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));
            _eventAggregator.GetEvent<MouseUpdatedEvent>().Subscribe(HandleMouseUpdatedEvent);

            // Init services
            _settingsManager = (ISettingsManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISettingsManager));
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public string Id { get; set; } = string.Empty;

        public float Left { get; set; }
        public float Top { get; set; }
        public float Height { get; set; }
        public float Width { get; set; }

        public string Caption { get; set; } = string.Empty;
        public string CaptionColor { get; set; } = "text";
        public string Image { get; set; } = string.Empty;
        public bool IsLocked { get; set; } = false;
        public bool IsVisible { get; set; } = false;
        public Stopwatch LockWatch { get; set; } = new Stopwatch();

        #endregion

        // Start of Event handlers region

        #region Event handlers

        protected void HandleMouseUpdatedEvent(MouseUpdatedEventParams mouseUpdatedEventParams)
        {
            bool isOnOverlayMenuItem = mouseUpdatedEventParams.CoordsMouseX >= Left && mouseUpdatedEventParams.CoordsMouseX <= Left + Width &&
                mouseUpdatedEventParams.CoordsMouseY >= Top && mouseUpdatedEventParams.CoordsMouseY <= Top + Height;

            if (isOnOverlayMenuItem)
            {
                if (!LockWatch.IsRunning && LockWatch.ElapsedMilliseconds == 0)
                {
                    // Start timer when mouse enters OverlayMenuItem
                    LockWatch.Start();
                }
                else
                {
                    if (LockWatch.IsRunning && LockWatch.ElapsedMilliseconds >= OverlayConstants.LockTimer)
                    {
                        // Stop timer and change lock state when mouse was on OverlayMenuItem for >= OverlayConstants.LockTimer ms
                        LockWatch.Stop();
                        IsLocked = !IsLocked;

                        // Let subscribers know when locked state has changed
                        if (IsLocked)
                        {
                            _eventAggregator.GetEvent<MenuLockedEvent>().Publish(new MenuLockedEventParams { Id = Id });
                        }
                        else
                        {
                            _eventAggregator.GetEvent<MenuUnlockedEvent>().Publish(new MenuUnlockedEventParams { Id = Id });
                        }
                    }
                }
            }
            else
            {
                // Stop and reset timer when mouse leaves OverlayMenuItem
                LockWatch.Reset();
            }

            // Update visibility
            if ((_settingsManager.Settings.ShowOverlayIcon && IsLocked) || isOnOverlayMenuItem)
            {
                IsVisible = true;
            }
            else if (!isOnOverlayMenuItem)
            {
                IsVisible = false;
            }
        }

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
