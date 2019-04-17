using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace GenLibrary.MVVM.Base.EventAttachCommandFull
{
    /*
     *使用下述附加类的方法(附加鼠标进入事件,譬如Button，只需要修改第一行)：
     public class MouseEnter : Attachment<Button, MouseEnterBehavior, MouseEnter>
    {
        private static readonly DependencyProperty BehaviorProperty = Behavior();
        public static readonly DependencyProperty CommandProperty = Command(BehaviorProperty);
        public static readonly DependencyProperty CommandParameterProperty = Parameter(BehaviorProperty);
        public static void SetCommand(Control control, ICommand command) { control.SetValue(CommandProperty, command); }
        public static ICommand GetCommand(Control control) { return control.GetValue(CommandProperty) as ICommand; }
        public static void SetCommandParameter(Control control, object parameter) { control.SetValue(CommandParameterProperty, parameter); }
        public static object GetCommandParameter(Control buttonBase) { return buttonBase.GetValue(CommandParameterProperty); }
    }
     * 然后编写行为，即当控件的事件触发时需要执行的代码
       public class MouseEnterBehavior : CommandBehaviorBase<Button>
    {
        public MouseEnterBehavior(Control selectableObject)
            : base(selectableObject)
        {
            selectableObject.MouseEnter += (sender, args) => ExecuteCommand();
        }
    }
     * 最后在XMAL中指定事件的附加行为
     * <Button Behaviors:MouseEnter.Command="{Binding MouseEnter}" Behaviors:MouseEnter.CommandParameter="Optional Paremeter"/>
     */
    /// <summary>
    /// 针对控件的事件附加相应的行为，实现控件的事件和ViewModel的关联
    /// 至于控件带Is的属性可以直接绑定到ViewModel的属性上
    /// </summary>
    /// <typeparam name="TargetT">指定被附加的对象</typeparam>
    /// <typeparam name="BehaviorT">指定行为类型</typeparam>
    /// <typeparam name="AttachmentT">控件的事件</typeparam>
    public class Attachment<TargetT, BehaviorT, AttachmentT>
        where TargetT : Control
        where BehaviorT : CommandBehaviorBase<TargetT>
    {
        public static DependencyProperty Behavior()
        {
            return DependencyProperty.RegisterAttached(
                typeof(BehaviorT).Name,
                typeof(BehaviorT),
                typeof(TargetT),
                null);
        }

        public static DependencyProperty Command(DependencyProperty behaviorProperty)
        {
            return DependencyProperty.RegisterAttached(
                "Command",
                typeof(ICommand),
                typeof(AttachmentT),
                new PropertyMetadata((target, args) => OnSetCommandCallback(target, args, behaviorProperty)));
        }

        public static DependencyProperty Parameter(DependencyProperty behaviorProperty)
        {
            return DependencyProperty.RegisterAttached(
                "CommandParameter",
                typeof(object),
                typeof(AttachmentT),
                new PropertyMetadata((target, args) => OnSetParameterCallback(target, args, behaviorProperty)));
        }

        protected static void OnSetCommandCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e, DependencyProperty behaviorProperty)
        {
            var target = dependencyObject as TargetT;
            if (target == null)
                return;

            GetOrCreateBehavior(target, behaviorProperty).Command = e.NewValue as ICommand;
        }

        protected static void OnSetParameterCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e, DependencyProperty behaviorProperty)
        {
            var target = dependencyObject as TargetT;
            if (target != null)
            {
                GetOrCreateBehavior(target, behaviorProperty).CommandParameter = e.NewValue;
            }
        }

        protected static BehaviorT GetOrCreateBehavior(Control control, DependencyProperty behaviorProperty)
        {
            var behavior = control.GetValue(behaviorProperty) as BehaviorT;
            if (behavior == null)
            {
                behavior = Activator.CreateInstance(typeof(BehaviorT), control) as BehaviorT;
                control.SetValue(behaviorProperty, behavior);
            }

            return behavior;
        }
    }//结束类
}//结束命名空间
