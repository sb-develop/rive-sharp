using System;
using System.ComponentModel;

namespace RiveSharp.Views
{
    // This base class wraps a custom, named state machine input value.
    public abstract class StateMachineInput : INotifyPropertyChanged
    {
        #region Fields

        private string _target;
        private WeakReference<RivePlayer> _rivePlayer;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        public string Target
        {
            get => _target; // Must be null-checked before use.
            set
            {
                if (_target != value)
                {
                    _target = value;
                    OnPropertyChanged(nameof(Target));
                    Apply();
                }
            }
        }

        protected WeakReference<RivePlayer> RivePlayer => _rivePlayer;

        #endregion

        #region Methods

        // Sets _rivePlayer to the given rivePlayer object and applies our input value to the state machine.
        // Does nothing if _rivePlayer was already equal to rivePlayer.
        internal void SetRivePlayer(WeakReference<RivePlayer> rivePlayer)
        {
            _rivePlayer = rivePlayer;
            Apply();
        }

        protected void Apply()
        {
            if (!string.IsNullOrEmpty(_target) && _rivePlayer is not null && _rivePlayer.TryGetTarget(out var rivePlayer))
            {
                Apply(rivePlayer, _target);
            }
        }

        // Applies our input value to the rivePlayer's state machine.
        // rivePlayer and inputName are guaranteed to not be null or empty.
        protected abstract void Apply(RivePlayer rivePlayer, string inputName);

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class BoolInput : StateMachineInput
    {
        #region Fields

        private bool _value;

        #endregion

        #region Properties

        public bool Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                    Apply();
                }
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override void Apply(RivePlayer rivePlayer, string inputName)
        {
            rivePlayer.SetBool(inputName, Value);
        }

        #endregion
    }

    public class NumberInput : StateMachineInput
    {
        #region Fields

        private double _value;

        #endregion

        #region Properties

        public double Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                    Apply();
                }
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override void Apply(RivePlayer rivePlayer, string inputName)
        {
            rivePlayer.SetNumber(inputName, (float)Value);
        }

        #endregion
    }

    public class TriggerInput : StateMachineInput
    {
        #region Methods

        public void Fire()
        {
            if (!string.IsNullOrEmpty(Target) && RivePlayer is not null && RivePlayer.TryGetTarget(out var rivePlayer))
            {
                rivePlayer.FireTrigger(Target);
            }
        }

        // Make a Fire() overload that matches the EventHandler delegate.
        // This allows us to bind it to events like Button.Click in WinForms.
        public void Fire(object sender, EventArgs e)
        {
            Fire();
        }

        /// <inheritdoc />
        protected override void Apply(RivePlayer rivePlayer, string inputName) { }

        #endregion
    }
}
