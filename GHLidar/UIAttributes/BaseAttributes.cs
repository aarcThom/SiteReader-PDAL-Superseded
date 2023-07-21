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

        // note to self: in C# use Action when you return void, and Func when you return value(s)
        /* documentation on base:
         https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/base
         */

        public BaseAttributes(GH_Component owner, Action<float> sliderValue, Action<bool> previewCld) : base(owner)
        {
            _returnSliderVal = sliderValue;
            _previewCld = previewCld;
        }

        //FIELDS ------------------------------------------------------------------

        //return values
        private readonly Action<float> _returnSliderVal;
        private readonly Action<bool> _previewCld;

        //rectangles for layouts
        private RectangleF _buttonBounds;
        private RectangleF _secondCapsuleBounds;
        private RectangleF _sliderBounds;
        private RectangleF _handleShape;
        private PointF _handleNum;

        //preview the Cloud?
        private bool _previewCloud = false;
        private string _buttonText = "false";

        //field for slider handle position and preview number
        private bool _slid = false;
        private bool _currentlySliding = false;
        private float _handlePosX;
        private float _handlePosY;


        private float _curHandleOffset = 0;
        private List<float> _handleOffsets;
        private float _cloudDensity = 0;


        private float _handleWidth = 8;
        private float _handleNumValue = 0;


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
            /* docs on constants
             https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/constants
             */

            const int horizSpacer = 10;
            const int sideSpacer = 2;
            const int extraHeight = 200;

            //here we can modify the bounds
            componentRec.Height += extraHeight; // for example

            //here we can assign the modified bounds to the component's bounds--------------------
            Bounds = componentRec;

            //here we can add extra stuff to the layout-------------------------------------------
            _secondCapsuleBounds = new RectangleF(left, bottom, width, extraHeight);

            _sliderBounds = new RectangleF(left, bottom + horizSpacer, width, 20);
            _sliderBounds.Inflate(-sideSpacer*4, 0);


            //slider handle and code to move it properly
             _handleWidth = 8;
            _handlePosY = _sliderBounds.Height / 2 - _handleWidth / 2 + _sliderBounds.Top;


            //getting the handle snap locations
            _handleOffsets = new List<float>();
            for (int i = 0; i < 11; i++)
            {
                var iFl = (float)i;
                var offsetX = iFl * (_sliderBounds.Width / 10) - _handleWidth/2;
                _handleOffsets.Add(offsetX);
            }

            //getting current position
            if (!_slid)
            {
                _handlePosX = _sliderBounds.Left - _handleWidth / 2;
            }
            
            else
            {
                _handlePosX = _sliderBounds.Left + _curHandleOffset;
            }
            
            
            _handleShape = new RectangleF(_handlePosX, _handlePosY, _handleWidth, _handleWidth);
            _handleNum = new PointF(_handlePosX + _handleWidth / 2, _handlePosY + _handleWidth + 5);
            


            //the button
            _buttonBounds = new RectangleF(left, bottom + horizSpacer*2 + _sliderBounds.Height, width, 20);
            _buttonBounds.Inflate(-sideSpacer, 0);


        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel); // handle the wires, draw nickname, name, etc.

            //the main component rendering channel
            if (channel == GH_CanvasChannel.Objects)
            {
                //declare the pens / brushes / pallets we will need to draw the custom objects - defaults for blank / message levels
                Pen outLine = CompStyles.BlankOutline;
                GH_Palette palette = GH_Palette.Normal;

                //use a switch statement to retrieve the proper pens / brushes from our CompColors class
                switch (Owner.RuntimeMessageLevel)
                {
                    case GH_RuntimeMessageLevel.Warning:
                        // assign warning values
                        outLine = CompStyles.WarnOutline;
                        palette = GH_Palette.Warning;
                        break;

                    case GH_RuntimeMessageLevel.Error:
                        // assign warning values
                        outLine = CompStyles.ErrorOutline;
                        palette = GH_Palette.Error;
                        break;
                }


                //render custom elements----------------------------------------------------------

                //secondary capsule
                GH_Capsule secondCap = GH_Capsule.CreateCapsule(_secondCapsuleBounds, palette);
                secondCap.Render(graphics, Selected, Owner.Locked, false);
                secondCap.Dispose();

                //slider line
                var sliderY = _sliderBounds.Top + _sliderBounds.Height / 2;
                graphics.DrawLine(outLine, _sliderBounds.Left, sliderY, _sliderBounds.Right, sliderY);

                //slider line vertical ticks
                int count = 0;
                foreach (var offset in _handleOffsets)
                {
                    var tickX = offset + _sliderBounds.Left + _handleWidth / 2;

                    float top;
                    if (count % 5 == 0)
                    {
                        top = _sliderBounds.Top + 2;
                    }
                    else
                    {
                        top = _sliderBounds.Top + 5;
                    }

                    graphics.DrawLine(outLine, tickX, sliderY, tickX, top);

                    count++;
                }

                //slider handle
                graphics.FillEllipse(CompStyles.HandleFill, _handleShape);
                graphics.DrawEllipse(outLine, _handleShape);


                //slider number value
                Font font = GH_FontServer.Small;
                // adjust fontsize to high resolution displays
                font = new Font(font.FontFamily, font.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);


                graphics.DrawString(_cloudDensity.ToString(), font, Brushes.Black, _handleNum, GH_TextRenderingConstants.CenterCenter);


                //preview cloud button
                GH_Capsule button = GH_Capsule.CreateTextCapsule(_buttonBounds, _buttonBounds, GH_Palette.Black, _buttonText);
                button.Render(graphics, Selected,Owner.Locked,false);
                button.Dispose();

            }

        }

        /* See David Rutten's explanation of GH_ObjectResponse
         * https://discourse.mcneel.com/t/why-i-cannot-move-a-custom-component/72959/4
         */

        //handling double clicks
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && Owner.RuntimeMessageLevel == GH_RuntimeMessageLevel.Blank) 
            {
                if (_buttonBounds.Contains(e.CanvasLocation))
                {
                    Owner.RecordUndoEvent("SiteReader button clicked");
                    _previewCloud = _previewCloud == false;
                    _previewCld(_previewCloud); //return the value to the component
                    _buttonText = _previewCloud.ToString();
                    Owner.ExpireSolution(true);
                    return GH_ObjectResponse.Handled;
                }
            }

            return base.RespondToMouseDoubleClick (sender, e);
        }
        
        //handling slider
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && !_previewCloud && Owner.RuntimeMessageLevel == GH_RuntimeMessageLevel.Blank)
            {
                if (_handleShape.Contains(e.CanvasLocation))
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

                /* NOT NEEDED FOR SNAPPING SLIDER - USE FOR SLIDING SLIDER

                //slide the handle around within limits
                if (currentX < _sliderBounds.Left)
                {
                    _curHandleOffset = -_handleWidth / 2;
                } 
                else if (currentX > _sliderBounds.Right)
                {
                    _curHandleOffset = _sliderBounds.Width - _handleWidth / 2;
                }
                else
                {
                    _curHandleOffset = currentX - _sliderBounds.Left - _handleWidth / 2;
                }
                */

                //update the number below the handle to snap to the nearest whole value + snap the slider
                var currentVal = currentX - _sliderBounds.Left;
                _curHandleOffset =
                    _handleOffsets.Aggregate((x, y) => Math.Abs(x - currentVal) < Math.Abs(y - currentVal) ? x : y);
                _cloudDensity = (_curHandleOffset + _handleWidth / 2) / _sliderBounds.Width;



                /* note sure why I can't access Owner.ExpireLayout() but the below works to refresh the display while NOT expiring the solution
                 https://discourse.mcneel.com/t/grasshopper-button-should-i-expire-solution/117368
                 */
                base.ExpireLayout();
                sender.Refresh();

                return GH_ObjectResponse.Ignore;
            }

            return base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {

            if (e.Button == MouseButtons.Left && _currentlySliding)
            {
                _returnSliderVal(_cloudDensity); // return the final value

                // again, we don't want to refresh the solution until the display button is clicked
                base.ExpireLayout();
                sender.Refresh();

                _currentlySliding = false;
                return GH_ObjectResponse.Release;

            }
            return base.RespondToMouseUp(sender, e);
        }


        //utility methods ----------------------------------------------------------------------------------------
        float RoundSliderValue(float currentX, bool updateSlider)
        {

            float roundedOffset = _handleOffsets.Aggregate((x, y) => Math.Abs(x - currentX) < Math.Abs(y - currentX) ? x : y);

            if (updateSlider)
            {
                _curHandleOffset = roundedOffset; //if true, the slider will follow the rounding. If not, returns rounded value for number preview
            }
            
            float rounded = (roundedOffset + _handleWidth / 2) / _sliderBounds.Width;
            return rounded;
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
