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
            // Set rotation resolution to 1 degree of sensitivity.
            radialController.RotationResolutionInDegrees = 5;

            // Declare input handlers for the RadialController.
            //radialController.ButtonClicked += (sender, args) =>
            //{ RadialController_ButtonClicked(sender, args); };
            //radialController.RotationChanged += (sender, args) =>
            //{ RadialController_RotationChanged(sender, args); };
            radialController.RotationChanged += RadialController_RotationChanged;
            radialController.ControlAcquired += RadialController_ControlAcquired;
            radialController.ButtonClicked += RadialController_ButtonClicked;

            //radialController.ControlAcquired += (sender, args) =>
            //{ RadialController_ControlAcquired(sender, args); };
            //radialController.ControlLost += (sender, args) =>
            //{ RadialController_ControlLost(sender, args); };
            //radialController.ScreenContactStarted += (sender, args) =>
            //{ RadialController_ScreenContactStarted(sender, args); };
            //radialController.ScreenContactContinued += (sender, args) =>
            //{ RadialController_ScreenContactContinued(sender, args); };
            //radialController.ScreenContactEnded += (sender, args) =>
            //{ RadialController_ScreenContactEnded(sender, args); };
            //AddToLog("Input handlers created");

            // Create the custom menu items.
            //CreateMenuItems();
            // Specify the menu items.
            //ConfigureMenu();

            AddCustomItems();
            SetDefaultItems();
        }

        private void RadialController_ControlAcquired(RadialController sender, RadialControllerControlAcquiredEventArgs args)
        {
            
        }


        // Occurs when the wheel device is rotated while a custom 
        // RadialController tool is active.
        // NOTE: Your app does not receive this event when the RadialController 
        // menu is active or a built-in tool is active
        // Send rotation input to slider of active region.
        private void RadialController_RotationChanged(RadialController sender,
          RadialControllerRotationChangedEventArgs args)
        {
            
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
            radialController.Menu.Items.Add(RadialControllerMenuItem.CreateFromKnownIcon("Ruler", RadialControllerMenuKnownIcon.Ruler));

            AddItemsFromFont();
        }

        private void AddItemsFromFont()
        {
            // Using system font
            radialController.Menu.Items.Add(RadialControllerMenuItem.CreateFromFontGlyph("System Font Icon", "\xD83D\xDC31\x200D\xD83D\xDC64", "Segoe UI Emoji"));
        }

        private void SetDefaultItems()
        {
            RadialControllerConfiguration radialControllerConfig;
            IRadialControllerConfigurationInterop radialControllerConfigInterop = (IRadialControllerConfigurationInterop)System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.GetActivationFactory(typeof(RadialControllerConfiguration));
            Guid guid = typeof(RadialControllerConfiguration).GetInterface("IRadialControllerConfiguration").GUID;

            radialControllerConfig = radialControllerConfigInterop.GetForWindow(new WindowInteropHelper(this).Handle, ref guid);
            radialControllerConfig.SetDefaultMenuItems(new[] { RadialControllerSystemMenuItemKind.Volume, RadialControllerSystemMenuItemKind.Scroll });
            radialControllerConfig.TrySelectDefaultMenuItem(RadialControllerSystemMenuItemKind.Scroll);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeController();
        }
    }
}
