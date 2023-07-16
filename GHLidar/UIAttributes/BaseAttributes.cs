using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Eto.Forms;
using Rhino.UI;
using MouseButtons = System.Windows.Forms.MouseButtons;

namespace SiteReader.UIAttributes
{
    public class BaseAttributes : GH_ComponentAttributes
    {
        public BaseAttributes(GH_Component owner, Action<float> sliderValue) : base(owner)
        {
            ReturnSliderVal = sliderValue;
        }

        //FIELDS ------------------------------------------------------------------

        //return values
        private readonly Action<float> ReturnSliderVal;

        //rectangles for layouts
        private RectangleF ButtonBounds;
        private RectangleF SecondCapsuleBounds;
        private RectangleF SliderBounds;
        private RectangleF HandleShape;

        //preview the Cloud?
        private bool PreviewCloud = false;
        private string buttonText = "false";

        //field for slider handle position
        private bool _slid = false;
        private bool _currentlySliding = false;
        private float _handlePosX;
        private float _handlePosY;
        private float _curHandleOffset = 0;
        private List<float> _handleOffsets;
        private float _handleWidth = 8;


        protected override void Layout()
        {
            base.Layout(); //handles the basic layout, computes the bounds, etc.
            Rectangle componentRec = GH_Convert.ToRectangle(Bounds); //getting component base bounds

            //saving the original bounds to refer to in custom layout
            var left = componentRec.Left;
            var top = componentRec.Top;
            var right = componentRec.Right;
            var bottom = componentRec.Bottom;
            var width = componentRec.Width;
            var height = componentRec.Height;

            //useful layout variables like spacers, etc.
            int horizSpacer = 10;
            int sideSpacer = 2;
            int extraHeight = 200;

            //here we can modify the bounds
            componentRec.Height += extraHeight; // for example

            //here we can assign the modified bounds to the component's bounds--------------------
            Bounds = componentRec;

            //here we can add extra stuff to the layout-------------------------------------------
            SecondCapsuleBounds = new RectangleF(left, bottom, width, extraHeight);

            SliderBounds = new RectangleF(left, bottom + horizSpacer, width, 20);
            SliderBounds.Inflate(-sideSpacer*4, 0);


            //slider handle and code to move it properly
             _handleWidth = 8;
            _handlePosY = SliderBounds.Height / 2 - _handleWidth / 2 + SliderBounds.Top;


            //getting the handle snap locations
            _handleOffsets = new List<float>();
            for (int i = 0; i < 11; i++)
            {
                var iFl = (float)i;
                var offsetX = iFl * (SliderBounds.Width / 10) - _handleWidth/2;
                _handleOffsets.Add(offsetX);
            }

            //getting current position
            if (!_slid)
            {
                _handlePosX = SliderBounds.Left - _handleWidth / 2;
            }
            
            else
            {
                _handlePosX = SliderBounds.Left + _curHandleOffset;
            }

            //return the slider value to the component remapped between 0 and 1
            if (!_currentlySliding)
            {
                ReturnSliderVal((_curHandleOffset + _handleWidth / 2) / SliderBounds.Width);
            }
            
            

            HandleShape = new RectangleF(_handlePosX, _handlePosY, _handleWidth, _handleWidth);


            //the button
            ButtonBounds = new RectangleF(left, bottom + horizSpacer*2 + SliderBounds.Height, width, 20);
            ButtonBounds.Inflate(-sideSpacer, 0);


        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel); // handle the wires, draw nickname, name, etc.

            //the main component rendering channel
            if (channel == GH_CanvasChannel.Objects)
            {
                //declare the pens / brushes / pallets we will need to draw the custom objects - defaults for blank / message levels
                Pen outLine = CompStyles.BlankOutline;
                GH_Palette pallete = GH_Palette.Normal;

                //use a switch statement to retrieve the proper pens / brushes from our CompColors class
                switch (Owner.RuntimeMessageLevel)
                {
                    case GH_RuntimeMessageLevel.Warning:
                        // assign warning values
                        outLine = CompStyles.WarnOutline;
                        pallete = GH_Palette.Warning;
                        break;

                    case GH_RuntimeMessageLevel.Error:
                        // assign warning values
                        outLine = CompStyles.ErrorOutline;
                        pallete = GH_Palette.Error;
                        break;
                }


                //render custom elements----------------------------------------------------------

                //secondary capsule
                GH_Capsule secondCap = GH_Capsule.CreateCapsule(SecondCapsuleBounds, pallete);
                secondCap.Render(graphics, Selected, Owner.Locked, false);
                secondCap.Dispose();

                //slider line
                var sliderY = SliderBounds.Top + SliderBounds.Height / 2;
                graphics.DrawLine(outLine, SliderBounds.Left, sliderY, SliderBounds.Right, sliderY);

                //slider line vertical ticks
                int count = 0;
                foreach (var offset in _handleOffsets)
                {
                    var tickX = offset + SliderBounds.Left + _handleWidth / 2;

                    float top;
                    if (count % 5 == 0)
                    {
                        top = SliderBounds.Top + 2;
                    }
                    else
                    {
                        top = SliderBounds.Top + 5;
                    }

                    graphics.DrawLine(outLine, tickX, sliderY, tickX, top);

                    count++;
                }

                //slider handle
                graphics.FillEllipse(CompStyles.HandleFill, HandleShape);
                graphics.DrawEllipse(outLine, HandleShape);


                //preview cloud button
                GH_Capsule button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Black, buttonText);
                button.Render(graphics, Selected,Owner.Locked,false);
                button.Dispose();

            }

        }

        //handling double clicks
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left /*&& Owner.RuntimeMessageLevel == GH_RuntimeMessageLevel.Blank*/) 
            {
                if (ButtonBounds.Contains(e.CanvasLocation))
                {
                    Owner.RecordUndoEvent("SiteReader button clicked");
                    PreviewCloud = PreviewCloud == false;
                    buttonText = PreviewCloud.ToString();
                    Owner.OnDisplayExpired(true);
                    return GH_ObjectResponse.Handled;
                }
            }

            return base.RespondToMouseDoubleClick (sender, e);
        }
        
        //handling slider
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && !PreviewCloud)
            {
                if (HandleShape.Contains(e.CanvasLocation))
                {
                    //use the drag cursor
                    Grasshopper.Instances.CursorServer.AttachCursor(sender, "GH_NumericSlider");

                    _currentlySliding = true;
                    return GH_ObjectResponse.Capture;
                }
            }


            return base.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && _currentlySliding)
            {
                _slid = true;
                
                var currentX = e.CanvasX;
                //slide the handle around within limits
                if (currentX < SliderBounds.Left)
                {
                    _curHandleOffset = -_handleWidth / 2;
                } 
                else if (currentX > SliderBounds.Right)
                {
                    _curHandleOffset = SliderBounds.Width - _handleWidth / 2;
                }
                else
                {
                    _curHandleOffset = currentX - SliderBounds.Left - _handleWidth / 2;
                }
                Owner.ExpireSolution(true);

                return GH_ObjectResponse.Handled;
            }

            return base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {

            if (e.Button == MouseButtons.Left && _currentlySliding)
            {

                //snap the handle to a notch
                var currentX = e.CanvasX - SliderBounds.Left;
                _curHandleOffset = _handleOffsets.Aggregate((x, y) => Math.Abs(x - currentX) < Math.Abs(y - currentX) ? x : y);

                _currentlySliding = false;
                Owner.ExpireSolution(true);
                return GH_ObjectResponse.Release;

            }
            return base.RespondToMouseUp(sender, e);
        }
    }

    /// <summary>
    ///Style class to define brushes / pens our custom component objects
    /// </summary>
    public class CompStyles
    {
        //fields
        private static readonly Color BlankOutlineCol = Color.FromArgb(255, 50, 50, 50);
        private static readonly Color WarnOutlineCol = Color.FromArgb(255, 80, 10, 0);
        private static readonly Color ErrorOutlineCol = Color.FromArgb(255, 60, 0, 0);

        //properties
        public static Pen BlankOutline => new Pen(BlankOutlineCol){EndCap = System.Drawing.Drawing2D.LineCap.Round};
        public static Pen WarnOutline => new Pen(WarnOutlineCol) { EndCap = System.Drawing.Drawing2D.LineCap.Round };
        public static Pen ErrorOutline => new Pen(ErrorOutlineCol) { EndCap = System.Drawing.Drawing2D.LineCap.Round };

        public static Brush HandleFill => new SolidBrush(Color.AliceBlue);
    }
}
