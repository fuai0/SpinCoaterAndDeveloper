using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace TemperatureControllerService.Extensions
{
    public class ListBoxScrollToBottomExtensions : Behavior<ListBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            ((INotifyCollectionChanged)base.AssociatedObject.Items).CollectionChanged += ListBoxScrollToBottomExtensions_CollectionChanged;
        }

        private void ListBoxScrollToBottomExtensions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (base.AssociatedObject.HasItems)
            {
                var controlCount = VisualTreeHelper.GetChildrenCount(AssociatedObject);
                if (controlCount != 0)  //如果找不到控件,TabControl未选中Tab不会初始化,会找不到控件
                {
                    Decorator decorator = (Decorator)VisualTreeHelper.GetChild(AssociatedObject, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)decorator.Child;
                    scrollViewer.ScrollToEnd();
                }
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            ((INotifyCollectionChanged)base.AssociatedObject.Items).CollectionChanged -= ListBoxScrollToBottomExtensions_CollectionChanged;
        }
    }
}
