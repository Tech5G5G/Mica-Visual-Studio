using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using MicaVisualStudio.Extensions;
using Microsoft.VisualStudio.Shell.Interop;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Expression = System.Linq.Expressions.Expression;

namespace MicaVisualStudio.VisualStudio;

public partial class VsWindowStyler
{
    #region Functions

    private Func<IVsWindowFrame, DependencyObject> get_WindowFrame_FrameView;
    private Func<DependencyObject, object> get_View_Content;
    private Func<DependencyObject, bool> get_View_IsActive;
    private Func<object, bool> IsDockTarget;

    #endregion

    private DependencyProperty View_ContentProperty, View_IsActiveProperty;

    private ILHook visualHook, sourceHook;

    private void Compile()
    {
        #region Functions

        var frameViewProp = Type.GetType("Microsoft.VisualStudio.Platform.WindowManagement.WindowFrame, Microsoft.VisualStudio.Platform.WindowManagement")
                                .GetProperty("FrameView");

        var frameParam = Expression.Parameter(typeof(IVsWindowFrame));
        get_WindowFrame_FrameView = frameParam.Convert(frameViewProp.DeclaringType)
                                              .Property(frameViewProp)
                                              .Convert<DependencyObject>()
                                              .Compile<IVsWindowFrame, DependencyObject>(frameParam);

        var viewType = Type.GetType("Microsoft.VisualStudio.PlatformUI.Shell.View, Microsoft.VisualStudio.Shell.ViewManager");
        var contentProp = viewType.GetProperty("Content");

        var viewParam = Expression.Parameter(typeof(DependencyObject));
        get_View_Content = viewParam.Convert(contentProp.DeclaringType)
                                    .Property(contentProp)
                                    .Compile<DependencyObject, object>(viewParam);

        View_ContentProperty = viewType.GetField("ContentProperty", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                                       .GetValue(null) as DependencyProperty;

        var isActiveProp = viewType.GetProperty("IsActive");
        get_View_IsActive = viewParam.Convert(isActiveProp.DeclaringType)
                                     .Property(isActiveProp)
                                     .Compile<DependencyObject, bool>(viewParam);

        View_IsActiveProperty = viewType.GetField("IsActiveProperty", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                                        .GetValue(null) as DependencyProperty;

        var dockType = Type.GetType("Microsoft.VisualStudio.PlatformUI.Shell.Controls.DockTarget, Microsoft.VisualStudio.Shell.ViewManager");
        var objectParam = Expression.Parameter(typeof(object));
        IsDockTarget = objectParam.TypeIs(dockType)
                                  .Compile<object, bool>(objectParam);

        #endregion

        #region Hooks

        visualHook = CreatePostfix<Visual, Visual>(
            typeof(Visual).GetMethod("AddVisualChild", BindingFlags.Instance | BindingFlags.NonPublic),
            AddVisualChild);

        sourceHook = CreatePostfix<HwndSource, Visual>(
            typeof(HwndSource).GetProperty("RootVisual").SetMethod,
            RootVisualChanged);

        static void AddVisualChild(Visual instance, Visual child)
        {
            if (instance is ContentControl or ContentPresenter or Decorator or Panel && // Avoid unnecessary work
                instance is FrameworkElement content &&
                GetIsTracked(content))
                Instance?.ApplyToContent(content, applyToDock: false);
        }

        static void RootVisualChanged(HwndSource instance, Visual value)
        {
            if (value is not null && instance.CompositionTarget is not null)
            {
                if (value is FrameworkElement root &&
                    root.Parent is Popup popup) // Check if owned by popup (popups use separate element as root of HwndSource)
                    Instance?.ApplyToPopup(instance, popup, root);

                else if (value is not Window) // Avoid already handled values
                    instance.CompositionTarget.BackgroundColor = Colors.Transparent;
            }
        }

        #endregion
    }

    private void RevertHooks()
    {
        visualHook?.Dispose();
        sourceHook?.Dispose();
        visualHook = sourceHook = null;
    }

    private static ILHook CreatePostfix<T0, T1>(MethodInfo info, Action<T0, T1> action) =>
        new(info, context =>
        {
            ILCursor cursor = new(context);
            cursor.Index = cursor.Instrs.Count - 1; // Move cursor to end, but before return

            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit(OpCodes.Ldarg_1); // First parameter

            cursor.EmitDelegate(action);
        });
}