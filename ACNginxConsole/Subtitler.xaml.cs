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
using System.Windows.Interop;
// Begin "Step 4: Basic RadialController menu customization"
// Using directives for RadialController functionality.
using Windows.UI.Input;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
// End "Step 4: Basic RadialController menu customization"

namespace ACNginxConsole
{
    /// <summary>
    /// Subtitler.xaml 的交互逻辑
    /// </summary>
    public partial class Subtitler : Window
    {
        private RadialController radialController;
        private RadialControllerConfiguration radialControllerConfig;

        public Subtitler()
        {
            InitializeComponent();
            
        }

        // Create and configure our radial controller.
        private void InitializeController()
        {
            // Create a reference to the RadialController.
            CreateController();
            // Set rotation resolution to 5 degree of sensitivity.
            radialController.RotationResolutionInDegrees = 5;

            radialController.RotationChanged += RadialController_RotationChanged;
            radialController.ButtonClicked += RadialController_ButtonClicked;

            AddCustomItems();
        }

        // Occurs when the wheel device is rotated while a custom 
        // RadialController tool is active.
        // NOTE: Your app does not receive this event when the RadialController 
        // menu is active or a built-in tool is active
        // Send rotation input to slider of active region.
        private void RadialController_RotationChanged(RadialController sender,
          RadialControllerRotationChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Rotated");
            //args.RotationDeltaInDegrees
            InvalidateVisual();
        }

        // Occurs when the wheel device is pressed and then released 
        // while a customRadialController tool is active.
        // NOTE: Your app does not receive this event when the RadialController 
        // menu is active or a built-in tool is active
        // Send click input to toggle button of active region.
        private void RadialController_ButtonClicked(RadialController sender,
          RadialControllerButtonClickedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Clicked");
            InvalidateVisual();
        }

        [System.Runtime.InteropServices.Guid("1B0535C9-57AD-45C1-9D79-AD5C34360513")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIInspectable)]
        interface IRadialControllerInterop
        {
            RadialController CreateForWindow(
            IntPtr hwnd,
            [System.Runtime.InteropServices.In] ref Guid riid);
        }

        [System.Runtime.InteropServices.Guid("787cdaac-3186-476d-87e4-b9374a7b9970")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIInspectable)]
        interface IRadialControllerConfigurationInterop
        {
            RadialControllerConfiguration GetForWindow(
            IntPtr hwnd,
            [System.Runtime.InteropServices.In] ref Guid riid);
        }

        private void CreateController()
        {
            IRadialControllerInterop interop = (IRadialControllerInterop)System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.GetActivationFactory(typeof(RadialController));
            Guid guid = typeof(RadialController).GetInterface("IRadialController").GUID;

            radialController = interop.CreateForWindow(new WindowInteropHelper(this).Handle, ref guid);
        }

        private void AddCustomItems()
        {
            radialController.Menu.Items.Add(RadialControllerMenuItem.CreateFromFontGlyph("字幕机", "⌨", "Segoe UI Emoji"));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeController();
        }
    }
}
