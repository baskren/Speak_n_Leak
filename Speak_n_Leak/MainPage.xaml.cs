using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Speak_n_Leak
{

    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        bool speaking;
        int count = 0;

        void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            speaking = !speaking;
            if (speaking)
                Speak();
        }

        async Task Speak()
        {
            while (speaking)
            {
                button.Text = "Speak n Leak : " + (++count);
                await Xamarin.Ess.TextToSpeech.SpeakAsync(count.ToString());
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Xamarin.Ess.TextToSpeech.SpeakAsync("Tap Speak n Leak to begin, tap again to stop.");
        }
    }
}
