using RiveSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace RiveSharp.Views
{
    // Implements a simple view that plays content from a .riv file.
    [ToolboxItem(true), DesignTimeVisible(true)]
    public partial class RivePlayer : SKControl, INotifyPropertyChanged
    {
        #region Delegates

        private delegate void PointerHandler(Vec2D pos);

        #endregion

        #region Nested Types: SceneUpdates

        private enum SceneUpdates
        {
            File = 3,
            Artboard = 2,
            AnimationOrStateMachine = 1,
        };

        #endregion

        #region Fields

        private CancellationTokenSource _activeSourceFileLoader = null;
        private Scene _scene = new Scene();
        private readonly ConcurrentQueue<Action> sceneActionsQueue = new ConcurrentQueue<Action>();

        private string _artboardName;
        private string _animationName;
        private string _stateMachineName;
        DateTime? _lastPaintTime;
        private List<Action> _deferredSMInputsDuringFileLoad = null;
        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();

        #endregion

        #region Constructor

        public RivePlayer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            if (!DesignMode)
            {
                StateMachineInputs = new StateMachineInputCollection(this);

                // Initialize SKControl for rendering
                PaintSurface += OnPaintSurface;

                _timer.Interval = 1000 / 60; // 60 fps
                _timer.Tick += (s, e) => { if (Visible) { Invalidate(); } };
                
                MouseDown += (s, e) => HandlePointerEvent(_scene.PointerDown, e);
                MouseMove += (s, e) => HandlePointerEvent(_scene.PointerMove, e);
                MouseUp += (s, e) => HandlePointerEvent(_scene.PointerUp, e);
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            _timer.Stop();
            _timer.Dispose();

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            _timer.Enabled = Visible;
        }

        private async void LoadSourceFileDataAsync(string name, CancellationToken cancellationToken)
        {
            byte[] data = null;
            if (Uri.TryCreate(name, UriKind.Absolute, out var uri))
            {
                using var client = new WebClient();
                data = await client.DownloadDataTaskAsync(uri);
            }
            else
            {
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
                if (File.Exists(filePath) && !cancellationToken.IsCancellationRequested)
                {
                    data = await File.ReadAllBytesAsync(filePath, cancellationToken);
                }
            }

            if (data != null && !cancellationToken.IsCancellationRequested)
            {
                sceneActionsQueue.Enqueue(() => UpdateScene(SceneUpdates.File, data));
                foreach (Action stateMachineInput in _deferredSMInputsDuringFileLoad)
                {
                    sceneActionsQueue.Enqueue(stateMachineInput);
                }
            }
            _deferredSMInputsDuringFileLoad = null;
            _activeSourceFileLoader = null;
        }

        private void EnqueueStateMachineInput(Action stateMachineInput)
        {
            if (_deferredSMInputsDuringFileLoad != null)
            {
                _deferredSMInputsDuringFileLoad.Add(stateMachineInput);
            }
            else
            {
                sceneActionsQueue.Enqueue(stateMachineInput);
            }
        }

        public void SetBool(string name, bool value)
        {
            EnqueueStateMachineInput(() => _scene.SetBool(name, value));
        }

        public void SetNumber(string name, float value)
        {
            EnqueueStateMachineInput(() => _scene.SetNumber(name, value));
        }

        public void FireTrigger(string name)
        {
            EnqueueStateMachineInput(() => _scene.FireTrigger(name));
        }

        private void HandlePointerEvent(PointerHandler handler, MouseEventArgs e)
        {
            if (_activeSourceFileLoader != null)
            {
                return;
            }

            var viewSize = ClientSize;
            var pointerPos = e.Location;

            sceneActionsQueue.Enqueue(() =>
            {
                Mat2D mat = ComputeAlignment(viewSize.Width, viewSize.Height);
                if (mat.Invert(out var inverse))
                {
                    Vec2D artboardPos = inverse * new Vec2D(pointerPos.X, pointerPos.Y);
                    handler(artboardPos);
                }
            });
        }       

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            while (sceneActionsQueue.TryDequeue(out var action))
            {
                action();
            }

            if (!_scene.IsLoaded)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (_lastPaintTime != null)
            {
                _scene.AdvanceAndApply((now - _lastPaintTime.Value).TotalSeconds);
            }
            _lastPaintTime = now;

            e.Surface.Canvas.Clear();
            var renderer = new Renderer(e.Surface.Canvas);
            renderer.Save();
            renderer.Transform(ComputeAlignment(e.Info.Width, e.Info.Height));
            _scene.Draw(renderer);
            renderer.Restore();
        }

        private void UpdateScene(SceneUpdates updates, byte[] sourceFileData = null)
        {
            if (updates >= SceneUpdates.File)
            {
                _scene.LoadFile(sourceFileData);
            }
            if (updates >= SceneUpdates.Artboard)
            {
                _scene.LoadArtboard(_artboardName);
            }
            if (updates >= SceneUpdates.AnimationOrStateMachine)
            {
                if (!string.IsNullOrEmpty(_stateMachineName))
                {
                    _scene.LoadStateMachine(_stateMachineName);
                }
                else if (!string.IsNullOrEmpty(_animationName))
                {
                    _scene.LoadAnimation(_animationName);
                }
                else
                {
                    if (!_scene.LoadStateMachine(null))
                    {
                        _scene.LoadAnimation(null);
                    }
                }
            }
        }

        private Mat2D ComputeAlignment(double width, double height)
        {
            return ComputeAlignment(new AABB(0, 0, (float)width, (float)height));
        }

        private Mat2D ComputeAlignment(AABB frame)
        {
            return Renderer.ComputeAlignment(Fit.Contain, Alignment.Center, frame,
                                             new AABB(0, 0, _scene.Width, _scene.Height));
        }

        #endregion
    }
}
