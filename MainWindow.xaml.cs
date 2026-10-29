using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RamTracking.Models;
using RamTracking.ViewModels;

namespace RamTracking;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const double MenuExpandedWidth = 210;
    private const double MenuCollapsedWidth = 62;

    public MainWindow()
    {
        InitializeComponent();
        // Vincular el evento del SlotMap al manejador de la ventana
        var slotMap = (Controls.SlotMap?)FindName("SlotMap");
        if (slotMap != null)
        {
            slotMap.ModuleSelectedChanged += OnModuleSelectedChanged;
        }
    }

    /// <summary>
    /// Manejador del evento de cambio de módulo seleccionado en SlotMap.
    /// </summary>
    private void OnModuleSelectedChanged(MemoryModule? module)
    {
        // El seleccionado se refleja automáticamente a través de la propiedad SelectedModule
    }

    /// <summary>
    /// Expande o contrae el menú lateral con una animación suave.
    /// El ancho base lo aporta el binding a IsMenuExpanded; la animación
    /// interpola desde el ancho actual para evitar saltos.
    /// </summary>
    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        vm.ToggleMenuCommand.Execute(null);
        AnimateMenu(vm.IsMenuExpanded);
    }

    private void AnimateMenu(bool expanded)
    {
        if (MenuPanel is null)
            return;

        double from = MenuPanel.ActualWidth > 0 ? MenuPanel.ActualWidth : MenuPanel.Width;
        double to = expanded ? MenuExpandedWidth : MenuCollapsedWidth;

        MenuPanel.BeginAnimation(WidthProperty, new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(260),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        });
    }

    /// <summary>
    /// Anima la entrada del contenido cada vez que se cambia de sección
    /// desde el menú hamburguesa.
    /// </summary>
    private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SectionHost is null)
            return;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(280);

        if (SectionHost.RenderTransform is TranslateTransform transform)
        {
            transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
            {
                From = 14,
                To = 0,
                Duration = duration,
                EasingFunction = ease
            });
        }

        SectionHost.BeginAnimation(OpacityProperty, new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = duration,
            EasingFunction = ease
        });
    }
}
