﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Comet.Controls
{
    public class RefreshableListView : ListView
    {
        private Border Root;
        private Border RefreshIndicator;
        private CompositeTransform RefreshIndicatorTransform;
        private ScrollViewer Scroller;
        private CompositeTransform ContentTransform;
        private Grid ScrollerContent;

        private TextBlock IndicatorContent;

        public event EventHandler RefreshActivated;

        private double lastOffset = 0.0;
        private double pullDistance = 0.0;

        private bool manipulating = false;
        DateTime lastRefreshActivation = default(DateTime);
        bool refreshActivated = false;


        public RefreshableListView()
        {
            DefaultStyleKey = typeof(RefreshableListView);
            SizeChanged += RefreshableListView_SizeChanged;
        }

        private void RefreshableListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height)
            };
        }

        protected override void OnApplyTemplate()
        {
            Root = GetTemplateChild("Root") as Border;

            Scroller = this.GetTemplateChild("ScrollViewer") as ScrollViewer;
            Scroller.DirectManipulationCompleted += Scroller_DirectManipulationCompleted;
            Scroller.DirectManipulationStarted += Scroller_DirectManipulationStarted;

            ContentTransform = GetTemplateChild("ContentTransform") as CompositeTransform;

            ScrollerContent = GetTemplateChild("ScrollerContent") as Grid;

            RefreshIndicator = GetTemplateChild("RefreshIndicator") as Border;
            RefreshIndicatorTransform = GetTemplateChild("RefreshIndicatorTransform") as CompositeTransform;

            IndicatorContent = GetTemplateChild("IndicatorContent") as TextBlock;

            RefreshIndicator.SizeChanged += (s, e) =>
            {
                RefreshIndicatorTransform.TranslateY = -RefreshIndicator.ActualHeight;
            };
            
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            OverscrollMultiplier = (OverscrollCoefficient * 10) / DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            base.OnApplyTemplate();
        }
        
        #region dependencyProperties

        private double OverscrollMultiplier;
        public double OverscrollCoefficient
        {
            get { return (double)GetValue(OverscrollCoefficientProperty); }
            set
            {
                if (value >= 0 && value <= 1)
                {
                    OverscrollMultiplier = (value * 10) / DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
                    SetValue(OverscrollCoefficientProperty, value);
                }
                else
                    throw new IndexOutOfRangeException("OverscrollCoefficient has to be a double value between 0 and 1 inclusive.");
            }
        }

        public static readonly DependencyProperty OverscrollCoefficientProperty =
            DependencyProperty.Register("OverscrollCoefficient", typeof(double), typeof(RefreshableListView), new PropertyMetadata(0.5));



        public double PullThreshold
        {
            get { return (double)GetValue(PullThresholdProperty); }
            set { SetValue(PullThresholdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PullThreshold.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PullThresholdProperty =
            DependencyProperty.Register("PullThreshold", typeof(double), typeof(RefreshableListView), new PropertyMetadata(100.0));




        public ICommand RefreshCommand
        {
            get { return (ICommand)GetValue(RefreshCommandProperty); }
            set { SetValue(RefreshCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RefreshCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RefreshCommandProperty =
            DependencyProperty.Register("RefreshCommand", typeof(ICommand), typeof(RefreshableListView), new PropertyMetadata(null));




        #endregion


        private void Scroller_DirectManipulationStarted(object sender, object e)
        {
            if (Scroller.VerticalOffset == 0)
                manipulating = true;
        }

        private void Scroller_DirectManipulationCompleted(object sender, object e)
        {
            manipulating = false;
            RefreshIndicatorTransform.TranslateY = -RefreshIndicator.ActualHeight;
            ContentTransform.TranslateY = 0;

            if (refreshActivated)
            {
                if (RefreshActivated != null)
                    RefreshActivated(this, new EventArgs());
                if (RefreshCommand != null && RefreshCommand.CanExecute(null))
                    RefreshCommand.Execute(null);
            }

            refreshActivated = false;
            lastRefreshActivation = default(DateTime);
            IndicatorContent.Text = "Pull to refresh";
        }



        

        private void CompositionTarget_Rendering(object sender, object e)
        {
            if (!manipulating) return;
            //else if (Scroller.VerticalOffset > 0)
            //{
            //    manipulating = false;
            //    return;
            //}
            Rect elementBounds = ScrollerContent.TransformToVisual(Root).TransformBounds(new Rect());

            var offset = elementBounds.Y;
            var delta = offset - lastOffset;
            lastOffset = offset;

            pullDistance += delta * OverscrollMultiplier;

            if (pullDistance > 0)
            {
                ContentTransform.TranslateY = pullDistance - offset;
                RefreshIndicatorTransform.TranslateY = pullDistance - offset - RefreshIndicator.ActualHeight;
            }
            else
            {
                ContentTransform.TranslateY = 0;
                RefreshIndicatorTransform.TranslateY = -RefreshIndicator.ActualHeight;

            }

            if (pullDistance >= PullThreshold)
            {
                lastRefreshActivation = DateTime.Now;
                refreshActivated = true;
                IndicatorContent.Text = "Relese to refresh";
            }
            else if (lastRefreshActivation != DateTime.MinValue)
            {
                TimeSpan timeSinceActivated = DateTime.Now - lastRefreshActivation;
                // if more then a second since activation, deactivate
                if (timeSinceActivated.TotalMilliseconds > 1000)
                {
                    refreshActivated = false;
                    lastRefreshActivation = default(DateTime);
                    IndicatorContent.Text = "Pull to refresh";
                }
            }
            

            
        }

    }


}
