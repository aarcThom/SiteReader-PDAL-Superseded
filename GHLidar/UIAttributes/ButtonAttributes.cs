using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Collections.Generic;
using Rhino.Display;
using System.Drawing.Drawing2D;
using Rhino.Resources;
using System.Windows.Forms;
using Rhino.Render;

namespace SiteReader.UIAttributes
{
    public class ButtonAttributes : GH_ComponentAttributes
    {
        public ButtonAttributes(GH_Component owner) : base(owner) { }

        private int _sliderVal;
        private readonly int _sliderMaxVal = 100;
        private readonly int _sliderMinVal = 0;
        private bool _notSlid = true; // initiate slider at 0 point when component first dropped
        private float _sliderPercent;
        private float _dragStartX = 0; // the slider handle position
        private float _deltaX; // the current move of the handle
        private bool currentlyDrag = false; //is the handle being dragged?
        private float currentHandlePos;

        private bool previewCloud = false;

        //rectangles for layout
        private System.Drawing.Rectangle ButtonBounds { get; set; }
        private System.Drawing.Rectangle SpacerBounds { get; set; }
        private System.Drawing.Rectangle CapsuleBounds { get; set; }
        private System.Drawing.RectangleF SliderBounds { get; set; }
        private float _sliderMidY;
        private System.Drawing.RectangleF SliderHandleBounds { get; set; }

        private System.Drawing.RectangleF SliderTxtBounds;

        protected override void Layout()
        {
            base.Layout();

            //getting component dimensions
            System.Drawing.Rectangle componentRec = GH_Convert.ToRectangle(Bounds);
            

            //spaces values
            int edgeSpace = 2; //spacing to edges and internal between boxes
            int internalSpace = 5; //spacing between UI elements
            int spacerHeight = 10; //spacer height
            int buttonHeight = 22; // the height of all buttons
            int sliderHeight = 22; // the height of the slider bounds

            int overallHeight = buttonHeight + spacerHeight + internalSpace * 3 + edgeSpace + sliderHeight;

            //creating the boundings for the bottom capsule
            System.Drawing.Rectangle capsuleRec =
                new Rectangle(componentRec.Left, componentRec.Bottom + 4, componentRec.Width, overallHeight - 4);
            CapsuleBounds = capsuleRec;

            //increasing the size of overall layout
            componentRec.Height += overallHeight;
            Bounds = componentRec;

            //slider bounds
            System.Drawing.Rectangle sliderRec = capsuleRec;
            sliderRec.Y = capsuleRec.Top + internalSpace;
            sliderRec.Height = sliderHeight;
            sliderRec.Inflate(-edgeSpace * 5, 0);
            SliderBounds = sliderRec;

            //adjusting the slider location

            if (_notSlid)
            {
                _sliderPercent = SliderBounds.Left;
            } else if (currentlyDrag)
            {
                _sliderPercent = currentHandlePos;
            }




            //vertical center of the slider bounds
            var boundsCenterY = SliderBounds.Top + SliderBounds.Height / 2;
            _sliderMidY = boundsCenterY;

            //slider handle
            int handleSize = 10;
            RectangleF handle = new RectangleF(_sliderPercent - handleSize / 2, boundsCenterY - handleSize / 2, handleSize, handleSize);
            SliderHandleBounds = handle;

            //button spacer layout
            System.Drawing.Rectangle spacerRec = componentRec;
            spacerRec.Y = componentRec.Bottom - buttonHeight - internalSpace - spacerHeight - edgeSpace;
            spacerRec.Height = spacerHeight + edgeSpace * 2;
            SpacerBounds = spacerRec;

            // button layout
            System.Drawing.Rectangle buttonRec = componentRec;
            buttonRec.Y = componentRec.Bottom - buttonHeight - edgeSpace;
            buttonRec.Height = buttonHeight;
            buttonRec.Inflate(-edgeSpace, 0);
            ButtonBounds = buttonRec;

            
        }


        //button appearance attributes
        private static readonly string ButtonUnsetText = "False";

        private string ButtonText { get; set; } = ButtonUnsetText;
        private GH_Palette ButtonAppearance { get; set; } = GH_Palette.Black;

        protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {

                // Define the default palette.
                GH_Palette palette = GH_Palette.White;
                Color compGrey = Color.FromArgb(255, 50, 50, 50);

                // Adjust palette based on the Owner's worst case messaging level.
                switch (Owner.RuntimeMessageLevel)
                {
                    case GH_RuntimeMessageLevel.Warning:
                        palette = GH_Palette.Warning;
                        compGrey = Color.FromArgb(255, 80, 10, 0);
                        break;

                    case GH_RuntimeMessageLevel.Error:
                        palette = GH_Palette.Error;
                        compGrey = Color.FromArgb(255, 60, 0, 0);
                        break;
                }

                //render the extra capsule-------------------------------------------
                GH_Capsule capsule = GH_Capsule.CreateCapsule(CapsuleBounds, palette);
                capsule.Render(graphics, Selected, Owner.Locked, true);

                capsule.Dispose();
                capsule = null;

                //render the slider line--------------------------------------------
                Pen sliderLine = new Pen(compGrey);
                graphics.DrawLine(sliderLine,SliderBounds.Left, _sliderMidY, SliderBounds.Right, _sliderMidY);

                //render the slider handle--------------------------------------
                Brush handleFill = new SolidBrush(Color.AliceBlue);
                graphics.FillEllipse(handleFill, SliderHandleBounds);
                graphics.DrawEllipse(sliderLine, SliderHandleBounds);

                //render the slider text---------------------------------------
                Font font = new Font(GH_FontServer.FamilyStandard, 7);
                // adjust fontsize to high resolution displays
                font = new Font(font.FontFamily, font.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);
                string val = _sliderVal.ToString();

                // slider text value to left or right of slider depending on location
                StringFormat sliderValLocation = _sliderVal > (_sliderMaxVal - _sliderMinVal) / 2
                    ? GH_TextRenderingConstants.NearCenter
                    : GH_TextRenderingConstants.FarCenter;

                //graphics.DrawString(val, font, Brushes.Black, sliderValLocation);


                //render the button spacer----------------------------
                Font smallFont = GH_FontServer.Small;
                smallFont = new Font(smallFont.FontFamily, smallFont.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular); // adjust fontsize on UIScale

                graphics.DrawString("Render Point Cloud", smallFont, Brushes.Black, SpacerBounds, GH_TextRenderingConstants.CenterCenter);
                
                //render the button----------------------------
                GH_Capsule button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, ButtonAppearance, ButtonText, 2, 0);
                button.Render(graphics, Selected, Owner.Locked, false);
                button.Dispose();
                button = null;
            }
        }

        //response for button double click
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && Owner.RuntimeMessageLevel != GH_RuntimeMessageLevel.Warning && Owner.RuntimeMessageLevel != GH_RuntimeMessageLevel.Error)
            {
                System.Drawing.RectangleF rec = ButtonBounds;
                if (rec.Contains(e.CanvasLocation))
                {
                    Owner.RecordUndoEvent("Button Clicked");
                    previewCloud = previewCloud == false;
                    ButtonText = previewCloud.ToString();
                    ButtonAppearance = previewCloud ? GH_Palette.White : GH_Palette.Black;
                    Owner.OnDisplayExpired(true);
                    return GH_ObjectResponse.Handled;
                }
            }
            return base.RespondToMouseDoubleClick(sender, e);
        }

        //response for slider hover
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left && previewCloud == false)
            {
                System.Drawing.RectangleF grabRec = SliderHandleBounds;
                if (grabRec.Contains(e.CanvasLocation))
                {
                    _dragStartX = e.CanvasLocation.X;
                    currentlyDrag = true;
                    Owner.ExpireSolution(true);
                    return GH_ObjectResponse.Capture;
                }

            }
            return base.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (currentlyDrag)
            {
                _deltaX = e.CanvasLocation.X - _dragStartX;
                currentHandlePos = e.CanvasLocation.X;

                //use the drag cursor
                Grasshopper.Instances.CursorServer.AttachCursor(sender, "GH_NumericSlider");

                Owner.ExpireSolution(true);
                return GH_ObjectResponse.Capture;
            }

            return base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (currentlyDrag)
                {
                    _dragStartX += _deltaX;
                    _deltaX = 0;
                    currentlyDrag = false;
                    Owner.ExpireSolution(true);

                    return GH_ObjectResponse.Release;
                }
            }


            return base.RespondToMouseUp(sender, e);
        }

    }
}