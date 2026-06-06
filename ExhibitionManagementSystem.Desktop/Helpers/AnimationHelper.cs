using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace ExhibitionManagementSystem.Desktop.Helpers
{
    public static class AnimationHelper
    {
        public static readonly DependencyProperty HoverShadowProperty =
            DependencyProperty.RegisterAttached(
                "HoverShadow",
                typeof(bool),
                typeof(AnimationHelper),
                new PropertyMetadata(false, OnHoverShadowChanged));

        public static bool GetHoverShadow(DependencyObject obj) => (bool)obj.GetValue(HoverShadowProperty);
        public static void SetHoverShadow(DependencyObject obj, bool value) => obj.SetValue(HoverShadowProperty, value);

        private static void OnHoverShadowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.MouseEnter += Element_MouseEnter;
                    element.MouseLeave += Element_MouseLeave;
                }
                else
                {
                    element.MouseEnter -= Element_MouseEnter;
                    element.MouseLeave -= Element_MouseLeave;
                }
            }
        }

        private static void Element_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is UIElement element)
            {
                var effect = element.Effect as DropShadowEffect;
                if (effect == null)
                {
                    effect = new DropShadowEffect
                    {
                        Color = System.Windows.Media.Color.FromArgb(25, 0, 0, 0),
                        BlurRadius = 8,
                        ShadowDepth = 2,
                        Opacity = 0.1
                    };
                    element.Effect = effect;
                }

                if (effect.IsFrozen)
                {
                    effect = effect.Clone();
                    element.Effect = effect;
                }

                var blurAnim = new DoubleAnimation(25, TimeSpan.FromSeconds(0.3))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                var depthAnim = new DoubleAnimation(6, TimeSpan.FromSeconds(0.3))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                var opacityAnim = new DoubleAnimation(0.2, TimeSpan.FromSeconds(0.3))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                effect.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
                effect.BeginAnimation(DropShadowEffect.ShadowDepthProperty, depthAnim);
                effect.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
            }
        }

        private static void Element_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is UIElement element)
            {
                if (element.Effect is DropShadowEffect effect)
                {
                    if (effect.IsFrozen)
                    {
                        effect = effect.Clone();
                        element.Effect = effect;
                    }

                    var blurAnim = new DoubleAnimation(8, TimeSpan.FromSeconds(0.3))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var depthAnim = new DoubleAnimation(2, TimeSpan.FromSeconds(0.3))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var opacityAnim = new DoubleAnimation(0.1, TimeSpan.FromSeconds(0.3))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    };

                    effect.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
                    effect.BeginAnimation(DropShadowEffect.ShadowDepthProperty, depthAnim);
                    effect.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
                }
            }
        }
    }
}
