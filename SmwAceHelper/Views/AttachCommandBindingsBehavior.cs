using Microsoft.Xaml.Behaviors;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;

namespace SmwAceHelper.Views
{
    public class AttachCommandBindingsBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty CommandBindingsProperty = DependencyProperty.Register(
            nameof(CommandBindings),
            typeof(ObservableCollection<CommandBinding>),
            typeof(AttachCommandBindingsBehavior),
            new PropertyMetadata(null, OnCommandBindingsChanged));

        public ObservableCollection<CommandBinding> CommandBindings
        {
            get { return (ObservableCollection<CommandBinding>)GetValue(CommandBindingsProperty); }
            set { SetValue(CommandBindingsProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            UpdateCommandBindings();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject?.CommandBindings.Clear();
        }

        private static void OnCommandBindingsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is AttachCommandBindingsBehavior behavior)
            {
                if (e.OldValue is ObservableCollection<CommandBinding> oldBindings)
                {
                    oldBindings.CollectionChanged -= behavior.OnCollectionChanged;
                }
                if (e.NewValue is ObservableCollection<CommandBinding> newBindings)
                {
                    newBindings.CollectionChanged += behavior.OnCollectionChanged;
                }
                behavior.UpdateCommandBindings();
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCommandBindings();
        }

        private void UpdateCommandBindings()
        {
            AssociatedObject?.CommandBindings.Clear();
            if (CommandBindings != null)
            {
                AssociatedObject?.CommandBindings.AddRange(CommandBindings);
            }
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
