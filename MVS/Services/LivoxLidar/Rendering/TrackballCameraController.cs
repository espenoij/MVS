using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace MVS
{
    /// <summary>
    /// Encapsulates trackball camera state (rotation, zoom, look-at, fit info) and
    /// applies it to a WPF <see cref="PerspectiveCamera"/>. Also keeps an optional
    /// secondary axis-indicator camera in sync with the main camera.
    ///
    /// The page only forwards mouse events to this controller and reads the latest
    /// rotation values when it needs to update its on-screen readout.
    /// </summary>
    internal sealed class TrackballCameraController
    {
        private readonly PerspectiveCamera _camera;
        private readonly Func<double> _aspectRatioProvider;

        // Trackball state
        private bool _isDragging;
        private Point _lastMousePos;
        private double _rotX;
        private double _rotY;
        private double _zoom = 15000.0;

        // Look-at point
        private double _lookAtX, _lookAtY, _lookAtZ;

        // Fit context (centroid, normal, vessel forward, default zoom)
        private double _centroidX, _centroidY, _centroidZ;
        private double _defaultZoom = 15000.0;
        private double _fitNormalX, _fitNormalY, _fitNormalZ = 1.0;
        private double _vesselFwdX = 1.0, _vesselFwdY, _vesselFwdZ;

        // Optional axis-indicator camera and roll readout callback
        private PerspectiveCamera _axisCamera;
        private Action<double, double, double> _rotationChanged;

        public TrackballCameraController(PerspectiveCamera camera, Func<double> aspectRatioProvider)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _aspectRatioProvider = aspectRatioProvider ?? (() => 1.0);
        }

        /// <summary>Optional secondary camera (e.g. axis indicator viewport).</summary>
        public void AttachAxisCamera(PerspectiveCamera axisCamera)
        {
            _axisCamera = axisCamera;
        }

        /// <summary>
        /// Callback fired after every camera change with (rotX, rotY, rotZ) in degrees,
        /// so the page can update its rotation readout.
        /// </summary>
        public void SetRotationChangedCallback(Action<double, double, double> callback)
        {
            _rotationChanged = callback;
        }

        public double RotX => _rotX;
        public double RotY => _rotY;
        public double Zoom => _zoom;

        // ── Fit context ──────────────────────────────────────────────────────

        public void SetCentroid(double cx, double cy, double cz)
        {
            _centroidX = cx; _centroidY = cy; _centroidZ = cz;
        }

        public void SetDefaultZoom(double zoom)
        {
            _defaultZoom = zoom;
        }

        public void SetFitContext(double centroidX, double centroidY, double centroidZ,
                                  double normalX, double normalY, double normalZ,
                                  double vesselFwdX, double vesselFwdY, double vesselFwdZ,
                                  double defaultZoom)
        {
            _centroidX = centroidX; _centroidY = centroidY; _centroidZ = centroidZ;
            _fitNormalX = normalX; _fitNormalY = normalY; _fitNormalZ = normalZ;
            _vesselFwdX = vesselFwdX; _vesselFwdY = vesselFwdY; _vesselFwdZ = vesselFwdZ;
            _defaultZoom = defaultZoom;
        }

        /// <summary>
        /// Compute the camera distance needed to fit two half-extents with the current viewport aspect ratio.
        /// </summary>
        public double ComputeFitZoom(double halfExtentH, double halfExtentV)
        {
            return CameraMath.ComputeFitZoom(halfExtentH, halfExtentV,
                _camera.FieldOfView, _aspectRatioProvider());
        }

        // ── View presets ─────────────────────────────────────────────────────

        public void ApplyTopDownView()
        {
            CameraMath.RotationFromNormal(_fitNormalX, _fitNormalY, _fitNormalZ,
                out _rotX, out _rotY);
            _zoom = _defaultZoom;

            _camera.UpDirection = new Vector3D(_vesselFwdX, _vesselFwdY, _vesselFwdZ);
            ApplyCameraTransform(_centroidX, _centroidY, _centroidZ);
        }

        /// <summary>
        /// Apply a perspective view derived from the fit normal plus configurable
        /// horizontal/vertical rotation offsets (degrees).
        /// </summary>
        public void ApplyPerspectiveView(double extraRotXDeg, double extraRotYDeg)
        {
            CameraMath.RotationFromNormal(_fitNormalX, _fitNormalY, _fitNormalZ,
                out _rotX, out _rotY);
            _rotX += extraRotXDeg;
            _rotY += extraRotYDeg;
            _zoom = _defaultZoom;

            _camera.UpDirection = new Vector3D(_vesselFwdX, _vesselFwdY, _vesselFwdZ);
            ApplyCameraTransform(_centroidX, _centroidY, _centroidZ);

            // Roll = 0: align UpDirection to world Z projected perpendicular to look direction.
            Vector3D? refUp = CameraMath.RollFreeUp(_camera.LookDirection);
            if (refUp.HasValue)
            {
                _camera.UpDirection = refUp.Value;
            }
            UpdateAxisCamera();
        }

        public void ApplyCameraTransform(double cx, double cy, double cz)
        {
            _lookAtX = cx; _lookAtY = cy; _lookAtZ = cz;
            Point3D pos = CameraMath.CameraPositionFromAngles(cx, cy, cz, _rotX, _rotY, _zoom);
            _camera.Position = pos;
            _camera.LookDirection = new Vector3D(cx - pos.X, cy - pos.Y, cz - pos.Z);
            UpdateAxisCamera();
        }

        // ── Mouse handling ───────────────────────────────────────────────────

        public void OnMouseLeftButtonDown(IInputElement source, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePos = e.GetPosition(source);
            source.CaptureMouse();
        }

        public void OnMouseLeftButtonUp(IInputElement source, MouseButtonEventArgs e)
        {
            _isDragging = false;
            source.ReleaseMouseCapture();
        }

        public void OnMouseMove(IInputElement source, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point pos = e.GetPosition(source);
            double dx = pos.X - _lastMousePos.X;
            double dy = pos.Y - _lastMousePos.Y;
            _lastMousePos = pos;

            var lookAt = new Point3D(_lookAtX, _lookAtY, _lookAtZ);
            Vector3D offset = _camera.Position - lookAt;

            Vector3D lookDir = _camera.LookDirection;
            Vector3D up = _camera.UpDirection;
            Vector3D right = Vector3D.CrossProduct(lookDir, up);
            if (right.Length > 1e-10) right.Normalize(); else return;

            up = Vector3D.CrossProduct(right, lookDir);
            if (up.Length > 1e-10) up.Normalize(); else return;

            double angleH = -dx * 0.5;
            double angleV = -dy * 0.5;

            offset = CameraMath.RotateVector(offset, up, angleH);
            offset = CameraMath.RotateVector(offset, right, angleV);

            _camera.Position = lookAt + offset;
            _camera.LookDirection = new Vector3D(-offset.X, -offset.Y, -offset.Z);

            Vector3D newUp = CameraMath.RotateVector(_camera.UpDirection, up, angleH);
            newUp = CameraMath.RotateVector(newUp, right, angleV);
            _camera.UpDirection = newUp;

            // Re-derive rot/zoom so presets and zoom continue to work afterwards.
            CameraMath.SyncAnglesFromOffset(_camera.Position - lookAt,
                out _rotX, out _rotY, out _zoom);

            UpdateAxisCamera();
        }

        public void OnMouseWheel(MouseWheelEventArgs e)
        {
            _zoom *= e.Delta > 0 ? 1.1 : 0.9;
            _zoom = Math.Max(100, _zoom);
            ApplyCameraTransform(_lookAtX, _lookAtY, _lookAtZ);
        }

        // ── Axis camera sync ─────────────────────────────────────────────────

        private void UpdateAxisCamera()
        {
            if (_axisCamera != null)
            {
                Vector3D lookDir = _camera.LookDirection;
                lookDir.Normalize();

                const double axisCamDist = 3000.0;
                _axisCamera.Position = new Point3D(-lookDir.X * axisCamDist,
                                                   -lookDir.Y * axisCamDist,
                                                   -lookDir.Z * axisCamDist);
                _axisCamera.LookDirection = lookDir;
                _axisCamera.UpDirection = _camera.UpDirection;
            }

            if (_rotationChanged != null)
            {
                double rotZ = CameraMath.ComputeRollAngleDeg(_camera.LookDirection, _camera.UpDirection);
                _rotationChanged(_rotX, _rotY, rotZ);
            }
        }
    }
}
