﻿using System;
using System.Windows;
using System.Windows.Media;

namespace Nodify
{
    /// <summary>
    /// Represents a line that has an arrow indicating its <see cref="BaseConnection.Direction"/>.
    /// </summary>
    public class DirectionalConnection : BaseConnection
    {
        public static readonly DependencyProperty ArrowSizeProperty = DependencyProperty.Register(nameof(ArrowSize), typeof(Size), typeof(DirectionalConnection), new FrameworkPropertyMetadata(new Size(7, 6), FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the size of the arrow head.
        /// </summary>
        public Size ArrowSize
        {
            get => (Size)GetValue(ArrowSizeProperty);
            set => SetValue(ArrowSizeProperty, value);
        }

        static DirectionalConnection()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DirectionalConnection), new FrameworkPropertyMetadata(typeof(DirectionalConnection)));
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                StreamGeometry geometry = new StreamGeometry
                {
                    FillRule = FillRule.EvenOdd
                };

                using (StreamGeometryContext context = geometry.Open())
                {
                    (Vector sourceOffset, Vector targetOffset) = GetOffset();
                    Point source = Source + sourceOffset;
                    Point target = Target + targetOffset;

                    DrawLineGeometry(context, source, target);
                }

                geometry.Freeze();
                return geometry;
            }
        }

        protected virtual void DrawLineGeometry(StreamGeometryContext context, Point source, Point target)
        {
            context.BeginFigure(source, true, false);
            context.LineTo(target, true, true);

            if (ArrowSize.Width != 0 && ArrowSize.Height != 0)
            {
                if (Direction == ConnectionDirection.Forward)
                {
                    DrawArrowGeometry(context, source, target);
                }
                else
                {
                    DrawArrowGeometry(context, target, source);
                }
            }
        }

        protected virtual void DrawArrowGeometry(StreamGeometryContext context, Point source, Point target)
        {
            Vector delta = source - target;
            double angle = Math.Atan2(delta.Y, delta.X);
            double sinT = Math.Sin(angle);
            double cosT = Math.Cos(angle);

            double headWidth = ArrowSize.Width;
            double headHeight = ArrowSize.Height;

            var from = new Point(target.X + (headWidth * cosT - headHeight * sinT), target.Y + (headWidth * sinT + headHeight * cosT));
            var to = new Point(target.X + (headWidth * cosT + headHeight * sinT), target.Y - (headHeight * cosT - headWidth * sinT));

            context.BeginFigure(target, true, true);
            context.LineTo(from, true, false);
            context.LineTo(to, true, false);
        }
    }
}
