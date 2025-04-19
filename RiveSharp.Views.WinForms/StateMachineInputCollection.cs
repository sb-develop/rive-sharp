using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace RiveSharp.Views
{
    // Manages a collection of StateMachineInput objects for RivePlayer.
    public class StateMachineInputCollection : ObservableCollection<StateMachineInput>
    {
        #region Fields

        private readonly WeakReference<RivePlayer> _rivePlayer;

        #endregion

        #region Constructor

        public StateMachineInputCollection(RivePlayer rivePlayer)
        {
            _rivePlayer = new WeakReference<RivePlayer>(rivePlayer);
            CollectionChanged += InputsVectorChanged;
        }

        #endregion

        #region Methods

        private void InputsVectorChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Replace:
                    if (e.NewItems != null)
                    {
                        foreach (StateMachineInput input in e.NewItems)
                        {
                            input.SetRivePlayer(_rivePlayer);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (StateMachineInput input in e.OldItems)
                        {
                            input.SetRivePlayer(null);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    foreach (StateMachineInput input in this)
                    {
                        input.SetRivePlayer(null);
                    }
                    break;
            }
        }

        #endregion
    }
}
