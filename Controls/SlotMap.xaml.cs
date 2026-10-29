using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RamTracking.Models;

namespace RamTracking.Controls;

/// <summary>
/// Control de usuario que muestra un mapa visual de los slots de memoria RAM.
/// Disponibilidad de módulos, información por slot y selección interactiva.
/// </summary>
public partial class SlotMap : UserControl
{
    /// <summary>
    /// Colección de módulos de memoria a mostrar en el mapa.
    /// </summary>
    public ObservableCollection<MemoryModule> Modules
    {
        get => (ObservableCollection<MemoryModule>)GetValue(ModulesProperty);
        set => SetValue(ModulesProperty, value);
    }

    /// <summary>
    /// Dependencia de la colección de módulos.
    /// </summary>
    public static readonly DependencyProperty ModulesProperty =
        DependencyProperty.Register(nameof(Modules), typeof(ObservableCollection<MemoryModule>),
            typeof(SlotMap), new PropertyMetadata(new ObservableCollection<MemoryModule>()));

    /// <summary>
    /// Colección enlazada para ItemsSource (compatibilidad XAML).
    /// </summary>
    public IEnumerable<object?>? ItemsSource
    {
        get => (IEnumerable<object?>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Dependencia de ItemsSource.
    /// </summary>
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<object?>),
            typeof(SlotMap), new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>
    /// Módulo actualmente seleccionado en el mapa.
    /// </summary>
    public MemoryModule? SelectedModule
    {
        get => (MemoryModule?)GetValue(SelectedModuleProperty);
        set => SetValue(SelectedModuleProperty, value);
    }

    /// <summary>
    /// Dependencia del módulo seleccionado.
    /// </summary>
    public static readonly DependencyProperty SelectedModuleProperty =
        DependencyProperty.Register(nameof(SelectedModule), typeof(MemoryModule),
            typeof(SlotMap), new PropertyMetadata(null, OnSelectedModuleChanged));

    /// <summary>
    /// Evento disparado cuando el módulo seleccionado cambia.
    /// </summary>
    public event Action<MemoryModule?>? ModuleSelectedChanged;

    /// <summary>
    /// Constructor por defecto.
    /// </summary>
    public SlotMap()
    {
        InitializeComponent();
        // Vincular ItemsSource a Modules cuando cambia
        SetBinding(ModulesProperty, new Binding { Source = this, Path = new PropertyPath("ItemsSource") });
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SlotMap)d;
        if (e.NewValue is IEnumerable<object?> newCollection)
        {
            control.Modules = new ObservableCollection<MemoryModule>(newCollection.OfType<MemoryModule>());
        }
    }

    private static void OnSelectedModuleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SlotMap)d;
        control.ModuleSelectedChanged?.Invoke(e.NewValue as MemoryModule);
    }
}