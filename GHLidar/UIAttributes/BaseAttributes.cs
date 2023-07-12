using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

namespace SiteReader.UIAttributes
{
    public class BaseAttributes : GH_ComponentAttributes
    {
        public BaseAttributes(GH_Component owner) : base(owner) { } //constructor

        protected override void Layout()
        {
            base.Layout(); //handles the basic layout, computes the bounds, etc.
            System.Drawing.Rectangle componentRec = GH_Convert.ToRectangle(Bounds); //getting component base bounds

            //here we can modify the bounds
            componentRec.Height += 200; // for example

            //here we can assign the modified bounds to the component's bounds
            Bounds = componentRec;

            //here we can add extra stuff to the layout



        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel); // handle the wires, draw nickname, name, etc.

            //the main component rendering channel
            if (channel == GH_CanvasChannel.Objects)
            {
                //declare the pens / brushes we will need to draw the custom objects - defaults for blank / message levels
                Pen outLine = CompStyles.BlankOutline;


                //use a switch statement to retrieve the proper pens / brushes from our CompColors class
                switch (Owner.RuntimeMessageLevel)
                {
                    case GH_RuntimeMessageLevel.Warning:
                        // assign warning values
                        outLine = CompStyles.WarnOutline;
                        break;

                    case GH_RuntimeMessageLevel.Error:
                        // assign warning values
                        outLine = CompStyles.ErrorOutline;
                        break;
                }
            }

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
