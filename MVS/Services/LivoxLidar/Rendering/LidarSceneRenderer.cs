using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MVS
{
    /// <summary>
    /// Owns all <see cref="ModelVisual3D"/> slots used by the LiDAR scene and exposes
    /// high-level Render* operations. Pure rendering only — no event subscriptions,
    /// no camera control, no VM coupling.
    /// </summary>
    internal sealed class LidarSceneRenderer
    {
        // All arrows in the main viewport derive their length and shaft radius from a
        // single source so they look visually consistent. The edge indicator tube keeps
        // its real physical length (2 × HalfLength) but adopts the shared shaft radius.
        private const double ArrowShaftRadiusFraction = 0.02;
        private const double DefaultArrowLength = 6000.0;

        // XAML-defined visuals (passed in)
        private readonly ModelVisual3D _pointCloudVisual;
        private readonly ModelVisual3D _planeVisual;

        // Programmatically created visuals (owned)
        public ModelVisual3D ForwardArrowVisual { get; } = new ModelVisual3D();
        public ModelVisual3D NormalArrowVisual  { get; } = new ModelVisual3D();
        public ModelVisual3D BowArrowVisual     { get; } = new ModelVisual3D();
        public ModelVisual3D DeckEdgeLineVisual   { get; } = new ModelVisual3D();
        public ModelVisual3D DeckEdgePointsVisual { get; } = new ModelVisual3D();
        public ModelVisual3D HexVerticesVisual    { get; } = new ModelVisual3D();

        // Optional axis-indicator visuals (separate viewport)
        private ModelVisual3D _axisXVisual;
        private ModelVisual3D _axisYVisual;
        private ModelVisual3D _axisZVisual;

        private MaterialFactory _materials;

        public LidarSceneRenderer(ModelVisual3D pointCloudVisual,
                                  ModelVisual3D planeVisual,
                                  MaterialFactory materials)
        {
            _pointCloudVisual = pointCloudVisual ?? throw new ArgumentNullException(nameof(pointCloudVisual));
            _planeVisual      = planeVisual      ?? throw new ArgumentNullException(nameof(planeVisual));
            _materials        = materials        ?? throw new ArgumentNullException(nameof(materials));
        }

        /// <summary>Add the renderer's owned visuals to the supplied viewport.</summary>
        public void AttachToViewport(System.Windows.Controls.Viewport3D viewport3D)
        {
            if (viewport3D == null) throw new ArgumentNullException(nameof(viewport3D));
            viewport3D.Children.Add(ForwardArrowVisual);
            viewport3D.Children.Add(NormalArrowVisual);
            viewport3D.Children.Add(BowArrowVisual);
            viewport3D.Children.Add(DeckEdgeLineVisual);
            viewport3D.Children.Add(DeckEdgePointsVisual);
            viewport3D.Children.Add(HexVerticesVisual);
        }

        /// <summary>Register the optional axis-indicator visuals (separate viewport).</summary>
        public void SetAxisIndicatorVisuals(ModelVisual3D x, ModelVisual3D y, ModelVisual3D z)
        {
            _axisXVisual = x;
            _axisYVisual = y;
            _axisZVisual = z;
        }

        /// <summary>Swap the material factory (e.g. when the emissive flag toggles).</summary>
        public void SetMaterials(MaterialFactory materials)
        {
            _materials = materials ?? throw new ArgumentNullException(nameof(materials));
        }

        // ── Clearing ─────────────────────────────────────────────────────────

        public void ClearAll()
        {
            _pointCloudVisual.Content     = null;
            _planeVisual.Content          = null;
            ForwardArrowVisual.Content    = null;
            NormalArrowVisual.Content     = null;
            BowArrowVisual.Content        = null;
            DeckEdgeLineVisual.Content    = null;
            DeckEdgePointsVisual.Content  = null;
            HexVerticesVisual.Content     = null;
            // Axis indicators remain visible in their separate viewport.
        }

        public void ClearDeckEdge()
        {
            BowArrowVisual.Content       = null;
            DeckEdgeLineVisual.Content   = null;
            DeckEdgePointsVisual.Content = null;
            HexVerticesVisual.Content    = null;
        }

        // ── Point cloud ──────────────────────────────────────────────────────

        /// <summary>
        /// Build and assign the point cloud mesh. Returns the result so callers can use
        /// <see cref="PointCloudMeshResult.DisplayedPointCount"/> for status text.
        /// </summary>
        public PointCloudMeshResult RenderPointCloud(IList<(float x, float y, float z)> points, int maxDisplay)
        {
            if (points == null || points.Count == 0)
            {
                _pointCloudVisual.Content = null;
                return null;
            }

            var meshResult = PointCloudMeshBuilder.Build(points, maxDisplay);
            if (meshResult == null)
            {
                _pointCloudVisual.Content = null;
                return null;
            }

            var mat = _materials.HeatmapMaterial;
            _pointCloudVisual.Content = new GeometryModel3D(meshResult.Mesh, mat) { BackMaterial = mat };
            return meshResult;
        }

        // ── Plane fit ────────────────────────────────────────────────────────

        public void RenderPlane(LivoxLidarPlaneFitResult fit)
        {
            var mesh = PlaneMeshBuilder.Build(fit);
            // Semi-transparent black is intentionally not cached — alpha matters and there is
            // only ever one plane in the scene.
            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)));
            _planeVisual.Content = new GeometryModel3D(mesh, material);
        }

        public void RenderFitArrows(LivoxLidarPlaneFitResult fit, double arrowLength, double shaftRadius)
        {
            var origin = new Point3D(0, 0, 0);

            // Vessel forward: +X in sensor coordinates (Mid-360: +X = forward)
            var forwardMesh = ArrowMeshBuilder.Build(origin, new Vector3D(1, 0, 0), arrowLength, shaftRadius);
            var forwardMat  = _materials.SolidMaterial(Colors.Red);
            ForwardArrowVisual.Content = new GeometryModel3D
            {
                Geometry = forwardMesh, Material = forwardMat, BackMaterial = forwardMat
            };

            // Plane normal
            var normalMesh = ArrowMeshBuilder.Build(origin,
                new Vector3D(fit.NormalX, fit.NormalY, fit.NormalZ), arrowLength, shaftRadius);
            var normalMat  = _materials.SolidMaterial(Color.FromRgb(0, 120, 255));
            NormalArrowVisual.Content = new GeometryModel3D
            {
                Geometry = normalMesh, Material = normalMat, BackMaterial = normalMat
            };
        }

        // ── Deck edge ────────────────────────────────────────────────────────

        public void RenderDeckEdge(LivoxLidarDeckEdgeResult edge, double arrowLength, double shaftRadius)
        {
            HexVerticesVisual.Content = null;

            // Edge line: plain tube along the bow edge.
            var edgeStart = new Point3D(
                edge.MidpointX - edge.HalfLength * edge.DirectionX,
                edge.MidpointY - edge.HalfLength * edge.DirectionY,
                edge.MidpointZ - edge.HalfLength * edge.DirectionZ);
            var edgeEnd = new Point3D(
                edge.MidpointX + edge.HalfLength * edge.DirectionX,
                edge.MidpointY + edge.HalfLength * edge.DirectionY,
                edge.MidpointZ + edge.HalfLength * edge.DirectionZ);
            var edgeMesh = TubeMeshBuilder.Build(edgeStart, edgeEnd, shaftRadius);
            var edgeMat  = _materials.SolidMaterial(Color.FromRgb(255, 140, 0));
            DeckEdgeLineVisual.Content = new GeometryModel3D
            {
                Geometry = edgeMesh, Material = edgeMat, BackMaterial = edgeMat
            };

            // Hull vertex markers — bright magenta cubes at each estimated boundary vertex
            double markerHalf = Math.Max(edge.HalfLength * 0.02, 75.0);
            var markerMesh = HullMarkerMeshBuilder.Build(edge.HullVertices3D, markerHalf);
            if (markerMesh != null)
            {
                var markerMat = _materials.SolidMaterial(Color.FromRgb(255, 0, 255));
                HexVerticesVisual.Content = new GeometryModel3D
                {
                    Geometry = markerMesh, Material = markerMat, BackMaterial = markerMat
                };
            }

            // Bow direction arrow
            var bowMesh = ArrowMeshBuilder.Build(new Point3D(0, 0, 0),
                new Vector3D(edge.VesselForwardX, edge.VesselForwardY, edge.VesselForwardZ),
                arrowLength, shaftRadius);
            var bowMat = _materials.SolidMaterial(Color.FromRgb(255, 140, 0));
            BowArrowVisual.Content = new GeometryModel3D
            {
                Geometry = bowMesh, Material = bowMat, BackMaterial = bowMat
            };

            RenderEdgePoints(edge.EdgePoints);
        }

        public void RenderEdgePoints(List<(float x, float y, float z)> points)
        {
            var mesh = EdgePointsMeshBuilder.Build(points);
            if (mesh == null)
            {
                DeckEdgePointsVisual.Content = null;
                return;
            }
            var mat = _materials.SolidMaterial(Color.FromRgb(0, 200, 80));
            DeckEdgePointsVisual.Content = new GeometryModel3D(mesh, mat);
        }

        // ── Axis indicators ──────────────────────────────────────────────────

        public void RenderAxisIndicators(double axisLength = 900.0, double axisRadius = 25.0)
        {
            if (_axisXVisual == null || _axisYVisual == null || _axisZVisual == null) return;

            var origin = new Point3D(0, 0, 0);

            var xMesh = ArrowMeshBuilder.Build(origin, new Vector3D(1, 0, 0), axisLength, axisRadius);
            var xMat = _materials.SolidMaterial(Colors.Red);
            _axisXVisual.Content = new GeometryModel3D { Geometry = xMesh, Material = xMat, BackMaterial = xMat };

            var yMesh = ArrowMeshBuilder.Build(origin, new Vector3D(0, 1, 0), axisLength, axisRadius);
            var yMat = _materials.SolidMaterial(Colors.Green);
            _axisYVisual.Content = new GeometryModel3D { Geometry = yMesh, Material = yMat, BackMaterial = yMat };

            var zMesh = ArrowMeshBuilder.Build(origin, new Vector3D(0, 0, 1), axisLength, axisRadius);
            var zMat = _materials.SolidMaterial(Color.FromRgb(0, 60, 180));
            _axisZVisual.Content = new GeometryModel3D { Geometry = zMesh, Material = zMat, BackMaterial = zMat };
        }

        // ── Shared arrow sizing ──────────────────────────────────────────────

        /// <summary>
        /// Compute consistent arrow dimensions for the current scene context, preferring the
        /// plane fit extent, falling back to the edge half-length, then to a constant.
        /// </summary>
        public static void GetArrowDimensions(LivoxLidarPlaneFitResult lastFit,
                                              LivoxLidarDeckEdgeResult lastEdge,
                                              out double length, out double shaftRadius)
        {
            double baseSize;
            if (lastFit != null && lastFit.IsValid)
            {
                double maxExtent = Math.Max(lastFit.ExtentPrimary, lastFit.ExtentSecondary);
                baseSize = maxExtent * 0.3;
            }
            else if (lastEdge != null && lastEdge.IsValid)
            {
                baseSize = lastEdge.HalfLength * 1.2;
            }
            else
            {
                baseSize = DefaultArrowLength;
            }

            length      = Math.Max(baseSize, DefaultArrowLength);
            shaftRadius = length * ArrowShaftRadiusFraction;
        }
    }
}
