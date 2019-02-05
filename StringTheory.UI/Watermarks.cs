using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace StringTheory.UI
{
    public static class Watermark
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.RegisterAttached(
            "Content",
            typeof(object),
            typeof(Watermark),
            new FrameworkPropertyMetadata(null, OnContentChanged));

        public static object GetContent(DependencyObject d) => d.GetValue(ContentProperty);
        public static void SetContent(DependencyObject d, object value) => d.SetValue(ContentProperty, value);

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.Loaded += OnLostFocus;
                textBox.GotKeyboardFocus += OnGotFocus;
                textBox.LostKeyboardFocus += OnLostFocus;
                textBox.TextChanged += OnGotFocus;
            }

            return;

            void OnGotFocus(object sender, RoutedEventArgs _)
            {
                Update(canRemove: true);
            }

            void OnLostFocus(object sender, RoutedEventArgs _)
            {
                Update(canRemove: false);
            }

            void Update(bool canRemove)
            {
                if (textBox.Text == "")
                {
                    ShowWatermark();
                }
                else if (canRemove)
                {
                    RemoveWatermark();
                }

                return;

                void RemoveWatermark()
                {
                    var layer = AdornerLayer.GetAdornerLayer(textBox);

                    // layer could be null if control is no longer in the visual tree
                    if (layer != null)
                    {
                        Adorner[] adorners = layer.GetAdorners(textBox);

                        if (adorners == null)
                        {
                            return;
                        }

                        foreach (Adorner adorner in adorners)
                        {
                            if (adorner is WatermarkAdorner)
                            {
                                adorner.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Hidden);
                                layer.Remove(adorner);
                            }
                        }
                    }
                }

                void ShowWatermark()
                {
                    var layer = AdornerLayer.GetAdornerLayer(textBox);
                    layer?.Add(new WatermarkAdorner(textBox, GetContent(textBox)));
                }
            }
        }

        private sealed class WatermarkAdorner : Adorner
        {
            private readonly ContentPresenter _contentPresenter;

            public WatermarkAdorner(UIElement adornedElement, object watermark)
                : base(adornedElement)
            {
                IsHitTestVisible = false;

                _contentPresenter = new ContentPresenter
                {
                    Content = watermark,
                    Opacity = 0.5,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(Control.Margin.Left + Control.Padding.Left, 0, 0, 0)
                };

                // Hide the control adorner when the adorned element is hidden
                SetBinding(VisibilityProperty, new Binding(nameof(IsVisible))
                {
                    Source = adornedElement,
                    Converter = new BooleanToVisibilityConverter()
                });
            }

            protected override int VisualChildrenCount => 1;

            private Control Control => (Control) AdornedElement;

            protected override Visual GetVisualChild(int index) => _contentPresenter;

            protected override Size MeasureOverride(Size constraint)
            {
                // Cover the whole control
                _contentPresenter.Measure(Control.RenderSize);
                return Control.RenderSize;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                _contentPresenter.Arrange(new Rect(finalSize));
                return finalSize;
            }
        }
    }
}