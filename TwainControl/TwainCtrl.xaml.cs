using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TwainWpf;
using TwainWpf.Wpf;

namespace TwainControl
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class TwainCtrl : UserControl, INotifyPropertyChanged, IDisposable
    {
        public static readonly RoutedCommand ScanImage = new RoutedCommand();

        private ScanSettings _settings;

        private bool arayüzetkin = true;

        private ObservableCollection<BitmapFrame> resimler;

        private string seçiliTarayıcı;

        private IList<string> tarayıcılar;

        private Twain twain;

        public TwainCtrl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool ArayüzEtkin
        {
            get => arayüzetkin;
            set
            {
                if (arayüzetkin != value)
                {
                    arayüzetkin = value;
                    OnPropertyChanged(nameof(ArayüzEtkin));
                }
            }
        }

        public ObservableCollection<BitmapFrame> Resimler
        {
            get => resimler;
            set
            {
                if (resimler != value)
                {
                    resimler = value;
                    OnPropertyChanged(nameof(Resimler));
                }
            }
        }

        public string SeçiliTarayıcı
        {
            get => seçiliTarayıcı;
            set
            {
                if (seçiliTarayıcı != value)
                {
                    seçiliTarayıcı = value;
                    OnPropertyChanged(nameof(SeçiliTarayıcı));
                }
            }
        }

        public IList<string> Tarayıcılar
        {
            get => tarayıcılar;
            set
            {
                if (tarayıcılar != value)
                {
                    tarayıcılar = value;
                    OnPropertyChanged(nameof(Tarayıcılar));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void HyperlinkSave_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Hyperlink)?.DataContext is BitmapFrame resim)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "Tif Resmi (*.tif)|*.tif|Jpg Resmi(*.jpg)|*.jpg" };
                if (saveFileDialog.ShowDialog() == true)
                {
                    switch (saveFileDialog.FilterIndex)
                    {
                        case 1:
                            switch (BlackAndWhiteCheckBox.IsChecked)
                            {
                                case true:
                                    {
                                        File.WriteAllBytes(saveFileDialog.FileName, resim.ToTiffJpegByteArray(Picture.Format.Tiff));
                                        break;
                                    }
                                case null:
                                case false:
                                    {
                                        File.WriteAllBytes(saveFileDialog.FileName, resim.ToTiffJpegByteArray(Picture.Format.TiffRenkli));
                                        break;
                                    }
                            }
                            break;
                        case 2:
                            File.WriteAllBytes(saveFileDialog.FileName, resim.ToTiffJpegByteArray(Picture.Format.Jpg));
                            break;
                    }
                }
            }
        }

        private void ScanButton_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = !Environment.Is64BitProcess;

        private void ScanButton_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ArayüzEtkin = false;
            _settings = new ScanSettings
            {
                UseDocumentFeeder = UseAdfCheckBox.IsChecked,
                ShowTwainUi = UseUiCheckBox.IsChecked ?? false,
                ShowProgressIndicatorUi = ShowProgressCheckBox.IsChecked,
                UseDuplex = UseDuplexCheckBox.IsChecked,
                ShouldTransferAllPages = true,
                Resolution = new ResolutionSettings { ColourSetting = BlackAndWhiteCheckBox.IsChecked ?? false ? ColourSetting.BlackAndWhite : ColourSetting.Colour, Dpi = (int?)IntInpResolution.Value },
                Rotation = new RotationSettings { AutomaticDeskew = UseDeskew.IsChecked ?? false, AutomaticRotate = AutoRotateCheckBox.IsChecked ?? false, AutomaticBorderDetection = AutoDetectBorderCheckBox.IsChecked ?? false }
            };
            if (Tarayıcılar.Count > 0)
            {
                twain.SelectSource(SeçiliTarayıcı);
                twain.StartScanning(_settings);
            }
        }

        #region IDisposable Support

        private bool disposedValue;

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Resimler = null;
                    twain = null;
                }

                disposedValue = true;
            }
        }

        #endregion IDisposable Support

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                twain = new Twain(new WindowMessageHook(Window.GetWindow(Parent)));
                Tarayıcılar = twain.SourceNames;
                if (twain.SourceNames?.Count > 0)
                {
                    SeçiliTarayıcı = twain.SourceNames[0];
                }

                Resimler = new ObservableCollection<BitmapFrame>();
                twain.TransferImage += (s, args) =>
                {
                    if (args.Image != null)
                    {
                        using (System.Drawing.Bitmap bmp = args.Image)
                        {
                            BitmapImage evrak = null;
                            switch (BlackAndWhiteCheckBox.IsChecked)
                            {
                                case true:
                                    evrak = bmp.ConvertBlackAndWhite((int)IntInpThreshold.Value).ToBitmapImage(ImageFormat.Tiff);
                                    break;

                                case null:
                                    evrak = bmp.ConvertBlackAndWhite((int)IntInpThreshold.Value, true).ToBitmapImage(ImageFormat.Jpeg);
                                    break;

                                case false:
                                    evrak = bmp.ToBitmapImage(ImageFormat.Jpeg);
                                    break;
                            }

                            evrak.Freeze();
                            BitmapSource önizleme = evrak.Resize(42, 59);
                            önizleme.Freeze();
                            BitmapFrame bitmapFrame = BitmapFrame.Create(evrak, önizleme);
                            bitmapFrame.Freeze();
                            Resimler.Add(bitmapFrame);
                            if (SeperateSaveCheckBox.IsChecked == true && BlackAndWhiteCheckBox.IsChecked == true)
                            {
                                File.WriteAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).SetUniqueFile($"{DateTime.Now.ToShortDateString()}Tarama", "tif"), evrak.ToTiffJpegByteArray(Picture.Format.Tiff));
                            }

                            if (SeperateSaveCheckBox.IsChecked == true && BlackAndWhiteCheckBox.IsChecked == false)
                            {
                                File.WriteAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).SetUniqueFile($"{DateTime.Now.ToShortDateString()}Tarama", "jpg"), evrak.ToTiffJpegByteArray(Picture.Format.Jpg));
                            }

                            evrak = null;
                            bitmapFrame = null;
                            önizleme = null;
                        }
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    }
                };
                twain.ScanningComplete += delegate { ArayüzEtkin = true; };
            }
            catch (Exception)
            {
            }
        }
    }
}
