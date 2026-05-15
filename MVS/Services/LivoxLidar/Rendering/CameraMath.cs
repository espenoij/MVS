using System;
using System.Windows.Media.Media3D;

namespace MVS
{
    /// <summary>
    /// Pure, side-effect free math used by the LiDAR camera/trackball controller.
    /// Kept independent of WPF Viewport3D / camera state so it can be unit tested.
    /// </summary>
    internal static class CameraMath
    {
        /// <summary>
        /// Compute the camera distance (zoom) needed so that two half-extents fit within
        /// the camera's horizontal/vertical FOV with a small margin.
        /// </summary>
        /// <param name="halfExtentH">Half-extent in the horizontal direction (mm).</param>
        /// <param name="halfExtentV">Half-extent in the vertical direction (mm).</param>
        /// <param name="horizontalFovDeg">PerspectiveCamera.FieldOfView (horizontal, degrees).</param>
        /// <param name="aspectRatio">Viewport width / height. Use 1.0 if unknown.</param>
        /// <param name="margin">Extra spacing factor (1.2 = 20% margin).</param>
        public static double ComputeFitZoom(double halfExtentH, double halfExtentV,
                                            double horizontalFovDeg, double aspectRatio,
                                            double margin = 1.2)
        {
            if (aspectRatio < 0.01) aspectRatio = 1.0;

            double hFovRad = horizontalFovDeg * Math.PI / 180.0 / 2.0; // half horizontal FOV
            double vFovRad = Math.Atan(Math.Tan(hFovRad) / aspectRatio);

            double zoomH = halfExtentH / Math.Tan(hFovRad);
            double zoomV = halfExtentV / Math.Tan(vFovRad);
            return Math.Max(zoomH, zoomV) * margin;
        }

        /// <summary>
        /// Rotate a vector around an arbitrary axis by the given angle (Rodrigues' formula).
        /// </summary>
        public static Vector3D RotateVector(Vector3D v, Vector3D axis, double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            double dot = Vector3D.DotProduct(v, axis);
            Vector3D cross = Vector3D.CrossProduct(axis, v);
            return v * cos + cross * sin + axis * dot * (1 - cos);
        }

        /// <summary>
        /// Derive the trackball rotation angles (rotX, rotY) that align the camera's
        /// look direction with the supplied (normalized) plane normal.
        /// </summary>
        public static void RotationFromNormal(double nx, double ny, double nz,
                                              out double rotXDeg, out double rotYDeg)
        {
            double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len > 1e-6) { nx /= len; ny /= len; nz /= len; }
            else { nx = 0; ny = 0; nz = 1; }

            rotXDeg = -Math.Asin(Math.Max(-1.0, Math.Min(1.0, ny))) * 180.0 / Math.PI;
            double cosX = Math.Cos(rotXDeg * Math.PI / 180.0);
            if (cosX < 1e-6) cosX = 1e-6;
            rotYDeg = Math.Atan2(nx / cosX, nz / cosX) * 180.0 / Math.PI;
        }

        /// <summary>
        /// Compute the camera position for the given centre, rotation angles and zoom distance.
        /// </summary>
        public static Point3D CameraPositionFromAngles(double cx, double cy, double cz,
                                                       double rotXDeg, double rotYDeg, double zoom)
        {
            double radX = rotXDeg * Math.PI / 180.0;
            double radY = rotYDeg * Math.PI / 180.0;
            double camX = cx + zoom * Math.Sin(radY) * Math.Cos(radX);
            double camY = cy - zoom * Math.Sin(radX);
            double camZ = cz + zoom * Math.Cos(radY) * Math.Cos(radX);
            return new Point3D(camX, camY, camZ);
        }

        /// <summary>
        /// Re-derive trackball angles and zoom from a camera offset vector
        /// (camera.Position − lookAt). Used after a free-form rotation.
        /// </summary>
        public static void SyncAnglesFromOffset(Vector3D offset,
                                                out double rotXDeg, out double rotYDeg, out double zoom)
        {
            zoom = offset.Length;
            rotXDeg = 0;
            rotYDeg = 0;
            if (zoom < 1e-10) return;
            double nx = offset.X / zoom;
            double ny = offset.Y / zoom;
            double nz = offset.Z / zoom;
            rotXDeg = Math.Asin(Math.Max(-1.0, Math.Min(1.0, -ny))) * 180.0 / Math.PI;
            double cosX = Math.Cos(rotXDeg * Math.PI / 180.0);
            if (Math.Abs(cosX) > 1e-6)
                rotYDeg = Math.Atan2(nx / cosX, nz / cosX) * 180.0 / Math.PI;
        }

        /// <summary>
        /// Project world Z onto the plane perpendicular to <paramref name="lookDir"/> to get
        /// the "natural zero-roll" up vector. Returns null if look direction is parallel to Z.
        /// </summary>
        public static Vector3D? RollFreeUp(Vector3D lookDir)
        {
            lookDir.Normalize();
            Vector3D worldZ = new Vector3D(0, 0, 1);
            Vector3D refUp = worldZ - Vector3D.DotProduct(worldZ, lookDir) * lookDir;
            double refLen = refUp.Length;
            if (refLen <= 1e-6) return null;
            return new Vector3D(refUp.X / refLen, refUp.Y / refLen, refUp.Z / refLen);
        }

        /// <summary>
        /// Compute the signed roll angle (degrees) of <paramref name="actualUp"/> relative
        /// to the zero-roll up vector for the given look direction.
        /// </summary>
        public static double ComputeRollAngleDeg(Vector3D lookDir, Vector3D actualUp)
        {
            Vector3D? refUpOpt = RollFreeUp(lookDir);
            if (refUpOpt == null) return 0.0;
            Vector3D refUp = refUpOpt.Value;
            actualUp.Normalize();
            double dot = Math.Max(-1.0, Math.Min(1.0, Vector3D.DotProduct(refUp, actualUp)));
            double angle = Math.Acos(dot) * 180.0 / Math.PI;
            Vector3D ld = lookDir; ld.Normalize();
            Vector3D cross = Vector3D.CrossProduct(refUp, actualUp);
            if (Vector3D.DotProduct(cross, ld) < 0) angle = -angle;
            return angle;
        }
    }
}
