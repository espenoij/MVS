using System;
using System.Collections.Specialized;
using System.Windows.Threading;
using Telerik.Windows.Data;

// Klasseutvidelse som gjør RadObservableCollection thread safe for visning i UI.
// Denne modifikasjonen skal etter det jeg har lest ikke gjøre observablecollection 100% thread safe (thread til thread).
public class RadObservableCollectionEx<t> : RadObservableCollection<t>
{
    public override event NotifyCollectionChangedEventHandler CollectionChanged;
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        NotifyCollectionChangedEventHandler CollectionChanged = this.CollectionChanged;
        if (CollectionChanged != null)
        {
            foreach (NotifyCollectionChangedEventHandler nh in CollectionChanged.GetInvocationList())
            {
                DispatcherObject dispObj = nh.Target as DispatcherObject;
                if (dispObj != null)
                {
                    Dispatcher dispatcher = dispObj.Dispatcher;
                    if (dispatcher != null && !dispatcher.CheckAccess())
                    {
                        dispatcher.BeginInvoke(
                            (Action)(() => nh.Invoke(this,
                                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))),
                            DispatcherPriority.DataBind);
                        continue;
                    }
                }
                nh?.Invoke(this, e); // TODO krasj her 2022.14.02
            }
        }
    }
}
