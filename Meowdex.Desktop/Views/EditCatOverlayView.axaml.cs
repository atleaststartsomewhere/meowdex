using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Meowdex.Desktop.ViewModels;
using System.Collections.Generic;

namespace Meowdex.Desktop.Views;

public sealed partial class EditCatOverlayView : UserControl
{
    public EditCatOverlayView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        AddHandler(InputElement.GotFocusEvent, OnAnyControlGotFocus, RoutingStrategies.Bubble);
        AddHandler(InputElement.KeyDownEvent, OnAnyControlKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        Dispatcher.UIThread.Post(() => Root.Focus(), DispatcherPriority.Input);
    }

    private void OnAnyControlGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (e.NavigationMethod != NavigationMethod.Tab)
        {
            return;
        }

        NumericUpDown? numeric = e.Source as NumericUpDown;
        if (numeric is null && e.Source is Control sourceControl)
        {
            numeric = sourceControl.FindAncestorOfType<NumericUpDown>();
        }

        if (numeric is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            FocusNumericForDirectEntry(numeric);
        }, DispatcherPriority.Input);
    }

    private static void FocusNumericForDirectEntry(NumericUpDown numeric)
    {
        var textBox = numeric.FindDescendantOfType<TextBox>();
        if (textBox is null)
        {
            numeric.Focus();
            return;
        }

        textBox.Focus();
        textBox.SelectAll();
    }

    private void OnAnyControlKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not EditCatOverlayViewModel vm)
        {
            return;
        }

        if (e.Key == Key.Tab)
        {
            if (HandleTabCycle(e))
            {
                e.Handled = true;
            }

            return;
        }

        if (e.Key == Key.Enter)
        {
            vm.SaveCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            vm.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }

    private bool HandleTabCycle(KeyEventArgs e)
    {
        var tabOrder = GetTabOrder();
        if (tabOrder.Count == 0)
        {
            return false;
        }

        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
        var owningTabStop = ResolveOwningTabStop(focused ?? e.Source as Control, tabOrder);
        var shiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        int currentIndex = owningTabStop is null ? -1 : tabOrder.IndexOf(owningTabStop);
        int nextIndex;
        if (currentIndex < 0)
        {
            nextIndex = shiftPressed ? tabOrder.Count - 1 : 0;
        }
        else if (shiftPressed)
        {
            nextIndex = (currentIndex - 1 + tabOrder.Count) % tabOrder.Count;
        }
        else
        {
            nextIndex = (currentIndex + 1) % tabOrder.Count;
        }

        FocusTabTarget(tabOrder[nextIndex], shiftPressed);
        return true;
    }

    private List<Control> GetTabOrder()
    {
        return new List<Control>
        {
            StrCurrentInput,
            StrBaseInput,
            DexCurrentInput,
            DexBaseInput,
            StamCurrentInput,
            StamBaseInput,
            IntCurrentInput,
            IntBaseInput,
            SpeedCurrentInput,
            SpeedBaseInput,
            CharCurrentInput,
            CharBaseInput,
            LuckCurrentInput,
            LuckBaseInput,
            SaveButton,
            CancelButton,
            CloseButton,
            NameInput,
            GenderInput,
            SexualityInput,
            RetiredInput,
            NotesInput
        };
    }

    private static Control? ResolveOwningTabStop(Control? source, IReadOnlyList<Control> tabOrder)
    {
        var current = source;
        while (current is not null)
        {
            for (int i = 0; i < tabOrder.Count; i++)
            {
                if (ReferenceEquals(tabOrder[i], current))
                {
                    return current;
                }
            }

            current = current.GetVisualParent() as Control;
        }

        return null;
    }

    private static void FocusTabTarget(Control control, bool shiftPressed)
    {
        var modifiers = shiftPressed ? KeyModifiers.Shift : KeyModifiers.None;
        if (control is NumericUpDown numeric)
        {
            FocusNumericForDirectEntry(numeric, modifiers);
            return;
        }

        control.Focus(NavigationMethod.Tab, modifiers);
        if (control is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private static void FocusNumericForDirectEntry(NumericUpDown numeric, KeyModifiers modifiers)
    {
        var textBox = numeric.FindDescendantOfType<TextBox>();
        if (textBox is null)
        {
            numeric.Focus(NavigationMethod.Tab, modifiers);
            return;
        }

        textBox.Focus(NavigationMethod.Tab, modifiers);
        textBox.SelectAll();
    }
}

