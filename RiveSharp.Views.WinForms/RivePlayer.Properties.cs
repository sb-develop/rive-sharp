using RiveSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace RiveSharp.Views
{
    public partial class RivePlayer
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Fields

        private string _source;
        private string _artboard;
        private string _stateMachine;
        private string _animation;
        private StateMachineInputCollection _stateMachineInputs;

        #endregion

        #region Properties

        // Filename of the .riv file to open. Can be a file path or a URL.
        public string Source
        {
            get => _source;
            set
            {
                if (_source != value)
                {
                    _source = value;
                    OnPropertyChanged(nameof(Source));
                    OnSourceNameChanged(value);
                }
            }
        }

        // Name of the artboard to load from the .riv file. If null or empty, the default artboard will be loaded.
        public string Artboard
        {
            get => _artboard;
            set
            {
                if (_artboard != value)
                {
                    _artboard = value;
                    OnPropertyChanged(nameof(Artboard));
                    OnArtboardNameChanged(value);
                }
            }
        }

        // Name of the state machine to load from the .riv file.
        public string StateMachine
        {
            get => _stateMachine;
            set
            {
                if (_stateMachine != value)
                {
                    _stateMachine = value;
                    OnPropertyChanged(nameof(StateMachine));
                    OnStateMachineNameChanged(value);
                }
            }
        }

        // Name of the fallback animation to load from the .riv if StateMachine is null or empty.
        public string Animation
        {
            get => _animation;
            set
            {
                if (_animation != value)
                {
                    _animation = value;
                    OnPropertyChanged(nameof(Animation));
                    OnAnimationNameChanged(value);
                }
            }
        }

        public StateMachineInputCollection StateMachineInputs
        {
            get => _stateMachineInputs;
            set
            {
                if (_stateMachineInputs != value)
                {
                    _stateMachineInputs = value;
                    OnPropertyChanged(nameof(StateMachineInputs));
                }
            }
        }

        #endregion

        #region Methods

        private void OnSourceNameChanged(string newValue)
        {
            // Clear the current Scene while we wait for the new one to load.
            sceneActionsQueue.Enqueue(() => _scene = new Scene());
            _activeSourceFileLoader?.Cancel();

            _activeSourceFileLoader = new CancellationTokenSource();
            // Defer state machine inputs here until the new file is loaded.
            _deferredSMInputsDuringFileLoad = new List<Action>();
            LoadSourceFileDataAsync(newValue, _activeSourceFileLoader.Token);
        }

        private void OnArtboardNameChanged(string newValue)
        {
            sceneActionsQueue.Enqueue(() => _artboardName = newValue);
            if (_activeSourceFileLoader != null)
            {
                // If a file is currently loading async, it will apply the new artboard once it completes.
                _deferredSMInputsDuringFileLoad.Clear();
            }
            else
            {
                sceneActionsQueue.Enqueue(() => UpdateScene(SceneUpdates.Artboard));
            }
        }

        private void OnStateMachineNameChanged(string newValue)
        {
            sceneActionsQueue.Enqueue(() => _stateMachineName = newValue);
            if (_activeSourceFileLoader != null)
            {
                // If a file is currently loading async, it will apply the new state machine once it completes.
                _deferredSMInputsDuringFileLoad.Clear();
            }
            else
            {
                sceneActionsQueue.Enqueue(() => UpdateScene(SceneUpdates.AnimationOrStateMachine));
            }
        }

        private void OnAnimationNameChanged(string newValue)
        {
            sceneActionsQueue.Enqueue(() => _animationName = newValue);
            // If a file is currently loading async, it will apply the new animation once it completes.
            if (_activeSourceFileLoader == null)
            {
                sceneActionsQueue.Enqueue(() => UpdateScene(SceneUpdates.AnimationOrStateMachine));
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
