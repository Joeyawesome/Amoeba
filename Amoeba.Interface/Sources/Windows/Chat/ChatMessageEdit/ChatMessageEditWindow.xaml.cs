using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Omnius.Configuration;
using Omnius.Wpf;

namespace Amoeba.Interface
{
    /// <summary>
    /// ChatMessageEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ChatMessageEditWindow : RestorableWindow
    {
        public ChatMessageEditWindow(ChatMessageEditWindowViewModel viewModel)
        {
            this.DataContext = viewModel;
            viewModel.CloseEvent += (sender, e) => this.Close();

            InitializeComponent();

            this.Icon = AmoebaEnvironment.Icons.AmoebaIcon;
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
