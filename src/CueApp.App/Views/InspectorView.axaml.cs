using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace CueApp.App.Views;

/// <summary>Правая панель: заметки и инспектор выбранной кью.</summary>
public partial class InspectorView : UserControl
{
    public InspectorView()
    {
        InitializeComponent();

        // Ушли из поля — перечитать значение из кью. Если ввод не принят («abc» во времени,
        // занятый номер), поле вернется к прежнему значению; принятый ввод приведется к виду «1:05.5».
        // Avalonia сама этого не делает: значение в кью не изменилось — «обновлять нечего».
        AddHandler(LostFocusEvent, (_, e) =>
        {
            if (e.Source is TextBox { AcceptsReturn: false } textBox)
                Dispatcher.UIThread.Post(() =>
                    BindingOperations.GetBindingExpressionBase(textBox, TextBox.TextProperty)?.UpdateTarget());
        }, RoutingStrategies.Bubble, handledEventsToo: true);
    }
}
