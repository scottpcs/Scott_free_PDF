using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PdfiumViewer;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Interop;
using System.Drawing.Drawing2D;

namespace PdfEditorApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private PdfDocument _document;
        private int _currentPageIndex;
        private double _zoomFactor = 1.0;
        private string _currentFilePath;
        private BitmapSource _currentPageImage;
        private ObservableCollection<BitmapSource> _pageImages;
        private float _renderDpi = 600f; // Increased from 300 to 600 for higher quality

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            PageImages = new ObservableCollection<BitmapSource>();
            CurrentPageIndex = 0;
        }

        public ObservableCollection<BitmapSource> PageImages
        {
            get { return _pageImages; }
            set
            {
                _pageImages = value;
                OnPropertyChanged();
            }
        }

        public BitmapSource CurrentPageImage
        {
            get { return _currentPageImage; }
            set
            {
                _currentPageImage = value;
                OnPropertyChanged();
            }
        }

        public int CurrentPageIndex
        {
            get { return _currentPageIndex; }
            set
            {
                if (value < 0)
                    _currentPageIndex = 0;
                else if (PageImages != null && value >= PageImages.Count && PageImages.Count > 0)
                    _currentPageIndex = PageImages.Count - 1;
                else
                    _currentPageIndex = value;

                if (PageImages != null && PageImages.Count > 0)
                    CurrentPageImage = PageImages[_currentPageIndex];
                
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPageDisplay));
                OnPropertyChanged(nameof(TotalPages));
            }
        }

        public string CurrentPageDisplay => (CurrentPageIndex + 1).ToString();

        public string TotalPages => PageImages?.Count.ToString() ?? "0";

        public double ZoomFactor
        {
            get { return _zoomFactor; }
            set
            {
                _zoomFactor = value;
                OnPropertyChanged();
                RefreshCurrentPage();
            }
        }

        public string ZoomPercentage => $"{Math.Round(ZoomFactor * 100)}%";

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Open PDF File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadPdfFile(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadPdfFile(string filePath)
        {
            // Clean up previous document if any
            if (_document != null)
            {
                _document.Dispose();
                _document = null;
            }

            _currentFilePath = filePath;
            
            // Load the PDF file using PdfiumViewer
            _document = PdfDocument.Load(filePath);

            PageImages.Clear();
            
            // Render and cache all pages
            for (int i = 0; i < _document.PageCount; i++)
            {
                var pageImage = RenderPage(i);
                PageImages.Add(pageImage);
            }

            // Force a garbage collection to release memory
            GC.Collect();
            GC.WaitForPendingFinalizers();

            CurrentPageIndex = 0;
            Title = $"PDF Editor - {Path.GetFileName(filePath)}";
            StatusText.Text = $"File loaded: {filePath}";
        }

        private BitmapSource RenderPage(int pageIndex)
        {
            // Calculate actual size based on zoom factor
            var pageSize = _document.PageSizes[pageIndex];
            
            // Use higher DPI for better quality
            float baseDpi = _renderDpi;
            
            // Calculate width and height based on zoom
            int width = (int)(pageSize.Width * ZoomFactor);
            int height = (int)(pageSize.Height * ZoomFactor);
            
            // Render the page with high quality settings
            using (var image = _document.Render(pageIndex, baseDpi, baseDpi, 
                                PdfRenderFlags.Annotations | PdfRenderFlags.LcdText | 
                                PdfRenderFlags.Transparent))
            {
                // Create a high-quality bitmap with 32bpp ARGB format for maximum detail
                using (var resizedBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                {
                    using (var graphics = Graphics.FromImage(resizedBitmap))
                    {
                        // Set high quality resizing options
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                        
                        // Clear with white background
                        graphics.Clear(Color.White);
                        
                        // Draw the image at the new size
                        graphics.DrawImage(image, 0, 0, width, height);
                    }
                    
                    // Convert to WPF BitmapSource
                    return ConvertBitmapToBitmapSource(resizedBitmap);
                }
            }
        }

        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            // Always use 32bit BGRA format for highest quality
            System.Windows.Media.PixelFormat pixelFormat = System.Windows.Media.PixelFormats.Bgra32;

            // Lock the bitmap for access to the pixel data
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);

            try
            {
                // Create a WPF BitmapSource with the appropriate pixel format
                BitmapSource bitmapSource = BitmapSource.Create(
                    bitmapData.Width, bitmapData.Height,
                    96, 96, // Keep DPI consistent at 96 for display
                    pixelFormat,
                    null,
                    bitmapData.Scan0,
                    bitmapData.Stride * bitmapData.Height,
                    bitmapData.Stride);
                
                // Freeze the bitmap to improve performance
                bitmapSource.Freeze();
                return bitmapSource;
            }
            finally
            {
                // Always unlock the bitmap
                bitmap.UnlockBits(bitmapData);
            }
        }

        private void RefreshCurrentPage()
        {
            if (_document != null && CurrentPageIndex < _document.PageCount)
            {
                var pageImage = RenderPage(CurrentPageIndex);
                if (CurrentPageIndex < PageImages.Count)
                {
                    PageImages[CurrentPageIndex] = pageImage;
                    CurrentPageImage = pageImage;
                }
            }
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            CurrentPageIndex--;
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            CurrentPageIndex++;
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomFactor *= 1.25;
            OnPropertyChanged(nameof(ZoomPercentage));
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomFactor /= 1.25;
            OnPropertyChanged(nameof(ZoomPercentage));
        }

        private void ZoomReset_Click(object sender, RoutedEventArgs e)
        {
            ZoomFactor = 1.0;
            OnPropertyChanged(nameof(ZoomPercentage));
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Saving functionality is not implemented in this simplified version.", 
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveFileAs_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Save As functionality is not implemented in this simplified version.", 
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddTextAnnotation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Text annotation functionality is not implemented in this simplified version.", 
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GoToPage_Click(object sender, RoutedEventArgs e)
        {
            if (_document == null)
                return;

            var inputDialog = new PageInputDialog(_document.PageCount);
            if (inputDialog.ShowDialog() == true)
            {
                CurrentPageIndex = inputDialog.PageNumber - 1;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Clean up resources
            if (_document != null)
            {
                _document.Dispose();
                _document = null;
            }
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    public class PageInputDialog : Window
    {
        private TextBox textBox;
        private int maxPage;
        
        public int PageNumber { get; private set; }
        
        public PageInputDialog(int maxPageNumber)
        {
            maxPage = maxPageNumber;
            
            Title = "Go to Page";
            Width = 250;
            Height = 120;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var label = new TextBlock
            {
                Text = $"Enter page number (1-{maxPage}):",
                Margin = new Thickness(10, 10, 10, 5)
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);
            
            textBox = new TextBox
            {
                Margin = new Thickness(10, 0, 10, 10)
            };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10, 0, 10, 10)
            };
            Grid.SetRow(buttonPanel, 2);
            
            var okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Height = 25,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += OkButton_Click;
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 75,
                Height = 25,
                IsCancel = true
            };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }
        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(textBox.Text, out int pageNum) && pageNum >= 1 && pageNum <= maxPage)
            {
                PageNumber = pageNum;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show($"Please enter a valid page number between 1 and {maxPage}.", 
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}