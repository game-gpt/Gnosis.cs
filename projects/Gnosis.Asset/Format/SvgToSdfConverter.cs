using Oak.Svg;

namespace Gnosis.Asset.Format;

/// <summary>
///     SDF 数据格式
/// </summary>
public enum SdfDataFormat : byte
{
    /// <summary>
    ///     8 位有符号距离
    /// </summary>
    Int8 = 0,

    /// <summary>
    ///     16 位有符号距离
    /// </summary>
    Float16 = 1,

    /// <summary>
    ///     32 位浮点距离
    /// </summary>
    Float32 = 2
}

/// <summary>
///     SDF 纹理数据
/// </summary>
public sealed class SdfData
{
    /// <summary>
    ///     名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     纹理宽度
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     纹理高度
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     数据格式
    /// </summary>
    public SdfDataFormat Format { get; init; } = SdfDataFormat.Float32;

    /// <summary>
    ///     SDF 像素数据（行优先，每像素一个 float 距离值）
    /// </summary>
    public float[] Distances { get; init; } = [];
}

/// <summary>
///     SVG 到 SDF 转换器，将 SVG 矢量图形转换为有符号距离场纹理
/// </summary>
public sealed class SvgToSdfConverter
{
    /// <summary>
    ///     将 SVG 文档转换为 SDF 纹理
    /// </summary>
    /// <param name="document">SVG 文档</param>
    /// <param name="width">输出 SDF 纹理宽度</param>
    /// <param name="height">输出 SDF 纹理高度</param>
    /// <param name="spread">SDF 扩散距离（像素）</param>
    public SdfData Convert(SvgDocument document, int width, int height, float spread = 8f)
    {
        if (document is { Width: <= 0 } or { Height: <= 0 })
        {
            throw new ArgumentException("SVG 文档尺寸无效");
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException($"SDF 尺寸无效：{width}x{height}");
        }

        var scaleX = width / document.Width;
        var scaleY = height / document.Height;
        var distances = new float[width * height];

        Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                var svgX = x / scaleX;
                var svgY = y / scaleY;
                var minDist = float.MaxValue;

                ComputeMinDistance(document.Root, svgX, svgY, ref minDist, 1f, 0f, 0f);

                var normalizedDist = minDist / spread;
                normalizedDist = Math.Clamp(normalizedDist, -1f, 1f);

                distances[y * width + x] = normalizedDist;
            }
        });

        return new SdfData
        {
            Name = "sdf",
            Width = width,
            Height = height,
            Format = SdfDataFormat.Float32,
            Distances = distances
        };
    }

    /// <summary>
    ///     将 SDF 数据转换为 RGBA 纹理数据（距离值编码到 R 通道）
    /// </summary>
    public byte[] SdfToRgbaBytes(SdfData sdf)
    {
        var pixels = new byte[sdf.Width * sdf.Height * 4];

        for (var i = 0; i < sdf.Distances.Length; i++)
        {
            var value = (sdf.Distances[i] + 1f) * 0.5f;
            var byteValue = (byte)Math.Clamp(value * 255, 0, 255);

            pixels[i * 4] = byteValue;
            pixels[i * 4 + 1] = byteValue;
            pixels[i * 4 + 2] = byteValue;
            pixels[i * 4 + 3] = 255;
        }

        return pixels;
    }

    private void ComputeMinDistance(
        SvgElement element,
        float px, float py,
        ref float minDist,
        float parentScaleX, float parentOffsetX, float parentOffsetY)
    {
        var (offsetX, offsetY, scaleX, scaleY) = ApplyTransform(
            element.Transforms, parentScaleX, parentOffsetX, parentOffsetY);

        var localX = (px - offsetX) / scaleX;
        var localY = (py - offsetY) / scaleY;

        switch (element)
        {
            case SvgCircleElement circle:
                UpdateMinDistance(CircleDistance(localX, localY, circle), ref minDist, scaleX, scaleY);
                break;

            case SvgRectElement rect:
                UpdateMinDistance(RectDistance(localX, localY, rect), ref minDist, scaleX, scaleY);
                break;

            case SvgEllipseElement ellipse:
                UpdateMinDistance(EllipseDistance(localX, localY, ellipse), ref minDist, scaleX, scaleY);
                break;

            case SvgLineElement line:
                UpdateMinDistance(LineDistance(localX, localY, line), ref minDist, scaleX, scaleY);
                break;

            case SvgPathElement path:
                UpdateMinDistance(PathDistance(localX, localY, path), ref minDist, scaleX, scaleY);
                break;

            case SvgPolygonElement polygon:
                UpdateMinDistance(PolygonDistance(localX, localY, polygon), ref minDist, scaleX, scaleY);
                break;

            case SvgPolylineElement polyline:
                UpdateMinDistance(PolylineDistance(localX, localY, polyline), ref minDist, scaleX, scaleY);
                break;
        }

        foreach (var child in element.Children)
        {
            ComputeMinDistance(child, px, py, ref minDist, scaleX, offsetX, offsetY);
        }
    }

    private static void UpdateMinDistance(float dist, ref float minDist, float scaleX, float scaleY)
    {
        var avgScale = (scaleX + scaleY) * 0.5f;
        var scaledDist = dist * avgScale;

        if (scaledDist < minDist)
        {
            minDist = scaledDist;
        }
    }

    private static (float offsetX, float offsetY, float scaleX, float scaleY) ApplyTransform(
        List<SvgTransform> transforms,
        float parentScaleX, float parentOffsetX, float parentOffsetY)
    {
        var offsetX = parentOffsetX;
        var offsetY = parentOffsetY;
        var scaleX = parentScaleX;
        var scaleY = parentScaleX;

        foreach (var transform in transforms)
        {
            switch (transform.Type)
            {
                case SvgTransformType.Translate:
                    if (transform.Arguments.Length >= 2)
                    {
                        offsetX += transform.Arguments[0] * scaleX;
                        offsetY += transform.Arguments[1] * scaleX;
                    }
                    else if (transform.Arguments.Length >= 1)
                    {
                        offsetX += transform.Arguments[0] * scaleX;
                    }

                    break;

                case SvgTransformType.Scale:
                    if (transform.Arguments.Length >= 2)
                    {
                        scaleX *= transform.Arguments[0];
                        scaleY *= transform.Arguments[1];
                    }
                    else if (transform.Arguments.Length >= 1)
                    {
                        scaleX *= transform.Arguments[0];
                        scaleY *= transform.Arguments[0];
                    }

                    break;
            }
        }

        return (offsetX, offsetY, scaleX, scaleY);
    }

    #region 基本形状距离函数

    private static float CircleDistance(float px, float py, SvgCircleElement circle)
    {
        var dx = px - circle.Cx;
        var dy = py - circle.Cy;
        var dist = MathF.Sqrt(dx * dx + dy * dy);
        return dist - circle.R;
    }

    private static float RectDistance(float px, float py, SvgRectElement rect)
    {
        if (rect.HasRoundedCorners)
        {
            return RoundedRectDistance(px, py, rect);
        }

        var dx = MathF.Abs(px - rect.X - rect.Width * 0.5f) - rect.Width * 0.5f;
        var dy = MathF.Abs(py - rect.Y - rect.Height * 0.5f) - rect.Height * 0.5f;

        var outsideDist = MathF.Sqrt(MathF.Max(dx, 0) * MathF.Max(dx, 0) + MathF.Max(dy, 0) * MathF.Max(dy, 0));
        var insideDist = MathF.Min(MathF.Max(dx, dy), 0);

        return outsideDist + insideDist;
    }

    private static float RoundedRectDistance(float px, float py, SvgRectElement rect)
    {
        var rx = MathF.Min(rect.Rx, rect.Width * 0.5f);
        var ry = MathF.Min(rect.Ry, rect.Height * 0.5f);

        var cx = rect.X + rect.Width * 0.5f;
        var cy = rect.Y + rect.Height * 0.5f;
        var halfW = rect.Width * 0.5f - rx;
        var halfH = rect.Height * 0.5f - ry;

        var dx = MathF.Abs(px - cx) - halfW;
        var dy = MathF.Abs(py - cy) - halfH;

        if (dx > 0 && dy > 0)
        {
            var ellipseDist = MathF.Sqrt((dx / rx) * (dx / rx) + (dy / ry) * (dy / ry));
            return (ellipseDist - 1f) * MathF.Min(rx, ry);
        }

        var outsideDist = MathF.Sqrt(MathF.Max(dx, 0) * MathF.Max(dx, 0) + MathF.Max(dy, 0) * MathF.Max(dy, 0));
        var insideDist = MathF.Min(MathF.Max(dx, dy), 0);

        return outsideDist + insideDist;
    }

    private static float EllipseDistance(float px, float py, SvgEllipseElement ellipse)
    {
        var dx = (px - ellipse.Cx) / ellipse.Rx;
        var dy = (py - ellipse.Cy) / ellipse.Ry;
        var dist = MathF.Sqrt(dx * dx + dy * dy);
        return (dist - 1f) * MathF.Min(ellipse.Rx, ellipse.Ry);
    }

    private static float LineDistance(float px, float py, SvgLineElement line)
    {
        var dx = line.X2 - line.X1;
        var dy = line.Y2 - line.Y1;
        var lenSq = dx * dx + dy * dy;

        if (lenSq < 1e-10f)
        {
            var d = MathF.Sqrt((px - line.X1) * (px - line.X1) + (py - line.Y1) * (py - line.Y1));
            return d;
        }

        var t = Math.Clamp(((px - line.X1) * dx + (py - line.Y1) * dy) / lenSq, 0f, 1f);
        var closestX = line.X1 + t * dx;
        var closestY = line.Y1 + t * dy;

        return MathF.Sqrt((px - closestX) * (px - closestX) + (py - closestY) * (py - closestY));
    }

    private static float PathDistance(float px, float py, SvgPathElement path)
    {
        if (path.Commands.Count == 0)
        {
            return float.MaxValue;
        }

        var minDist = float.MaxValue;
        var currentX = 0f;
        var currentY = 0f;
        var startX = 0f;
        var startY = 0f;
        var lastControlX = 0f;
        var lastControlY = 0f;
        var lastCommand = SvgPathCommandType.ClosePath;

        foreach (var cmd in path.Commands)
        {
            switch (cmd.Type)
            {
                case SvgPathCommandType.MoveTo:
                    currentX = cmd.Arguments[0];
                    currentY = cmd.Arguments[1];
                    startX = currentX;
                    startY = currentY;
                    break;

                case SvgPathCommandType.RelativeMoveTo:
                    currentX += cmd.Arguments[0];
                    currentY += cmd.Arguments[1];
                    startX = currentX;
                    startY = currentY;
                    break;

                case SvgPathCommandType.LineTo:
                    {
                        var dist = SegmentDistance(px, py, currentX, currentY, cmd.Arguments[0], cmd.Arguments[1]);
                        if (dist < minDist) minDist = dist;
                        currentX = cmd.Arguments[0];
                        currentY = cmd.Arguments[1];
                        break;
                    }

                case SvgPathCommandType.RelativeLineTo:
                    {
                        var endX = currentX + cmd.Arguments[0];
                        var endY = currentY + cmd.Arguments[1];
                        var dist = SegmentDistance(px, py, currentX, currentY, endX, endY);
                        if (dist < minDist) minDist = dist;
                        currentX = endX;
                        currentY = endY;
                        break;
                    }

                case SvgPathCommandType.HorizontalLineTo:
                    {
                        var dist = SegmentDistance(px, py, currentX, currentY, cmd.Arguments[0], currentY);
                        if (dist < minDist) minDist = dist;
                        currentX = cmd.Arguments[0];
                        break;
                    }

                case SvgPathCommandType.RelativeHorizontalLineTo:
                    {
                        var endX = currentX + cmd.Arguments[0];
                        var dist = SegmentDistance(px, py, currentX, currentY, endX, currentY);
                        if (dist < minDist) minDist = dist;
                        currentX = endX;
                        break;
                    }

                case SvgPathCommandType.VerticalLineTo:
                    {
                        var dist = SegmentDistance(px, py, currentX, currentY, currentX, cmd.Arguments[0]);
                        if (dist < minDist) minDist = dist;
                        currentY = cmd.Arguments[0];
                        break;
                    }

                case SvgPathCommandType.RelativeVerticalLineTo:
                    {
                        var endY = currentY + cmd.Arguments[0];
                        var dist = SegmentDistance(px, py, currentX, currentY, currentX, endY);
                        if (dist < minDist) minDist = dist;
                        currentY = endY;
                        break;
                    }

                case SvgPathCommandType.CurveTo:
                    {
                        var dist = CubicBezierDistance(px, py,
                            currentX, currentY,
                            cmd.Arguments[0], cmd.Arguments[1],
                            cmd.Arguments[2], cmd.Arguments[3],
                            cmd.Arguments[4], cmd.Arguments[5]);
                        if (dist < minDist) minDist = dist;
                        lastControlX = cmd.Arguments[2];
                        lastControlY = cmd.Arguments[3];
                        currentX = cmd.Arguments[4];
                        currentY = cmd.Arguments[5];
                        break;
                    }

                case SvgPathCommandType.RelativeCurveTo:
                    {
                        var cp1X = currentX + cmd.Arguments[0];
                        var cp1Y = currentY + cmd.Arguments[1];
                        var cp2X = currentX + cmd.Arguments[2];
                        var cp2Y = currentY + cmd.Arguments[3];
                        var endX = currentX + cmd.Arguments[4];
                        var endY = currentY + cmd.Arguments[5];
                        var dist = CubicBezierDistance(px, py, currentX, currentY, cp1X, cp1Y, cp2X, cp2Y, endX, endY);
                        if (dist < minDist) minDist = dist;
                        lastControlX = cp2X;
                        lastControlY = cp2Y;
                        currentX = endX;
                        currentY = endY;
                        break;
                    }

                case SvgPathCommandType.QuadraticCurveTo:
                    {
                        var dist = QuadraticBezierDistance(px, py,
                            currentX, currentY,
                            cmd.Arguments[0], cmd.Arguments[1],
                            cmd.Arguments[2], cmd.Arguments[3]);
                        if (dist < minDist) minDist = dist;
                        lastControlX = cmd.Arguments[0];
                        lastControlY = cmd.Arguments[1];
                        currentX = cmd.Arguments[2];
                        currentY = cmd.Arguments[3];
                        break;
                    }

                case SvgPathCommandType.RelativeQuadraticCurveTo:
                    {
                        var cpX = currentX + cmd.Arguments[0];
                        var cpY = currentY + cmd.Arguments[1];
                        var endX = currentX + cmd.Arguments[2];
                        var endY = currentY + cmd.Arguments[3];
                        var dist = QuadraticBezierDistance(px, py, currentX, currentY, cpX, cpY, endX, endY);
                        if (dist < minDist) minDist = dist;
                        lastControlX = cpX;
                        lastControlY = cpY;
                        currentX = endX;
                        currentY = endY;
                        break;
                    }

                case SvgPathCommandType.ArcTo:
                    {
                        var dist = ArcDistance(px, py, currentX, currentY,
                            cmd.Arguments[0], cmd.Arguments[1], cmd.Arguments[2],
                            cmd.Arguments[3] > 0.5f, cmd.Arguments[4] > 0.5f,
                            cmd.Arguments[5], cmd.Arguments[6]);
                        if (dist < minDist) minDist = dist;
                        currentX = cmd.Arguments[5];
                        currentY = cmd.Arguments[6];
                        break;
                    }

                case SvgPathCommandType.RelativeArcTo:
                    {
                        var endX = currentX + cmd.Arguments[5];
                        var endY = currentY + cmd.Arguments[6];
                        var dist = ArcDistance(px, py, currentX, currentY,
                            cmd.Arguments[0], cmd.Arguments[1], cmd.Arguments[2],
                            cmd.Arguments[3] > 0.5f, cmd.Arguments[4] > 0.5f,
                            endX, endY);
                        if (dist < minDist) minDist = dist;
                        currentX = endX;
                        currentY = endY;
                        break;
                    }

                case SvgPathCommandType.ClosePath:
                    {
                        var dist = SegmentDistance(px, py, currentX, currentY, startX, startY);
                        if (dist < minDist) minDist = dist;
                        currentX = startX;
                        currentY = startY;
                        break;
                    }
            }

            lastCommand = cmd.Type;
        }

        return minDist;
    }

    private static float PolygonDistance(float px, float py, SvgPolygonElement polygon)
    {
        return PolylineDistanceCore(px, py, polygon.Points, true);
    }

    private static float PolylineDistance(float px, float py, SvgPolylineElement polyline)
    {
        return PolylineDistanceCore(px, py, polyline.Points, false);
    }

    private static float PolylineDistanceCore(float px, float py, float[] points, bool closed)
    {
        if (points.Length < 4)
        {
            return float.MaxValue;
        }

        var minDist = float.MaxValue;
        var count = points.Length / 2;

        for (var i = 0; i < count - 1; i++)
        {
            var dist = SegmentDistance(px, py,
                points[i * 2], points[i * 2 + 1],
                points[(i + 1) * 2], points[(i + 1) * 2 + 1]);

            if (dist < minDist) minDist = dist;
        }

        if (closed && count >= 3)
        {
            var dist = SegmentDistance(px, py,
                points[(count - 1) * 2], points[(count - 1) * 2 + 1],
                points[0], points[1]);

            if (dist < minDist) minDist = dist;
        }

        return minDist;
    }

    #endregion

    #region 曲线距离函数

    private static float SegmentDistance(float px, float py, float x1, float y1, float x2, float y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var lenSq = dx * dx + dy * dy;

        if (lenSq < 1e-10f)
        {
            return MathF.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        }

        var t = Math.Clamp(((px - x1) * dx + (py - y1) * dy) / lenSq, 0f, 1f);
        var closestX = x1 + t * dx;
        var closestY = y1 + t * dy;

        return MathF.Sqrt((px - closestX) * (px - closestX) + (py - closestY) * (py - closestY));
    }

    private static float CubicBezierDistance(
        float px, float py,
        float x0, float y0,
        float x1, float y1,
        float x2, float y2,
        float x3, float y3)
    {
        var minDist = float.MaxValue;
        const int steps = 16;

        for (var i = 0; i < steps; i++)
        {
            var t1 = (float)i / steps;
            var t2 = (float)(i + 1) / steps;

            var p1 = CubicBezierPoint(t1, x0, y0, x1, y1, x2, y2, x3, y3);
            var p2 = CubicBezierPoint(t2, x0, y0, x1, y1, x2, y2, x3, y3);

            var dist = SegmentDistance(px, py, p1.X, p1.Y, p2.X, p2.Y);
            if (dist < minDist) minDist = dist;
        }

        return minDist;
    }

    private static (float X, float Y) CubicBezierPoint(
        float t,
        float x0, float y0,
        float x1, float y1,
        float x2, float y2,
        float x3, float y3)
    {
        var u = 1f - t;
        var uu = u * u;
        var uuu = uu * u;
        var tt = t * t;
        var ttt = tt * t;

        var x = uuu * x0 + 3f * uu * t * x1 + 3f * u * tt * x2 + ttt * x3;
        var y = uuu * y0 + 3f * uu * t * y1 + 3f * u * tt * y2 + ttt * y3;

        return (x, y);
    }

    private static float QuadraticBezierDistance(
        float px, float py,
        float x0, float y0,
        float x1, float y1,
        float x2, float y2)
    {
        var minDist = float.MaxValue;
        const int steps = 12;

        for (var i = 0; i < steps; i++)
        {
            var t1 = (float)i / steps;
            var t2 = (float)(i + 1) / steps;

            var p1 = QuadraticBezierPoint(t1, x0, y0, x1, y1, x2, y2);
            var p2 = QuadraticBezierPoint(t2, x0, y0, x1, y1, x2, y2);

            var dist = SegmentDistance(px, py, p1.X, p1.Y, p2.X, p2.Y);
            if (dist < minDist) minDist = dist;
        }

        return minDist;
    }

    private static (float X, float Y) QuadraticBezierPoint(
        float t,
        float x0, float y0,
        float x1, float y1,
        float x2, float y2)
    {
        var u = 1f - t;
        var x = u * u * x0 + 2f * u * t * x1 + t * t * x2;
        var y = u * u * y0 + 2f * u * t * y1 + t * t * y2;

        return (x, y);
    }

    private static float ArcDistance(
        float px, float py,
        float x1, float y1,
        float rx, float ry,
        float xRotation,
        bool largeArc, bool sweep,
        float x2, float y2)
    {
        if (rx < 1e-6f || ry < 1e-6f)
        {
            return SegmentDistance(px, py, x1, y1, x2, y2);
        }

        var phi = xRotation * MathF.PI / 180f;
        var cosPhi = MathF.Cos(phi);
        var sinPhi = MathF.Sin(phi);

        var dx = (x1 - x2) * 0.5f;
        var dy = (y1 - y2) * 0.5f;
        var x1p = cosPhi * dx + sinPhi * dy;
        var y1p = -sinPhi * dx + cosPhi * dy;

        var x1pSq = x1p * x1p;
        var y1pSq = y1p * y1p;
        var rxSq = rx * rx;
        var rySq = ry * ry;

        var lambda = x1pSq / rxSq + y1pSq / rySq;

        if (lambda > 1f)
        {
            var sqrtLambda = MathF.Sqrt(lambda);
            rx *= sqrtLambda;
            ry *= sqrtLambda;
            rxSq = rx * rx;
            rySq = ry * ry;
        }

        var num = rxSq * rySq - rxSq * y1pSq - rySq * x1pSq;
        var den = rxSq * y1pSq + rySq * x1pSq;
        var sq = MathF.Max(num / den, 0f);
        var sqrtSq = MathF.Sqrt(sq);

        if (largeArc == sweep) sqrtSq = -sqrtSq;

        var cxp = sqrtSq * rx * y1p / ry;
        var cyp = -sqrtSq * ry * x1p / rx;

        var cx = cosPhi * cxp - sinPhi * cyp + (x1 + x2) * 0.5f;
        var cy = sinPhi * cxp + cosPhi * cyp + (y1 + y2) * 0.5f;

        var theta1 = VectorAngle((x1p - cxp) / rx, (y1p - cyp) / ry);
        var dTheta = VectorAngle((-x1p - cxp) / rx, (-y1p - cyp) / ry) - theta1;

        if (!sweep && dTheta > 0) dTheta -= 2f * MathF.PI;
        if (sweep && dTheta < 0) dTheta += 2f * MathF.PI;

        var minDist = float.MaxValue;
        const int steps = 16;

        for (var i = 0; i < steps; i++)
        {
            var t1 = (float)i / steps;
            var t2 = (float)(i + 1) / steps;

            var angle1 = theta1 + dTheta * t1;
            var angle2 = theta1 + dTheta * t2;

            var p1x = cosPhi * rx * MathF.Cos(angle1) - sinPhi * ry * MathF.Sin(angle1) + cx;
            var p1y = sinPhi * rx * MathF.Cos(angle1) + cosPhi * ry * MathF.Sin(angle1) + cy;
            var p2x = cosPhi * rx * MathF.Cos(angle2) - sinPhi * ry * MathF.Sin(angle2) + cx;
            var p2y = sinPhi * rx * MathF.Cos(angle2) + cosPhi * ry * MathF.Sin(angle2) + cy;

            var dist = SegmentDistance(px, py, p1x, p1y, p2x, p2y);
            if (dist < minDist) minDist = dist;
        }

        return minDist;
    }

    private static float VectorAngle(float ux, float uy)
    {
        var angle = MathF.Atan2(uy, ux);
        return angle;
    }

    #endregion
}
