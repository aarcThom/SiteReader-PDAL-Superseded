using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System.Windows.Forms;

namespace SiteReader.UIAttributes
{
    public class BaseAttributes : GH_ComponentAttributes
    {
        public BaseAttributes(GH_Component owner) : base(owner) { } //constructor

        //FIELDS ------------------------------------------------------------------
        //rectangles for layouts
        private RectangleF ButtonBounds;
        private RectangleF SecondCapsuleBounds;

        //preview the Cloud?
        private bool PreviewCloud = false;
        private string buttonText = "false";


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

            //here we can add extra STATIC stuff to the layout-------------------------------------------
            SecondCapsuleBounds = new RectangleF(left, bottom, width, extraHeight);


            ButtonBounds = new RectangleF(left, bottom + horizSpacer, width, 20);
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

                GH_Capsule button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Black, buttonText);
                button.Render(graphics, Selected,Owner.Locked,false);
                button.Dispose();

            }

        }

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
        public static Pen BlankOutline => new Pen(BlankOutlineCol);
        public static Pen WarnOutline => new Pen(WarnOutlineCol);
        public static Pen ErrorOutline => new Pen(ErrorOutlineCol);
    }
}
