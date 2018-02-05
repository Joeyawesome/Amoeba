using System;
using System.Windows;

namespace Amoeba.Interface
{
    /// <summary>
    /// UploadDirectoryInfoEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class UploadDirectoryInfoEditWindow : Window
    {
        public UploadDirectoryInfoEditWindow(UploadDirectoryInfoEditWindowViewModel viewModel)
        {
            this.DataContext = viewModel;
            viewModel.CloseEvent += (sender, e) => this.Close();

            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            this.MaxHeight = this.RenderSize.Height;
            this.MinHeight = this.RenderSize.Height;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (this.DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
