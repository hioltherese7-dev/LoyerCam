using Loyercam.Models;
using Loyercam.PageModels;

namespace Loyercam.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}