using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MVS
{
    /// <summary>
    /// Centralised factory for the brushes and materials used by the LiDAR 3-D scene.
    /// Brushes and materials are constructed once per factory instance and frozen,
    /// so they can be reused across many <see cref="System.Windows.Media.Media3D.GeometryModel3D"/>
    /// allocations without per-frame GC pressure.
    /// <para>
    /// The factory is immutable with respect to its <see cref="EmissiveEnabled"/> flag:
    /// when the user toggles the setting, replace the factory instance rather than mutating it.
    /// </para>
    /// </summary>
    public class MaterialFactory
    {
        // Heat-map gradient stops used by the point-cloud surface.
        // Blue (low Z) → cyan → green → yellow → red (high Z).
        private static readonly (Color Color, double Offset)[] HeatmapStops =
        {
            (Colors.Blue,   0.00),
            (Colors.Cyan,   0.25),
            (Colors.Lime,   0.50),
            (Colors.Yellow, 0.75),
            (Colors.Red,    1.00),
        };

        private readonly Dictionary<Color, SolidColorBrush> _solidBrushCache
            = new Dictionary<Color, SolidColorBrush>();
        private readonly Dictionary<Color, Material> _solidMaterialCache
            = new Dictionary<Color, Material>();

        private LinearGradientBrush _heatmapBrush;
        private Material            _heatmapMaterial;

        public MaterialFactory(bool emissiveEnabled)
        {
            EmissiveEnabled = emissiveEnabled;
        }

        /// <summary>True if an additional <see cref="EmissiveMaterial"/> should be layered
        /// on top of the diffuse material so colours render at full brightness regardless
        /// of scene lighting.</summary>
        public bool EmissiveEnabled { get; }

        /// <summary>Frozen heat-map gradient brush (left = low Z, right = high Z).</summary>
        public LinearGradientBrush HeatmapBrush
        {
            get
            {
                if (_heatmapBrush == null)
                {
                    var brush = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0.5),
                        EndPoint   = new Point(1, 0.5),
                    };
                    foreach (var (color, offset) in HeatmapStops)
                        brush.GradientStops.Add(new GradientStop(color, offset));
                    brush.Freeze();
                    _heatmapBrush = brush;
                }
                return _heatmapBrush;
            }
        }

        /// <summary>
        /// Material suitable for the point-cloud quad mesh: diffuse heat-map gradient
        /// (plus optional emissive layer when <see cref="EmissiveEnabled"/> is true).
        /// The same instance is returned on every call.
        /// </summary>
        public Material HeatmapMaterial
            => _heatmapMaterial ?? (_heatmapMaterial = BuildAndFreezeMaterial(HeatmapBrush));

        /// <summary>
        /// Material with a single solid colour. Cached per <paramref name="color"/>.
        /// </summary>
        public Material SolidMaterial(Color color)
        {
            if (!_solidMaterialCache.TryGetValue(color, out var mat))
            {
                mat = BuildAndFreezeMaterial(GetSolidBrush(color));
                _solidMaterialCache[color] = mat;
            }
            return mat;
        }

        /// <summary>
        /// Solid frozen brush, cached per <paramref name="color"/>.
        /// </summary>
        public SolidColorBrush GetSolidBrush(Color color)
        {
            if (!_solidBrushCache.TryGetValue(color, out var brush))
            {
                brush = new SolidColorBrush(color);
                brush.Freeze();
                _solidBrushCache[color] = brush;
            }
            return brush;
        }

        private Material BuildAndFreezeMaterial(Brush brush)
        {
            var group = new MaterialGroup();
            group.Children.Add(new DiffuseMaterial(brush));
            if (EmissiveEnabled)
                group.Children.Add(new EmissiveMaterial(brush));
            group.Freeze();
            return group;
        }
    }
}
