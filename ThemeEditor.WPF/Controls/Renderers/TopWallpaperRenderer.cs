﻿// --------------------------------------------------
// 3DS Theme Editor - TopWallpaperRenderer.cs
// --------------------------------------------------

using System;
using System.Windows;
using System.Windows.Media;
using ThemeEditor.WPF.Effects;
using ThemeEditor.WPF.Localization.Enums;
using ThemeEditor.WPF.RenderTools;
using ThemeEditor.WPF.Themes;

namespace ThemeEditor.WPF.Controls.Renderers
{
    internal class TopWallpaperRenderer : FrameworkElement
    {
        private static readonly RenderToolFactory RenderToolFactory = new RenderToolFactory();
        private static readonly Rect ScreenArea;

        public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register
            (nameof(Theme),
                typeof (ThemeViewModel),
                typeof (TopWallpaperRenderer),
                new FrameworkPropertyMetadata(default(ThemeViewModel), FrameworkPropertyMetadataOptions.AffectsRender));

        private bool _isListening;

        //private WarpEffect _warpEffect;

        public ThemeViewModel Theme
        {
            get { return (ThemeViewModel) GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        static TopWallpaperRenderer()
        {
            ScreenArea = new Rect(0, 0, 412, 240);

            RenderToolFactory.RegisterTool<PenTool, Pen>
                (key => new Pen(new SolidColorBrush(key.Color)
                {
                    Opacity = key.Opacity
                },
                    key.Width));

            RenderToolFactory.RegisterTool<SolidColorBrushTool, Brush>
                (key => new SolidColorBrush(key.Color)
                {
                    Opacity = key.Opacity
                });

            RenderToolFactory.RegisterTool<LinearGradientBrushTool, Brush>
                (key => new LinearGradientBrush(key.ColorA, key.ColorB, key.Angle)
                {
                    Opacity = key.Opacity
                });

            RenderToolFactory.RegisterTool<ImageBrushTool, Brush>
                (key => new ImageBrush(key.Image)
                {
                    TileMode = key.Mode,
                    ViewportUnits = key.ViewportUnits,
                    Viewport = key.Viewport,
                    Opacity = key.Opacity
                });

            Type ownerType = typeof (TopWallpaperRenderer);
            IsEnabledProperty
                .OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false, OnIsEnabledChanged));

            ClipToBoundsProperty.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(true, null, (o, value) => true));
            WidthProperty.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(412.0, null, (o, value) => 412.0));
            HeightProperty.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(240.0, null, (o, value) => 240.0));

            /*
            EffectProperty.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(default(WarpEffect),
                    null,
                    (o, value) => ((TopWallpaperRenderer) o).GetWarpEffectInstance()));
                    */
        }

        public TopWallpaperRenderer()
        {
            ViewModelBase.ViewModelChanged += ViewModelBaseOnViewModelChanged;
        }

        private static void OnIsEnabledChanged(DependencyObject elem, DependencyPropertyChangedEventArgs args)
        {
            var target = elem as TopWallpaperRenderer;
            if (target == null)
            {
                return;
            }
            bool oldValue = (bool) args.OldValue;
            bool newValue = (bool) args.NewValue;
            target.OnIsEnabledChanged(oldValue, newValue);
        }

        private static void OnRender_3DCorners(DrawingContext dc)
        {
            var rect3DL = new Rect(0, 0, 6, 240);
            var rect3DR = new Rect(406, 0, 6, 240);
            var brush3D = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
            dc.DrawRectangle(brush3D, null, rect3DL);
            dc.DrawRectangle(brush3D, null, rect3DR);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Theme == null)
            {
                //var topTex = DefaultTopSquares.Bitmap;
                var background = Color.FromArgb(255, 205, 205, 217);
                const float GRADIENT = 0.5f;

                OnRender_BackgroundSolid(dc, background, GRADIENT, true);
                return;
            }

            var drawType = Theme.Flags.TopDrawType;
            switch (drawType)
            {
                case TopDrawType.SolidColor:
                {
                    OnRender_BackgroundSolid(dc,
                        Theme.Colors.TopBackground.Main,
                        (float) Theme.Colors.TopBackground.Gradient,
                        true);
                    break;
                }
                case TopDrawType.SolidColorTexture:
                {
                    OnRender_BackgroundSolid(dc,
                        Theme.Colors.TopBackground.Main,
                        (float) Theme.Colors.TopBackground.Gradient,
                        Theme.Colors.TopBackground.FadeToWhite);

                    break;
                }
                case TopDrawType.Texture:
                {
                    OnRender_BackgroundTexture(dc, Theme.Flags.TopFrameType, Theme.Textures.Top.Bitmap);

                    break;
                }
                case TopDrawType.None:
                {
                    var background = Color.FromArgb(255, 205, 205, 217);
                    const float GRADIENT = 0.5f;

                    OnRender_BackgroundSolid(dc, background, GRADIENT, true);

                    break;
                }
            }

            OnRender_3DCorners(dc);
        }

        /*
        private object GetWarpEffectInstance()
        {
            return _warpEffect ?? (_warpEffect = new WarpEffect());
        }
        */

        private void OnIsEnabledChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                if (!_isListening)
                {
                    StartListening();
                }
            }
            else
            {
                if (_isListening)
                {
                    StopListening();
                }
            }

            if (oldValue != newValue)
            {
                InvalidateVisual();
            }
        }

        private void OnRender_BackgroundSolid(DrawingContext dc, Color color, float gradient, bool fadeToWhite)
        {
            var opaque = color;
            var faded = opaque.Blend(fadeToWhite
                ? Colors.White
                : Colors.Black,
                gradient);
            var lgBrush = RenderToolFactory.GetTool<Brush>(new LinearGradientBrushTool(faded, opaque, 90));
            dc.DrawRectangle(lgBrush, null, ScreenArea);
        }

        private void OnRender_BackgroundTexture(DrawingContext dc, TopFrameType frameType, ImageSource wallpaper)
        {
            //SetEnableWarp(false);

            var scrollEnable = frameType == TopFrameType.SlowScroll || frameType == TopFrameType.FastScroll;

            const int SCR_OFFSET = 1008 - (412 / 2);
            const int OFFSET_3D = 6;
            if (scrollEnable)
            {
                var posMap = _isListening
                    ? (Math.Sin(CompositionTargetEx.SecondsFromStart / 3) + 1) * SCR_OFFSET
                    : 0;
                posMap -= OFFSET_3D;

                if (posMap <= OFFSET_3D)
                    dc.DrawImage(wallpaper, new Rect(-posMap - 1008, 0, wallpaper.Width, wallpaper.Height));
                if (posMap < 1008)
                    dc.DrawImage(wallpaper, new Rect(-posMap, 0, wallpaper.Width, wallpaper.Height));
                if (posMap + 412 > 1008)
                    dc.DrawImage(wallpaper, new Rect(-posMap + 1007, 0, wallpaper.Width, wallpaper.Height));
            }
            else
            {
                dc.DrawImage(wallpaper, new Rect(0, 0, wallpaper.Width, wallpaper.Height));
            }

            OnRender_3DCorners(dc);
        }

        private void OnRendering(object sender, EventArgs eventArgs)
        {
            InvalidateVisual();
        }

        private void StartListening()
        {
            VerifyAccess();
            if (_isListening)
                return;
            _isListening = true;
            CompositionTargetEx.FrameUpdating += OnRendering;
        }

        private void StopListening()
        {
            VerifyAccess();
            if (!_isListening)
                return;
            _isListening = false;
            CompositionTargetEx.FrameUpdating -= OnRendering;
        }

        private void ViewModelBaseOnViewModelChanged(ViewModelBase.ViewModelChangedArgs args)
        {
            if (Theme == null)
                return;
            if (args.ViewModel.Tag == Theme.Tag)
                InvalidateVisual();
        }
    }
}