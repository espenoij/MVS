# Ridge Corner Detection Fix

## Problem Summary

The helideck corner location method was finding corners at ridge locations instead of at the actual deck boundary. Additionally, even when corners were correctly identified, they appeared **off the fitted plane in the 3D view**, displaying at the cloud point positions rather than on the fitted plane surface.

## Root Cause Analysis

### Issue #1: Missing Post-Filtering

#### What Was Supposed to Happen (According to Documentation)

The RIDGE_NORMALIZATION_CHANGES.md document described a **dual-stage filtering approach**:

1. **Pre-filtering (Step 2.5)**: Filter ridge points after RANSAC but before PCA refinement
2. **Post-filtering (Step 3.5)**: Filter remaining ridge points after normalization using MAD (Median Absolute Deviation)

#### What Was Actually Implemented

Only **Step 3** (projection onto plane) and **Step 3.5** (rotation to normalize) were implemented. The critical **Step 3.6 post-filtering** was **completely missing** from the code.

#### Why This Caused the Problem

1. RANSAC (Step 2) identifies inlier points within 50mm of the fitted plane
   - Ridge points close to the plane are included as inliers

2. Projection (Step 3) projects all inliers onto the fitted plane
   - Ridge points keep their X-Y footprint (horizontal position)
   - Only their Z elevation is removed

3. Normalization (Step 3.5) rotates the plane to be horizontal
   - Ridge points still retain their horizontal positions

4. **MISSING: Post-filtering** should remove ridge points based on Z deviation in normalized space
   - Without this, ridge points at deck edges become "extremal points"

5. Corner detection (Step 4+) finds extremal points in 2D projection
   - Ridge points at edges are selected as "corners"

### Issue #2: Inconsistent Corner Visualization vs Vessel Forward Calculation

Even after implementing MAD filtering, there was still a visualization problem where corners appeared off the fitted plane in the 3D view.

#### The Discrepancy

**Vessel Forward Calculation** (CORRECT):
```csharp
// Lines 467-472 in FindEdgeHexagon
double f3x = R[0, 0] * fU + R[1, 0] * fW;  // Uses fU, fW only
double f3y = R[0, 1] * fU + R[1, 1] * fW;  // No Z component
double f3z = R[0, 2] * fU + R[1, 2] * fW;  // Implicitly Z=0
```
- Uses only 2D coordinates from normalized space
- Assumes Z = 0 (on the fitted plane)
- Result: Direction lies in the fitted plane

**Corner Visualization** (INCORRECT - BEFORE FIX):
```csharp
// Lines 323-330 in FindEdgeHexagon (original)
double nz = normalized[vIdx[k]].z;  // ❌ Uses actual Z from point cloud
double ox = R[0, 0] * nu + R[1, 0] * nw + R[2, 0] * nz;
```
- Used the actual Z coordinate from `normalized[vIdx[k]].z`
- This Z value is non-zero due to:
  - Floating point precision in projection/rotation
  - Original point cloud noise
  - Points not perfectly on the fitted plane
- Result: Corners displayed at cloud point positions, not on fitted plane

#### Why This Matters

1. **Visual inconsistency**: The fitted plane is shown as a flat surface, but corners float above/below it
2. **Misleading representation**: Corners should represent the deck boundary **on the fitted plane**, not arbitrary cloud point heights
3. **Conceptual mismatch**: The algorithm works in 2D (after projection), so the 3D visualization should reflect this by placing corners on the plane

## The Solution

### Fix #1: Implemented MAD-Based Post-Filtering

Added **Step 3.6: Post-filtering using MAD** between normalization and corner detection:

```csharp
// After normalizing to make deck horizontal:

1. Calculate median Z value of all normalized points
2. Calculate MAD (Median Absolute Deviation) of Z values
3. Set threshold = max(2.5 * MAD, 80mm)
4. Remove points where |Z - median| > threshold
```

#### Why This Works

- **Main deck points** in normalized space have Z ≈ 0 (within a few mm due to noise)
- **Ridge points** have Z values significantly different from 0 (typically 100mm+ depending on ridge height)
- **MAD is robust** to outliers, so it correctly identifies the main deck plane even with ridges present
- **2.5 × MAD threshold** is aggressive enough to remove ridge points while preserving deck boundary points

#### Parameters Chosen

- **Threshold factor: 2.5** (more aggressive than typical 3.0)
  - Ensures ridge removal at cost of slightly more filtering
  - In normal distribution, 2.5·MAD ≈ 1.65σ (~90% of data)
  - Ridge points are typically 5-10+ MAD away, so easily detected

- **Minimum threshold: 80mm**
  - Prevents over-filtering on extremely flat decks where MAD might be very small
  - Allows for reasonable point cloud noise and minor deck irregularities

### Fix #2: Consistent Z=0 for Corner Visualization

Changed all corner transformation code to use **Z=0** instead of the actual normalized point Z coordinate:

**FindEdgeHexagon** (lines 318-331):
```csharp
double nz = 0.0;  // ✅ Force Z=0 to place corners on the fitted plane
```

**FindEdgeSquare** (lines 554-588):
```csharp
double nz0 = 0.0, nz1 = 0.0;  // ✅ SquareRoundedBow corners
double nz = 0.0;               // ✅ Square corners
```

**FindEdgeConvexHull** (lines 806-815):
```csharp
double nz = 0.0;  // ✅ Force Z=0 to place hull vertices on the fitted plane
```

This ensures that:
- Corner markers appear **on the fitted plane surface** in the 3D view
- Consistent with how vessel forward direction is calculated
- Visual representation matches the conceptual model (2D corner detection on a flat plane)

## Results

After both fixes:

1. ✅ Ridge points are filtered out before corner detection (MAD filtering)
2. ✅ Corners are found only at true deck boundary extremal points
3. ✅ **Corner markers in 3D view appear exactly on the fitted plane**
4. ✅ **Consistent with vessel forward calculation (both use Z=0)**
5. ✅ Vessel forward calculation uses correct deck geometry
6. ✅ Algorithm is now consistent with the documented approach
7. ✅ Visual representation matches conceptual model

## Code Changes

**File**: `MVS\Services\LivoxLidar\LivoxLidarDeckEdgeFinder.cs`

### Change #1: MAD-Based Post-Filtering
**Location**: After Step 3.5 (normalization), before Step 4 (corner detection)  
**Lines Added**: ~45 lines implementing MAD-based Z-filtering in normalized space

### Change #2: Corner Visualization Consistency
**Locations**:
- `FindEdgeHexagon`: lines 318-331 (hex corner transformation)
- `FindEdgeSquare`: lines 554-588 (square corner transformations)
- `FindEdgeConvexHull`: lines 806-815 (convex hull vertex transformation)

**Change**: All corner transformations now use `nz = 0.0` instead of `nz = normalized[vIdx[k]].z`

The fixes maintain backward compatibility and don't change the API or any method signatures.
