# Point Cloud Normalization for Ridge-Resistant Corner & Vessel Forward Detection

## Problem
The corner finding and vessel forward calculation methods were failing when the deck had a ridge or significant slope. The original algorithm:
1. Fit a plane using RANSAC
2. Projected points onto that tilted plane
3. Performed 2D corner/edge detection

When a ridge was present, the projection distorted the geometry, causing incorrect corner detection and vessel forward calculations. **Additionally, ridge points were being detected as corners because they were extremal points.**

## Solution
Implemented **dual-stage ridge filtering with point cloud normalization** that:
1. Pre-filters ridge points after RANSAC to prevent biasing the PCA plane refinement
2. Flattens the deck plane via normalization
3. **Post-filters remaining ridge points in normalized space**
4. Performs corner/edge detection on the clean, flat deck representation

### Key Changes

1. **Rotation Matrix Construction** (`BuildRotationToAlignWithZ`)
   - Added a new helper method that builds a 3×3 rotation matrix using Rodrigues' formula
   - Rotates the fitted plane normal to align with the +Z axis (0, 0, 1)
   - This makes the tilted/ridged deck completely horizontal in the transformed space

2. **Pre-Filtering (NEW - Step 2.5)** ⭐
   - **Why needed**: RANSAC threshold (50mm) can include ridge points that are close to the plane
   - **Solution**: Filter BEFORE PCA refinement to prevent ridge bias
   - Uses MAD on **signed distances** from RANSAC plane:
     - Calculates median signed distance (accounts for systematic offset)
     - Computes MAD of deviations
     - Filters out points with |distance - median| > max(2.5·MAD, 60mm)
   - **Result**: PCA refinement works on clean deck points only

3. **Point Cloud Normalization** (Step 3)
   - After RANSAC and pre-filtering, all remaining inliers are transformed to normalized coordinates
   - In this system, the deck plane is perfectly flat (Z ≈ constant)
   - This removes the distorting effects of ridges and slopes

4. **Post-Filtering (Step 3.5)** ⭐
   - **Second line of defense**: Catches any remaining ridge points
   - After normalization, ridge points have Z-values significantly different from the main plane
   - Uses **Median Absolute Deviation (MAD)** for robust outlier detection:
     - Calculates median Z value (should be ≈ 0 for main deck plane)
     - Computes MAD of Z deviations
     - Filters out points with |Z - median| > max(2.5·MAD, 80mm)
   - **Why 2.5·MAD instead of 3.0?** More aggressive to ensure ridge removal
   - **Result**: Ridge points are excluded, only true deck boundary points remain
   - Since the plane is now horizontal, the 2D projection is simply the X-Y coordinates
   - No complex basis vector calculations needed
   - Corner/edge detection works on a truly flat representation

5. **Inverse Transformation**
   - After finding corners/edges in the normalized space, results are transformed back to original 3D coordinates
   - Updated all three detection methods:
     - `FindEdgeHexagon` - 6-vertex hexagonal detection
     - `FindEdgeSquare` - 4-vertex rectangular detection
     - `FindEdgeConvexHull` - Generic convex hull detection
   - Each method now receives the filtered normalized points and rotation matrix
   - 3D vertices, edge directions, midpoints, and vessel forward vectors are all transformed back correctly

### Technical Details

**Rotation Matrix Properties:**
- The matrix R rotates points so that (nx, ny, nz) → (0, 0, 1)
- R is orthogonal, so R^T (transpose) is its inverse
- To transform back: multiply by R^T (which is implemented by swapping indices in matrix multiplication)

**Coordinate Transformations:**
- Forward: `normalized = R * (point - centroid)`
- Inverse: `original = R^T * normalized + centroid`

**Ridge Filtering Algorithm:**
```
1. Transform all points to normalized space
2. Find median Z value (robust center of main plane)
3. Calculate MAD = median(|Z_i - median_Z|)
4. Threshold = max(3·MAD, 100mm)
5. Keep only points where |Z - median_Z| ≤ threshold
```

**Why 3·MAD?**
- In a normal distribution, 3·MAD ≈ 2σ (covers ~95% of data)
- Ridge points are typically 5-10+ MAD away from the main plane
- 100mm minimum prevents over-filtering on very flat decks

### Benefits

1. **Ridge Tolerance**: Deck ridges/slopes no longer distort the geometry
2. **Correct Corner Detection**: Ridge highpoints are filtered out, not mistaken for corners ⭐
3. **Improved Accuracy**: Corner detection works on a truly flat 2D representation
4. **Robust Vessel Forward**: Direction calculation is unaffected by deck tilt
5. **Maintains Compatibility**: All existing detection logic and thresholds work unchanged
6. **Mathematically Sound**: Uses standard rotation matrices for precise transformations
7. **Outlier Resistant**: MAD-based filtering is robust to noise and partial ridge data

### Algorithm Flow

```
Raw Points
    ↓
RANSAC Plane Fit (Step 2) → Initial plane, may include ridge points
    ↓
PRE-FILTER (Step 2.5) → Remove ridge outliers BEFORE PCA refinement
    ├─ Use MAD on signed distances from RANSAC plane
    └─ Keep points within median ± 2.5·MAD (≥60mm threshold)
    ↓
PCA Plane Refinement → Better plane fit without ridge bias
    ↓
Rotate to Horizontal (Step 3) → Normalized Space (Z ≈ 0 for deck)
    ↓
POST-FILTER (Step 3.5) → Catch any remaining ridge points
    ├─ Use MAD on Z values in normalized space
    └─ Keep points within median ± 2.5·MAD (≥80mm threshold)
    ↓
2D Projection (Step 4) → Simple X,Y coordinates
    ↓
Corner/Edge Detection → On clean, flat deck boundary
    ↓
Inverse Transform → Back to original 3D coordinates
    ↓
Result: Accurate corners, edges, vessel forward (NO ridge highpoints!)
```

### Why Dual-Stage Filtering?

**Problem**: Ridge points can enter the pipeline in two ways:

1. **During RANSAC** (if ridge slopes gently):
   - RANSAC threshold = 50mm
   - Gently sloping ridge points can be < 50mm from fitted plane
   - These get marked as "inliers"
   - **Solution**: Pre-filter with MAD before PCA

2. **After PCA refinement** (if ridge is substantial):
   - PCA might fit a plane between deck and ridge
   - Some ridge points survive normalization
   - **Solution**: Post-filter in normalized space

**Result**: Two-stage defense ensures ridge points are removed!

### Filter Threshold Tuning

| Stage | Method | Threshold | Minimum | Why |
|-------|--------|-----------|---------|-----|
| Pre-filter | MAD on signed distance | 2.5·MAD | 60mm | Prevent PCA bias, aggressive |
| Post-filter | MAD on Z values | 2.5·MAD | 80mm | Final cleanup, slightly looser |

**Why 2.5·MAD?**
- 3.0·MAD covers ~95% of normal data (too permissive for ridges)
- 2.5·MAD covers ~90% (better balance)
- 2.0·MAD would risk removing valid deck boundary points

**Why different minimums?**
- Pre-filter: 60mm (tighter, working with potentially biased data)
- Post-filter: 80mm (looser, working with cleaner data after normalization)

### Example: What Gets Filtered

**Raw RANSAC inliers**:
- Main deck: 5000 points, distance = 0 ± 30mm
- Ridge slope: 200 points, distance = 40-80mm
- Ridge top: 50 points, distance = 150mm+

**After Pre-filter (Step 2.5)**:
- Median distance = 5mm, MAD = 25mm
- Threshold = max(2.5×25, 60) = 62.5mm
- Keep: Main deck (distance < 62.5mm) ✅
- Remove: Most ridge (distance > 62.5mm) ❌
- Keep: ~50 ridge slope points near threshold 😕

**After Normalization (Step 3)**:
- Deck points: Z ≈ 0 ± 40mm
- Remaining ridge points: Z ≈ 80-150mm

**After Post-filter (Step 3.5)**:
- Median Z = 2mm, MAD = 35mm  
- Threshold = max(2.5×35, 80) = 87.5mm
- Keep: Deck (|Z - 2| < 87.5mm) ✅
- Remove: Ridge remnants (|Z - 2| > 87.5mm) ❌

**Final Result**: Only true deck boundary points! 🎯

### Alternative Approaches Considered

Other methods could include:
- Robust plane fitting with outlier rejection (but wouldn't solve the projection distortion)
- Working directly in 3D without projection (more complex, computationally expensive)
- Local plane fitting per region (could handle complex surfaces but adds complexity)
- Fixed Z-threshold filtering (less robust to varying ridge heights)

The normalization + MAD filtering approach was chosen because it's elegant, mathematically precise, robust, and requires minimal changes to existing well-tested detection logic.

## Testing Recommendations

1. Test on flat decks to ensure no regression
2. Test on decks with ridges at various heights (200mm, 500mm, 800mm)
3. Test on decks with ridges at various angles
4. Test on decks with significant pitch/roll
5. Verify vessel forward accuracy across all deck shapes
6. Check corner detection precision on hexagon, square, and round shapes
7. Test edge cases: very low ridges (<100mm), multiple ridges, partial ridge data
