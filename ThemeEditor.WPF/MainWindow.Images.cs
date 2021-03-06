// --------------------------------------------------
// 3DS Theme Editor - MainWindow.Images.cs
// --------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ThemeEditor.Common.Graphics;
using ThemeEditor.WPF.Localization;
using ThemeEditor.WPF.Markup;

namespace ThemeEditor.WPF
{
    partial class MainWindow
    {
        static readonly string[] VALID_IMAGE_EXT = { ".jpg", ".jpeg", ".png", ".bmp" };

        public ICommand CopyResizeSMDHIconCommandCommand { get; private set; }
        public ICommand DragImageCommand { get; set; }

        public ICommand DropBottomImageCommand { get; set; }
        public ICommand DropTopImageCommand { get; set; }
        public ICommand DropTopAltImageCommand { get; set; }
        public ICommand DropFileLargeImageCommand { get; set; }
        public ICommand DropFileSmallImageCommand { get; set; }
        public ICommand DropFolderOpenImageCommand { get; set; }
        public ICommand DropFolderClosedImageCommand { get; set; }

        public ICommand ExportImageCommand { get; private set; }
        public ICommand RemoveImageCommand { get; private set; }

        public ICommand ReplaceImageCommand { get; private set; }

        private bool CanExecute_ImageExists(TargetImage args)
        {
            if (!CanExecute_ViewModelLoaded())
                return false;
            switch (args)
            {
                case TargetImage.Top:
                    return ViewModel.Textures.Top.Exists;

                case TargetImage.Bottom:
                    return ViewModel.Textures.Bottom.Exists;

                case TargetImage.FileLarge:
                    return ViewModel.Textures.FileLarge.Exists;

                case TargetImage.FileSmall:
                    return ViewModel.Textures.FileSmall.Exists;

                case TargetImage.FolderOpen:
                    return ViewModel.Textures.FolderOpen.Exists;

                case TargetImage.FolderClosed:
                    return ViewModel.Textures.FolderClosed.Exists;
                case TargetImage.TopAlt:
                    return ViewModel.Textures.TopAlt.Exists;

                case TargetImage.SmallIcon:
                    return ViewModel.Info.SmallIcon.Exists;
                case TargetImage.LargeIcon:
                    return ViewModel.Info.LargeIcon.Exists;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DragImage_Execute(DragEventArgs args)
        {
            args.Handled = true;
            args.Effects = DragDropEffects.None;
            var hasFileDrop = args.Data.GetDataPresent(DataFormats.FileDrop);
            if (hasFileDrop)
            {
                var file = args.Data.GetData(DataFormats.FileDrop) as string[];
                if (file?.Length != 1)
                    return;
                var ext = Path.GetExtension(file[0]);
                if (VALID_IMAGE_EXT.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    args.Effects = DragDropEffects.Copy;
            }
        }

        private Task<LoadImageResults> DropImage_Execute(DragEventArgs args, TargetImage target)
        {
            var task = new Task<LoadImageResults>(() =>
            {
                var results = new LoadImageResults()
                {
                    Loaded = false,
                    Target = target
                };
                if (args.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])args.Data.GetData(DataFormats.FileDrop);
                    var file = files[0];
                    try
                    {
                        using (var fs = File.OpenRead(file))
                        {
                            BitmapImage bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.StreamSource = fs;
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();
                            bmp.Freeze();

                            results.OriginalWidth = bmp.PixelWidth;
                            results.OriginalHeight = bmp.PixelHeight;

                            var potBmp = bmp.CreateResizedNextPot();
                            potBmp.Freeze();

                            results.Loaded = true;
                            results.Image = potBmp;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                return results;
            });
            task.Start();
            args.Handled = true;
            return task;
        }

        private Task<SaveImageResults> ExportImage_Execute(TargetImage targetImage)
        {
            var busyPickingFile = MainResources.Busy_PickingFile;
            var busySavingImage = MainResources.Busy_SavingImage;

            BitmapSource target;
            switch (targetImage)
            {
                case TargetImage.Top:
                    target = ViewModel.Textures.Top.Bitmap;
                    break;
                case TargetImage.Bottom:
                    target = ViewModel.Textures.Bottom.Bitmap;
                    break;
                case TargetImage.FileLarge:
                    target = ViewModel.Textures.FileLarge.Bitmap;
                    break;
                case TargetImage.FileSmall:
                    target = ViewModel.Textures.FileSmall.Bitmap;
                    break;
                case TargetImage.FolderOpen:
                    target = ViewModel.Textures.FolderOpen.Bitmap;
                    break;
                case TargetImage.FolderClosed:
                    target = ViewModel.Textures.FolderClosed.Bitmap;
                    break;
                case TargetImage.TopAlt:
                    target = ViewModel.Textures.TopAlt.Bitmap;
                    break;
                case TargetImage.SmallIcon:
                    target = ViewModel.Info.SmallIcon.Bitmap;
                    break;
                case TargetImage.LargeIcon:
                    target = ViewModel.Info.LargeIcon.Bitmap;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }

            var task = new Task<SaveImageResults>(() =>
            {
                BusyText = busyPickingFile;
                var svfl = new SaveFileDialog
                {
                    Filter = "PNG Files|*.png",
                    FileName = targetImage.ToString().ToLower()
                };
                var dlg = svfl.ShowDialog();
                SaveImageResults results = new SaveImageResults
                {
                    Saved = false,
                    Target = targetImage,
                    Path = svfl.FileName
                };

                if (!dlg.HasValue || dlg.Value)
                {
                    BusyText = busySavingImage;
                    try
                    {
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(target));
                        using (var fs = File.Open(results.Path, FileMode.Create))
                        {
                            encoder.Save(fs);
                            results.Saved = true;
                        }
                    }
                    catch
                    {
                        // Ignore;
                    }
                }
                return results;
            });
            task.Start();
            return task;
        }

        private void ExportImage_PostExecute(SaveImageResults obj)
        {
            IsBusy = false;
        }

        private Task<LoadImageResults> LoadImage_Execute(TargetImage targetImage)
        {
            var busyPickingFile = MainResources.Busy_PickingFile;
            var busyOpeningImage = MainResources.Busy_OpeningImage;
            var task = new Task<LoadImageResults>(() =>
            {
                BusyText = busyPickingFile;
                var opfl = new OpenFileDialog
                {
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                    Multiselect = false
                };
                var dlg = opfl.ShowDialog();
                LoadImageResults results = new LoadImageResults
                {
                    Loaded = false,
                    Target = targetImage
                };
                if (!dlg.HasValue || dlg.Value)
                {
                    BusyText = busyOpeningImage;
                    try
                    {
                        using (var fs = File.OpenRead(opfl.FileName))
                        {
                            BitmapImage bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.StreamSource = fs;
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();
                            bmp.Freeze();

                            results.OriginalWidth = bmp.PixelWidth;
                            results.OriginalHeight = bmp.PixelHeight;

                            BitmapSource bmpSrc;

                            switch (targetImage)
                            {
                                case TargetImage.Bottom:
                                case TargetImage.Top:
                                case TargetImage.FileLarge:
                                case TargetImage.FileSmall:
                                case TargetImage.FolderOpen:
                                case TargetImage.FolderClosed:
                                case TargetImage.TopAlt:
                                    bmpSrc = bmp.CreateResizedNextPot();
                                    bmpSrc.Freeze();
                                    break;
                                case TargetImage.SmallIcon:
                                case TargetImage.LargeIcon:
                                default:
                                    bmpSrc = bmp;
                                    break;
                            }

                            results.Loaded = true;
                            results.Image = bmpSrc;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                return results;
            });
            task.Start();
            return task;
        }

        private void LoadImage_PostExecute(LoadImageResults args)
        {
            var errInvalidSize = MainResources.Message_InvalidImageSize;
            var errValidSize = MainResources.Message_InvalidImageSize_AllowedSizes;
            if (args.Loaded)
            {
                var bmp = args.Image;
                var bmpW = bmp.PixelWidth;
                var bmpH = bmp.PixelHeight;
                var target = args.Target;

                var validSizes = _validImageSizes[target];
                try
                {
                    var targetSize = validSizes.First(pair => pair.X == bmpW && pair.Y == bmpH);
                    switch (target)
                    {
                        case TargetImage.Top:
                            {
                                ViewModel.Textures.Top.EncodeTexture(args.Image, targetSize.Format);
                                ViewModel.Textures.Top.EdgeBleed(0, 0, args.OriginalWidth, args.OriginalHeight);
                                break;
                            }
                        case TargetImage.Bottom:
                            {
                                ViewModel.Textures.Bottom.EncodeTexture(args.Image, targetSize.Format);
                                ViewModel.Textures.Bottom.EdgeBleed(0, 0, args.OriginalWidth, args.OriginalHeight);
                                break;
                            }
                        case TargetImage.FileLarge:
                            {
                                ViewModel.Textures.FileLarge.EncodeTexture(args.Image, targetSize.Format);
                                ViewModel.Textures.FileLarge.EdgeBleed(0, 0, args.OriginalWidth, args.OriginalHeight);
                                break;
                            }
                        case TargetImage.FileSmall:
                            {
                                ViewModel.Textures.FileSmall.EncodeTexture(args.Image, targetSize.Format);
                                ViewModel.Textures.FileSmall.EdgeBleed(0, 0, args.OriginalWidth, args.OriginalHeight);
                                break;
                            }
                        case TargetImage.FolderOpen:
                            {
                                ViewModel.Textures.FolderOpen.EncodeTexture(args.Image, targetSize.Format);
                                ViewModel.Textures.FolderOpen.EdgeBleed(0, 0, args.OriginalWidth, args.OriginalHeight);
                                break;
                            }
                        case TargetImage.FolderClosed:
                            {
                                ViewModel.Textures.FolderClosed.EncodeTexture(args.Image, targetSize.Format);
                                ViewModel.Textures.FolderClosed.EdgeBleed(0, 0, args.OriginalWidth, args.OriginalHeight);
                                break;
                            }
                        case TargetImage.TopAlt:
                            {
                                ViewModel.Textures.TopAlt.EncodeTexture(args.Image, targetSize.Format);
                                ViewModel.Textures.TopAlt.EdgeBleed(0, 0, args.OriginalWidth, args.OriginalHeight);
                                break;
                            }
                        case TargetImage.SmallIcon:
                            {
                                ViewModel.Info.SmallIcon.EncodeTexture(args.Image, targetSize.Format);
                                break;
                            }
                        case TargetImage.LargeIcon:
                            {
                                ViewModel.Info.LargeIcon.EncodeTexture(args.Image, targetSize.Format);
                                var sml = Extensions.CreateResizedImage(args.Image, 24, 24);
                                ViewModel.Info.SmallIcon.EncodeTexture((BitmapSource)sml, targetSize.Format);
                                break;
                            }
                        default:
                            {
                                throw new ArgumentOutOfRangeException();
                            }
                    }
                    
                }
                catch (InvalidOperationException)
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat("{2}: {0}x{1}\n", bmpW, bmpH, errInvalidSize);
                    sb.AppendFormat("{0}:\n", errValidSize);
                    foreach (var pair in validSizes)
                        sb.AppendFormat("\t- {0}x{1}\n", pair.X, pair.Y);

                    MessageBox.Show(sb.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            IsBusy = false;
        }

        private void RemoveImage_Execute(TargetImage args)
        {
            switch (args)
            {
                case TargetImage.Top:
                    ViewModel.Textures.Top.ClearTexture();
                    break;
                case TargetImage.Bottom:
                    ViewModel.Textures.Bottom.ClearTexture();
                    break;
                case TargetImage.FileLarge:
                    ViewModel.Textures.FileLarge.ClearTexture();
                    break;
                case TargetImage.FileSmall:
                    ViewModel.Textures.FileSmall.ClearTexture();
                    break;
                case TargetImage.FolderOpen:
                    ViewModel.Textures.FolderOpen.ClearTexture();
                    break;
                case TargetImage.FolderClosed:
                    ViewModel.Textures.FolderClosed.ClearTexture();
                    break;
                case TargetImage.TopAlt:
                    ViewModel.Textures.TopAlt.ClearTexture();
                    break;
                case TargetImage.SmallIcon:
                    {
                        var icex = new IconExtension(@"/ThemeEditor.WPF;component/Resources/Icons/app_icn.ico", 24);
                        var large = ((BitmapSource)icex.ProvideValue(null)).CreateResizedImage(24, 24);
                        ViewModel.Info.SmallIcon.EncodeTexture(large, RawTexture.DataFormat.Bgr565);
                        break;
                    }
                case TargetImage.LargeIcon:
                    {
                        var icex = new IconExtension(@"/ThemeEditor.WPF;component/Resources/Icons/app_icn.ico", 48);
                        var large = ((BitmapSource)icex.ProvideValue(null)).CreateResizedImage(48, 48);
                        ViewModel.Info.LargeIcon.EncodeTexture(large, RawTexture.DataFormat.Bgr565);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        partial void SetupImageCommands()
        {
            // Menus

            RemoveImageCommand = new RelayCommand<TargetImage>(RemoveImage_Execute, CanExecute_ImageExists);
            ReplaceImageCommand = new RelayCommandAsync<TargetImage, LoadImageResults>(LoadImage_Execute,
                image => CanExecute_ViewModelLoaded(),
                image => PreExecute_SetBusy(),
                LoadImage_PostExecute);
            ExportImageCommand = new RelayCommandAsync<TargetImage, SaveImageResults>(ExportImage_Execute,
                CanExecute_ImageExists,
                image => PreExecute_SetBusy(),
                ExportImage_PostExecute);

            // Drop Targets

            DragImageCommand = new RelayCommand<DragEventArgs>(DragImage_Execute, image => CanExecute_ViewModelLoaded());

            Func<TargetImage, ICommand> genCommand = target => new RelayCommandAsync<DragEventArgs, LoadImageResults>(
                e => DropImage_Execute(e, target),
                image => CanExecute_ViewModelLoaded(),
                image => PreExecute_SetBusy(),
                LoadImage_PostExecute);

            DropTopImageCommand = genCommand(TargetImage.Top);
            DropTopAltImageCommand = genCommand(TargetImage.TopAlt);
            DropBottomImageCommand = genCommand(TargetImage.Bottom);
            DropFileLargeImageCommand = genCommand(TargetImage.FileLarge);
            DropFileSmallImageCommand = genCommand(TargetImage.FileSmall);
            DropFolderClosedImageCommand = genCommand(TargetImage.FolderClosed);
            DropFolderOpenImageCommand = genCommand(TargetImage.FolderOpen);

            // SMDH Icon

            CopyResizeSMDHIconCommandCommand = new RelayCommand<bool>(CopySMDHLargeToSmall_Execute);
        }

        private void CopySMDHLargeToSmall_Execute(bool direction)
        {
            if (direction)
            {
                var sml = Extensions.CreateResizedImage(ViewModel.Info.LargeIcon.Bitmap, 24, 24);
                ViewModel.Info.SmallIcon.EncodeTexture((BitmapSource)sml, ViewModel.Info.SmallIcon.DataFormat);
            }
            else
            {
                var sml = Extensions.CreateResizedImage(ViewModel.Info.SmallIcon.Bitmap, 48, 48);
                ViewModel.Info.LargeIcon.EncodeTexture((BitmapSource)sml, ViewModel.Info.LargeIcon.DataFormat);
            }
        }

        private class SaveImageResults
        {
            public string Path;
            public bool Saved;
            public TargetImage Target;
        }

        private class LoadImageResults
        {
            public int OriginalWidth;
            public int OriginalHeight;
            public BitmapSource Image;
            public bool Loaded;
            public TargetImage Target;
        }
    }
}